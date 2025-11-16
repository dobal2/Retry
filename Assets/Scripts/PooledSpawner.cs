using UnityEngine;
using System.Collections;

public class PooledSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private string enemyPoolTag = "Enemy";
    [SerializeField] private float baseSpawnInterval = 3f;
    [SerializeField] private int baseMaxEnemies = 10;
    
    [Header("난이도 설정")]
    [SerializeField] private float difficultyIncreaseInterval = 60f;
    [SerializeField] private float spawnIntervalDecrease = 0.2f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private int maxEnemiesIncrease = 2;
    [SerializeField] private int absoluteMaxEnemies = 50;
    [SerializeField] private float enemyHealthMultiplierIncrease = 0.1f;
    [SerializeField] private float enemyDamageMultiplierIncrease = 0.05f;
    
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
    
    private int currentDifficultyLevel = 0;
    private float currentSpawnInterval;
    private int currentMaxEnemies;
    private float currentHealthMultiplier = 1f;
    private float currentDamageMultiplier = 1f;
    private float difficultyTimer = 0f;
    
    void Start()
    {
        currentSpawnInterval = baseSpawnInterval;
        currentMaxEnemies = baseMaxEnemies;
        
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
                Debug.LogWarning("[PooledSpawner] 플레이어를 찾을 수 없습니다.");
            }
        }
        
        Debug.Log("[PooledSpawner] 준비 완료. GameManager의 신호 대기 중...");
    }
    
    void Update()
    {
        if (!isSpawning) return;
        
        difficultyTimer += Time.deltaTime;
        
        if (difficultyTimer >= difficultyIncreaseInterval)
        {
            IncreaseDifficulty();
            difficultyTimer = 0f;
        }
    }
    
    private void IncreaseDifficulty()
    {
        currentDifficultyLevel++;
        
        currentSpawnInterval = Mathf.Max(minSpawnInterval, currentSpawnInterval - spawnIntervalDecrease);
        currentMaxEnemies = Mathf.Min(absoluteMaxEnemies, currentMaxEnemies + maxEnemiesIncrease);
        currentHealthMultiplier += enemyHealthMultiplierIncrease;
        currentDamageMultiplier += enemyDamageMultiplierIncrease;
        
        Debug.Log($"[PooledSpawner] 난이도 증가! Level {currentDifficultyLevel}");
        Debug.Log($"  - 스폰 간격: {currentSpawnInterval:F1}s");
        Debug.Log($"  - 최대 적 수: {currentMaxEnemies}");
        Debug.Log($"  - 체력 배율: x{currentHealthMultiplier:F1}");
        Debug.Log($"  - 데미지 배율: x{currentDamageMultiplier:F1}");
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"[PooledSpawner] 타겟 설정됨: {newTarget.name}");
    }
    
    public void StartSpawning()
    {
        if (isSpawning)
        {
            Debug.LogWarning("[PooledSpawner] 이미 스폰 중입니다!");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError("[PooledSpawner] 타겟이 설정되지 않았습니다!");
            return;
        }
        
        isSpawning = true;
        difficultyTimer = 0f;
        Debug.Log("[PooledSpawner] ✓ 적 스폰 시작!");
        StartCoroutine(SpawnRoutine());
    }
    
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
            if (currentActiveCount < currentMaxEnemies)
            {
                SpawnEnemy();
            }
            
            yield return new WaitForSeconds(currentSpawnInterval);
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
            Debug.LogWarning("[PooledSpawner] 타겟이 없습니다!");
            StopSpawning();
            return;
        }
        
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        GameObject enemy = ObjectPool.Instance.Spawn(enemyPoolTag, spawnPos, Quaternion.identity);
        
        if (enemy != null)
        {
            currentActiveCount++;
            
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetDifficultyMultipliers(currentHealthMultiplier, currentDamageMultiplier);
            }
            
            EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
            if (notifier == null)
            {
                notifier = enemy.AddComponent<EnemyDeathNotifier>();
            }
            notifier.OnDeath = () => OnEnemyDied();
            
            if (currentActiveCount <= 5)
            {
                Debug.Log($"[PooledSpawner] 적 #{currentActiveCount} 스폰 (Level {currentDifficultyLevel})");
            }
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
            
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                
                if (slopeAngle < 45f)
                {
                    Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                    return spawnPos;
                }
            }
        }
        
        return target.position + Vector3.up * spawnHeightOffset + Random.insideUnitSphere * 3f;
    }
    
    void OnEnemyDied()
    {
        currentActiveCount = Mathf.Max(0, currentActiveCount - 1);
    }
    
    public int GetActiveEnemyCount() => currentActiveCount;
    public bool IsSpawning() => isSpawning;
    public int GetDifficultyLevel() => currentDifficultyLevel;
    public float GetCurrentSpawnInterval() => currentSpawnInterval;
    public int GetCurrentMaxEnemies() => currentMaxEnemies;
    public float GetHealthMultiplier() => currentHealthMultiplier;
    public float GetDamageMultiplier() => currentDamageMultiplier;
    
    public void ResetDifficulty()
    {
        currentDifficultyLevel = 0;
        currentSpawnInterval = baseSpawnInterval;
        currentMaxEnemies = baseMaxEnemies;
        currentHealthMultiplier = 1f;
        currentDamageMultiplier = 1f;
        difficultyTimer = 0f;
        
        Debug.Log("[PooledSpawner] 난이도 초기화됨");
    }
    
    public void ClearAllEnemies()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.DespawnAll(enemyPoolTag);
            currentActiveCount = 0;
            Debug.Log("[PooledSpawner] 모든 적 제거됨");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        DrawCircle(target.position, minDistance, 32);
        
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        DrawCircle(target.position, maxDistance, 32);
        
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