using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnchorBoxManager : MonoBehaviour
{
    public static AnchorBoxManager Instance { get; private set; }
    
    [Header("UI Canvas")]
    [SerializeField] private Canvas anchorBoxCanvas;
    [SerializeField] private RectTransform uiContainer;
    public GameObject textTemplate;
    
    [Header("Display Mode")]
    [SerializeField] private bool showOnlyClosestToCenter = false;
    [SerializeField] private float centerDetectionRadius = 200f;
    [SerializeField] private LayerMask occlusionLayers = -1;
    
    private List<TargetAnchorBox> registeredTargets = new List<TargetAnchorBox>();
    private Camera mainCamera;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetupCanvas();
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (showOnlyClosestToCenter)
        {
            UpdateClosestTarget();
        }
    }
    
    private void UpdateClosestTarget()
    {
        if (mainCamera == null || registeredTargets.Count == 0) return;
        
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        
        TargetAnchorBox closestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (var target in registeredTargets)
        {
            if (target == null || !target.gameObject.activeInHierarchy) continue;
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.transform.position);
            
            // 화면 뒤에 있으면 스킵
            if (screenPos.z < 0) continue;
            
            // ★ 오클루전 체크 (가려진 적은 제외)
            if (!IsVisibleFromCamera(target.transform.position))
                continue;
            
            float distanceToCenter = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
            
            // 반경 내에 있고 가장 가까운지 체크
            if (distanceToCenter < centerDetectionRadius && distanceToCenter < closestDistance)
            {
                closestDistance = distanceToCenter;
                closestTarget = target;
            }
        }
        
        // 모든 타겟의 가시성 업데이트
        foreach (var target in registeredTargets)
        {
            if (target == null) continue;
            
            if (target == closestTarget)
            {
                target.SetVisibility(true);
            }
            else
            {
                target.SetVisibility(false);
            }
        }
    }
    
    /// <summary>
    /// ★ 카메라에서 타겟이 보이는지 확인 (오클루전 체크)
    /// </summary>
    private bool IsVisibleFromCamera(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - mainCamera.transform.position;
        float distance = direction.magnitude;
        
        if (Physics.Raycast(mainCamera.transform.position, direction.normalized, out RaycastHit hit, distance, occlusionLayers))
        {
            // 맞은 오브젝트가 타겟 또는 타겟의 자식이면 보임
            Transform hitRoot = hit.transform.root;
            Vector3 targetRoot = targetPosition;
            
            // 타겟 근처에 맞았으면 OK
            if (Vector3.Distance(hit.point, targetPosition) < 2f)
            {
                return true;
            }
            
            return false;
        }
        
        // 아무것도 안 맞음 = 보임
        return true;
    }
    
    private void SetupCanvas()
    {
        if (anchorBoxCanvas == null)
        {
            GameObject canvasObj = new GameObject("AnchorBoxCanvas");
            canvasObj.transform.SetParent(transform);
        
            anchorBoxCanvas = canvasObj.AddComponent<Canvas>();
            anchorBoxCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            anchorBoxCanvas.worldCamera = Camera.main;
            anchorBoxCanvas.planeDistance = 10f;
            anchorBoxCanvas.sortingOrder = 100;
        
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        
            canvasObj.AddComponent<GraphicRaycaster>();
        }
    
        if (uiContainer == null)
        {
            GameObject containerObj = new GameObject("UIContainer");
            containerObj.transform.SetParent(anchorBoxCanvas.transform, false);
        
            uiContainer = containerObj.AddComponent<RectTransform>();
            uiContainer.anchorMin = Vector2.zero;
            uiContainer.anchorMax = Vector2.one;
            uiContainer.sizeDelta = Vector2.zero;
            uiContainer.anchoredPosition = Vector2.zero;
        }
    }
    
    public void RegisterTarget(TargetAnchorBox target)
    {
        if (!registeredTargets.Contains(target))
        {
            registeredTargets.Add(target);
            target.CreateUI(uiContainer);
        }
    }
    
    public void UnregisterTarget(TargetAnchorBox target)
    {
        registeredTargets.Remove(target);
    }
    
    public void SetShowOnlyClosest(bool enabled)
    {
        showOnlyClosestToCenter = enabled;
        
        if (!enabled)
        {
            foreach (var target in registeredTargets)
            {
                if (target != null)
                {
                    target.SetVisibility(true);
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showOnlyClosestToCenter) return;
        
        // 화면 중앙에 원 표시
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        
        // 감지 반경을 화면 공간에 표시
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2f;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            
            Vector2 point1 = screenCenter + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * centerDetectionRadius;
            Vector2 point2 = screenCenter + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * centerDetectionRadius;
            
            if (mainCamera != null)
            {
                Vector3 world1 = mainCamera.ScreenToWorldPoint(new Vector3(point1.x, point1.y, 10f));
                Vector3 world2 = mainCamera.ScreenToWorldPoint(new Vector3(point2.x, point2.y, 10f));
                Gizmos.DrawLine(world1, world2);
            }
        }
    }
}