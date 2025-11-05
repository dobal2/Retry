using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OptimizedTerrainGenerator terrainGenerator;
    [SerializeField] private PooledSpawner enemySpawner;
    
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float playerSpawnHeight = 50f;
    [SerializeField] private Vector3 playerSpawnOffset = Vector3.zero;
    
    [Header("Spawn Delay")]
    [SerializeField] private float playerSpawnDelay = 0.5f;
    [SerializeField] private float enemySpawnDelay = 5f;
    
    private GameObject spawnedPlayer;
    private bool isInitialized = false;
    
    public static GameManager Instance { get; private set; }
    public event System.Action OnTerrainReady;
    
    void Awake()
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
    
    void Start()
    {
        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<OptimizedTerrainGenerator>();
        }
        
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<PooledSpawner>();
        }
        
        SpawnPlayerAtHighPosition();
    }
    
    public void OnTerrainGenerationComplete()
    {
        OnTerrainReady?.Invoke();
        StartCoroutine(ActivatePlayerAfterLoading());
    }
    
    IEnumerator ActivatePlayerAfterLoading()
    {
        // ★ IsLoading 프로퍼티로 변경
        while (LoadingUI.Instance != null && LoadingUI.Instance.IsLoading)
        {
            yield return new WaitForSeconds(0.1f);
        }
    
        yield return new WaitForSeconds(playerSpawnDelay);
    
        Debug.Log("Teleporting player to ground...");
        TeleportPlayerToGround();
    
        Debug.Log("Activating player physics...");
        ActivatePlayer();
    
        yield return new WaitForSeconds(enemySpawnDelay);
    
        Debug.Log("Starting enemy spawning...");
        StartEnemySpawning();
        isInitialized = true;
        Debug.Log("Game fully initialized!");
    }
    
    void SpawnPlayerAtHighPosition()
    {
        if (playerPrefab == null) return;
        
        Vector3 spawnPosition = new Vector3(0, playerSpawnHeight, 0) + playerSpawnOffset;
        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        spawnedPlayer.name = "Player";
        
        if (!spawnedPlayer.CompareTag("Player"))
        {
            spawnedPlayer.tag = "Player";
        }
        
        Rigidbody rb = spawnedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    void TeleportPlayerToGround()
    {
        if (spawnedPlayer == null) return;
        
        Vector3 targetPosition = Vector3.zero + playerSpawnOffset;
        Vector3 rayStart = targetPosition + Vector3.up * playerSpawnHeight;
        
        RaycastHit hit;
        Vector3 groundPosition;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, playerSpawnHeight * 2f))
        {
            groundPosition = hit.point + Vector3.up * 2f;
        }
        else
        {
            groundPosition = targetPosition + Vector3.up * 5f;
        }
        
        spawnedPlayer.transform.position = groundPosition;
        
        if (enemySpawner != null)
        {
            enemySpawner.SetTarget(spawnedPlayer.transform);
        }
    }
    
    void ActivatePlayer()
    {
        if (spawnedPlayer == null) return;
        
        Rigidbody rb = spawnedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.mass = 1f;
        }
    }
    
    void StartEnemySpawning()
    {
        if (enemySpawner == null) return;
        enemySpawner.StartSpawning();
    }
    
    public void AlignPlayerToGround()
    {
        if (spawnedPlayer == null) return;
        
        Vector3 rayStart = spawnedPlayer.transform.position + Vector3.up * 100f;
        RaycastHit hit;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f))
        {
            spawnedPlayer.transform.position = hit.point + Vector3.up * 2f;
        }
    }
    
    public void RestartGame()
    {
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
        }
        
        isInitialized = false;
        SpawnPlayerAtHighPosition();
    }
    
    void OnDrawGizmos()
    {
        Vector3 targetPos = Vector3.zero + playerSpawnOffset;
        Vector3 rayStart = targetPos + Vector3.up * playerSpawnHeight;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayStart, 1f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * playerSpawnHeight * 2f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 2f);
        Gizmos.DrawWireCube(targetPos, new Vector3(4f, 0.1f, 4f));
    }
}