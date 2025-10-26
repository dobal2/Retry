using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HS_ProjectileMover : MonoBehaviour
{
    [SerializeField] protected float speed = 15f;
    [SerializeField] protected float hitOffset = 0f;
    [SerializeField] protected bool UseFirePointRotation;
    [SerializeField] protected Vector3 rotationOffset = new Vector3(0, 0, 0);
    [SerializeField] protected GameObject hit;
    [SerializeField] protected ParticleSystem hitPS;
    [SerializeField] protected GameObject flash;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Collider col;
    [SerializeField] protected Light lightSourse;
    [SerializeField] protected GameObject[] Detached;
    [SerializeField] protected ParticleSystem projectilePS;
    private bool startChecker = false;
    [SerializeField] protected bool notDestroy = false;

    // 호밍 관련 변수
    [Header("Homing Settings")]
    [SerializeField] protected bool isHoming = false;
    [SerializeField] protected float homingDelay = 0.5f; // 호밍 시작 딜레이
    [SerializeField] protected float homingRange = 20f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected string targetTag = "Enemy";
    
    protected Transform target;
    protected bool homingActive = false; // 실제 호밍 활성화 여부

    protected virtual void Start()
    {
        if (!startChecker)
        {
            if (flash != null)
            {
                flash.transform.parent = null;
            }
        }
        
        // 호밍 딜레이 시작
        if (isHoming)
        {
            StartCoroutine(ActivateHomingAfterDelay());
        }
        
        if (notDestroy)
            StartCoroutine(DisableTimer(5));
        else
            Destroy(gameObject, 5);
        startChecker = true;
    }

    // 딜레이 후 호밍 활성화
    protected virtual IEnumerator ActivateHomingAfterDelay()
    {
        homingActive = false;
        yield return new WaitForSeconds(homingDelay);
        homingActive = true;
    }

    protected virtual IEnumerator DisableTimer(float time)
    {
        yield return new WaitForSeconds(time);
        if(gameObject.activeSelf)
            gameObject.SetActive(false);
        yield break;
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
            col.enabled = true;
            rb.constraints = RigidbodyConstraints.None;
            
            // 호밍 초기화
            if (isHoming)
            {
                target = null;
                homingActive = false;
                StartCoroutine(ActivateHomingAfterDelay());
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        // 호밍이 활성화되고 딜레이가 지났을 때만 추적
        if (isHoming && homingActive)
        {
            FindAndTrackTarget();
        }
        
        if (speed != 0)
        {
            rb.linearVelocity = transform.forward * speed;      
        }
    }

    // 타겟 찾기 및 추적
    protected virtual void FindAndTrackTarget()
    {
        // 타겟이 없거나 범위 밖이면 새로운 타겟 찾기
        if (target == null || Vector3.Distance(transform.position, target.position) > homingRange)
        {
            target = FindClosestTarget();
        }

        // 타겟이 있으면 추적
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // 가장 가까운 타겟 찾기
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
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Projectile"))
        {
            return;
        }
        
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
            if (UseFirePointRotation) { hit.transform.rotation = gameObject.transform.rotation * Quaternion.Euler(0, 180f, 0); }
            else if (rotationOffset != Vector3.zero) { hit.transform.rotation = Quaternion.Euler(rotationOffset); }
            else { hit.transform.LookAt(contact.point + contact.normal); }
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
            StartCoroutine(DisableTimer(hitPS.main.duration));
        else
        {
            if (hitPS != null)
            {
                Destroy(gameObject, hitPS.main.duration);
            }
            else
                Destroy(gameObject, 1);
        }
    }
}