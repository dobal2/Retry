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

    private bool isBodyRotating = false; // 몸통이 회전 중인지

    protected override void Update()
    {
        base.Update(); // 기본 AI 로직 실행

        if (isDead || player == null) return;

        // 거미 특화: 항상 플레이어 바라보기
        if (!isFrozen)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                LookAtPlayer();
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

    void Shoot()
    {
        Vector3 playerDirection = (player.position + new Vector3(0,headLookYOffset,0) - transform.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(playerDirection);
        
        GameObject newProjectile = Instantiate(projectilePrefab, shootPos.position, rotation);
    }

    void LookAtPlayer()
    {
        Vector3 directionToPlayer = (player.position + new Vector3(0,headLookYOffset,0) - transform.position).normalized;

        // 1. 머리가 먼저 빠르게 회전 (Y축 포함, 위아래도 바라봄)
        if (headTransform != null)
        {
            Quaternion headTargetRotation = Quaternion.LookRotation(directionToPlayer);
            headTransform.rotation = Quaternion.Slerp(
                headTransform.rotation,
                headTargetRotation,
                headRotationSpeed * Time.deltaTime
            );
        }

        // 2. 몸통 회전 판단 (정지 상태일 때)
        if (moveDirection == Vector3.zero)
        {
            directionToPlayer.y = 0; // 몸통은 Y축 제외하고 회전
            
            // 현재 몸통 방향과 플레이어 방향의 각도 차이 계산
            float angleDifference = Vector3.Angle(transform.forward, directionToPlayer);
            
            // 각도 차이가 임계값보다 크면 몸통 회전
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
        base.Move(); // 기본 이동 실행
    }
    
    protected override void FixedUpdate()
    {
        if (isDead) return;
        if (isFrozen) return;

        if (TimeStopManager.Instance.IsTimeStopped)
        {
            anim.SetBool("isWalking", false);
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