using UnityEngine;
using System.Collections;

public class SpiderLegsIK : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        [Header("Leg Bones")]
        public Transform upperLeg;  
        public Transform lowerLeg;  
        public Transform foot;
        
        [Header("IK Target")]
        public Transform defaultFootTarget;
        
        [Header("IK Settings")]
        [Tooltip("무릎이 구부러지는 방향 힌트")]
        public Vector3 poleDirection = Vector3.forward;
        
        [HideInInspector] public Vector3 currentFootPos;
        [HideInInspector] public Vector3 targetFootPos;
        [HideInInspector] public bool isStepping;
    }
    
    [Header("Legs")]
    [SerializeField] private Leg[] legs = new Leg[4];
    
    [Header("IK Settings")]
    [SerializeField] private float stepDistance = 1.5f;
    [SerializeField] private float stepHeight = 0.3f;
    [SerializeField] private float stepDuration = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Body Reference")]
    [SerializeField] private Transform bodyTransform;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    void Start()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i].foot != null && legs[i].defaultFootTarget != null)
            {
                legs[i].currentFootPos = legs[i].defaultFootTarget.position;
                legs[i].targetFootPos = legs[i].currentFootPos;
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i].upperLeg != null && legs[i].lowerLeg != null && legs[i].foot != null)
            {
                UpdateLeg(legs[i], i);
            }
        }
    }

    void UpdateLeg(Leg leg, int legIndex)
    {
        Vector3 desiredFootPos = leg.defaultFootTarget.position;
        
        if (Physics.Raycast(desiredFootPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, groundLayer))
        {
            desiredFootPos = hit.point;
        }
        
        float distanceToTarget = Vector3.Distance(leg.currentFootPos, desiredFootPos);
        
        if (distanceToTarget > stepDistance && !leg.isStepping && !IsOppositeLegStepping(legIndex))
        {
            leg.targetFootPos = desiredFootPos;
            StartCoroutine(StepToTarget(leg));
        }
        
        SolveTwoBoneIK(leg);
    }

    bool IsOppositeLegStepping(int legIndex)
    {
        int oppositeLeg = 3 - legIndex;
        if (oppositeLeg >= 0 && oppositeLeg < legs.Length)
        {
            return legs[oppositeLeg].isStepping;
        }
        return false;
    }

    IEnumerator StepToTarget(Leg leg)
    {
        leg.isStepping = true;
        Vector3 startPos = leg.currentFootPos;
        float elapsedTime = 0f;

        while (elapsedTime < stepDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / stepDuration;
            
            leg.currentFootPos = Vector3.Lerp(startPos, leg.targetFootPos, t);
            leg.currentFootPos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;
            
            yield return null;
        }

        leg.currentFootPos = leg.targetFootPos;
        leg.isStepping = false;
    }

    void SolveTwoBoneIK(Leg leg)
{
    Vector3 aPosition = leg.upperLeg.position;
    Vector3 bPosition = leg.lowerLeg.position;
    Vector3 targetPosition = leg.currentFootPos;
    
    // foot의 로컬 오프셋 (lowerLeg 기준)
    Vector3 footLocalOffset = leg.lowerLeg.InverseTransformPoint(leg.foot.position);
    float footOffsetLength = footLocalOffset.magnitude;
    
    float upperLength = Vector3.Distance(aPosition, bPosition);
    float lowerLength = footOffsetLength; // foot까지의 실제 거리
    
    float acLength = Vector3.Distance(aPosition, targetPosition);
    float totalLength = upperLength + lowerLength;
    
    // 너무 멀면 최대로 뻗기
    if (acLength >= totalLength - 0.01f)
    {
        Vector3 dir = (targetPosition - aPosition).normalized;
        leg.upperLeg.rotation = Quaternion.LookRotation(dir);
        
        // lowerLeg도 같은 방향으로 향하되, foot 오프셋 고려
        Vector3 kneePos = aPosition + dir * upperLength;
        Vector3 footDir = (targetPosition - kneePos).normalized;
        leg.lowerLeg.rotation = Quaternion.LookRotation(footDir) * Quaternion.Inverse(Quaternion.LookRotation(footLocalOffset.normalized));
        
        return;
    }
    
    // 너무 가까우면 스킵
    if (acLength < 0.01f) return;
    
    // 중간 관절(무릎) 위치 계산
    float adLength = (acLength * acLength + upperLength * upperLength - lowerLength * lowerLength) / (2f * acLength);
    float dhLength = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - adLength * adLength));
    
    Vector3 acDirection = (targetPosition - aPosition).normalized;
    Vector3 adPosition = aPosition + acDirection * adLength;
    
    // 폴 방향 계산
    Vector3 poleWorldDir = leg.upperLeg.TransformDirection(leg.poleDirection);
    Vector3 perpendicular = Vector3.Cross(acDirection, poleWorldDir);
    if (perpendicular.sqrMagnitude < 0.001f)
        perpendicular = Vector3.Cross(acDirection, Vector3.up);
    perpendicular.Normalize();
    
    Vector3 dhDirection = Vector3.Cross(perpendicular, acDirection).normalized;
    Vector3 jointPosition = adPosition + dhDirection * dhLength;
    
    // Upper Leg 회전
    Vector3 upperDirection = (jointPosition - aPosition).normalized;
    leg.upperLeg.rotation = Quaternion.LookRotation(upperDirection);
    
    // Lower Leg 회전 (foot 오프셋 고려)
    Vector3 lowerDirection = (targetPosition - jointPosition).normalized;
    
    // lowerLeg의 피봇에서 foot의 로컬 방향을 고려한 회전
    Quaternion targetRotation = Quaternion.LookRotation(lowerDirection);
    Quaternion footOffsetRotation = Quaternion.Inverse(Quaternion.LookRotation(footLocalOffset.normalized));
    leg.lowerLeg.rotation = targetRotation * footOffsetRotation;
}

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || legs == null) return;
        
        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];
            if (leg.upperLeg == null || leg.lowerLeg == null || leg.foot == null) continue;
            
            // Upper Leg (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leg.upperLeg.position, leg.lowerLeg.position);
            
            // Lower Leg (청록색)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leg.lowerLeg.position, leg.foot.position);
            
            // Foot (주황색)
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(leg.foot.position, 0.12f);
            
            // 목표 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leg.currentFootPos, 0.1f);
            
            // 타겟 (흰색)
            if (leg.defaultFootTarget != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(leg.defaultFootTarget.position, 0.15f);
            }
            
            // 폴 방향 표시 (마젠타)
            Gizmos.color = Color.magenta;
            Vector3 poleWorldDir = leg.upperLeg.TransformDirection(leg.poleDirection);
            Gizmos.DrawRay(leg.upperLeg.position, poleWorldDir * 0.5f);
        }
    }
}