using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Terrain Scanner Particle")]
    [SerializeField] private ParticleSystem terrainScannerPrefab;
    [SerializeField] private bool enableTerrainScanner = true;
    [SerializeField] private float scannerRepeatInterval = 5f;
    [SerializeField] private float scannerHeightOffset = 0.5f;
    [SerializeField] private Color scannerColor = Color.cyan;
    [SerializeField] private bool pauseScannerOnTimeStop = true;
    
    [Header("Upgrade Energy Cost")]
    [SerializeField] private int baseEnergyCost = 30;
    [SerializeField] private float costMultiplierPerLevel = 1.3f;
    

    
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
    private ParticleSystem terrainScannerInstance;
    private Vector3 targetScale;
    private Coroutine growCoroutine;
    private Coroutine scannerCoroutine;
    private Coroutine scanDamageCoroutine;
    private float damageTimer;
    private bool isPlayerInside = true;
    private Coroutine fogTransitionCoroutine;
    private bool wasTimeStopped = false;
    private bool wasScannerPlaying = false;
    
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
        CreateTerrainScanner();
        
        // 초기 Fog 설정
        if (fogMaterial != null)
        {
            fogMaterial.SetFloat(densityPropertyName, insideDensity);
        }
        
        // 테레인 스캐너 시작
        if (enableTerrainScanner && terrainScannerInstance != null)
        {
            scannerCoroutine = StartCoroutine(TerrainScannerRoutine());
        }
    }
    
    private void Update()
    {
        if (PlayerStats.Instance == null) 
        {
            return;
        }
        
        CheckPlayerPosition();
        CheckTimeStopState();
        
        // 테스트용
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        {
            LevelUp();
        }
        
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            LevelDown();
        }
        
        // 테레인 스캐너 수동 트리거 (테스트용)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            TriggerTerrainScan();
        }
    }
    
    public int GetRequiredEnergyForNextLevel()
    {
        return Mathf.RoundToInt(baseEnergyCost + (currentLevel - 1) * baseEnergyCost * costMultiplierPerLevel);
    }
    
    public bool TryLevelUpWithEnergy()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log("Force Field is already at max level!");
            return false;
        }
    
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("PlayerStats not found!");
            return false;
        }
    
        int requiredEnergy = GetRequiredEnergyForNextLevel();
        int playerEnergy = PlayerStats.Instance.GetEnergy();
    
        if (playerEnergy >= requiredEnergy)
        {
            PlayerStats.Instance.ConsumeEnergy(requiredEnergy);
            LevelUp();
            Debug.Log($"Force Field upgraded! Energy consumed: {requiredEnergy}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough energy! Required: {requiredEnergy}, Current: {playerEnergy}");
            return false;
        }
    }
    
    public int GetMaxLevel()
    {
        return maxLevel;
    }
    
    private void CheckTimeStopState()
    {
        if (!pauseScannerOnTimeStop || terrainScannerInstance == null) return;
        if (TimeStopManager.Instance == null) return;
        
        bool isTimeStopped = TimeStopManager.Instance.IsTimeStopped && 
                            TimeStopManager.Instance.AffectForceFields;
        
        if (isTimeStopped != wasTimeStopped)
        {
            if (isTimeStopped)
            {
                OnTimeStop();
            }
            else
            {
                OnTimeResume();
            }
            
            wasTimeStopped = isTimeStopped;
        }
    }
    
    private void OnTimeStop()
    {
        if (terrainScannerInstance == null) return;
        
        wasScannerPlaying = terrainScannerInstance.isPlaying;
        terrainScannerInstance.Pause();
        
        if (scannerCoroutine != null)
        {
            StopCoroutine(scannerCoroutine);
            scannerCoroutine = null;
        }
        
        if (scanDamageCoroutine != null)
        {
            StopCoroutine(scanDamageCoroutine);
            scanDamageCoroutine = null;
        }
        
        Debug.Log("[ForceField] Terrain Scanner paused due to time stop");
    }
    
    private void OnTimeResume()
    {
        if (terrainScannerInstance == null) return;
        
        if (wasScannerPlaying)
        {
            terrainScannerInstance.Play();
        }
        
        if (enableTerrainScanner && scannerCoroutine == null)
        {
            scannerCoroutine = StartCoroutine(TerrainScannerRoutine());
        }
        
        Debug.Log("[ForceField] Terrain Scanner resumed");
    }
    
    private void CreateTerrainScanner()
    {
        if (terrainScannerPrefab == null)
        {
            Debug.LogWarning("Terrain Scanner Prefab is not assigned!");
            return;
        }
    
        Vector3 spawnPosition = Vector3.zero + Vector3.up * scannerHeightOffset;
        terrainScannerInstance = Instantiate(terrainScannerPrefab, spawnPosition, Quaternion.identity);
        terrainScannerInstance.name = "TerrainScanner";
        terrainScannerInstance.transform.SetParent(transform);
    
        // 초기 크기 설정
        UpdateTerrainScannerSize();
        
        // 처음에는 재생하지 않음
        terrainScannerInstance.Stop();
    
        Debug.Log("Terrain Scanner created at map center");
    }
    
    private void UpdateTerrainScannerSize()
    {
        if (terrainScannerInstance == null) return;
    
        float currentSize = GetCurrentSize();
        float radius = currentSize / 2f;
    
        // ParticleSystem의 Shape 모듈 크기 조정
        var shape = terrainScannerInstance.shape;
        shape.radius = radius;
    
        // ParticleSystem의 Start Size 조정
        var main = terrainScannerInstance.main;
        main.startSize = currentSize;
        main.startColor = scannerColor;
    
        Debug.Log($"Terrain Scanner size updated - Radius: {radius}, Diameter: {currentSize}");
    }
    
    private IEnumerator TerrainScannerRoutine()
    {
        yield return new WaitForSeconds(1f);
        
        while (true)
        {
            if (TimeStopManager.Instance != null && 
                TimeStopManager.Instance.IsTimeStopped && 
                TimeStopManager.Instance.AffectForceFields &&
                pauseScannerOnTimeStop)
            {
                yield return null;
                continue;
            }
            
            TriggerTerrainScan();
            yield return new WaitForSeconds(scannerRepeatInterval);
        }
    }
    
    private void TriggerTerrainScan()
    {
        if (terrainScannerInstance != null)
        {
            if (TimeStopManager.Instance != null && 
                TimeStopManager.Instance.IsTimeStopped && 
                TimeStopManager.Instance.AffectForceFields &&
                pauseScannerOnTimeStop)
            {
                Debug.Log("Terrain Scanner blocked - Time is stopped");
                return;
            }
        
            UpdateTerrainScannerSize();
            terrainScannerInstance.Play();
            
            // 웨이브 확장 데미지 코루틴 시작
            if (scanDamageCoroutine != null)
            {
                StopCoroutine(scanDamageCoroutine);
            }
            
            Debug.Log("Terrain Scanner triggered");
        }
    }
    
    
    private void CheckPlayerPosition()
    {
        float distanceFromCenter = Vector3.Distance(PlayerStats.Instance.gameObject.transform.position, forceFieldInstance.transform.position);
        float forceFieldRadius = GetCurrentSize() / 2f;
        
        bool wasInside = isPlayerInside;
        isPlayerInside = distanceFromCenter <= forceFieldRadius;
        
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
        
        UpdateTerrainScannerSize();
        
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
    
        // 펄스는 성장 애니메이션 끝난 후에
        if (enablePulseOnLevelUp)
        {
            StartCoroutine(PulseAfterGrow());
        }
    
        if (enableTerrainScanner)
        {
            TriggerTerrainScan();
        }
    }

    private IEnumerator PulseAfterGrow()
    {
        // GrowToSize가 끝날 때까지 대기
        while (growCoroutine != null)
        {
            yield return null;
        }
    
        yield return StartCoroutine(PulseEffect());
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
    
        if (terrainScannerInstance != null)
        {
            Destroy(terrainScannerInstance.gameObject);
            Debug.Log("Terrain Scanner destroyed");
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
    
    public void SetScannerRepeatInterval(float interval)
    {
        scannerRepeatInterval = interval;
        
        if (scannerCoroutine != null)
        {
            StopCoroutine(scannerCoroutine);
        }
        
        if (enableTerrainScanner && terrainScannerInstance != null)
        {
            scannerCoroutine = StartCoroutine(TerrainScannerRoutine());
        }
    }
    
    public void SetScannerColor(Color color)
    {
        scannerColor = color;
        UpdateTerrainScannerSize();
    }
    
    public void ToggleTerrainScanner(bool enable)
    {
        enableTerrainScanner = enable;
        
        if (enable)
        {
            if (scannerCoroutine == null && terrainScannerInstance != null)
            {
                scannerCoroutine = StartCoroutine(TerrainScannerRoutine());
            }
        }
        else
        {
            if (scannerCoroutine != null)
            {
                StopCoroutine(scannerCoroutine);
                scannerCoroutine = null;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (forceFieldInstance != null)
        {
            Gizmos.color = isPlayerInside ? Color.green : Color.red;
            Gizmos.DrawWireSphere(forceFieldInstance.transform.position, GetCurrentSize() / 2f);
            
            #if UNITY_EDITOR
            string timeStatus = "";
            if (TimeStopManager.Instance != null && TimeStopManager.Instance.IsTimeStopped)
            {
                timeStatus = "\n⏸ TIME STOPPED";
            }
            
            UnityEditor.Handles.Label(
                forceFieldInstance.transform.position + Vector3.up * (GetCurrentSize() / 2f + 2f),
                $"Level {currentLevel}\nSize: {GetCurrentSize():F1}\nPlayer: {(isPlayerInside ? "SAFE" : "DANGER")}\nScanner: {(enableTerrainScanner ? "ON" : "OFF")}{timeStatus}"
            );
            #endif
        }
    }
}