using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HS_ProjectileMover : MonoBehaviour
{
    [Header("Damage")]
    private float baseDamage = 1f;
    [SerializeField] private LayerMask damageableLayers; 
    
    [Header("Movement")]
    [SerializeField] protected float baseSpeed = 15f;
    [SerializeField] protected float hitOffset = 0f;
    [SerializeField] protected bool UseFirePointRotation;
    [SerializeField] protected Vector3 rotationOffset = new Vector3(0, 0, 0);
    
    [Header("Effects")]
    [SerializeField] protected GameObject hit;
    [SerializeField] protected ParticleSystem hitPS;
    [SerializeField] protected GameObject flash;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Collider col;
    [SerializeField] protected Light lightSourse;
    [SerializeField] protected GameObject[] Detached;
    [SerializeField] protected ParticleSystem projectilePS;
    [SerializeField] protected bool notDestroy = false;
    
    private bool startChecker = false;
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Projectile;

    [Header("Homing Settings")]
    [SerializeField] protected bool isHoming = false;
    [SerializeField] protected float homingDelay = 0.5f;
    [SerializeField] protected float homingRange = 20f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected string targetTag = "Enemy";
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    protected Transform target;
    protected bool homingActive = false;

    private bool isFrozen;
    private float destroyTimer = 0f;
    private float destroyDuration = 5f;
    
    private bool allowFreezeCheck = false;
    
    private float FinalDamage => baseDamage * PlayerStats.Instance.GetProjectileDamage();

    protected virtual void Start()
    {
        if (!startChecker)
        {
            if (flash != null)
            {
                flash.transform.parent = null;
            }
        }
    
        if (isHoming)
        {
            StartCoroutine(ActivateHomingAfterDelay());
        }
    
        destroyTimer = 0f;
        destroyDuration = 5f;
    
        if (TimeStopManager.Instance.ShouldBeFrozen(entityType))
        {
            if (col != null)
                col.enabled = false;
        }
    
        StartCoroutine(EnableFreezeCheckAfterDelay());
    
        startChecker = true;
    }

    protected virtual void OnEnable()
    {
        if (startChecker)
        {
            if (flash != null)
            {
                flash.transform.parent = null;
            }
            if (lightSourse != null)
                lightSourse.enabled = true;
        
            if (!TimeStopManager.Instance.ShouldBeFrozen(entityType))
            {
                col.enabled = true;
            }
            else
            {
                col.enabled = false;
            }
        
            rb.constraints = RigidbodyConstraints.None;
        
            destroyTimer = 0f;
        
            allowFreezeCheck = false;
            StartCoroutine(EnableFreezeCheckAfterDelay());
        
            if (isHoming)
            {
                target = null;
                homingActive = false;
                StartCoroutine(ActivateHomingAfterDelay());
            }
        }
    }
    
    IEnumerator EnableFreezeCheckAfterDelay()
    {
        allowFreezeCheck = false;
        yield return new WaitForSeconds(0.05f);
        allowFreezeCheck = true;
    }

    protected virtual IEnumerator ActivateHomingAfterDelay()
    {
        homingActive = false;
        yield return new WaitForSeconds(homingDelay);
        homingActive = true;
    }

    protected virtual void FixedUpdate()
    {
        if (allowFreezeCheck)
        {
            CheckTimeStopState();
        }
        
        if (isHoming && homingActive && !isFrozen)
        {
            FindAndTrackTarget();
        }
        
        if (baseSpeed != 0 && !isFrozen)
        {
            rb.linearVelocity = transform.forward * baseSpeed;      
        }
        
        if (!isFrozen)
        {
            destroyTimer += Time.fixedDeltaTime;
            
            if (destroyTimer >= destroyDuration)
            {
                if (notDestroy)
                    gameObject.SetActive(false);
                else
                    Destroy(gameObject);
            }
        }
    }
    
    public void SetProjectileSpeed(float speed)
    {
        this.baseSpeed = speed;
    }
    
    private void CheckTimeStopState()
    {
        bool shouldBeFrozen = TimeStopManager.Instance.ShouldBeFrozen(entityType);

        if (shouldBeFrozen && !isFrozen)
        {
            FreezeProjectile();
        }
        else if (!shouldBeFrozen && isFrozen)
        {
            UnfreezeProjectile();
        }
    }

    private void FreezeProjectile()
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

        if (col != null)
        {
            col.enabled = false;
        }

        if (projectilePS != null)
        {
            var main = projectilePS.main;
            main.simulationSpeed = 0f;
        }

        foreach (var detached in Detached)
        {
            if (detached != null)
            {
                ParticleSystem ps = detached.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.simulationSpeed = 0f;
                }
            }
        }
    }

    private void UnfreezeProjectile()
    {
        isFrozen = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = frozenVelocity;
            rb.angularVelocity = frozenAngularVelocity;
        }

        if (col != null)
        {
            col.enabled = true;
        }

        if (projectilePS != null)
        {
            var main = projectilePS.main;
            main.simulationSpeed = 1f;
        }

        foreach (var detached in Detached)
        {
            if (detached != null)
            {
                ParticleSystem ps = detached.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.simulationSpeed = 1f;
                }
            }
        }
    }

    protected virtual void FindAndTrackTarget()
    {
        if (target == null || Vector3.Distance(transform.position, target.position) > homingRange)
        {
            target = FindClosestTarget();
        }

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    protected virtual Transform FindClosestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
        Transform closestEnemy = null;
        float closestDistance = homingRange;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isFrozen)
            return;
            
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Projectile"))
        {
            return;
        }
        
        ApplyExplosionDamage(collision.contacts[0].point);
        
        rb.constraints = RigidbodyConstraints.FreezeAll;
        if (lightSourse != null)
            lightSourse.enabled = false;
        col.enabled = false;
        if (projectilePS)
        {
            projectilePS.Stop();
            projectilePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point + contact.normal * hitOffset;

        if (hit != null)
        {
            hit.transform.rotation = rot;
            hit.transform.position = pos;
            if (UseFirePointRotation) 
            { 
                hit.transform.rotation = gameObject.transform.rotation * Quaternion.Euler(0, 180f, 0); 
            }
            else if (rotationOffset != Vector3.zero) 
            { 
                hit.transform.rotation = Quaternion.Euler(rotationOffset); 
            }
            else 
            { 
                hit.transform.LookAt(contact.point + contact.normal); 
            }
            hitPS.Play();
        }

        foreach (var detachedPrefab in Detached)
        {
            if (detachedPrefab != null)
            {
                ParticleSystem detachedPS = detachedPrefab.GetComponent<ParticleSystem>();
                detachedPS.Stop();
            }
        }
        
        if (notDestroy)
            StartCoroutine(DisableTimerRealtime(hitPS != null ? hitPS.main.duration : 1f));
        else
        {
            if (hitPS != null)
            {
                StartCoroutine(DestroyAfterRealtime(hitPS.main.duration));
            }
            else
                Destroy(gameObject, 1);
        }
    }

    protected virtual void ApplyExplosionDamage(Vector3 explosionPoint)
    {
        float radius = 0;
        if (col != null)
        {
            if (col is SphereCollider sphereCol)
            {
                radius = sphereCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            }
            else if (col is BoxCollider boxCol)
            {
                Vector3 size = boxCol.size;
                radius = Mathf.Max(size.x, size.y, size.z) * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z) * 0.5f;
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                radius = capsuleCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.z);
            }
            else
            {
                radius = 1f;
            }
        }

        Debug.Log($"Explosion at {explosionPoint} with radius {radius}");

        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, radius, damageableLayers);
        
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

        foreach (Collider hitCol in hitColliders)
        {
            if (hitCol.gameObject == gameObject)
                continue;

            if (hitCol.CompareTag("Player") || hitCol.CompareTag("Projectile"))
                continue;

            GameObject rootObject = hitCol.transform.root.gameObject;
            
            if (damagedObjects.Contains(rootObject))
                continue;

            bool damageApplied = TryApplyDamageToObject(hitCol.gameObject);
            
            if (damageApplied)
            {
                damagedObjects.Add(rootObject);
                Debug.Log($"Explosion damaged: {hitCol.gameObject.name}");
            }
        }

        Debug.Log($"Explosion hit {damagedObjects.Count} targets");
    }

    protected virtual void ApplyDamage(GameObject hitObject)
    {
        TryApplyDamageToObject(hitObject);
    }

    protected virtual bool TryApplyDamageToObject(GameObject hitObject)
    {
        EnemyAI enemy = hitObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(FinalDamage);
            return true;
        }

        IDamageable damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(FinalDamage);
            return true;
        }

        EnemyAI parentEnemy = hitObject.GetComponentInParent<EnemyAI>();
        if (parentEnemy != null)
        {
            parentEnemy.TakeDamage(FinalDamage);
            return true;
        }

        IDamageable parentDamageable = hitObject.GetComponentInParent<IDamageable>();
        if (parentDamageable != null)
        {
            parentDamageable.TakeDamage(FinalDamage);
            return true;
        }

        return false;
    }

    IEnumerator DisableTimerRealtime(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (!isFrozen)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    IEnumerator DestroyAfterRealtime(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (!isFrozen)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            
        float radius = 0;
        if (col != null)
        {
            if (col is SphereCollider sphereCol)
            {
                radius = sphereCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            }
            else if (col is BoxCollider boxCol)
            {
                Vector3 size = boxCol.size;
                radius = Mathf.Max(size.x, size.y, size.z) * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z) * 0.5f;
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                radius = capsuleCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.z);
            }
        }
            
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

public interface IDamageable
{
    void TakeDamage(float damage);
}