using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.ParticleSystemJobs;

public class SpiderAI : EnemyAI
{
    [Header("Spider - Head Rotation")] 
    [SerializeField] private Transform headTransform;
    [SerializeField] private float headRotationSpeed = 10f;
    [SerializeField] private float bodyRotationSpeed = 3f;
    [SerializeField] private float bodyRotationThreshold = 30f;
    [SerializeField] private Transform shootPos;
    [SerializeField] private float shootCoolTime;
    [SerializeField] private GameObject projectilePrefab;
    private float shootCurTime;

    [SerializeField] private float headLookYOffset;
    
    [Header("Retreat Settings")]
    [SerializeField] private float retreatDelay = 0.3f; // 후퇴 딜레이 시간
    
    private bool isBodyRotating = false;
    private bool isRetreating = false; // 현재 후퇴 중인지
    private float retreatTimer = 0f; // 후퇴 딜레이 타이머

    void Shoot()
    {
        Vector3 playerDirection = (player.position + new Vector3(0,headLookYOffset,0) - transform.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(playerDirection);
        
        GameObject newProjectile = Instantiate(projectilePrefab, shootPos.position, rotation);
        newProjectile.GetComponent<EnemyProjectile>().SetDamage(damage);
    }

    void LookAtPlayer()
    {
        Vector3 directionToPlayer = (player.position + new Vector3(0,headLookYOffset,0) - transform.position).normalized;

        if (headTransform != null)
        {
            Quaternion headTargetRotation = Quaternion.LookRotation(directionToPlayer);
            headTransform.rotation = Quaternion.Slerp(
                headTransform.rotation,
                headTargetRotation,
                headRotationSpeed * Time.deltaTime
            );
        }

        if (moveDirection == Vector3.zero)
        {
            directionToPlayer.y = 0;
            
            float angleDifference = Vector3.Angle(transform.forward, directionToPlayer);
            
            if (angleDifference > bodyRotationThreshold)
            {
                isBodyRotating = true;
                Quaternion bodyTargetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    bodyTargetRotation,
                    bodyRotationSpeed * Time.deltaTime
                );
            }
            else
            {
                isBodyRotating = false;
            }
        }
        else
        {
            isBodyRotating = false;
        }
    }

    protected override void Move()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > stoppingDistance)
        {
            // 앞으로 이동 중이면 후퇴 타이머 리셋
            isRetreating = false;
            retreatTimer = 0f;
            
            Vector3 newPosition = rb.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
        else if(distanceToPlayer < stoppingDistance)
        {
            // 후퇴 딜레이 처리
            if (!isRetreating)
            {
                retreatTimer += Time.fixedDeltaTime;
                
                if (retreatTimer >= retreatDelay)
                {
                    isRetreating = true;
                }
            }
            
            // 딜레이가 지나면 후퇴
            if (isRetreating)
            {
                Vector3 backPosition = rb.position - moveDirection * (moveSpeed / 1.5f) * Time.fixedDeltaTime;
                rb.MovePosition(backPosition);
            }
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
    }
    
    protected override void Update()
    {
        if (isDead || player == null) return;
    
        CheckTimeStopState();

        if (!isFrozen)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                CalculateMovement();
                LookAtPlayer();
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }

        if (!isFrozen)
        {
            shootCurTime += Time.deltaTime;
            if (shootCurTime >= shootCoolTime)
            {
                shootCurTime = 0;
                Shoot();
            }    
        }
    }
    
    
    protected override void FixedUpdate()
    {
        if (isDead) return;
        if (isFrozen)
        {
            anim.SetBool("isWalking", false);
            return;
        }
        
        
        if (moveDirection != Vector3.zero)
        {
            Move();
            anim.SetBool("isWalking", true);
        }
        else if (isBodyRotating)
        {
            anim.SetBool("isWalking", true);
        }
        else
        {
            anim.SetBool("isWalking", false);
        }
    }
}