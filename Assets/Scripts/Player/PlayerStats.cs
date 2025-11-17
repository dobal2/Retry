using System;
using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerStats : MonoBehaviour
{
    #region SINGLETON
    
    private static PlayerStats instance;
    public static PlayerStats Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerStats>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayerStats");
                    instance = go.AddComponent<PlayerStats>();
                }
            }
            return instance;
        }
    }
    
    #endregion

    #region FIELDS SERIALIZED
    
    [SerializeField]
    private float maxHealth = 100f;
    
    [SerializeField]
    private float currentHealth = 100f;

    [Header("Combat Stats")]
    [SerializeField]
    private float attackSpeedMultiplier = 1.0f;

    [Header("Projectile Stats")] 
    [SerializeField]
    private float projectileDamage = 1.0f;
    
    [SerializeField]
    private float projectileSpeedMultiplier = 1.0f;
    
    [SerializeField]
    private float projectileSizeMultiplier = 1.0f;

    [Header("Movement Stats")]
    [SerializeField]
    private float moveSpeedMultiplier = 1.0f;
    
    [SerializeField]
    private float jumpForceMultiplier = 1.0f;
    
    [SerializeField]
    private int maxJumpCount = 1;

    [SerializeField] private int currentEnergy;

    [Header("Damage Visual Effects")]
    [SerializeField] private ShakeData takeDamageShake;
    [SerializeField] private CameraShaker shaker;
    [SerializeField] private float damageEffectDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private AnimationCurve damageEffectCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Material Damage Effect")]
    [SerializeField] private Material damageMaterial;
    [SerializeField] private float originalBlurOffset = 0.0025f;
    [SerializeField] private float maxBlurOffset = 0.005f;
    [SerializeField] private float originalIntensity = 0.4f;
    [SerializeField] private float maxIntensity = 1f;

    private GameObject GameoverScreen;

    [SerializeField] private float healInterval;
    private float healTimer;
    

    [Header("Debug")]
    [SerializeField]
    private bool showDebugInfo = false;

    #endregion

    #region PRIVATE FIELDS
    
    private Volume volume;
    private Vignette vignette;
    private float originalVignetteIntensity;
    private Coroutine damageEffectCoroutine;
    
    
    
    #endregion

    #region UNITY

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        //DontDestroyOnLoad(gameObject);
        
        GameoverScreen = GameObject.Find("GameOver");
        if (GameoverScreen != null)
        {
            GameoverScreen.SetActive(false);
        }
        
        currentHealth = maxHealth;
        
        InitializeDamageEffects();
    }

    private void InitializeDamageEffects()
    {
        // Volume 찾기
        if (volume == null)
        {
            volume = FindObjectOfType<Volume>();
        }
        
        // Vignette 설정
        if (volume != null && volume.profile.TryGet(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
        }
        else
        {
            Debug.LogWarning("No Volume for damage effect");
        }
        
        // CameraShaker 찾기
        if (shaker == null)
        {
            shaker = FindObjectOfType<CameraShaker>();
        }
        
        // Material 초기값 설정
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("=== Player Stats ===");
        GUILayout.Label($"HP: {currentHealth:F0} / {maxHealth:F0}");
        GUILayout.Label($"Attack Speed: x{attackSpeedMultiplier:F2}");
        GUILayout.Label($"Projectile Damage: x{projectileDamage:F2}");
        GUILayout.Label($"Projectile Speed: x{projectileSpeedMultiplier:F2}");
        GUILayout.Label($"Projectile Size: x{projectileSizeMultiplier:F2}");
        GUILayout.Label($"Move Speed: x{moveSpeedMultiplier:F2}");
        GUILayout.Label($"Jump Force: x{jumpForceMultiplier:F2}");
        GUILayout.Label($"Max Jump Count: {maxJumpCount}");
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        // 원래 값으로 복원
        if (vignette != null)
        {
            vignette.intensity.value = originalVignetteIntensity;
        }
        
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
    }

    private void Update()
    {
        healTimer += Time.deltaTime;
        if (currentHealth <= 0)
        {
            GameoverScreen.SetActive(true);
            Time.timeScale = 0;
            StopAllCoroutines();
            vignette.intensity.value = 0.1f;
        }
        else
        {
            if (healInterval <= healTimer)
            {
                healTimer = 0;
                if(currentHealth < maxHealth)
                    currentHealth += 1;
            }
        }
    }

    #endregion

    #region HEALTH METHODS

    public float GetCurrentHealth() => currentHealth;

    public float GetMaxHealth() => maxHealth;

    public float GetHealthRatio() => currentHealth / maxHealth;

    public bool IsAlive() => currentHealth > 0;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        if (showDebugInfo)
            Debug.Log($"Damage: {damage}, Health: {currentHealth}");
        
        // ★ 데미지 이펙트 재생
        PlayDamageEffect();
        
        if (currentHealth <= 0)
        {
            OnPlayerDeath();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        if (showDebugInfo)
            Debug.Log($"Heal: {amount}, Health: {currentHealth}");
    }

    public void FullHeal()
    {
        currentHealth = maxHealth;
        
        if (showDebugInfo)
            Debug.Log($"Full Heal! Health: {currentHealth}");
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player Death!");
    }

    #endregion

    #region DAMAGE EFFECTS

    private void PlayDamageEffect()
    {
        // 카메라 흔들기
        if (shaker != null && takeDamageShake != null)
        {
            shaker.Shake(takeDamageShake);
        }
        
        // 화면 효과
        if (damageEffectCoroutine != null)
        {
            StopCoroutine(damageEffectCoroutine);
        }
        
        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    private IEnumerator DamageEffectCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < damageEffectDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / damageEffectDuration;
            float curveValue = damageEffectCurve.Evaluate(normalizedTime);
            
            // Vignette 효과
            if (vignette != null)
            {
                vignette.intensity.value = originalVignetteIntensity + (maxVignetteIntensity * curveValue);
            }
            
            // Material 프로퍼티 애니메이션
            if (damageMaterial != null)
            {
                if (damageMaterial.HasProperty("_BlurOffset"))
                {
                    float blurOffset = Mathf.Lerp(originalBlurOffset, maxBlurOffset, curveValue);
                    damageMaterial.SetFloat("_BlurOffset", blurOffset);
                }
                
                if (damageMaterial.HasProperty("_Intensity"))
                {
                    float intensity = Mathf.Lerp(originalIntensity, maxIntensity, curveValue);
                    damageMaterial.SetFloat("_Intensity", intensity);
                }
            }
            
            yield return null;
        }
        
        // 원래 값으로 복원
        if (vignette != null)
            vignette.intensity.value = originalVignetteIntensity;
            
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
        
        damageEffectCoroutine = null;
    }

    #endregion

    #region COMBAT STATS METHODS

    public float GetAttackSpeedMultiplier() => attackSpeedMultiplier;

    public void SetAttackSpeedMultiplier(float multiplier)
    {
        attackSpeedMultiplier = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Attack Speed: x{attackSpeedMultiplier}");
    }

    public void AddAttackSpeed(float amount)
    {
        SetAttackSpeedMultiplier(attackSpeedMultiplier + amount);
    }

    #endregion

    #region PROJECTILE STATS METHODS
    
    public float GetProjectileDamage() => projectileDamage;
    
    public void SetProjectileDamage(float multiplier)
    {
        projectileDamage = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Projectile Damage: x{projectileDamage}");
    }
    
    public void AddProjectileDamage(float amount)
    {
        SetProjectileDamage(projectileDamage + amount);
    }
    
    public float GetProjectileSpeedMultiplier() => projectileSpeedMultiplier;

    public void SetProjectileSpeedMultiplier(float multiplier)
    {
        projectileSpeedMultiplier = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Projectile Speed: x{projectileSpeedMultiplier}");
    }

    public void AddProjectileSpeed(float amount)
    {
        SetProjectileSpeedMultiplier(projectileSpeedMultiplier + amount);
    }

    public float GetProjectileSizeMultiplier() => projectileSizeMultiplier;

    public void SetProjectileSizeMultiplier(float multiplier)
    {
        projectileSizeMultiplier = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Projectile Size: x{projectileSizeMultiplier}");
    }

    public void AddProjectileSize(float amount)
    {
        SetProjectileSizeMultiplier(projectileSizeMultiplier + amount);
    }

    #endregion

    #region MOVEMENT STATS METHODS

    public float GetMoveSpeedMultiplier() => moveSpeedMultiplier;

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Move Speed: x{moveSpeedMultiplier}");
    }

    public void AddMoveSpeed(float amount)
    {
        SetMoveSpeedMultiplier(moveSpeedMultiplier + amount);
    }

    public float GetJumpForceMultiplier() => jumpForceMultiplier;

    public void SetJumpForceMultiplier(float multiplier)
    {
        jumpForceMultiplier = multiplier;
        
        if (showDebugInfo)
            Debug.Log($"Jump Force: x{jumpForceMultiplier}");
    }

    public void AddJumpForce(float amount)
    {
        SetJumpForceMultiplier(jumpForceMultiplier + amount);
    }

    public int GetMaxJumpCount() => maxJumpCount;

    public void SetMaxJumpCount(int count)
    {
        maxJumpCount = count;
        
        if (showDebugInfo)
            Debug.Log($"Max Jump Count: {maxJumpCount}");
    }

    public void AddJumpCount(int amount)
    {
        SetMaxJumpCount(maxJumpCount + amount);
    }

    public void AddEnergy(int amount)
    {
        currentEnergy += amount;
    }
    
    public int GetEnergy()
    {
        return currentEnergy; // 에너지 필드
    }

    public void ConsumeEnergy(int amount)
    {
        currentEnergy -= amount;
    }

    #endregion

    #region UTILITY METHODS

    public void ResetAllStats()
    {
        currentHealth = maxHealth;
        attackSpeedMultiplier = 1.0f;
        projectileSpeedMultiplier = 1.0f;
        projectileSizeMultiplier = 1.0f;
        moveSpeedMultiplier = 1.0f;
        jumpForceMultiplier = 1.0f;
        maxJumpCount = 1;
        
        Debug.Log("All stats reset!");
    }

    #endregion
}