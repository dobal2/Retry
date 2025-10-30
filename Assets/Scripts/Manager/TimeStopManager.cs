using UnityEngine;

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

    public bool IsTimeStopped => isTimeStopped;
    public bool AffectEnemies => affectEnemies;
    public bool AffectProjectiles => affectProjectiles;
    public bool AffectEnvironment => affectEnvironment;

    private void Awake()
    {
        // 싱글톤 패턴
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
        isTimeStopped = true;
        Debug.Log("[TimeStop] Time stopped!");
    }

    /// <summary>
    /// 시간 정지 해제
    /// </summary>
    public void StopTimeStop()
    {
        isTimeStopped = false;
        Debug.Log("[TimeStop] Time resumed!");
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
            EntityType.Player => false, // 플레이어는 항상 움직임
            _ => false
        };
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
    Environment
}