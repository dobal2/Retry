using UnityEngine;
using System.Collections;

public class ForceFieldManager : MonoBehaviour
{
    [Header("Force Field Settings")]
    [SerializeField] private GameObject forceFieldPrefab;
    
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private float baseSize = 10f;
    [SerializeField] private float sizePerLevel = 5f;
    
    [Header("Animation Settings")]
    [SerializeField] private float growDuration = 2f;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Visual Effects")]
    [SerializeField] private bool enablePulseOnLevelUp = true;
    [SerializeField] private float pulseAmount = 1.2f;
    [SerializeField] private float pulseDuration = 0.5f;
    
    [Header("Player Protection")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageInterval = 1f;
    
    [Header("Fog Settings")]
    [SerializeField] private Material fogMaterial;
    [SerializeField] private string densityPropertyName = "_DensityMultiplier";
    [SerializeField] private float insideDensity = 0f;
    [SerializeField] private float outsideDensity = 1.88f;
    [SerializeField] private float fogTransitionSpeed = 2f;
    
    private GameObject forceFieldInstance;
    private Vector3 targetScale;
    private Coroutine growCoroutine;
    private float damageTimer;
    private bool isPlayerInside = true;
    private Coroutine fogTransitionCoroutine;
    
    public static ForceFieldManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        CreateForceField();
        
        // 초기 Fog 설정
        if (fogMaterial != null)
        {
            fogMaterial.SetFloat(densityPropertyName, insideDensity);
        }
    }
    
