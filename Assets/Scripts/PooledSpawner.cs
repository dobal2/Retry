using UnityEngine;
using System.Collections;

/// <summary>
/// 오브젝트 풀을 사용하는 적 스포너
/// </summary>
public class PooledSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private string enemyPoolTag = "Enemy";
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxActiveEnemies = 10;
    [SerializeField] private float delayAfterTerrainLoad = 5f; // 지형 로딩 후 대기 시간
    
    [Header("스폰 범위")]
    [SerializeField] private float minDistance = 10f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float spawnHeightOffset = 1f;
    
    [Header("타겟")]
    [SerializeField] private Transform target;
    
    [Header("지형 감지")]
    [SerializeField] private float raycastHeight = 100f;
    [SerializeField] private float raycastDistance = 200f;
    [SerializeField] private LayerMask groundLayer = -1;
    
    [Header("물 설정")]
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private bool avoidWater = true;
    
    private int currentActiveCount = 0;
    private bool isSpawning = false;
    private bool isTerrainReady = false;
    
    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        
        // ★ GameManager의 지형 생성 완료 이벤트 대기
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTerrainReady += OnTerrainGenerationComplete;
        }
        else
        {
            Debug.LogWarning("[PooledSpawner] GameManager를 찾을 수 없습니다. 5초 후 자동 시작합니다.");
            StartCoroutine(DelayedStart(5f));
        }
    }
    
    /// <summary>
    /// 지형 생성 완료 시 호출됨
    /// </summary>
    private void OnTerrainGenerationComplete()
    {
        if (!isTerrainReady)
        {
            isTerrainReady = true;
            Debug.Log($"[PooledSpawner] 지형 생성 완료! {delayAfterTerrainLoad}초 후 스폰 시작...");
            StartCoroutine(DelayedStart(delayAfterTerrainLoad));
        }
    }
    
    /// <summary>
    /// 지정된 시간 후 스폰 시작
    /// </summary>
    IEnumerator DelayedStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartSpawning();
    }
    
    /// <summary>
    /// 외부에서 타겟(플레이어) 설정
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"[PooledSpawner] 타겟 설정됨: {newTarget.name}");
    }
    
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            Debug.Log("[PooledSpawner] 적 스폰 시작!");
            StartCoroutine(SpawnRoutine());
        }
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
        Debug.Log("[PooledSpawner] 적 스폰 중지!");
    }
    
    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            if (currentActiveCount < maxActiveEnemies)
            {
                SpawnEnemy();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    void SpawnEnemy()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("[PooledSpawner] ObjectPool이 씬에 없습니다!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[PooledSpawner] 타겟이 설정되지 않았습니다!");
            return;
        }
        
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        GameObject enemy = ObjectPool.Instance.Spawn(enemyPoolTag, spawnPos, Quaternion.identity);
        
        if (enemy != null)
        {
            currentActiveCount++;
            
            EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
            if (notifier == null)
            {
                notifier = enemy.AddComponent<EnemyDeathNotifier>();
            }
            notifier.OnDeath = () => OnEnemyDied();
            
            Debug.Log($"[PooledSpawner] 적 스폰: {spawnPos} (활성: {currentActiveCount}/{maxActiveEnemies})");
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = 10;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistance, maxDistance);
            
            Vector3 randomPos = target.position + new Vector3(
                Mathf.Cos(angle) * distance,
                raycastHeight,
                Mathf.Sin(angle) * distance
            );
            
            RaycastHit hit;
            if (Physics.Raycast(randomPos, Vector3.down, out hit, raycastDistance, groundLayer))
            {
                if (avoidWater && hit.point.y < waterLevel)
                {
                    Debug.DrawRay(randomPos, Vector3.down * raycastDistance, Color.blue, 2f);
                    continue;
                }
                
                Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                
                Debug.DrawLine(randomPos, hit.point, Color.green, 2f);
                return spawnPos;
            }
            else
            {
                Debug.DrawRay(randomPos, Vector3.down * raycastDistance, Color.red, 2f);
            }
        }
        
        Debug.LogWarning("[PooledSpawner] 적합한 스폰 위치를 찾지 못했습니다. 타겟 위치 사용");
        float safeHeight = Mathf.Max(waterLevel + 2f, spawnHeightOffset);
        return target.position + Vector3.up * safeHeight;
    }
    
    void OnEnemyDied()
    {
        currentActiveCount--;
        Debug.Log($"[PooledSpawner] 적 사망 (남은 적: {currentActiveCount})");
    }
    
    void OnDestroy()
    {
        // ★ 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTerrainReady -= OnTerrainGenerationComplete;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        Gizmos.color = Color.yellow;
        DrawCircle(target.position, minDistance);
        
        Gizmos.color = Color.red;
        DrawCircle(target.position, maxDistance);
        
        if (avoidWater)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Vector3 waterCenter = target.position;
            waterCenter.y = waterLevel;
            
            Gizmos.DrawWireSphere(waterCenter, maxDistance);
            
            Gizmos.color = new Color(0, 0.5f, 1f, 0.1f);
            Gizmos.DrawCube(waterCenter, new Vector3(maxDistance * 2, 0.1f, maxDistance * 2));
        }
    }
    
    void DrawCircle(Vector3 center, float radius)
    {
        int segments = 36;
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}

/// <summary>
/// 적이 죽었을 때 스포너에 알림을 보내는 헬퍼 컴포넌트
/// </summary>
public class EnemyDeathNotifier : MonoBehaviour
{
    public System.Action OnDeath;
    
    void OnDisable()
    {
        if (OnDeath != null)
        {
            OnDeath.Invoke();
            OnDeath = null;
        }
    }
}