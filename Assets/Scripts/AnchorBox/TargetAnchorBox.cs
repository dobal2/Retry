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
    [SerializeField] private float maxVisibleDistance = 100f;
    [SerializeField] private float minVisibleDistance = 2f;
    
    private Camera mainCamera;
    private RectTransform anchorBoxUI;
    private Image[] borderLines;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI distanceText;
    
    private Renderer targetRenderer;
    private Collider targetCollider;
    private EnemyAI enemyAI;
    
    private void Start()
    {
        mainCamera = Camera.main;
        enemyAI = GetComponent<EnemyAI>();
        targetRenderer = GetComponentInChildren<Renderer>();
        targetCollider = GetComponent<Collider>();
        
        SetDefaultColorByType();
        AnchorBoxManager.Instance?.RegisterTarget(this);
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
    
    private void OnDestroy()
    {
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
    
        // ★ healthText 변수 할당
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
        
        UpdateAnchorBox();
    }
    
    private void UpdateAnchorBox()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        
        // 거리 체크
        if (distance < minVisibleDistance || distance > maxVisibleDistance)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        // 카메라 뒤 체크
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        // 화면 밖 체크
        if (screenPos.x < -100 || screenPos.x > Screen.width + 100 || 
            screenPos.y < -100 || screenPos.y > Screen.height + 100)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        anchorBoxUI.gameObject.SetActive(alwaysShow);
        
        // 바운드 계산
        Bounds bounds = GetScreenBounds();
        
        // 박스 크기 및 위치 설정
        Vector2 boxSize = new Vector2(bounds.size.x, bounds.size.y);
        anchorBoxUI.sizeDelta = boxSize;
        anchorBoxUI.anchoredPosition = new Vector2(bounds.center.x, bounds.center.y);
        
        // 테두리 업데이트
        if (useCornerOnly)
            UpdateCornerLines(boxSize);
        else
            UpdateFullBorderLines(boxSize);
        
        // 거리 업데이트
        if (distanceText != null)
        {
            distanceText.text = $"{distance:F0}m";
        }
        
        // ★ HP 업데이트 (EnemyAI에서 가져오기)
        if (healthText != null && enemyAI != null)
        {
            float currentHealth = enemyAI.GetHealth();
            
            healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)}";
        }
        
        // 거리에 따른 페이드
        if (fadeWithDistance)
        {
            float alpha = 1f - Mathf.Clamp01((distance - minVisibleDistance) / (maxVisibleDistance - minVisibleDistance));
            ApplyAlpha(alpha);
        }
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
        // 상단
        borderLines[0].rectTransform.anchorMin = new Vector2(0, 1);
        borderLines[0].rectTransform.anchorMax = new Vector2(1, 1);
        borderLines[0].rectTransform.sizeDelta = new Vector2(0, lineThickness);
        borderLines[0].rectTransform.anchoredPosition = Vector2.zero;
        
        // 하단
        borderLines[1].rectTransform.anchorMin = new Vector2(0, 0);
        borderLines[1].rectTransform.anchorMax = new Vector2(1, 0);
        borderLines[1].rectTransform.sizeDelta = new Vector2(0, lineThickness);
        borderLines[1].rectTransform.anchoredPosition = Vector2.zero;
        
        // 좌측
        borderLines[2].rectTransform.anchorMin = new Vector2(0, 0);
        borderLines[2].rectTransform.anchorMax = new Vector2(0, 1);
        borderLines[2].rectTransform.sizeDelta = new Vector2(lineThickness, 0);
        borderLines[2].rectTransform.anchoredPosition = Vector2.zero;
        
        // 우측
        borderLines[3].rectTransform.anchorMin = new Vector2(1, 0);
        borderLines[3].rectTransform.anchorMax = new Vector2(1, 1);
        borderLines[3].rectTransform.sizeDelta = new Vector2(lineThickness, 0);
        borderLines[3].rectTransform.anchoredPosition = Vector2.zero;
    }
    
    private void UpdateCornerLines(Vector2 boxSize)
    {
        float halfWidth = boxSize.x * 0.5f;
        float halfHeight = boxSize.y * 0.5f;
        
        // 좌상단 모서리
        SetCornerLine(0, new Vector2(-halfWidth, halfHeight), cornerLength, lineThickness);
        SetCornerLine(1, new Vector2(-halfWidth, halfHeight), lineThickness, cornerLength);
        
        // 우상단 모서리
        SetCornerLine(2, new Vector2(halfWidth - cornerLength, halfHeight), cornerLength, lineThickness);
        SetCornerLine(3, new Vector2(halfWidth, halfHeight), lineThickness, cornerLength);
        
        // 좌하단 모서리
        SetCornerLine(4, new Vector2(-halfWidth, -halfHeight), cornerLength, lineThickness);
        SetCornerLine(5, new Vector2(-halfWidth, -halfHeight + cornerLength), lineThickness, cornerLength);
        
        // 우하단 모서리
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
    
    private void ApplyAlpha(float alpha)
    {
        Color color = boxColor;
        color.a = alpha;
        
        foreach (var line in borderLines)
        {
            if (line != null) line.color = color;
        }
        
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
        
        // ★ HP 텍스트 페이드
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