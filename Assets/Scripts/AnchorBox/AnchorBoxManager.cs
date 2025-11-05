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
    
    private List<TargetAnchorBox> registeredTargets = new List<TargetAnchorBox>();
    
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
    
    private void SetupCanvas()
    {
        if (anchorBoxCanvas == null)
        {
            GameObject canvasObj = new GameObject("AnchorBoxCanvas");
            canvasObj.transform.SetParent(transform);
        
            anchorBoxCanvas = canvasObj.AddComponent<Canvas>();
            anchorBoxCanvas.renderMode = RenderMode.ScreenSpaceCamera; // ★ Camera 모드
            anchorBoxCanvas.worldCamera = Camera.main; // ★ 카메라 할당
            anchorBoxCanvas.planeDistance = 10f; // ★ 카메라로부터의 거리 (조절 가능)
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
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}