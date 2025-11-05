using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour,IDamageable,IPoolable

{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 2f;
    
    [Header("Detection")]
    [SerializeField] protected float  detectionRange = 15f;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 2f;
    [SerializeField] private float avoidanceAngle = 45f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Water Settings")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterRayDistance = 8f; // 아래 대각선 레이 거리
    [SerializeField] private bool canSwim = true; // 물 안에서 다닐 수 있는지
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Enemy;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Effect")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Drop Item")] 
    [SerializeField] private GameObject dropItem;
    [SerializeField] private float dropForce = 5f;
    [SerializeField] private float dropUpwardForce = 3f;
    [SerializeField] private float dropRandomRadius = 2f;
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    protected Transform player;
    protected Vector3 moveDirection;
    protected Rigidbody rb;
    protected bool isDead = false;
    protected bool isFrozen;
    
    // ★ 물 관련
    private bool isInWater = false;
    
    private TargetAnchorBox anchorBox;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        
        currentHealth = maxHealth;
        
        anchorBox = GetComponent<TargetAnchorBox>();
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found!");
    }
    
    
    // ★ 물 감지 (Trigger)
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            if (canSwim)
            {
                isInWater = true;
                rb.useGravity = false;
                rb.linearDamping = 2f; // 물 저항
                Debug.Log($"<color=cyan>[Enemy] {gameObject.name} entered water!</color>");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            isInWater = false;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            Debug.Log($"<color=green>[Enemy] {gameObject.name} exited water!</color>");
        }
    }
    
    public void OnSpawn()
    {
        currentHealth = maxHealth;
        isDead = false;
        isFrozen = false;
        moveDirection = Vector3.zero;
        isInWater = false;
    
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 0f;
        }
    
        ResetTransparency();
        
        if (anchorBox != null)
        {
            anchorBox.SetUIAlpha(1f);
        }
    }

    void ResetTransparency()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material.HasProperty("_Transparency"))
        {
            meshRenderer.material.SetFloat("_Transparency", 0f);
        }
    }

    public float GetHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        if (player == null) return;
        
        CheckTimeStopState();

        if (!isFrozen)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange && distanceToPlayer > stoppingDistance)
            {
                CalculateMovement();
            }
            else
            {
                moveDirection = Vector3.zero;
            }   
        }
    }
    
    private void CheckTimeStopState()
    {
        bool shouldBeFrozen = TimeStopManager.Instance.ShouldBeFrozen(entityType);

        if (shouldBeFrozen && !isFrozen)
        {
            FreezeEntity();
        }
        else if (!shouldBeFrozen && isFrozen)
        {
            UnfreezeEntity();
        }
    }

    private void FreezeEntity()
    {
        isFrozen = true;

        if (rb != null)
        {
            frozenVelocity = rb.linearVelocity;
            frozenAngularVelocity = rb.angularVelocity;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Debug.Log($"[Enemy] {gameObject.name} frozen!");
    }
    
    private void UnfreezeEntity()
    {
        isFrozen = false;
        
        if (rb != null)
        {
            rb.linearVelocity = frozenVelocity;
            rb.angularVelocity = frozenAngularVelocity;
            rb.isKinematic = false;
        }

        Debug.Log($"[Enemy] {gameObject.name} unfrozen!");
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if(isFrozen)
            return;
        
        if (moveDirection != Vector3.zero)
        {
            Move();
        }
    }

    void CalculateMovement()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        
        // ★ 물 안에 있지 않을 때만 물 체크 (물 안에서는 자유롭게 이동)
        if (!isInWater && !canSwim)
        {
            // 플레이어 방향으로 가는 경로에 물이 있는지 체크
            if (IsWaterAhead(directionToPlayer))
            {
                // 물이 있으면 멈춤
                moveDirection = Vector3.zero;
                return;
            }
        }
        
        // 일반 장애물 감지
        if (Physics.Raycast(transform.position, directionToPlayer, obstacleDetectionDistance, obstacleLayer))
        {
            Vector3 avoidDirection = FindAvoidanceDirection(directionToPlayer);
            moveDirection = avoidDirection;
        }
        else
        {
            moveDirection = directionToPlayer;
        }
    }

    // ★ 아래 대각선으로 긴 레이를 쏴서 물 감지
    bool IsWaterAhead(Vector3 direction)
    {
        // 시작 위치 (적의 중심)
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        
        // 아래 대각선 방향 (앞으로 가면서 아래로)
        Vector3 diagonalDown = (direction + Vector3.down * 0.5f).normalized;
        
        // 긴 레이 발사
        if (Physics.Raycast(rayStart, diagonalDown, out RaycastHit hit, waterRayDistance, waterLayer))
        {
            Debug.DrawRay(rayStart, diagonalDown * waterRayDistance, Color.red, 0.1f);
            return true;
        }
        
        Debug.DrawRay(rayStart, diagonalDown * waterRayDistance, Color.green, 0.1f);
        return false;
    }

    Vector3 FindAvoidanceDirection(Vector3 forward)
    {
        Vector3 right = Quaternion.Euler(0, avoidanceAngle, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -avoidanceAngle, 0) * forward;

        bool rightClear = !Physics.Raycast(transform.position, right, obstacleDetectionDistance, obstacleLayer);
        bool leftClear = !Physics.Raycast(transform.position, left, obstacleDetectionDistance, obstacleLayer);

        if (rightClear && leftClear)
            return Random.value > 0.5f ? right : left;
        else if (rightClear)
            return right;
        else if (leftClear)
            return left;
        else
            return -forward;
    }

    protected virtual void Move()
    {
        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
        
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Current HP: {currentHealth}/{maxHealth}");
        
        OnHit();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void OnHit()
    {

        
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} died!");

        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        DropItem();

        StartCoroutine(FadeOutAndDestroy());
    }

    void DropItem()
    {
        if (dropItem == null) return;

        Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
        GameObject droppedItem = Instantiate(dropItem, dropPosition, Quaternion.identity);
    }

    IEnumerator FadeOutAndDestroy()
    {
        float elapsedTime = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            SetTransparency(currentAlpha);
            
            if (anchorBox != null)
            {
                anchorBox.SetUIAlpha(1f - currentAlpha);
            }

            yield return null;
        }

        yield return new WaitForSeconds(destroyDelay);

        gameObject.SetActive(false);
    }
    
    void SetTransparency(float transparency)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material mat = meshRenderer.material;
        
        if (mat.HasProperty("_Transparency"))
        {
            mat.SetFloat("_Transparency", transparency);
        }
        else
        {
            Debug.LogError("No TransparencyProperty");
        }
    }

    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * obstacleDetectionDistance);
        
        // ★ 아래 대각선 물 감지 레이 시각화
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 diagonalDown = (transform.forward + Vector3.down * 0.5f).normalized;
        Gizmos.DrawRay(rayStart, diagonalDown * waterRayDistance);
    }
}