using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class TargetAnchorBox : MonoBehaviour
{
    public enum TargetType
    {
        Enemy,
        Box,
        EnergyCore,
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
    [SerializeField] private float boundsUpdateInterval = 0.2f; // ★ 0.1f → 0.2f
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
    [SerializeField] private bool checkOcclusion = false; // ★ 기본 OFF
    [SerializeField] private LayerMask occlusionLayers = -1;
    [SerializeField] private float occlusionCheckInterval = 0.3f; // ★ 추가
    
    [Header("Performance Settings")]
    [SerializeField] private float uiUpdateInterval = 0.033f; // ★ 30fps로 UI 업데이트
    [SerializeField] private float infoTextUpdateInterval = 0.1f; // ★ 텍스트는 더 느리게
    
    private Camera mainCamera;
    private Transform mainCameraTransform; // ★ 캐싱
    private Canvas parentCanvas;
    protected RectTransform anchorBoxUI;
    private Image[] borderLines;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI infoText;
    
    private Renderer targetRenderer;
    private Collider targetCollider;
    private EnemyAI enemyAI;
    
    private float forceAlpha = 1f;
    private bool isForceAlphaActive = false;
    
    private Bounds lastValidBounds;
    private Bounds cachedBounds;
    private float lastBoundsUpdateTime;
    private float lastUIUpdateTime;
    private float lastInfoTextUpdateTime;
    private float lastOcclusionCheckTime;
    private bool lastOcclusionResult = true;
    private bool isDying = false;
    
    private Color originalBoxColor;
    private Coroutine colorTransitionCoroutine;
    
    // ★ 캐싱
    private Vector3[] boundsCorners = new Vector3[8];
    private Vector2 cachedScreenMin;
    private Vector2 cachedScreenMax;
    private float cachedDistance;
    private Vector3 cachedCameraPosition;
    private StringBuilder stringBuilder = new StringBuilder(64); // ★ 문자열 재사용
    
    // ★ 스크린 Bounds 캐싱
    private Bounds cachedScreenBounds;
    private float lastScreenBoundsUpdateTime;
    private float screenBoundsUpdateInterval = 0.05f; // ★ 20fps
    
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            mainCameraTransform = mainCamera.transform;
            
        enemyAI = GetComponent<EnemyAI>();
        
        targetCollider = GetComponent<Collider>();
        targetRenderer = GetComponentInChildren<Renderer>();
        
        SetDefaultColorByType();
        originalBoxColor = boxColor;
        
        AnchorBoxManager.Instance?.RegisterTarget(this);
        UpdateCachedBounds();
        
        // ★ 초기 코너 계산
        PrecomputeBoundsCorners();
    }
    
    private void SetDefaultColorByType()
    {
        if (boxColor != Color.red) return; // 이미 설정됨
        
        switch (targetType)
        {
            case TargetType.Box:
                boxColor = new Color(1f, 0.8f, 0f);
                break;
            case TargetType.EnergyCore:
                boxColor = Color.cyan;
                break;
            case TargetType.Ally:
                boxColor = Color.green;
                break;
            case TargetType.Item:
                boxColor = Color.yellow;
                break;
        }
    }
    
    // ★ Bounds 코너 미리 계산 (상대 위치)
    private void PrecomputeBoundsCorners()
    {
        Bounds bounds = cachedBounds;
        boundsCorners[0] = bounds.min;
        boundsCorners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        boundsCorners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        boundsCorners[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        boundsCorners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        boundsCorners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        boundsCorners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        boundsCorners[7] = bounds.max;
    }
    
    private void OnDisable()
    {
        if (anchorBoxUI != null)
            anchorBoxUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (anchorBoxUI != null)
            anchorBoxUI.gameObject.SetActive(true);
        
        isDying = false;
        UpdateCachedBounds();
    }

    private void OnDestroy()
    {
        if (anchorBoxUI != null)
            Destroy(anchorBoxUI.gameObject);
    
        AnchorBoxManager.Instance?.UnregisterTarget(this);
    }
    
    public void CreateUI(Transform uiParent)
    {
        parentCanvas = uiParent.GetComponentInParent<Canvas>();
        
        if (parentCanvas == null) return;
        
        GameObject boxObj = new GameObject($"AnchorBox_{gameObject.name}");
        boxObj.transform.SetParent(uiParent, false);
        
        anchorBoxUI = boxObj.AddComponent<RectTransform>();
        anchorBoxUI.anchorMin = new Vector2(0.5f, 0.5f);
        anchorBoxUI.anchorMax = new Vector2(0.5f, 0.5f);
        anchorBoxUI.pivot = new Vector2(0.5f, 0.5f);
        
        CreateBorderLines();
        
        if (showName)
            CreateNameText();
        
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
    
    private void CreateInfoText()
    {
        GameObject textObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
        textObj.name = "InfoText";
        textObj.transform.SetParent(anchorBoxUI, false);

        infoText = textObj.GetComponent<TextMeshProUGUI>();
        infoText.color = boxColor;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        
        switch (targetType)
        {
            case TargetType.Enemy:
                infoText.alignment = TextAlignmentOptions.Left;
                infoText.fontSize = 34;
                textRect.anchorMin = new Vector2(1f, 0.5f);
                textRect.anchorMax = new Vector2(1f, 0.5f);
                textRect.pivot = new Vector2(0f, 0.5f);
                textRect.sizeDelta = new Vector2(420, 60);
                textRect.anchoredPosition = new Vector2(10, 0);
                break;
                
            case TargetType.Box:
                infoText.alignment = TextAlignmentOptions.Center;
                infoText.fontSize = 30;
                textRect.anchorMin = new Vector2(0.5f, 0f);
                textRect.anchorMax = new Vector2(0.5f, 0f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.sizeDelta = new Vector2(400, 50);
                textRect.anchoredPosition = new Vector2(0, -10);
                break;
                
            case TargetType.EnergyCore:
                infoText.alignment = TextAlignmentOptions.Left;
                infoText.fontSize = 34;
                textRect.anchorMin = new Vector2(1f, 0.5f);
                textRect.anchorMax = new Vector2(1f, 0.5f);
                textRect.pivot = new Vector2(0f, 0.5f);
                textRect.sizeDelta = new Vector2(250, 70);
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
        if (anchorBoxUI == null) return;
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                mainCameraTransform = mainCamera.transform;
            return;
        }
        
        // ★ UI 업데이트 주기 체크
        if (Time.time - lastUIUpdateTime < uiUpdateInterval)
            return;
        
        lastUIUpdateTime = Time.time;
        UpdateAnchorBoxOptimized();
    }
    
    private void UpdateAnchorBoxOptimized()
    {
        // ★ 거리 계산 (캐싱)
        cachedCameraPosition = mainCameraTransform.position;
        cachedDistance = Vector3.Distance(cachedCameraPosition, transform.position);
        
        // ★ 빠른 거리 체크
        if (cachedDistance < minVisibleDistance || cachedDistance > maxVisibleDistance)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        // ★ 카메라 뒤 체크 (빠른 dot product)
        Vector3 toTarget = transform.position - cachedCameraPosition;
        if (Vector3.Dot(mainCameraTransform.forward, toTarget) < 0)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        // ★ Occlusion 체크 (주기적으로만)
        if (checkOcclusion && Time.time - lastOcclusionCheckTime > occlusionCheckInterval)
        {
            lastOcclusionResult = IsVisibleFromCameraFast();
            lastOcclusionCheckTime = Time.time;
        }
        
        if (checkOcclusion && !lastOcclusionResult)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        anchorBoxUI.gameObject.SetActive(alwaysShow);
        
        // ★ Bounds 업데이트 (주기적으로만)
        if (Time.time - lastBoundsUpdateTime > boundsUpdateInterval)
        {
            UpdateCachedBounds();
            PrecomputeBoundsCorners();
            lastBoundsUpdateTime = Time.time;
        }
        
        // ★ 스크린 Bounds 계산 (주기적으로만)
        if (Time.time - lastScreenBoundsUpdateTime > screenBoundsUpdateInterval)
        {
            cachedScreenBounds = GetScreenBoundsOptimized();
            lastScreenBoundsUpdateTime = Time.time;
        }
        
        if (cachedScreenBounds.size.magnitude > 10000f || cachedScreenBounds.size.magnitude < 1f)
        {
            anchorBoxUI.gameObject.SetActive(false);
            return;
        }
        
        // ★ UI 위치/크기 업데이트
        Vector2 boxSize = new Vector2(cachedScreenBounds.size.x, cachedScreenBounds.size.y);
        anchorBoxUI.sizeDelta = boxSize;
        anchorBoxUI.anchoredPosition = new Vector2(cachedScreenBounds.center.x, cachedScreenBounds.center.y);
        
        if (useCornerOnly)
            UpdateCornerLines(boxSize);
        else
            UpdateFullBorderLines(boxSize);
        
        // ★ 텍스트 업데이트 (더 느리게)
        if (Time.time - lastInfoTextUpdateTime > infoTextUpdateInterval)
        {
            UpdateTypeSpecificInfoOptimized();
            lastInfoTextUpdateTime = Time.time;
        }
        
        // ★ Alpha 계산
        if (isForceAlphaActive)
        {
            ApplyBorderAlpha(forceAlpha);
            ApplyTextAlpha(forceAlpha);
        }
        else if (fadeWithDistance)
        {
            float borderAlpha = 1f - (cachedDistance - minVisibleDistance) / (maxVisibleDistance - minVisibleDistance);
            float textAlpha = 1f - (cachedDistance - minVisibleDistance) / (maxTextVisibleDistance - minVisibleDistance);
            ApplyBorderAlpha(Mathf.Clamp01(borderAlpha));
            ApplyTextAlpha(Mathf.Clamp01(textAlpha));
        }
    }
    
    // ★ 최적화된 Occlusion 체크 (1개 레이만)
    private bool IsVisibleFromCameraFast()
    {
        Vector3 direction = cachedBounds.center - cachedCameraPosition;
        float distance = direction.magnitude;
        
        if (Physics.Raycast(cachedCameraPosition, direction / distance, out RaycastHit hit, distance, occlusionLayers))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }
        
        return true;
    }
    
    // ★ 최적화된 스크린 Bounds 계산
    private Bounds GetScreenBoundsOptimized()
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        // ★ 8개 코너를 한 번에 처리
        for (int i = 0; i < 8; i++)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(boundsCorners[i]);
            
            if (screenPoint.z < 0) continue;
            
            float canvasX = screenPoint.x - Screen.width * 0.5f;
            float canvasY = screenPoint.y - Screen.height * 0.5f;
            
            if (canvasX < minX) minX = canvasX;
            if (canvasY < minY) minY = canvasY;
            if (canvasX > maxX) maxX = canvasX;
            if (canvasY > maxY) maxY = canvasY;
        }

        minX -= boxPadding * 0.5f;
        minY -= boxPadding * 0.5f;
        maxX += boxPadding * 0.5f;
        maxY += boxPadding * 0.5f;

        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 size = new Vector2(maxX - minX, maxY - minY);

        return new Bounds(center, size);
    }
    
    private void UpdateCachedBounds()
    {
        if (useManualCenter)
        {
            cachedBounds = new Bounds(transform.position + manualCenterOffset, manualBoundsSize);
        }
        else if (targetCollider != null)
        {
            cachedBounds = targetCollider.bounds;
        }
        else if (targetRenderer != null)
        {
            cachedBounds = targetRenderer.bounds;
        }
        else
        {
            cachedBounds = new Bounds(transform.position, Vector3.one);
        }
        
        // ★ 간단한 유효성 검사
        Vector3 size = cachedBounds.size;
        if (size.x < minBoundsSize || size.y < minBoundsSize || size.z < minBoundsSize)
        {
            cachedBounds = new Bounds(cachedBounds.center, Vector3.Max(size, Vector3.one * minBoundsSize));
        }
    }
    
    // ★ 문자열 최적화 (StringBuilder 재사용)
    private void UpdateTypeSpecificInfoOptimized()
    {
        if (infoText == null) return;
        
        stringBuilder.Clear();
        
        switch (targetType)
        {
            case TargetType.Enemy:
                UpdateEnemyInfoOptimized();
                break;
            case TargetType.Box:
                UpdateBoxInfoOptimized();
                break;
            case TargetType.EnergyCore:
                UpdateEnergyCoreInfoOptimized();
                break;
            default:
                infoText.text = "";
                break;
        }
    }
    
    private void UpdateEnemyInfoOptimized()
    {
        if (enemyAI != null)
        {
            float hp = enemyAI.GetHealth();
            float dmg = enemyAI.GetDamage();
            
            stringBuilder.Append("<color=#");
            stringBuilder.Append(ColorUtility.ToHtmlStringRGB(boxColor));
            stringBuilder.Append(">HP: ");
            stringBuilder.Append(hp.ToString("F1")); // ★ F1 → F0
            stringBuilder.Append("</color>\n<color=#FF8800>Damage: ");
            stringBuilder.Append(dmg.ToString("F1"));
            stringBuilder.Append("</color>");
        }
        else
        {
            stringBuilder.Append("<color=#");
            stringBuilder.Append(ColorUtility.ToHtmlStringRGB(boxColor));
            stringBuilder.Append(">HP: ???</color>");
        }
        
        infoText.text = stringBuilder.ToString();
    }
    
    private void UpdateBoxInfoOptimized()
    {
        var boxAnchorBox = GetComponent<BoxAnchorBox>();
    
        if (boxAnchorBox != null)
        {
            int required = boxAnchorBox.GetCurrentCost();
            int playerEnergy = PlayerStats.Instance != null ? PlayerStats.Instance.GetEnergy() : 0;
            
            stringBuilder.Append("<color=#");
            stringBuilder.Append(ColorUtility.ToHtmlStringRGB(boxColor));
            stringBuilder.Append(">");
            
            if (playerEnergy >= required)
            {
                stringBuilder.Append("[F] Open Box\nEnergy: ");
                stringBuilder.Append(playerEnergy);
                stringBuilder.Append("/");
                stringBuilder.Append(required);
                stringBuilder.Append("</color>");
            }
            else
            {
                stringBuilder.Append("Locked Box</color>\n<color=#FF0000>Energy: ");
                stringBuilder.Append(playerEnergy);
                stringBuilder.Append("/");
                stringBuilder.Append(required);
                stringBuilder.Append("</color>");
            }
        }
        
        infoText.text = stringBuilder.ToString();
    }
    
    private void UpdateEnergyCoreInfoOptimized()
    {
        if (ForceFieldManager.Instance != null)
        {
            int level = ForceFieldManager.Instance.GetCurrentLevel();
            int maxLevel = ForceFieldManager.Instance.GetMaxLevel();
            
            stringBuilder.Append("Level ");
            stringBuilder.Append(level);
            
            if (level >= maxLevel)
            {
                stringBuilder.Append("\n<color=#00FF00>MAX LEVEL</color>");
            }
            else
            {
                int required = ForceFieldManager.Instance.GetRequiredEnergyForNextLevel();
                int playerEnergy = PlayerStats.Instance != null ? PlayerStats.Instance.GetEnergy() : 0;
                
                if (playerEnergy >= required)
                {
                    stringBuilder.Append("\n<color=#00FF00>[F] Upgrade: ");
                    stringBuilder.Append(playerEnergy);
                    stringBuilder.Append("/");
                    stringBuilder.Append(required);
                    stringBuilder.Append("</color> Energy");
                }
                else
                {
                    stringBuilder.Append("\n<color=#FF0000>Require: ");
                    stringBuilder.Append(playerEnergy);
                    stringBuilder.Append("/");
                    stringBuilder.Append(required);
                    stringBuilder.Append("</color>");
                }
            }
        }
        
        infoText.text = stringBuilder.ToString();
    }
    
    public void CacheBoundsForDeath()
    {
        lastValidBounds = cachedBounds;
        isDying = true;
    }
    
    public void SetUIAlpha(float alpha)
    {
        forceAlpha = Mathf.Clamp01(alpha);
        isForceAlphaActive = true;
    
        if (alpha < 1f && !isDying)
        {
            isDying = true;
            lastValidBounds = cachedBounds;
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
            StopCoroutine(colorTransitionCoroutine);
        
        colorTransitionCoroutine = StartCoroutine(ColorTransitionCoroutine(targetColor, speed));
    }
    
    public void RestoreOriginalColor(float speed)
    {
        if (colorTransitionCoroutine != null)
            StopCoroutine(colorTransitionCoroutine);
        
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
            boxColor = Color.Lerp(currentColor, targetColor, elapsed / duration);
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
                    Color c = boxColor;
                    c.a = line.color.a;
                    line.color = c;
                }
            }
        }
        
        if (nameText != null)
        {
            Color c = boxColor;
            c.a = nameText.color.a;
            nameText.color = c;
        }
        
        if (infoText != null)
        {
            Color c = boxColor;
            c.a = infoText.color.a;
            infoText.color = c;
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
            Color c = nameText.color;
            c.a = alpha;
            nameText.color = c;
        }
        
        if (infoText != null)
        {
            Color c = infoText.color;
            c.a = alpha;
            infoText.color = c;
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
}