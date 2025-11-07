using UnityEngine;

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

    [Header("Debug")]
    [SerializeField]
    private bool showDebugInfo = false;

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
        DontDestroyOnLoad(gameObject);
        
        currentHealth = maxHealth;
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