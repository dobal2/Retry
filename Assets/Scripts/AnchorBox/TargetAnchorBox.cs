using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private bool showDamage = true;
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
    
    [Header("Camera View Settings")]
    [SerializeField] private bool showOnlyInCameraView = false;
    [SerializeField] private float cameraViewAngle = 60f;
    
    private Camera mainCamera;
    private Canvas parentCanvas;
    private RectTransform anchorBoxUI;
    private Image[] borderLines;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI damageText;
    
    private Renderer targetRenderer;
    private Collider targetCollider;
    private EnemyAI enemyAI;
    
    private float forceAlpha = 1f;
    private bool isForceAlphaActive = false;
    
    private Bounds lastValidBounds;
    private bool isDying = false;
    
    private Color originalBoxColor;
    private Coroutine colorTransitionCoroutine;
    
    private void Start()
    {
        mainCamera = Camera.main;
        enemyAI = GetComponent<EnemyAI>();
        
        targetCollider = GetComponent<Collider>();
        targetRenderer = GetComponentInChildren<Renderer>();
        
        SetDefaultColorByType();
        originalBoxColor = boxColor;
        AnchorBoxManager.Instance?.RegisterTarget(this);
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

        Bounds bounds;
        if (targetRenderer != null)
            bounds = targetRenderer.bounds;
        else if (targetCollider != null)
            bounds = targetCollider.bounds;
        else
            bounds = new Bounds(transform.position, Vector3.one);

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
        
        isDying = false;
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
        // ★ Canvas 참조 가져오기
        parentCanvas = uiParent.GetComponentInParent<Canvas>();
        
        GameObject boxObj = new GameObject($"AnchorBox_{gameObject.name}");
        boxObj.transform.SetParent(uiParent, false);
        
        anchorBoxUI = boxObj.AddComponent<RectTransform>();
        
        // ★ Canvas 중앙을 기준으로 설정
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            anchorBoxUI.anchorMin = new Vector2(0.5f, 0.5f);
            anchorBoxUI.anchorMax = new Vector2(0.5f, 0.5f);
            anchorBoxUI.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            // Screen Space - Overlay
            anchorBoxUI.anchorMin = Vector2.zero;
            anchorBoxUI.anchorMax = Vector2.zero;
            anchorBoxUI.pivot = new Vector2(0.5f, 0.5f);
        }
        
        CreateBorderLines();
        
        if (showName)
            CreateNameText();
        
        if (showDamage)
            CreateDamageText();
        
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
    
    private void CreateDamageText()
    {
        GameObject textObj = Instantiate(AnchorBoxManager.Instance.textTemplate);
        textObj.name = "DamageText";
        textObj.transform.SetParent(anchorBoxUI, false);
    
        damageText = textObj.GetComponent<TextMeshProUGUI>();
        damageText.color = Color.red;
        damageText.alignment = TextAlignmentOptions.Left;
    
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.sizeDelta = new Vector2(300, 20);
        textRect.anchoredPosition = new Vector2(10, showHealthText ? -20 : 0);
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
    
    private void Update()
    {
        // ★ Camera 재확인
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
        
        Vector2 boxSize = new Vector2(screenBounds.size.x, screenBounds.size.y);
        anchorBoxUI.sizeDelta = boxSize;
        anchorBoxUI.anchoredPosition = new Vector2(screenBounds.center.x, screenBounds.center.y);
        
        if (useCornerOnly)
            UpdateCornerLines(boxSize);
        else
            UpdateFullBorderLines(boxSize);
        
        if (damageText != null && enemyAI != null)
        {
            float enemyDamage = enemyAI.GetDamage();
            damageText.text = $"Damage: {Mathf.RoundToInt(enemyDamage)}";
        }
        
        if (healthText != null && enemyAI != null)
        {
            float currentHealth = enemyAI.GetHealth();
            healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)}";
        }
        
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
    
    private Bounds GetScreenBoundsForCanvasMode()
    {
        Bounds worldBounds;
    
        if (isDying)
        {
            worldBounds = lastValidBounds;
        }
        else
        {
            // ★ Collider 우선 사용
            if (targetCollider != null)
            {
                worldBounds = targetCollider.bounds;
            }
            else if (targetRenderer != null)
            {
                worldBounds = targetRenderer.bounds;
            }
            else
            {
                worldBounds = new Bounds(transform.position, Vector3.one);
            }
        }

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
    
    // ★ World 좌표를 Canvas 좌표로 변환
    private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
    {
        // ★ mainCamera null 체크
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return Vector2.zero;
            }
        }
        
        if (parentCanvas == null)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(screenPoint.x, screenPoint.y);
        }
        
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        
        // World → Screen
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        
        // Screen → Canvas Local Point
        Vector2 canvasPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPos, 
            parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : null,
            out canvasPoint
        );
        
        return canvasPoint;
    }
    
    public void SetUIAlpha(float alpha)
    {
        forceAlpha = Mathf.Clamp01(alpha);
        isForceAlphaActive = true;
    
        if (alpha < 1f && !isDying)
        {
            isDying = true;
            // ★ Collider 우선으로 저장
            if (targetCollider != null)
                lastValidBounds = targetCollider.bounds;
            else if (targetRenderer != null)
                lastValidBounds = targetRenderer.bounds;
            else
                lastValidBounds = new Bounds(transform.position, Vector3.one);
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
        
        if (healthText != null)
        {
            Color textColor = boxColor;
            textColor.a = healthText.color.a;
            healthText.color = textColor;
        }
        
        if (damageText != null)
        {
            Color textColor = boxColor;
            textColor.a = damageText.color.a;
            damageText.color = textColor;
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
        
        if (damageText != null)
        {
            Color textColor = damageText.color;
            textColor.a = alpha;
            damageText.color = textColor;
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
        originalBoxColor = color;
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