using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected float stoppingDistance = 2f;
    
    [Header("Damage")]
    [SerializeField] protected float damage = 1;
    
    [Header("Detection")]
    [SerializeField] protected float detectionRange = 15f;
    
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
    [SerializeField] protected float fadeOutDuration = 1.5f;
    [SerializeField] private float destroyDelay = 0.5f;
    
    
    [Header("Energy Drop")]
    [SerializeField] private GameObject energyCorePrefab;
    [SerializeField] private int baseMinDrop = 2;
    [SerializeField] private int baseMaxDrop = 3;

    [Header("Material")] 
    [SerializeField] private Material[] materials;
    
    [SerializeField] private GameObject mapIcon;
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    protected Transform player;
    protected Vector3 moveDirection;
    protected Rigidbody rb;
    protected bool isDead = false;
    protected bool isFrozen;
    
    private bool isInWater = false;
    
    protected TargetAnchorBox anchorBox;
    protected Animator anim;
    
    private MeshRenderer[] meshRenderers;
    private List<Material[]> allMaterialInstances = new List<Material[]>();
    
    private float baseMaxHealth;
    private float baseDamage;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        
        baseMaxHealth = maxHealth;
        baseDamage = damage;
    }
    
    void Start()
    {
        mapIcon.SetActive(true);
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        
        currentHealth = maxHealth;
        
        anchorBox = GetComponent<TargetAnchorBox>();
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found!");
        
        InitializeMaterialInstances();
    }
    
    void InitializeMaterialInstances()
    {
        allMaterialInstances.Clear();

        if (meshRenderers != null && meshRenderers.Length > 0)
        {
            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    Material[] originalMaterials = renderer.sharedMaterials;
                    Material[] instanceMaterials = new Material[originalMaterials.Length];
                    
                    for (int i = 0; i < originalMaterials.Length; i++)
                    {
                        if (originalMaterials[i] != null)
                        {
                            instanceMaterials[i] = new Material(originalMaterials[i]);
                        }
                    }
                    
                    renderer.materials = instanceMaterials;
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
        }
    }
    
    public void OnSpawn()
    {
        maxHealth = baseMaxHealth;
        damage = baseDamage;
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
    
    public void SetDifficultyMultipliers(float healthMultiplier, float damageMultiplier)
    {
        maxHealth = baseMaxHealth * healthMultiplier;
        currentHealth = maxHealth;
        damage = baseDamage * damageMultiplier;
    }
    
    protected void CheckTimeStopState()
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
    }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;
        if (isFrozen) return;
        
        if (moveDirection != Vector3.zero)
        {
            Move();
        }
    }

    protected void CalculateMovement()
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
            return true;
        }
        
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
        
        OnHit();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void OnHit()
    {
        
    }
    
    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (anim != null)
        {
            anim.SetBool("isWalking", false);
        }

        if (anchorBox != null)
        {
            anchorBox.CacheBoundsForDeath();
            anchorBox.SetUIAlpha(0.99f);
        }

        DropEnergy();

        StartCoroutine(FadeOutAndDestroy());
    }
    
    
    protected virtual void DropEnergy()
    {
        if (energyCorePrefab == null) return;
    
        int difficultyBonus = 0;
        PooledSpawner spawner = FindObjectOfType<PooledSpawner>();
        if (spawner != null)
        {
            difficultyBonus = spawner.GetDifficultyLevel();
        }
    
        int minDrop = baseMinDrop + difficultyBonus;
        int maxDrop = baseMaxDrop + difficultyBonus;
    
        int dropCount = Random.Range(minDrop, maxDrop + 1);
    
        float angleStep = 360f / dropCount;
    
        for (int i = 0; i < dropCount; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject energyObj = Instantiate(energyCorePrefab, spawnPos, Quaternion.identity);
        
            Energy energyCore = energyObj.GetComponent<Energy>();
            if (energyCore != null)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                energyCore.LaunchWithDirection(direction, 3f, 4f);
            }
        }
    }

    protected virtual IEnumerator FadeOutAndDestroy()
    {
        float elapsedTime = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        yield return null;
        yield return null;
        
        mapIcon.SetActive(false);

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
    
    public float GetDamage()
    {
        return damage;
    }
    
    protected void SetTransparency(float transparency)
    {
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