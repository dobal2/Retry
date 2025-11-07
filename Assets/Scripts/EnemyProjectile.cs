using System.Collections;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 25f;
    
    [Header("Movement")]
    [SerializeField] protected float speed = 15f;
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
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    protected Transform target;

    private bool isFrozen;
    private float destroyTimer = 0f;
    private float destroyDuration = 5f;
    
    // ✅ 트레일 생성을 위한 딜레이
    private bool allowFreezeCheck = false;

    protected virtual void Start()
    {
        if (!startChecker)
        {
            if (flash != null)
            {
                flash.transform.parent = null;
            }
        }
        
    
        // 타이머 초기화
        destroyTimer = 0f;
        destroyDuration = 5f;
    
        // ✅ 시간 정지 중이면 즉시 Collider 비활성화
        if (TimeStopManager.Instance.ShouldBeFrozen(entityType))
        {
            if (col != null)
                col.enabled = false;
        }
    
        // 트레일이 생성될 수 있도록 잠깐 딜레이
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
        
            // ✅ 시간 정지 중이 아닐 때만 Collider 활성화
            if (!TimeStopManager.Instance.ShouldBeFrozen(entityType))
            {
                col.enabled = true;
            }
            else
            {
                col.enabled = false;
            }
        
            rb.constraints = RigidbodyConstraints.None;
        
            // 타이머 리셋
            destroyTimer = 0f;
        
            // OnEnable에서도 딜레이 적용
            allowFreezeCheck = false;
            StartCoroutine(EnableFreezeCheckAfterDelay());
            
        }
    }
    

    IEnumerator EnableFreezeCheckAfterDelay()
    {
        allowFreezeCheck = false;
        // 2-3 프레임 대기 (트레일 초기화 시간)
        yield return null;
        yield return null;
        yield return null;
        allowFreezeCheck = true;
    }
    

    protected virtual void FixedUpdate()
    {
        // ✅ 트레일 생성 후에만 정지 체크
        if (allowFreezeCheck)
        {
            CheckTimeStopState();
        }
        
        if (speed != 0 && !isFrozen)
        {
            rb.linearVelocity = transform.forward * speed;      
        }
        
        // 시간 정지 중이 아닐 때만 타이머 증가
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
        this.speed = speed;
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

        // ✅ 충돌 비활성화
        if (col != null)
        {
            col.enabled = false;
        }

        // 파티클 시뮬레이션 속도를 0으로
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

        // ✅ 충돌 재활성화
        if (col != null)
        {
            col.enabled = true;
        }

        // 시뮬레이션 속도 복구
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

   

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isFrozen)
            return;
        // 플레이어나 다른 총알과는 충돌 무시
        if (collision.gameObject.CompareTag("EnemyProjectile"))
        {
            return;
        }
        
        // 데미지 처리 추가
        ApplyDamage(collision.gameObject);
        
        // 충돌 후 처리
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
        
        // 충돌 후 파괴도 시간 정지 영향 받도록
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

    /// <summary>
    /// 시간 정지를 고려한 Disable 타이머
    /// </summary>
    IEnumerator DisableTimerRealtime(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // 시간 정지 중이 아닐 때만 시간 증가
            if (!isFrozen)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 시간 정지를 고려한 Destroy 타이머
    /// </summary>
    IEnumerator DestroyAfterRealtime(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // 시간 정지 중이 아닐 때만 시간 증가
            if (!isFrozen)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        
        Destroy(gameObject);
    }

    protected virtual void ApplyDamage(GameObject hitObject)
    {
        PlayerHealth enemy = hitObject.GetComponent<PlayerHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Projectile dealt {damage} damage to {hitObject.name}");
            return;
        }

        
    }
}