    private void Update()
    {
        if (PlayerStats.Instance == null) 
        {
            return;
        }
        
        CheckPlayerPosition();
        
        // 테스트용
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        {
            LevelUp();
        }
        
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            LevelDown();
        }
    }
    
    private void CheckPlayerPosition()
    {

        
        float distanceFromCenter = Vector3.Distance(PlayerStats.Instance.gameObject.transform.position, forceFieldInstance.transform.position);
        float forceFieldRadius = GetCurrentSize() / 2f;
        
        bool wasInside = isPlayerInside;
        isPlayerInside = distanceFromCenter <= forceFieldRadius;
        
        // 상태 변경 시 Fog 전환
        if (wasInside != isPlayerInside)
        {
            if (isPlayerInside)
            {
                OnPlayerEnterForceField();
            }
            else
            {
                OnPlayerExitForceField();
            }
        }
        
        // 밖에 있으면 데미지
        if (!isPlayerInside)
        {
            damageTimer += Time.deltaTime;
            
            if (damageTimer >= damageInterval)
            {
                DealDamageToPlayer();
                damageTimer = 0f;
            }
        }
        else
        {
            damageTimer = 0f;
        }
    }
    
    private void OnPlayerEnterForceField()
    {
        Debug.Log("Player entered force field - Safe!");
        TransitionFog(insideDensity);
    }
    
    private void OnPlayerExitForceField()
    {
        Debug.Log("Player exited force field - Taking damage!");
        TransitionFog(outsideDensity);
    }
    
    private void TransitionFog(float targetDensity)
    {
        if (fogMaterial == null) return;
        
        if (fogTransitionCoroutine != null)
        {
            StopCoroutine(fogTransitionCoroutine);
        }
        
        fogTransitionCoroutine = StartCoroutine(FogTransitionCoroutine(targetDensity));
    }
    
    private IEnumerator FogTransitionCoroutine(float targetDensity)
    {
        float currentDensity = fogMaterial.GetFloat(densityPropertyName);
        float elapsed = 0f;
        float duration = 1f / fogTransitionSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float newDensity = Mathf.Lerp(currentDensity, targetDensity, t);
            fogMaterial.SetFloat(densityPropertyName, newDensity);
            
            yield return null;
        }
        
        fogMaterial.SetFloat(densityPropertyName, targetDensity);
        fogTransitionCoroutine = null;
    }
    
    private void DealDamageToPlayer()
    {
        // PlayerStats 있다면
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(damagePerSecond);
            Debug.Log($"Player taking damage: {damagePerSecond}");
        }
        else
        {
            Debug.LogWarning("PlayerStats not found!");
        }
    }
    
    private void CreateForceField()
    {
        if (forceFieldPrefab == null)
        {
            Debug.LogError("Force Field Prefab is not assigned!");
            return;
        }
        
        Vector3 spawnPosition = Vector3.zero;
        forceFieldInstance = Instantiate(forceFieldPrefab, spawnPosition, Quaternion.identity);
        forceFieldInstance.name = "ForceField";
        
        SetForceFieldLevel(currentLevel, false);
        
        Debug.Log($"Force Field created at {spawnPosition} with level {currentLevel}");
    }
    
    public void SetForceFieldLevel(int level, bool animate = true)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        
        float targetSize = baseSize + (currentLevel - 1) * sizePerLevel;
        targetScale = Vector3.one * targetSize;
        
        if (forceFieldInstance == null)
        {
            Debug.LogWarning("Force Field instance not found!");
            return;
        }
        
        if (animate)
        {
            if (growCoroutine != null)
            {
                StopCoroutine(growCoroutine);
            }
            growCoroutine = StartCoroutine(GrowToSize(targetScale));
        }
        else
        {
            forceFieldInstance.transform.localScale = targetScale;
        }
        
        Debug.Log($"Force Field level set to {currentLevel}, size: {targetSize}");
    }
    
    public void LevelUp()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log("Force Field is already at max level!");
            return;
        }
        
        SetForceFieldLevel(currentLevel + 1, true);
        
        if (enablePulseOnLevelUp)
        {
            StartCoroutine(PulseEffect());
        }
    }
    
    public void LevelDown()
    {
        if (currentLevel <= 1)
        {
            Debug.Log("Force Field is already at minimum level!");
            return;
        }
        
        SetForceFieldLevel(currentLevel - 1, true);
    }
    
    private IEnumerator GrowToSize(Vector3 targetSize)
    {
        Vector3 startScale = forceFieldInstance.transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            float curveValue = growCurve.Evaluate(t);
            
            forceFieldInstance.transform.localScale = Vector3.Lerp(startScale, targetSize, curveValue);
            
            yield return null;
        }
        
        forceFieldInstance.transform.localScale = targetSize;
        growCoroutine = null;
    }
    
    private IEnumerator PulseEffect()
    {
        Vector3 originalScale = forceFieldInstance.transform.localScale;
        Vector3 pulseScale = originalScale * pulseAmount;
        
        float elapsed = 0f;
        float halfDuration = pulseDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            forceFieldInstance.transform.localScale = Vector3.Lerp(originalScale, pulseScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            forceFieldInstance.transform.localScale = Vector3.Lerp(pulseScale, originalScale, t);
            yield return null;
        }
        
        forceFieldInstance.transform.localScale = originalScale;
    }
    
    public void DestroyForceField()
    {
        if (forceFieldInstance != null)
        {
            Destroy(forceFieldInstance);
            Debug.Log("Force Field destroyed");
        }
    }
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public float GetCurrentSize()
    {
        return baseSize + (currentLevel - 1) * sizePerLevel;
    }
    
    public GameObject GetForceFieldInstance()
    {
        return forceFieldInstance;
    }
    
    public bool IsPlayerInside()
    {
        return isPlayerInside;
    }
    
    private void OnDrawGizmos()
    {
        if (forceFieldInstance != null)
        {
            Gizmos.color = isPlayerInside ? Color.green : Color.red;
            Gizmos.DrawWireSphere(forceFieldInstance.transform.position, GetCurrentSize() / 2f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                forceFieldInstance.transform.position + Vector3.up * (GetCurrentSize() / 2f + 2f),
                $"Level {currentLevel}\nSize: {GetCurrentSize():F1}\nPlayer: {(isPlayerInside ? "SAFE" : "DANGER")}"
            );
            #endif
        }
    }
}