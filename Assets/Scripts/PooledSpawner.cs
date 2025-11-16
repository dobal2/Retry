using UnityEngine;
using System.Collections;
using TMPro;

public class PooledSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private string enemyPoolTag = "Enemy";
    [SerializeField] private float baseSpawnInterval = 3f;
    [SerializeField] private int baseMaxEnemies = 10;
    
    [Header("난이도 설정")]
    [SerializeField] private float difficultyIncreaseInterval = 60f;
    [SerializeField] private float baseSpawnIntervalDecrease = 0.1f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private int baseMaxEnemiesIncrease = 1;
    [SerializeField] private int absoluteMaxEnemies = 50;
    [SerializeField] private float baseHealthMultiplierIncrease = 0.05f;
    [SerializeField] private float baseDamageMultiplierIncrease = 0.025f;
    [SerializeField] private float difficultyScalingRate = 0.1f;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI difficultyText;
    
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
            }
        }
        
        UpdateDifficultyUI();
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
        
        UpdateDifficultyUI();
    }
    
    private void IncreaseDifficulty()
    {
        currentDifficultyLevel++;
        
        float scalingFactor = 1f + (currentDifficultyLevel * difficultyScalingRate);
        
        float spawnDecrease = baseSpawnIntervalDecrease * scalingFactor;
        currentSpawnInterval = Mathf.Max(minSpawnInterval, currentSpawnInterval - spawnDecrease);
        
        int enemyIncrease = Mathf.RoundToInt(baseMaxEnemiesIncrease * scalingFactor);
        currentMaxEnemies = Mathf.Min(absoluteMaxEnemies, currentMaxEnemies + enemyIncrease);
        
        float healthIncrease = baseHealthMultiplierIncrease * scalingFactor;
        currentHealthMultiplier += healthIncrease;
        
        float damageIncrease = baseDamageMultiplierIncrease * scalingFactor;
        currentDamageMultiplier += damageIncrease;
    }
    
    private void UpdateDifficultyUI()
    {
        if (difficultyText == null) return;
        
        string difficultyName;
        
        if (currentDifficultyLevel == 0)
            difficultyName = "Difficulty: Easy";
        else if (currentDifficultyLevel <= 2)
            difficultyName = "Difficulty: Normal";
        else if (currentDifficultyLevel <= 4)
            difficultyName = "Difficulty: Medium";
        else if (currentDifficultyLevel <= 6)
            difficultyName = "Difficulty: Hard";
        else if (currentDifficultyLevel <= 9)
            difficultyName = "Difficulty: Very Hard";
        else if (currentDifficultyLevel <= 12)
            difficultyName = "Difficulty: Insane";
        else
            difficultyName = "Difficulty: Gay";
        
        difficultyText.text = difficultyName;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void StartSpawning()
    {
        if (isSpawning) return;
        
        if (target == null) return;
        
        isSpawning = true;
        difficultyTimer = 0f;
        StartCoroutine(SpawnRoutine());
    }
    
    public void StopSpawning()
    {
        if (!isSpawning) return;
        
        isSpawning = false;
        StopAllCoroutines();
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
    }
    
    void SpawnEnemy()
    {
        if (ObjectPool.Instance == null)
        {
            StopSpawning();
            return;
        }
        
        if (target == null)
        {
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
    }
    
    public void ClearAllEnemies()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.DespawnAll(enemyPoolTag);
            currentActiveCount = 0;
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