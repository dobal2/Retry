using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour,IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 2f;
    
    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 2f;
    [SerializeField] private float avoidanceAngle = 45f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Enemy;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Effect")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float destroyDelay = 0.5f; // 완전히 투명해진 후 대기 시간
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    private Transform player;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool isDead = false;

    private bool isFrozen;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Rigidbody 설정
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        
        // 체력 초기화
        currentHealth = maxHealth;
        
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found!");
    }

    public float GetHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    void Update()
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

        // 상태 변화가 있을 때만 처리
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
        
        // 애니메이션 정지
        // if (animator != null)
        // {
        //     animator.speed = 0f;
        // }

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
        
        
        // if (animator != null)
        // {
        //     animator.speed = 1f;
        // }

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

    void Move()
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

    /// <summary>
    /// 데미지를 받는 메서드 (외부에서 호출)
    /// </summary>
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

        // 움직임 멈추기
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 페이드 아웃 시작
        StartCoroutine(FadeOutAndDestroy());
    }

    /// <summary>
    /// 투명도를 0에서 1로 변경하며 페이드 아웃
    /// </summary>
    IEnumerator FadeOutAndDestroy()
    {
        float elapsedTime = 0f;

        // 시작 알파값 (불투명)
        float startAlpha = 0f;
        // 목표 알파값 (투명)
        float targetAlpha = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;
            
            // 현재 알파값 계산 (1 → 0)
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            SetTransparency(currentAlpha);

            yield return null;
        }

        // 완전히 투명하게
        //SetTransparency(1f);

        // 약간 대기 후 삭제
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
    }
}