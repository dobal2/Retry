using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private float waterRayDistance = 8f;
    [SerializeField] private bool canSwim = true;
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Enemy;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Effect")]
    [SerializeField] protected float fadeOutDuration = 1.5f; // ★ protected로 변경
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Drop Item")] 
    [SerializeField] private GameObject dropItem;

    [Header("Material")] 
    [SerializeField] private Material[] materials;
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    protected Transform player;
    protected Vector3 moveDirection;
    protected Rigidbody rb;
    protected bool isDead = false;
    protected bool isFrozen;
    
    private bool isInWater = false;
    
    protected TargetAnchorBox anchorBox; // ★ protected로 변경
    protected Animator anim; // ★ protected로 변경
    
    // ★ 여러 렌더러의 메테리얼을 각각 관리
    private MeshRenderer[] meshRenderers;
    private List<Material[]> allMaterialInstances = new List<Material[]>(); // 각 렌더러별 메테리얼 배열
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        
        currentHealth = maxHealth;
        
        anchorBox = GetComponent<TargetAnchorBox>();
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found!");
        
        // ★ 메테리얼 인스턴스화 (독립적인 메테리얼 생성)
        InitializeMaterialInstances();
    }
    
    // ★ 모든 렌더러의 메테리얼을 인스턴스화
    void InitializeMaterialInstances()
    {
        allMaterialInstances.Clear();

        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    // 각 렌더러의 메테리얼을 복사하여 인스턴스 생성
                    Material[] originalMaterials = renderer.sharedMaterials;
                    Material[] instanceMaterials = new Material[originalMaterials.Length];
                    
                    for (int i = 0; i < originalMaterials.Length; i++)
                    {
                        if (originalMaterials[i] != null)
                        {
                            instanceMaterials[i] = new Material(originalMaterials[i]);
                        }
                    }
                    
                    // 렌더러에 인스턴스화된 메테리얼 적용
                    renderer.materials = instanceMaterials;
                    
                    // 리스트에 저장
                    allMaterialInstances.Add(instanceMaterials);
                }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            if (canSwim)
            {
                isInWater = true;
                rb.useGravity = false;
                rb.linearDamping = 2f;
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
    
        // ★ 메테리얼 재인스턴스화 (풀링 시에도 독립적으로)
        InitializeMaterialInstances();
        ResetTransparency();
        
        if (anchorBox != null)
        {
            anchorBox.SetUIAlpha(1f);
        }
    }

    void ResetTransparency()
    {
        foreach (Material[] materialArray in allMaterialInstances)
        {
            foreach (Material mat in materialArray)
            {
                if (mat != null && mat.HasProperty("_Transparency"))
                {
                    mat.SetFloat("_Transparency", 0f);
                }
            }
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

    protected virtual void FixedUpdate() // ★ virtual로 변경
    {
        if (isDead) return;
        if(isFrozen)
            return;
        
        if (moveDirection != Vector3.zero)
        {
            Move();
            anim.SetBool("isWalking",true);
        }
        else
        {
            anim.SetBool("isWalking",false);
        }
    }

    void CalculateMovement()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        
        if (!isInWater && !canSwim)
        {
            if (IsWaterAhead(directionToPlayer))
            {
                moveDirection = Vector3.zero;
                return;
            }
        }
        
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

    bool IsWaterAhead(Vector3 direction)
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 diagonalDown = (direction + Vector3.down * 0.5f).normalized;
        
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
    
    protected virtual void Die() // ★ virtual로 변경
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"{gameObject.name} died!");

        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // ★ 애니메이션 정지
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
        }

        DropItem();

        StartCoroutine(FadeOutAndDestroy());
    }

    void DropItem()
    {
        if (dropItem == null) return;

        Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
        GameObject droppedItem = Instantiate(dropItem, dropPosition, Quaternion.identity);
    }

    protected virtual IEnumerator FadeOutAndDestroy() // ★ virtual로 변경
    {
        float elapsedTime = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        // ★ 모든 콜라이더 끄기
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

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
    
    protected void SetTransparency(float transparency) // ★ protected로 변경
    {
        // ★ 모든 렌더러의 모든 메테리얼에 적용
        foreach (Material[] materialArray in allMaterialInstances)
        {
            foreach (Material material in materialArray)
            {
                if (material != null)
                {
                    if (material.HasProperty("_Transparency"))
                    {
                        material.SetFloat("_Transparency", transparency);
                    }
                }
            }
        }
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    // ★ 메테리얼 인스턴스 메모리 정리
    void OnDestroy()
    {
        foreach (Material[] materialArray in allMaterialInstances)
        {
            foreach (Material mat in materialArray)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
        allMaterialInstances.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * obstacleDetectionDistance);
        
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 diagonalDown = (transform.forward + Vector3.down * 0.5f).normalized;
        Gizmos.DrawRay(rayStart, diagonalDown * waterRayDistance);
    }
}