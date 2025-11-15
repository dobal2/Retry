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
    
    private int currentActiveCount = 0;
    private bool isSpawning = false;
    
    void Start()
    {
        // ★ 타겟 자동 찾기 (GameManager에서 SetTarget으로 설정하는 것이 더 안전)
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"[PooledSpawner] 타겟 자동 발견: {player.name}");
            }
            else
            {
                Debug.LogWarning("[PooledSpawner] 플레이어를 찾을 수 없습니다. SetTarget()으로 수동 설정이 필요합니다.");
            }
        }
        
        // ★ GameManager에서 StartSpawning()을 호출할 때까지 대기
        Debug.Log("[PooledSpawner] 준비 완료. GameManager의 신호 대기 중...");
    }
    
    /// <summary>
    /// 외부에서 타겟(플레이어) 설정
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"[PooledSpawner] 타겟 설정됨: {newTarget.name}");
    }
    
    /// <summary>
    /// 적 스폰 시작 (GameManager에서 호출)
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning)
        {
            Debug.LogWarning("[PooledSpawner] 이미 스폰 중입니다!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError("[PooledSpawner] 타겟이 설정되지 않았습니다! 스폰을 시작할 수 없습니다.");
            return;
        }
        
        isSpawning = true;
        Debug.Log("[PooledSpawner] ✓ 적 스폰 시작!");
        StartCoroutine(SpawnRoutine());
    }
    
    /// <summary>
    /// 적 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        if (!isSpawning)
        {
            Debug.LogWarning("[PooledSpawner] 스폰이 시작되지 않았습니다!");
            return;
        }
        
        isSpawning = false;
        StopAllCoroutines();
        Debug.Log("[PooledSpawner] ✓ 적 스폰 중지!");
    }
    
    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            if (currentActiveCount < maxActiveEnemies)
            {
                SpawnEnemy();
            }
            else
            {
                // 최대 수에 도달하면 로그 (너무 많이 출력되지 않도록 조건부)
                if (Random.value < 0.1f) // 10% 확률로만 출력
                {
                    Debug.Log($"[PooledSpawner] 최대 적 수 도달: {currentActiveCount}/{maxActiveEnemies}");
                }
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
        
        Debug.Log("[PooledSpawner] 스폰 루틴 종료");
    }
    
    void SpawnEnemy()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("[PooledSpawner] ObjectPool이 씬에 없습니다!");
            StopSpawning();
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("[PooledSpawner] 타겟이 없습니다! 스폰 중지...");
            StopSpawning();
            return;
        }
        
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        GameObject enemy = ObjectPool.Instance.Spawn(enemyPoolTag, spawnPos, Quaternion.identity);
        
        if (enemy != null)
        {
            currentActiveCount++;
            
            // EnemyDeathNotifier 설정
            EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
            if (notifier == null)
            {
                notifier = enemy.AddComponent<EnemyDeathNotifier>();
            }
            notifier.OnDeath = () => OnEnemyDied();
            
            // 첫 5마리만 로그 출력
            if (currentActiveCount <= 5)
            {
                Debug.Log($"[PooledSpawner] 적 #{currentActiveCount} 스폰: {spawnPos} (활성: {currentActiveCount}/{maxActiveEnemies})");
            }
        }
        else
        {
            Debug.LogWarning($"[PooledSpawner] 적 스폰 실패! 풀에 '{enemyPoolTag}' 태그의 오브젝트가 있는지 확인하세요.");
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = 10;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // 타겟 주변 원형으로 랜덤 위치 생성
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistance, maxDistance);
            
            Vector3 randomPos = target.position + new Vector3(
                Mathf.Cos(angle) * distance,
                raycastHeight,
                Mathf.Sin(angle) * distance
            );
            
            // 지형에 레이캐스트
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // 경사도 체크 (너무 가파른 곳 제외)
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                
                if (slopeAngle < 45f) // 45도 이하만 허용
                {
                    Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                    
                    // 디버그 라인 (녹색 = 성공)
                    Debug.DrawLine(randomPos, hit.point, Color.green, 2f);
                    return spawnPos;
                }
                else
                {
                    // 너무 가파름 (노란색)
                    Debug.DrawLine(randomPos, hit.point, Color.yellow, 2f);
                }
            }
            else
            {
                // 땅을 찾지 못함 (빨간색)
                Debug.DrawRay(randomPos, Vector3.down * raycastDistance, Color.red, 2f);
            }
        }
        
        // 모든 시도 실패 시 타겟 근처로 폴백
        Debug.LogWarning("[PooledSpawner] 적절한 스폰 위치를 찾지 못했습니다. 타겟 근처에 스폰합니다.");
        return target.position + Vector3.up * spawnHeightOffset + Random.insideUnitSphere * 3f;
    }
    
    void OnEnemyDied()
    {
        currentActiveCount = Mathf.Max(0, currentActiveCount - 1);
        
        // 처음 몇 마리만 로그 출력
        if (currentActiveCount < 5)
        {
            Debug.Log($"[PooledSpawner] 적 사망 (남은 적: {currentActiveCount}/{maxActiveEnemies})");
        }
    }
    
    /// <summary>
    /// 현재 활성화된 적의 수
    /// </summary>
    public int GetActiveEnemyCount()
    {
        return currentActiveCount;
    }
    
    /// <summary>
    /// 스폰 중인지 확인
    /// </summary>
    public bool IsSpawning()
    {
        return isSpawning;
    }
    
    /// <summary>
    /// 모든 적 제거
    /// </summary>
    public void ClearAllEnemies()
    {
        if (ObjectPool.Instance != null)
        {
            // 풀의 모든 적 비활성화
            ObjectPool.Instance.DespawnAll(enemyPoolTag);
            currentActiveCount = 0;
            Debug.Log("[PooledSpawner] 모든 적 제거됨");
        }
    }
    
    // ★ Gizmos로 스폰 범위 표시
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // 최소 거리 (녹색)
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        DrawCircle(target.position, minDistance, 32);
        
        // 최대 거리 (노란색)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        DrawCircle(target.position, maxDistance, 32);
        
        // 타겟 위치
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, 1f);
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
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