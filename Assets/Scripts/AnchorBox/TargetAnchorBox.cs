using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetAnchorBox : MonoBehaviour
{
    public enum TargetType
    {
        Enemy,
        Ally,
        Item,
        Objective,
        Vehicle,
        NPC,
        Custom
    }
    
    [Header("Target Settings")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;
    [SerializeField] private string displayName = "";
    [SerializeField] private bool alwaysShow = true;
    
    [Header("Box Style")]
    [SerializeField] private Color boxColor = Color.red;
    [SerializeField] private float boxPadding = 20f;
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private bool useCornerOnly = false;
    [SerializeField] private float cornerLength = 20f;
    
    [Header("Display Options")]
    [SerializeField] private bool showName = true;
    [SerializeField] private bool showDistance = true;
    [SerializeField] private bool showHealthText = false;
    
    [Header("Distance Settings")]
    [SerializeField] private bool fadeWithDistance = true;
    [SerializeField] private float maxTextVisibleDistance;
    [SerializeField] private float maxVisibleDistance = 100f;
    [SerializeField] private float minVisibleDistance = 2f;
    
    [Header("Occlusion Settings")]
    [SerializeField] private bool checkOcclusion = true;
    [SerializeField] private LayerMask occlusionLayers = -1;
    [SerializeField] private int raycastCount = 3;
    
    private Camera mainCamera;
    private RectTransform anchorBoxUI;
    private Image[] borderLines;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI distanceText;
    
    private Renderer targetRenderer;
    private Collider targetCollider;
    private EnemyAI enemyAI;
    
    // ★ 강제 알파값 (dissolve 시 사용)
    private float forceAlpha = 1f;
    private bool isForceAlphaActive = false;
    
    private void Start()
    {
        mainCamera = Camera.main;
        enemyAI = GetComponent<EnemyAI>();
        targetRenderer = GetComponentInChildren<Renderer>();
        targetCollider = GetComponent<Collider>();
        
        SetDefaultColorByType();
        AnchorBoxManager.Instance?.RegisterTarget(this);
    }
    
    // ★ 엄격한 Occlusion 체크 (모든 지점이 보여야만 표시)
    private bool IsVisibleFromCamera()
    {
        if (!checkOcclusion) return true;
        if (targetCollider == null && targetRenderer == null) return true;

        Bounds bounds;
        if (targetRenderer != null)
            bounds = targetRenderer.bounds;
        else if (targetCollider != null)
            bounds = targetCollider.bounds;
        else
            bounds = new Bounds(transform.position, Vector3.one);

        // ★ 여러 지점 체크 (중심, 상단, 좌우 등)
        Vector3[] checkPoints = new Vector3[raycastCount];
        checkPoints[0] = bounds.center; // 중심
    
        if (raycastCount >= 2)
            checkPoints[1] = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z); // 상단
    
        if (raycastCount >= 3)
        {
            checkPoints[2] = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z); // 왼쪽
        }
    
        if (raycastCount >= 4)
        {
            checkPoints[3] = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z); // 오른쪽
        }
    
        if (raycastCount >= 5)
        {
            checkPoints[4] = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); // 하단
        }

        // ★★★ 모든 지점이 보여야만 true 반환 (하나라도 가려지면 숨김) ★★★
        for (int i = 0; i < Mathf.Min(raycastCount, checkPoints.Length); i++)
        {
            Vector3 direction = checkPoints[i] - mainCamera.transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(mainCamera.transform.position, direction.normalized, out RaycastHit hit, distance, occlusionLayers))
            {
                // ★ 자기 자신이나 자식 오브젝트에 맞은 경우는 OK
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue; // 다음 지점 체크
                }
                else
                {
                    // ★ 다른 오브젝트에 가려짐 = 즉시 false 반환
                    return false;
                }
            }
            // 아무것도 안 맞음 = 해당 지점은 보임 (계속 체크)
        }

        // ★ 모든 지점이 통과했으면 true
        return true;
    }
    
    private void SetDefaultColorByType()
    {
        if (boxColor != Color.red) return;
        
        switch (targetType)
        {
            case TargetType.Enemy:
                boxColor = Color.red;
                break;
            case TargetType.Ally:
                boxColor = Color.green;
                break;
            case TargetType.Item:
                boxColor = Color.yellow;
                break;
            case TargetType.Objective:
                boxColor = Color.cyan;
                break;
            case TargetType.Vehicle:
                boxColor = Color.magenta;
                break;
            case TargetType.NPC:
                boxColor = Color.white;
                break;
        }
    }
    
    private void OnDisable()
    {
        if (anchorBoxUI != null)
        {
            anchorBoxUI.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (anchorBoxUI != null)
        {
            anchorBoxUI.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (anchorBoxUI != null)
        {
            Destroy(anchorBoxUI.gameObject);
        }
    
        AnchorBoxManager.Instance?.UnregisterTarget(this);
    }
    
    public void CreateUI(Transform uiParent)
    {
        GameObject boxObj = new GameObject($"AnchorBox_{gameObject.name}");
        boxObj.transform.SetParent(uiParent, false);
        
        anchorBoxUI = boxObj.AddComponent<RectTransform>();
        anchorBoxUI.anchorMin = Vector2.zero;
        anchorBoxUI.anchorMax = Vector2.zero;
        anchorBoxUI.pivot = new Vector2(0.5f, 0.5f);
        
        CreateBorderLines();
        
        if (showName)
            CreateNameText();
        
        if (showDistance)
            CreateDistanceText();
        
        if (showHealthText)
            CreateHealthText();
    }
    
    private void CreateBorderLines()
    {
        int lineCount = useCornerOnly ? 8 : 4;
        borderLines = new Image[lineCount];
        
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject($"Line_{i}");
            lineObj.transform.SetParent(anchorBoxUI, false);
            
            borderLines[i] = lineObj.AddComponent<Image>();
            borderLines[i].color = boxColor;
            borderLines[i].raycastTarget = false;
        }
    }
    
    private void CreateNameText()
    {
        GameObject textObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
        textObj.name = "NameText";
        textObj.transform.SetParent(anchorBoxUI, false);

        nameText = textObj.GetComponent<TextMeshProUGUI>();
        nameText.text = displayName;
        nameText.color = boxColor;
    
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.sizeDelta = new Vector2(200, 25);
        textRect.anchoredPosition = new Vector2(0, 10);
    }
    
    private void CreateDistanceText()
    {
        GameObject textObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
        textObj.name = "DistanceText";
        textObj.transform.SetParent(anchorBoxUI, false);
    
        distanceText = textObj.GetComponent<TextMeshProUGUI>();
        distanceText.color = boxColor;
        distanceText.alignment = TextAlignmentOptions.Left;
    
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.sizeDelta = new Vector2(100, 20);
        textRect.anchoredPosition = new Vector2(10, showHealthText ? 10 : 0);
    }
    
    private void CreateHealthText()
    {
        GameObject hpTextObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
        hpTextObj.name = "HPText";
        hpTextObj.transform.SetParent(anchorBoxUI, false);
    
        healthText = hpTextObj.GetComponent<TextMeshProUGUI>();
        healthText.color = boxColor;
        healthText.alignment = TextAlignmentOptions.Left;
        healthText.text = "HP: 100";
    
        RectTransform textRect = hpTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.sizeDelta = new Vector2(120, 20);
        textRect.anchoredPosition = new Vector2(10, 45);
    }
    
    private void LateUpdate()
    {
        if (anchorBoxUI == null || mainCamera == null) return;
        
        if (!gameObject.activeInHierarchy || this == null)
        {
            if (anchorBoxUI != null)
            {
                Destroy(anchorBoxUI.gameObject);
                anchorBoxUI = null;
            }
            return;
        }
        
        UpdateAnchorBox();
    }
    
    private void UpdateAnchorBox()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        
        if (distance < minVisibleDistance || distance > maxVisibleDistance)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        if (!IsVisibleFromCamera())
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        if (screenPos.x < -100 || screenPos.x > Screen.width + 100 || 
            screenPos.y < -100 || screenPos.y > Screen.height + 100)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        anchorBoxUI.gameObject.SetActive(alwaysShow);
        
        Bounds bounds = GetScreenBounds();
        
        Vector2 boxSize = new Vector2(bounds.size.x, bounds.size.y);
        anchorBoxUI.sizeDelta = boxSize;
        anchorBoxUI.anchoredPosition = new Vector2(bounds.center.x, bounds.center.y);
        
        if (useCornerOnly)
            UpdateCornerLines(boxSize);
        else
            UpdateFullBorderLines(boxSize);
        
        if (distanceText != null)
        {
            distanceText.text = $"{distance:F0}m";
        }
        
        if (healthText != null && enemyAI != null)
        {
            float currentHealth = enemyAI.GetHealth();
            healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)}";
        }
        
        // ★ 알파 적용 (dissolve 시 강제 알파값 우선)
        if (isForceAlphaActive)
        {
            ApplyBorderAlpha(forceAlpha);
            ApplyTextAlpha(forceAlpha);
        }
        else if (fadeWithDistance)
        {
            float borderAlpha = 1f - Mathf.Clamp01((distance - minVisibleDistance) / (maxVisibleDistance - minVisibleDistance));
            float textAlpha = 1f - Mathf.Clamp01((distance - minVisibleDistance) / (maxTextVisibleDistance - minVisibleDistance));
            ApplyBorderAlpha(borderAlpha);
            ApplyTextAlpha(textAlpha);
        }
    }
    
    /// <summary>
    /// ★ 외부에서 UI 알파값 강제 설정 (dissolve 시 사용)
    /// </summary>
    public void SetUIAlpha(float alpha)
    {
        forceAlpha = Mathf.Clamp01(alpha);
        isForceAlphaActive = true;
    }
    
    /// <summary>
    /// ★ 강제 알파 모드 해제 (일반 페이드 모드로 복귀)
    /// </summary>
    public void ResetUIAlpha()
    {
        isForceAlphaActive = false;
        forceAlpha = 1f;
    }
    
    private Bounds GetScreenBounds()
    {
        Bounds bounds = new Bounds();
        
        if (targetRenderer != null)
        {
            bounds = targetRenderer.bounds;
        }
        else if (targetCollider != null)
        {
            bounds = targetCollider.bounds;
        }
        else
        {
            bounds = new Bounds(transform.position, Vector3.one);
        }
        
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = bounds.max;
        
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        
        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(corner);
            
            if (screenPoint.x < min.x) min.x = screenPoint.x;
            if (screenPoint.y < min.y) min.y = screenPoint.y;
            if (screenPoint.x > max.x) max.x = screenPoint.x;
            if (screenPoint.y > max.y) max.y = screenPoint.y;
        }
        
        min.x -= boxPadding * 0.5f;
        min.y -= boxPadding * 0.5f;
        max.x += boxPadding * 0.5f;
        max.y += boxPadding * 0.5f;
        
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        
        return new Bounds(center, size);
    }
    
    private void UpdateFullBorderLines(Vector2 boxSize)
    {
        borderLines[0].rectTransform.anchorMin = new Vector2(0, 1);
        borderLines[0].rectTransform.anchorMax = new Vector2(1, 1);
        borderLines[0].rectTransform.sizeDelta = new Vector2(0, lineThickness);
        borderLines[0].rectTransform.anchoredPosition = Vector2.zero;
        
        borderLines[1].rectTransform.anchorMin = new Vector2(0, 0);
        borderLines[1].rectTransform.anchorMax = new Vector2(1, 0);
        borderLines[1].rectTransform.sizeDelta = new Vector2(0, lineThickness);
        borderLines[1].rectTransform.anchoredPosition = Vector2.zero;
        
        borderLines[2].rectTransform.anchorMin = new Vector2(0, 0);
        borderLines[2].rectTransform.anchorMax = new Vector2(0, 1);
        borderLines[2].rectTransform.sizeDelta = new Vector2(lineThickness, 0);
        borderLines[2].rectTransform.anchoredPosition = Vector2.zero;
        
        borderLines[3].rectTransform.anchorMin = new Vector2(1, 0);
        borderLines[3].rectTransform.anchorMax = new Vector2(1, 1);
        borderLines[3].rectTransform.sizeDelta = new Vector2(lineThickness, 0);
        borderLines[3].rectTransform.anchoredPosition = Vector2.zero;
    }
    
    private void UpdateCornerLines(Vector2 boxSize)
    {
        float halfWidth = boxSize.x * 0.5f;
        float halfHeight = boxSize.y * 0.5f;
        
        SetCornerLine(0, new Vector2(-halfWidth, halfHeight), cornerLength, lineThickness);
        SetCornerLine(1, new Vector2(-halfWidth, halfHeight), lineThickness, cornerLength);
        
        SetCornerLine(2, new Vector2(halfWidth - cornerLength, halfHeight), cornerLength, lineThickness);
        SetCornerLine(3, new Vector2(halfWidth, halfHeight), lineThickness, cornerLength);
        
        SetCornerLine(4, new Vector2(-halfWidth, -halfHeight), cornerLength, lineThickness);
        SetCornerLine(5, new Vector2(-halfWidth, -halfHeight + cornerLength), lineThickness, cornerLength);
        
        SetCornerLine(6, new Vector2(halfWidth - cornerLength, -halfHeight), cornerLength, lineThickness);
        SetCornerLine(7, new Vector2(halfWidth, -halfHeight + cornerLength), lineThickness, cornerLength);
    }
    
    private void SetCornerLine(int index, Vector2 position, float width, float height)
    {
        RectTransform rt = borderLines[index].rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = position;
    }
    
    private void ApplyBorderAlpha(float alpha)
    {
        Color color = boxColor;
        color.a = alpha;
        
        foreach (var line in borderLines)
        {
            if (line != null) line.color = color;
        }
    }
    
    private void ApplyTextAlpha(float alpha)
    {
        if (nameText != null)
        {
            Color textColor = nameText.color;
            textColor.a = alpha;
            nameText.color = textColor;
        }
        
        if (distanceText != null)
        {
            Color textColor = distanceText.color;
            textColor.a = alpha;
            distanceText.color = textColor;
        }
        
        if (healthText != null)
        {
            Color textColor = healthText.color;
            textColor.a = alpha;
            healthText.color = textColor;
        }
    }
    
    public void SetBoxColor(Color color)
    {
        boxColor = color;
        foreach (var line in borderLines)
        {
            if (line != null) line.color = color;
        }
    }
    
    public void SetVisibility(bool visible)
    {
        alwaysShow = visible;
    }
}