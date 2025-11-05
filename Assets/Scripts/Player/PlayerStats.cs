// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

/// <summary>
/// 플레이어의 모든 스탯을 총괄하는 스크립트
/// </summary>
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

    [Header("Health Stats")]
    [Tooltip("플레이어 최대 체력")]
    [SerializeField]
    private float maxHealth = 100f;
    
    [Tooltip("플레이어 현재 체력")]
    [SerializeField]
    private float currentHealth = 100f;

    [Header("Combat Stats")]
    [Tooltip("공격속도 배율 (1.0 = 기본, 2.0 = 2배 빠름)")]
    [SerializeField]
    private float attackSpeedMultiplier = 1.0f;

    [Header("Projectile Stats")]
    [Tooltip("프로젝타일 속도 배율 (1.0 = 기본)")]
    [SerializeField]
    private float projectileSpeedMultiplier = 1.0f;
    
    [Tooltip("프로젝타일 크기 배율 (1.0 = 기본)")]
    [SerializeField]
    private float projectileSizeMultiplier = 1.0f;

    [Header("Movement Stats")]
    [Tooltip("이동속도 배율 (1.0 = 기본)")]
    [SerializeField]
    private float moveSpeedMultiplier = 1.0f;
    
    [Tooltip("점프력 배율 (1.0 = 기본)")]
    [SerializeField]
    private float jumpForceMultiplier = 1.0f;
    
    [Tooltip("점프 가능 횟수 (1 = 1단 점프, 2 = 2단 점프)")]
    [SerializeField]
    private int maxJumpCount = 1;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugInfo = false;

    #endregion

    #region UNITY

    private void Awake()
    {
        // 싱글톤 중복 체크
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 현재 체력을 최대 체력으로 초기화
        currentHealth = maxHealth;
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("=== Player Stats ===");
        GUILayout.Label($"HP: {currentHealth:F0} / {maxHealth:F0}");
        GUILayout.Label($"공격속도: x{attackSpeedMultiplier:F2}");
        GUILayout.Label($"프로젝타일 속도: x{projectileSpeedMultiplier:F2}");
        GUILayout.Label($"프로젝타일 크기: x{projectileSizeMultiplier:F2}");
        GUILayout.Label($"이동속도: x{moveSpeedMultiplier:F2}");
        GUILayout.Label($"점프력: x{jumpForceMultiplier:F2}");
        GUILayout.Label($"최대 점프 횟수: {maxJumpCount}");
        GUILayout.EndArea();
    }

    #endregion

    #region HEALTH METHODS

    /// <summary>
    /// 현재 체력 가져오기
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// 최대 체력 가져오기
    /// </summary>
    public float GetMaxHealth() => maxHealth;

    /// <summary>
    /// 체력 비율 가져오기 (0.0 ~ 1.0)
    /// </summary>
    public float GetHealthRatio() => currentHealth / maxHealth;

    /// <summary>
    /// 플레이어가 살아있는지 확인
    /// </summary>
    public bool IsAlive() => currentHealth > 0;

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (showDebugInfo)
            Debug.Log($"<color=red>[PlayerStats] 데미지: {damage}, 남은 체력: {currentHealth}</color>");
        
        if (currentHealth <= 0)
        {
            OnPlayerDeath();
        }
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlayerStats] 회복: {amount}, 현재 체력: {currentHealth}</color>");
    }

    /// <summary>
    /// 체력 완전 회복
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlayerStats] 완전 회복! 체력: {currentHealth}</color>");
    }

    /// <summary>
    /// 최대 체력 설정
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    /// <summary>
    /// 플레이어 사망 처리
    /// </summary>
    private void OnPlayerDeath()
    {
        Debug.Log("<color=red>[PlayerStats] 플레이어 사망!</color>");
        // 여기에 사망 처리 로직 추가 (예: 리스폰, 게임오버 등)
    }

    #endregion

    #region COMBAT STATS METHODS

    /// <summary>
    /// 공격속도 배율 가져오기
    /// </summary>
    public float GetAttackSpeedMultiplier() => attackSpeedMultiplier;

    /// <summary>
    /// 공격속도 배율 설정
    /// </summary>
    public void SetAttackSpeedMultiplier(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        
        if (showDebugInfo)
            Debug.Log($"<color=yellow>[PlayerStats] 공격속도 변경: x{attackSpeedMultiplier}</color>");
    }

    /// <summary>
    /// 공격속도 증가 (기존 값에 더하기)
    /// </summary>
    public void AddAttackSpeed(float amount)
    {
        SetAttackSpeedMultiplier(attackSpeedMultiplier + amount);
    }

    #endregion

    #region PROJECTILE STATS METHODS

    /// <summary>
    /// 프로젝타일 속도 배율 가져오기
    /// </summary>
    public float GetProjectileSpeedMultiplier() => projectileSpeedMultiplier;

    /// <summary>
    /// 프로젝타일 속도 배율 설정
    /// </summary>
    public void SetProjectileSpeedMultiplier(float multiplier)
    {
        projectileSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlayerStats] 프로젝타일 속도 변경: x{projectileSpeedMultiplier}</color>");
    }

    /// <summary>
    /// 프로젝타일 속도 증가
    /// </summary>
    public void AddProjectileSpeed(float amount)
    {
        SetProjectileSpeedMultiplier(projectileSpeedMultiplier + amount);
    }

    /// <summary>
    /// 프로젝타일 크기 배율 가져오기
    /// </summary>
    public float GetProjectileSizeMultiplier() => projectileSizeMultiplier;

    /// <summary>
    /// 프로젝타일 크기 배율 설정
    /// </summary>
    public void SetProjectileSizeMultiplier(float multiplier)
    {
        projectileSizeMultiplier = Mathf.Max(0.1f, multiplier);
        
        if (showDebugInfo)
            Debug.Log($"<color=cyan>[PlayerStats] 프로젝타일 크기 변경: x{projectileSizeMultiplier}</color>");
    }

    /// <summary>
    /// 프로젝타일 크기 증가
    /// </summary>
    public void AddProjectileSize(float amount)
    {
        SetProjectileSizeMultiplier(projectileSizeMultiplier + amount);
    }

    #endregion

    #region MOVEMENT STATS METHODS

    /// <summary>
    /// 이동속도 배율 가져오기
    /// </summary>
    public float GetMoveSpeedMultiplier() => moveSpeedMultiplier;

    /// <summary>
    /// 이동속도 배율 설정
    /// </summary>
    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlayerStats] 이동속도 변경: x{moveSpeedMultiplier}</color>");
    }

    /// <summary>
    /// 이동속도 증가
    /// </summary>
    public void AddMoveSpeed(float amount)
    {
        SetMoveSpeedMultiplier(moveSpeedMultiplier + amount);
    }

    /// <summary>
    /// 점프력 배율 가져오기
    /// </summary>
    public float GetJumpForceMultiplier() => jumpForceMultiplier;

    /// <summary>
    /// 점프력 배율 설정
    /// </summary>
    public void SetJumpForceMultiplier(float multiplier)
    {
        jumpForceMultiplier = Mathf.Max(0.1f, multiplier);
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlayerStats] 점프력 변경: x{jumpForceMultiplier}</color>");
    }

    /// <summary>
    /// 점프력 증가
    /// </summary>
    public void AddJumpForce(float amount)
    {
        SetJumpForceMultiplier(jumpForceMultiplier + amount);
    }

    /// <summary>
    /// 최대 점프 횟수 가져오기
    /// </summary>
    public int GetMaxJumpCount() => maxJumpCount;

    /// <summary>
    /// 최대 점프 횟수 설정
    /// </summary>
    public void SetMaxJumpCount(int count)
    {
        maxJumpCount = Mathf.Max(1, count);
        
        if (showDebugInfo)
            Debug.Log($"<color=green>[PlayerStats] 최대 점프 횟수 변경: {maxJumpCount}</color>");
    }

    /// <summary>
    /// 점프 횟수 증가
    /// </summary>
    public void AddJumpCount(int amount)
    {
        SetMaxJumpCount(maxJumpCount + amount);
    }

    #endregion

    #region UTILITY METHODS

    /// <summary>
    /// 모든 스탯 초기화
    /// </summary>
    public void ResetAllStats()
    {
        currentHealth = maxHealth;
        attackSpeedMultiplier = 1.0f;
        projectileSpeedMultiplier = 1.0f;
        projectileSizeMultiplier = 1.0f;
        moveSpeedMultiplier = 1.0f;
        jumpForceMultiplier = 1.0f;
        maxJumpCount = 1;
        
        Debug.Log("<color=white>[PlayerStats] 모든 스탯 초기화 완료!</color>");
    }

    #endregion
}