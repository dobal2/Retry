using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 시간정지 상태를 관리하는 싱글톤 매니저
/// </summary>
public class TimeStopManager : MonoBehaviour
{
    public static TimeStopManager Instance { get; private set; }

    [Header("Time Stop State")]
    [SerializeField] private bool isTimeStopped = false;
    
    [Header("Affected Entities")]
    [SerializeField] private bool affectEnemies = true;
    [SerializeField] private bool affectProjectiles = true;
    [SerializeField] private bool affectEnvironment = false;
    [SerializeField] private bool affectForceFields = true; 

    public bool IsTimeStopped => isTimeStopped;
    public bool AffectEnemies => affectEnemies;
    public bool AffectProjectiles => affectProjectiles;
    public bool AffectEnvironment => affectEnvironment;
    public bool AffectForceFields => affectForceFields;

    // ★ ForceField 추적
    private List<ForceFieldAnimationController> registeredForceFields = new List<ForceFieldAnimationController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 시간 정지 활성화
    /// </summary>
    public void StartTimeStop()
    {
        if (isTimeStopped) return;
        
        isTimeStopped = true;
        Debug.Log("[TimeStop] ⏸ Time stopped!");
        
        // ★ 모든 ForceField에 알림
        NotifyForceFields();
    }

    /// <summary>
    /// 시간 정지 해제
    /// </summary>
    public void StopTimeStop()
    {
        if (!isTimeStopped) return;
        
        isTimeStopped = false;
        Debug.Log("[TimeStop] ▶ Time resumed!");
        
        // ★ 모든 ForceField에 알림
        NotifyForceFields();
    }

    /// <summary>
    /// 시간정지 토글
    /// </summary>
    public void ToggleTimeStop()
    {
        if (isTimeStopped)
        {
            StopTimeStop();
        }
        else
        {
            StartTimeStop();
        }
    }

    /// <summary>
    /// 특정 엔티티가 시간정지 영향을 받는지 확인
    /// </summary>
    public bool ShouldBeFrozen(EntityType entityType)
    {
        if (!isTimeStopped) return false;

        return entityType switch
        {
            EntityType.Enemy => affectEnemies,
            EntityType.Projectile => affectProjectiles,
            EntityType.Environment => affectEnvironment,
            EntityType.ForceField => affectForceFields,
            EntityType.Player => false,
            _ => false
        };
    }
    
    /// <summary>
    /// ForceField 등록 (옵션)
    /// </summary>
    public void RegisterForceField(ForceFieldAnimationController forceField)
    {
        if (!registeredForceFields.Contains(forceField))
        {
            registeredForceFields.Add(forceField);
            Debug.Log($"[TimeStop] ForceField registered: {forceField.name}");
        }
    }
    
    /// <summary>
    /// ForceField 등록 해제 (옵션)
    /// </summary>
    public void UnregisterForceField(ForceFieldAnimationController forceField)
    {
        registeredForceFields.Remove(forceField);
    }
    
    /// <summary>
    /// 모든 ForceField에 상태 변경 알림 (옵션)
    /// </summary>
    private void NotifyForceFields()
    {
        foreach (var forceField in registeredForceFields)
        {
            if (forceField != null)
            {
                // 필요시 추가 로직
            }
        }
    }

    // ★ 디버그용 키 입력
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTimeStop();
        }
    }
}

/// <summary>
/// 엔티티 타입 정의
/// </summary>
public enum EntityType
{
    Player,
    Enemy,
    Projectile,
    Environment,
    ForceField // ★ 추가
}