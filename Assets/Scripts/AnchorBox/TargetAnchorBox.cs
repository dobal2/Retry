using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TargetAnchorBox : MonoBehaviour
{
    public enum TargetType
    {
        Enemy,       // HP, Damage 표시
        Box,         // 여는데 필요한 에너지 표시
        EnergyCore,  // 현재 레벨, 다음 레벨까지 필요한 것
        Ally,
        Item,
    }
    
    public enum BoundsSourceType
    {
        Renderer,
        Collider,
        Auto
    }
    
    [Header("Target Settings")]
    [SerializeField] private TargetType targetType = TargetType.Enemy;
    [SerializeField] private string displayName = "";
    [SerializeField] private bool alwaysShow = true;
    
    [Header("Bounds Settings")]
    [SerializeField] private BoundsSourceType boundsSource = BoundsSourceType.Auto;
    [SerializeField] private bool cacheBounds = true;
    [SerializeField] private float boundsUpdateInterval = 0.1f;
    [SerializeField] private bool autoFixBounds = true;
    [SerializeField] private float minBoundsSize = 0.5f;
    [SerializeField] private float maxBoundsSize = 50f;
    
    [Header("Manual Bounds Override")]
    [SerializeField] private bool useManualCenter = false;
    [SerializeField] private Vector3 manualCenterOffset = Vector3.zero;
    [SerializeField] private Vector3 manualBoundsSize = Vector3.one * 2f;
    
    [Header("Box Style")]
    [SerializeField] private Color boxColor = Color.red;
    [SerializeField] private float boxPadding = 20f;
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private bool useCornerOnly = false;
    [SerializeField] private float cornerLength = 20f;
    
    [Header("Display Options")]
    [SerializeField] private bool showName = true;
    
    [Header("Distance Settings")]
    [SerializeField] private bool fadeWithDistance = true;
    [SerializeField] private float maxTextVisibleDistance = 50f;
    [SerializeField] private float maxVisibleDistance = 100f;
    [SerializeField] private float minVisibleDistance = 2f;
    
    [Header("Occlusion Settings")]
    [SerializeField] private bool checkOcclusion = true;
    [SerializeField] private LayerMask occlusionLayers = -1;
    [SerializeField] private int raycastCount = 3;
    
    [Header("Camera View Settings")]
    [SerializeField] private bool showOnlyInCameraView = false;
    [SerializeField] private float cameraViewAngle = 60f;
    
    [Header("Performance Settings")]
    [SerializeField] private bool useLateUpdate = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;
    
    private Camera mainCamera;
    private Canvas parentCanvas;
    protected RectTransform anchorBoxUI;
    private Image[] borderLines;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI infoText; // ★ 통합 정보 텍스트
    
    private Renderer targetRenderer;
    private Collider targetCollider;
    private EnemyAI enemyAI;
    
    private float forceAlpha = 1f;
    private bool isForceAlphaActive = false;
    
    private Bounds lastValidBounds;
    private Bounds cachedBounds;
    private float lastBoundsUpdateTime;
    private bool isDying = false;
    
    private Color originalBoxColor;
    private Coroutine colorTransitionCoroutine;
    
    private void Start()
    {
        mainCamera = Camera.main;
        enemyAI = GetComponent<EnemyAI>();
        
        targetCollider = GetComponent<Collider>();
        targetRenderer = GetComponentInChildren<Renderer>();
        
        ValidateBounds();
        SetDefaultColorByType();
        
        originalBoxColor = boxColor;
        AnchorBoxManager.Instance?.RegisterTarget(this);
        
        UpdateCachedBounds();
    }
    
    private void SetDefaultColorByType()
    {
        switch (targetType)
        {
            case TargetType.Enemy:
                if (boxColor == Color.red) boxColor = Color.red;
                break;
            case TargetType.Box:
                if (boxColor == Color.red) boxColor = new Color(1f, 0.8f, 0f); // 골드색
                break;
            case TargetType.EnergyCore:
                if (boxColor == Color.red) boxColor = Color.cyan;
                break;
            case TargetType.Ally:
                if (boxColor == Color.red) boxColor = Color.green;
                break;
            case TargetType.Item:
                if (boxColor == Color.red) boxColor = Color.yellow;
                break;
        }
    }
    
    private void ValidateBounds()
    {
        Bounds testBounds = GetBoundsFromSource();
        
        if (testBounds.size.magnitude < 0.1f)
        {
            Debug.LogWarning($"{gameObject.name}: Bounds too small! Consider using Manual Bounds.");
        }
        
        if (testBounds.size.magnitude > 100f)
        {
            Debug.LogWarning($"{gameObject.name}: Bounds too large! Check Renderer/Collider.");
        }
        
        float distance = Vector3.Distance(testBounds.center, transform.position);
        if (distance > 10f)
        {
            Debug.LogWarning($"{gameObject.name}: Bounds center far from object! Distance: {distance}m");
        }
        
        if (targetCollider == null && targetRenderer == null)
        {
            Debug.LogWarning($"{gameObject.name}: No Collider or Renderer found! Using fallback bounds.");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Bounds bounds = GetBoundsFromSource();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, 
            $"{gameObject.name}\nBounds: {bounds.size}\nCenter: {bounds.center}");
        #endif
    }
    
    private bool IsInCameraView()
    {
        if (!showOnlyInCameraView) return true;
        
        Vector3 directionToTarget = (transform.position - mainCamera.transform.position).normalized;
        float angle = Vector3.Angle(mainCamera.transform.forward, directionToTarget);
        
        return angle < cameraViewAngle;
    }
    
    private bool IsVisibleFromCamera()
    {
        if (!checkOcclusion) return true;
        if (targetCollider == null && targetRenderer == null) return true;

        Bounds bounds = GetCurrentBounds();

        Vector3[] checkPoints = new Vector3[raycastCount];
        checkPoints[0] = bounds.center;
    
        if (raycastCount >= 2)
            checkPoints[1] = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    
        if (raycastCount >= 3)
        {
            checkPoints[2] = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);
        }
    
        if (raycastCount >= 4)
        {
            checkPoints[3] = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
        }
    
        if (raycastCount >= 5)
        {
            checkPoints[4] = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        for (int i = 0; i < Mathf.Min(raycastCount, checkPoints.Length); i++)
        {
            Vector3 direction = checkPoints[i] - mainCamera.transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(mainCamera.transform.position, direction.normalized, out RaycastHit hit, distance, occlusionLayers))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }
    
    private void UpdateCachedBounds()
    {
        cachedBounds = GetBoundsFromSource();
        lastBoundsUpdateTime = Time.time;
    }
    
    private Bounds GetBoundsFromSource()
    {
        if (useManualCenter)
        {
            return new Bounds(transform.position + manualCenterOffset, manualBoundsSize);
        }
        
        Bounds bounds;
        
        switch (boundsSource)
        {
            case BoundsSourceType.Renderer:
                if (targetRenderer != null)
                    bounds = targetRenderer.bounds;
                else
                    bounds = new Bounds(transform.position, Vector3.one);
                break;
                
            case BoundsSourceType.Collider:
                if (targetCollider != null)
                    bounds = targetCollider.bounds;
                else
                    bounds = new Bounds(transform.position, Vector3.one);
                break;
                
            case BoundsSourceType.Auto:
                if (targetCollider != null)
                    bounds = targetCollider.bounds;
                else if (targetRenderer != null)
                    bounds = targetRenderer.bounds;
                else
                    bounds = new Bounds(transform.position, Vector3.one);
                break;
                
            default:
                bounds = new Bounds(transform.position, Vector3.one);
                break;
        }
        
        if (autoFixBounds)
        {
            bounds = FixInvalidBounds(bounds);
        }
        
        return bounds;
    }
    
    private Bounds FixInvalidBounds(Bounds bounds)
    {
        Vector3 size = bounds.size;
        Vector3 center = bounds.center;
        
        if (size.x < minBoundsSize) size.x = minBoundsSize;
        if (size.y < minBoundsSize) size.y = minBoundsSize;
        if (size.z < minBoundsSize) size.z = minBoundsSize;
        
        if (size.x > maxBoundsSize || size.y > maxBoundsSize || size.z > maxBoundsSize)
        {
            Debug.LogWarning($"{gameObject.name}: Bounds too large! {size}");
            size = Vector3.one * 2f;
            center = transform.position;
        }
        
        float distanceFromObject = Vector3.Distance(center, transform.position);
        if (distanceFromObject > maxBoundsSize)
        {
            Debug.LogWarning($"{gameObject.name}: Bounds center too far! {distanceFromObject}m");
            center = transform.position;
        }
        
        if (float.IsNaN(size.x) || float.IsInfinity(size.x) ||
            float.IsNaN(center.x) || float.IsInfinity(center.x))
        {
            Debug.LogError($"{gameObject.name}: Invalid Bounds values!");
            return new Bounds(transform.position, Vector3.one * 2f);
        }
        
        return new Bounds(center, size);
    }
    
    private Bounds GetCurrentBounds()
    {
        if (isDying)
        {
            return lastValidBounds;
        }
        
        if (cacheBounds)
        {
            if (Time.time - lastBoundsUpdateTime > boundsUpdateInterval)
            {
                UpdateCachedBounds();
            }
            return cachedBounds;
        }
        else
        {
            return GetBoundsFromSource();
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
        
        isDying = false;
        UpdateCachedBounds();
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
        parentCanvas = uiParent.GetComponentInParent<Canvas>();
        
        if (parentCanvas == null)
        {
            Debug.LogError($"Canvas not found for {gameObject.name}!");
            return;
        }
        
        GameObject boxObj = new GameObject($"AnchorBox_{gameObject.name}");
        boxObj.transform.SetParent(uiParent, false);
        
        anchorBoxUI = boxObj.AddComponent<RectTransform>();
        anchorBoxUI.anchorMin = new Vector2(0.5f, 0.5f);
        anchorBoxUI.anchorMax = new Vector2(0.5f, 0.5f);
        anchorBoxUI.pivot = new Vector2(0.5f, 0.5f);
        
        CreateBorderLines();
        
        if (showName)
            CreateNameText();
        
        // ★ 타입별 UI 생성
        CreateInfoText();
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
    
    // ★ 통합 정보 텍스트 생성 (타입별로 위치 조정)
    private void CreateInfoText()
{
    GameObject textObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
    textObj.name = "InfoText";
    textObj.transform.SetParent(anchorBoxUI, false);

    infoText = textObj.GetComponent<TextMeshProUGUI>();
    infoText.color = boxColor; // ★ boxColor 따라감
    
    RectTransform textRect = textObj.GetComponent<RectTransform>();
    
    // ★ 타입별 위치 및 스타일 조정
    switch (targetType)
    {
        case TargetType.Enemy:
            infoText.alignment = TextAlignmentOptions.Left;
            infoText.fontSize = 34; // ★ 14 → 18
            textRect.anchorMin = new Vector2(1f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.sizeDelta = new Vector2(420, 60); // ★ 크기 증가
            textRect.anchoredPosition = new Vector2(10, 0);
            break;
            
        case TargetType.Box:
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.fontSize = 30; // ★ 14 → 16
            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = new Vector2(400, 50); // ★ 크기 증가
            textRect.anchoredPosition = new Vector2(0, -10);
            break;
            
        case TargetType.EnergyCore:
            // ★ Enemy처럼 왼쪽 정렬, 옆에 표시
            infoText.alignment = TextAlignmentOptions.Left;
            infoText.fontSize = 34; // ★ 16 → 18
            textRect.anchorMin = new Vector2(1f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.sizeDelta = new Vector2(250, 70); // ★ 크기 증가
            textRect.anchoredPosition = new Vector2(10, 0);
            break;
            
        default:
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.fontSize = 34;
            textRect.anchorMin = new Vector2(0.5f, 0f);
            textRect.anchorMax = new Vector2(0.5f, 0f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = new Vector2(180, 40);
            textRect.anchoredPosition = new Vector2(0, -10);
            break;
    }
}
    
    private void LateUpdate()
    {
        if (!useLateUpdate) return;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
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
    
    private void Update()
    {
        if (useLateUpdate) return;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
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
        
        if (!IsInCameraView())
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        if (!IsVisibleFromCamera())
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        anchorBoxUI.gameObject.SetActive(alwaysShow);
        
        Bounds screenBounds = GetScreenBoundsForCanvasMode();
        
        if (screenBounds.size.magnitude > 10000f || screenBounds.size.magnitude < 1f)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        Vector2 boxSize = new Vector2(screenBounds.size.x, screenBounds.size.y);
        anchorBoxUI.sizeDelta = boxSize;
        anchorBoxUI.anchoredPosition = new Vector2(screenBounds.center.x, screenBounds.center.y);
        
        if (useCornerOnly)
            UpdateCornerLines(boxSize);
        else
            UpdateFullBorderLines(boxSize);
        
        // ★ 타입별 정보 업데이트
        UpdateTypeSpecificInfo();
        
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
    
    // ★ 타입별 정보 업데이트
    private void UpdateTypeSpecificInfo()
    {
        if (infoText == null) return;
        
        switch (targetType)
        {
            case TargetType.Enemy:
                UpdateEnemyInfo();
                break;
            case TargetType.Box:
                UpdateBoxInfo();
                break;
            case TargetType.EnergyCore:
                UpdateEnergyCoreInfo();
                break;
            default:
                infoText.text = "";
                break;
        }
    }
    
    private void UpdateEnemyInfo()
    {
        if (enemyAI != null)
        {
            float currentHealth = enemyAI.GetHealth();
            float enemyDamage = enemyAI.GetDamage();
    
            string hpColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
            infoText.text = $"<color=#{hpColorHex}>HP: {currentHealth:F1}</color>\n<color=#FF8800>Damage: {enemyDamage:F1}</color>";
        }
        else
        {
            string hpColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
            infoText.text = $"<color=#{hpColorHex}>HP: ???</color>\n<color=#FF8800>Damage: ???</color>";
        }
    }

    
    private void UpdateBoxInfo()
    {
        var boxAnchorBox = GetComponent<BoxAnchorBox>();
    
        if (boxAnchorBox != null)
        {
            int requiredEnergy = boxAnchorBox.GetCurrentCost();
            int playerEnergy = PlayerStats.Instance != null ? PlayerStats.Instance.GetEnergy() : 0;
        
            string boxColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
        
            if (playerEnergy >= requiredEnergy)
            {
                infoText.text = $"<color=#{boxColorHex}>[F] Open Box\nEnergy: {playerEnergy}/{requiredEnergy}</color>";
            }
            else
            {
                infoText.text = $"<color=#{boxColorHex}>Locked Box</color>\n<color=#FF0000>Energy: {playerEnergy}/{requiredEnergy}</color>";
            }
        }
        else
        {
            string boxColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
            infoText.text = $"<color=#{boxColorHex}>[F] Open Box\nEnergy: ?/?</color>";
        }
    }
    
    private void UpdateEnergyCoreInfo()
    {
        if (ForceFieldManager.Instance != null)
        {
            int currentLevel = ForceFieldManager.Instance.GetCurrentLevel();
            int maxLevel = ForceFieldManager.Instance.GetMaxLevel();
        
            string boxColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
        
            if (currentLevel >= maxLevel)
            {
                infoText.text = $"Level {currentLevel}\n<color=#00FF00>MAX LEVEL</color></color>";
            }
            else
            {
                int requiredEnergy = ForceFieldManager.Instance.GetRequiredEnergyForNextLevel();
                int playerEnergy = PlayerStats.Instance != null ? PlayerStats.Instance.GetEnergy() : 0;
            
                if (playerEnergy >= requiredEnergy)
                {
                    infoText.text = $"Level {currentLevel}</color>\n<color=#00FF00>[F] Upgrade: {playerEnergy}/{requiredEnergy}</color> Energy";
                }
                else
                {
                    infoText.text = $"Level {currentLevel}</color>\n<color=#FF0000>Require Energy: {playerEnergy}/{requiredEnergy}</color>";
                }
            }
        }
        else
        {
            string boxColorHex = ColorUtility.ToHtmlStringRGB(boxColor);
            infoText.text = $"<color=#{boxColorHex}>Energy Core\nInitializing...</color>";
        }
    }
    
    // ★ 레벨별 필요 에너지 계산 (ForceFieldManager와 연동)
    private int GetRequiredEnergyForNextLevel(int currentLevel)
    {
        // 레벨별 필요 에너지 (예시)
        // Level 1→2: 100, Level 2→3: 200, Level 3→4: 300, ...
        return currentLevel * 100;
    }
    
    public void CacheBoundsForDeath()
    {
        lastValidBounds = GetBoundsFromSource(); // 현재 Bounds 저장
        isDying = true; // 죽는 중 플래그 설정
        Debug.Log($"{gameObject.name}: Bounds cached for death - {lastValidBounds.size}");
    }
    
    private Bounds GetScreenBoundsForCanvasMode()
    {
        Bounds worldBounds = GetCurrentBounds();

        Vector3[] corners = new Vector3[8];
        corners[0] = worldBounds.min;
        corners[1] = new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z);
        corners[2] = new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z);
        corners[3] = new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z);
        corners[4] = new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z);
        corners[5] = new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z);
        corners[6] = new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z);
        corners[7] = worldBounds.max;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 corner in corners)
        {
            Vector2 canvasPoint = WorldToCanvasPoint(corner);
    
            if (canvasPoint.x < min.x) min.x = canvasPoint.x;
            if (canvasPoint.y < min.y) min.y = canvasPoint.y;
            if (canvasPoint.x > max.x) max.x = canvasPoint.x;
            if (canvasPoint.y > max.y) max.y = canvasPoint.y;
        }

        min.x -= boxPadding * 0.5f;
        min.y -= boxPadding * 0.5f;
        max.x += boxPadding * 0.5f;
        max.y += boxPadding * 0.5f;

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;

        return new Bounds(center, size);
    }
    
    private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return Vector2.zero;
            }
        }
        
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(
                screenPoint.x - Screen.width * 0.5f,
                screenPoint.y - Screen.height * 0.5f
            );
        }
        
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        
        if (screenPos.z < 0)
        {
            return Vector2.zero;
        }
        
        Vector2 canvasPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPos, 
            parentCanvas.worldCamera,
            out canvasPoint
        );
        
        if (!success)
        {
            return new Vector2(
                screenPos.x - Screen.width * 0.5f,
                screenPos.y - Screen.height * 0.5f
            );
        }
        
        return canvasPoint;
    }
    
    public void SetUIAlpha(float alpha)
    {
        forceAlpha = Mathf.Clamp01(alpha);
        isForceAlphaActive = true;
    
        if (alpha < 1f && !isDying)
        {
            isDying = true;
            lastValidBounds = GetCurrentBounds();
        }
    }
    
    public void ResetUIAlpha()
    {
        isForceAlphaActive = false;
        forceAlpha = 1f;
        isDying = false;
    }
    
    public void TransitionToColor(Color targetColor, float speed)
    {
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
        
        colorTransitionCoroutine = StartCoroutine(ColorTransitionCoroutine(targetColor, speed));
    }
    
    public void RestoreOriginalColor(float speed)
    {
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
        
        colorTransitionCoroutine = StartCoroutine(ColorTransitionCoroutine(originalBoxColor, speed));
    }
    
    private IEnumerator ColorTransitionCoroutine(Color targetColor, float speed)
    {
        Color currentColor = boxColor;
        float elapsed = 0f;
        float duration = 1f / speed;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            boxColor = Color.Lerp(currentColor, targetColor, t);
            UpdateUIColors();
            
            yield return null;
        }
        
        boxColor = targetColor;
        UpdateUIColors();
        
        colorTransitionCoroutine = null;
    }
    
    private void UpdateUIColors()
    {
        if (borderLines != null)
        {
            foreach (var line in borderLines)
            {
                if (line != null)
                {
                    Color lineColor = boxColor;
                    lineColor.a = line.color.a;
                    line.color = lineColor;
                }
            }
        }
        
        if (nameText != null)
        {
            Color textColor = boxColor;
            textColor.a = nameText.color.a;
            nameText.color = textColor;
        }
        
        if (infoText != null)
        {
            Color textColor = boxColor;
            textColor.a = infoText.color.a;
            infoText.color = textColor;
        }
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
        
        if (infoText != null)
        {
            Color textColor = infoText.color;
            textColor.a = alpha;
            infoText.color = textColor;
        }
    }
    
    public void SetBoxColor(Color color)
    {
        boxColor = color;
        originalBoxColor = color;
        if (borderLines != null)
        {
            foreach (var line in borderLines)
            {
                if (line != null) line.color = color;
            }
        }
    }
    
    public void SetVisibility(bool visible)
    {
        alwaysShow = visible;
    }
    
    public void SetBoundsSource(BoundsSourceType sourceType)
    {
        boundsSource = sourceType;
        UpdateCachedBounds();
    }
}