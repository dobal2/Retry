using UnityEngine;
using System.Collections;

public class SpiderAI : EnemyAI
{
    [Header("Spider - Head Rotation")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private float headRotationSpeed = 10f;
    [SerializeField] private float bodyRotationSpeed = 3f;
    
    [Header("Spider - Leg IK (Optional)")]
    [SerializeField] private Transform[] legTargets; // IK 타겟들
    [SerializeField] private float legStepDistance = 1f;
    [SerializeField] private float legStepHeight = 0.5f;
    [SerializeField] private LayerMask groundLayer;

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
    }

    void LookAtPlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        
        // 1. 머리가 먼저 빠르게 회전
        if (headTransform != null)
        {
            Quaternion headTargetRotation = Quaternion.LookRotation(directionToPlayer);
            headTransform.rotation = Quaternion.Slerp(
                headTransform.rotation, 
                headTargetRotation, 
                headRotationSpeed * Time.deltaTime
            );
        }
        
        // 2. 몸통이 천천히 따라감
        if (moveDirection == Vector3.zero) // 정지 상태일 때만 몸통 회전
        {
            Quaternion bodyTargetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                bodyTargetRotation, 
                bodyRotationSpeed * Time.deltaTime
            );
        }
    }

    protected override void Move()
    {
        base.Move(); // 기본 이동 실행
        
        // 이동 중에는 moveDirection으로 회전
        // LookAtPlayer는 Update에서 처리됨
    }

    // 거미 전용 - 다리 IK (나중에 구현)
    void UpdateLegIK()
    {
        // Procedural leg animation
        // Ray로 지면 감지 + IK 타겟 이동
    }
}