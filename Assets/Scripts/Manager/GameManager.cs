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
    [SerializeField] private float spawnRadiusMin = 5f;
    [SerializeField] private float spawnRadiusMax = 20f;
    [SerializeField] private int maxSpawnAttempts = 10;
    
    [Header("Spawn Delay")]
    [SerializeField] private float enemySpawnDelay = 3f;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private bool autoDisableSceneCamera = true;
    [SerializeField] private string firstPersonViewLayer = "FirstPersonView"; // ★ FPV 레이어 이름

    [Header("UI Settings")]
    [SerializeField] private Canvas anchorBoxCanvas;
    
    private GameObject spawnedPlayer;
    private Camera playerCamera;
    private bool isInitialized = false;
    private bool isPlayerOnGround = false;
    private bool isPlayerControlEnabled = false;
    private int firstPersonViewLayerMask; // ★ FPV 레이어 마스크
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        Application.targetFrameRate = -1;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }
        
        // ★ FirstPersonView 레이어 마스크 계산
        int layerIndex = LayerMask.NameToLayer(firstPersonViewLayer);
        if (layerIndex == -1)
        {
            Debug.LogError($"[GameManager] '{firstPersonViewLayer}' 레이어가 존재하지 않습니다! Tags & Layers에서 생성하세요.");
        }
        else
        {
            firstPersonViewLayerMask = 1 << layerIndex;
            Debug.Log($"[GameManager] FirstPersonView Layer: {layerIndex}, Mask: {firstPersonViewLayerMask}");
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
        SetupLoadingUICamera();
    }
    
    void SetupLoadingUICamera()
    {
        if (playerCamera == null)
        {
            Debug.LogError("[GameManager] Player camera not found!");
            return;
        }
        
        // ★ FirstPersonView 레이어 숨기기
        HideFirstPersonView();
        
        if (LoadingUI.Instance != null)
        {
            Canvas loadingCanvas = LoadingUI.Instance.GetComponent<Canvas>();
            
            if (loadingCanvas != null)
            {
                loadingCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                loadingCanvas.worldCamera = playerCamera;
                loadingCanvas.planeDistance = 1f;
                
                Debug.Log("[GameManager] ✓ Loading Canvas assigned to player camera");
            }
        }
        
        if (sceneCamera != null && autoDisableSceneCamera)
        {
            sceneCamera.gameObject.SetActive(false);
            Debug.Log("[GameManager] Scene camera disabled");
        }
    }
    
    // ★ FirstPersonView 숨기기 (로딩 중)
    void HideFirstPersonView()
    {
        if (playerCamera == null) return;
        
        if (firstPersonViewLayerMask != 0)
        {
            // Culling Mask에서 FirstPersonView 레이어 제거
            playerCamera.cullingMask &= ~firstPersonViewLayerMask;
            Debug.Log($"[GameManager] ✓ FirstPersonView 레이어 숨김 (Culling Mask: {playerCamera.cullingMask})");
        }
    }
    
    // ★ FirstPersonView 보이기 (로딩 완료)
    void ShowFirstPersonView()
    {
        if (playerCamera == null) return;
        
        if (firstPersonViewLayerMask != 0)
        {
            // Culling Mask에 FirstPersonView 레이어 추가
            playerCamera.cullingMask |= firstPersonViewLayerMask;
            Debug.Log($"[GameManager] ✓ FirstPersonView 레이어 표시 (Culling Mask: {playerCamera.cullingMask})");
        }
    }
    
    public void OnTerrainReady()
    {
        if (isPlayerOnGround)
        {
            Debug.LogWarning("[GameManager] Player already on ground!");
            return;
        }
        
        Debug.Log("[GameManager] === TERRAIN READY - Teleporting player ===");
        TeleportPlayerToGround();
        isPlayerOnGround = true;
        
        AssignCanvasesToPlayerCamera();
    }
    
    public void OnAlmostReady()
    {
        if (isPlayerControlEnabled)
        {
            Debug.LogWarning("[GameManager] Player control already enabled!");
            return;
        }
    
        Debug.Log("[GameManager] === ALMOST READY - Enabling player control ===");
    
        // ★ ShowFirstPersonView() 제거 - 로딩 완료 후로 이동
    
        EnablePlayerControl();
        isPlayerControlEnabled = true;
    }

    public void OnTerrainGenerationComplete()
    {
        Debug.Log("[GameManager] === LOADING COMPLETE ===");
        StartCoroutine(FinalizeGameStart());
    }

    IEnumerator FinalizeGameStart()
    {
        // ★ 로딩 UI가 완전히 숨겨질 때까지 대기
        while (LoadingUI.Instance != null && LoadingUI.Instance.IsLoading)
        {
            yield return new WaitForSeconds(0.1f);
        }
    
        // ★ 로딩 UI가 숨겨진 직후 추가 대기 (UI 애니메이션 완료 대기)
        yield return new WaitForSeconds(0.3f);
    
        // ★ FirstPersonView 보이기 (로딩 UI 완전히 사라진 후)
        Debug.Log("[GameManager] === SHOWING FIRST PERSON VIEW ===");
        ShowFirstPersonView();
    
        yield return new WaitForSeconds(enemySpawnDelay);
    
        Debug.Log("[GameManager] Starting enemy spawning...");
        StartEnemySpawning();
    
        isInitialized = true;
        Debug.Log("[GameManager] === GAME FULLY INITIALIZED ===");
    }
    
    void SpawnPlayerAtHighPosition()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Player prefab not assigned!");
            return;
        }
        
        Vector3 spawnPosition = new Vector3(0, playerSpawnHeight, 0) + playerSpawnOffset;
        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        spawnedPlayer.name = "Player";
        
        if (!spawnedPlayer.CompareTag("Player"))
        {
            spawnedPlayer.tag = "Player";
        }
        
        playerCamera = spawnedPlayer.GetComponentInChildren<Camera>();
        
        if (playerCamera == null)
        {
            Debug.LogError("[GameManager] Player camera not found in player prefab!");
        }
        else
        {
            if ((playerCamera.cullingMask & (1 << LayerMask.NameToLayer("UI"))) == 0)
            {
                playerCamera.cullingMask |= (1 << LayerMask.NameToLayer("UI"));
            }
            
            Debug.Log($"[GameManager] Player camera found: {playerCamera.name}");
        }
        
        AssignFirstPersonViewLayer();
        DisablePlayerPhysicsAndControl();
        
        // ★ 플레이어를 TerrainGenerator에 전달
        if (terrainGenerator != null)
        {
            terrainGenerator.SetPlayer(spawnedPlayer.transform);
            Debug.Log("[GameManager] ✓ Player assigned to TerrainGenerator");
        }
        
        Debug.Log($"[GameManager] Player spawned at high position: {spawnPosition}");
    }
    
    // ★ FirstPersonView 레이어를 플레이어 오브젝트에 할당
    void AssignFirstPersonViewLayer()
    {
        if (spawnedPlayer == null) return;
        
        int layerIndex = LayerMask.NameToLayer(firstPersonViewLayer);
        if (layerIndex == -1)
        {
            Debug.LogError($"[GameManager] '{firstPersonViewLayer}' 레이어를 찾을 수 없습니다!");
            return;
        }
        
        // ★ 플레이어의 특정 자식 오브젝트들에 레이어 할당
        // 일반적으로 FirstPersonView는 "Arms", "Hands", "Weapon", "PlayerModel" 같은 이름을 가짐
        
        Transform[] children = spawnedPlayer.GetComponentsInChildren<Transform>(true);
        int assignedCount = 0;
        
        foreach (Transform child in children)
        {
            string childName = child.name.ToLower();
            
            // ★ FirstPersonView로 간주할 오브젝트 이름 패턴
            if (childName.Contains("arm") || 
                childName.Contains("hand") || 
                childName.Contains("weapon") || 
                childName.Contains("gun") ||
                childName.Contains("firstperson") ||
                childName.Contains("fpv") ||
                childName.Contains("playermodel") ||
                childName.Contains("body"))
            {
                child.gameObject.layer = layerIndex;
                assignedCount++;
                Debug.Log($"[GameManager] '{child.name}' → FirstPersonView 레이어 할당");
            }
        }
        
        if (assignedCount == 0)
        {
            Debug.LogWarning("[GameManager] FirstPersonView 레이어를 할당할 오브젝트를 찾지 못했습니다. 수동으로 레이어를 할당하세요.");
        }
        else
        {
            Debug.Log($"[GameManager] ✓ {assignedCount}개 오브젝트에 FirstPersonView 레이어 할당됨");
        }
    }
    
    void DisablePlayerPhysicsAndControl()
    {
        if (spawnedPlayer == null) return;
        
        Rigidbody rb = spawnedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        DisablePlayerControllers();
        
        Debug.Log("[GameManager] Player physics and control disabled");
    }
    
    void DisablePlayerControllers()
    {
        if (spawnedPlayer == null) return;
        
        var characterController = spawnedPlayer.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        MonoBehaviour[] scripts = spawnedPlayer.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            string typeName = script.GetType().Name.ToLower();
            if (typeName.Contains("player") || typeName.Contains("controller") || 
                typeName.Contains("movement") || typeName.Contains("input"))
            {
                script.enabled = false;
                Debug.Log($"[GameManager] Disabled: {script.GetType().Name}");
            }
        }
    }
    
    void EnablePlayerControllers()
    {
        if (spawnedPlayer == null) return;
        
        var characterController = spawnedPlayer.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        MonoBehaviour[] scripts = spawnedPlayer.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            string typeName = script.GetType().Name.ToLower();
            if (typeName.Contains("player") || typeName.Contains("controller") || 
                typeName.Contains("movement") || typeName.Contains("input"))
            {
                script.enabled = true;
                Debug.Log($"[GameManager] Enabled: {script.GetType().Name}");
            }
        }
    }
    
    void TeleportPlayerToGround()
    {
        if (spawnedPlayer == null) return;
        
        Vector3 groundPosition = Vector3.zero;
        bool foundValidPosition = false;
        
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle;
            float randomRadius = Random.Range(spawnRadiusMin, spawnRadiusMax);
            
            Vector3 randomOffset = new Vector3(
                randomCircle.x * randomRadius,
                0,
                randomCircle.y * randomRadius
            );
            
            Vector3 targetPosition = Vector3.zero + randomOffset + playerSpawnOffset;
            Vector3 rayStart = targetPosition + Vector3.up * playerSpawnHeight;
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, playerSpawnHeight * 2f, LayerMask.GetMask("Ground")))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                
                if (slopeAngle < 30f)
                {
                    groundPosition = hit.point + Vector3.up * 2f;
                    foundValidPosition = true;
                    
                    Debug.Log($"[GameManager] Found valid spawn at ({groundPosition.x:F1}, {groundPosition.y:F1}, {groundPosition.z:F1})");
                    break;
                }
            }
        }
        
        if (!foundValidPosition)
        {
            Debug.LogWarning("[GameManager] Using center fallback position");
            
            Vector3 centerRayStart = Vector3.zero + Vector3.up * playerSpawnHeight + playerSpawnOffset;
            
            if (Physics.Raycast(centerRayStart, Vector3.down, out RaycastHit centerHit, playerSpawnHeight * 2f, LayerMask.GetMask("Ground")))
            {
                groundPosition = centerHit.point + Vector3.up * 2f;
            }
            else
            {
                groundPosition = Vector3.zero + Vector3.up * 5f + playerSpawnOffset;
            }
        }
        
        spawnedPlayer.transform.position = groundPosition;
        
        Rigidbody rb = spawnedPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (enemySpawner != null)
        {
            enemySpawner.SetTarget(spawnedPlayer.transform);
        }
        
        Debug.Log("[GameManager] Player teleported to ground (physics active, control disabled)");
    }
    
    void EnablePlayerControl()
    {
        if (spawnedPlayer == null) return;
        
        EnablePlayerControllers();
        
        Debug.Log("[GameManager] ✓ Player control enabled - Ready to play!");
    }
    
    void AssignCanvasesToPlayerCamera()
    {
        if (playerCamera == null) return;
        
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        int assignedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || 
                canvas.worldCamera == sceneCamera ||
                canvas.worldCamera == null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = playerCamera;
                canvas.planeDistance = 1f;
                assignedCount++;
            }
        }
        
        if (anchorBoxCanvas != null)
        {
            anchorBoxCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            anchorBoxCanvas.worldCamera = playerCamera;
            anchorBoxCanvas.planeDistance = 1f;
        }
        
        Debug.Log($"[GameManager] ✓ {assignedCount} canvases assigned to player camera");
    }
    
    void StartEnemySpawning()
    {
        if (enemySpawner == null) return;
        enemySpawner.StartSpawning();
    }
    
    public GameObject GetPlayer() => spawnedPlayer;
    public Camera GetPlayerCamera() => playerCamera;
    
    void OnDrawGizmosSelected()
    {
        Vector3 centerPos = Vector3.zero + playerSpawnOffset;
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(centerPos, spawnRadiusMin, 32);
        
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        DrawCircle(centerPos, spawnRadiusMax, 32);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPos, 1f);
    }

    public void Retry()
    {
        Time.timeScale = 1;
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