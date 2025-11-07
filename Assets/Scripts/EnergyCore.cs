using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnergyCore : MonoBehaviour
{
    [Header("Initial Launch")]
    [SerializeField] private bool autoLaunchOnStart = true;
    [SerializeField] private float launchForce = 8f;
    [SerializeField] private float launchUpwardForce = 5f;
    
    [Header("Hover Settings")]
    [SerializeField] private float hoverHeight = 1.5f; // 기본 떠있을 높이
    [SerializeField] private float activationDistance = 2.5f;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Bobbing Animation")]
    [SerializeField] private float bobbingSpeed = 2f; // 위아래 속도
    [SerializeField] private float bobbingAmount = 0.3f; // 위아래 움직임 크기
    
    [Header("Rotation")]
    [SerializeField] private float hoverRotationSpeed = 100f;
    
    [Header("Collection")]
    [SerializeField] private string playerTag = "Player";
    
    private Rigidbody rb;
    private bool isHovering = false; // 호버 모드 여부
    private bool hasLaunched = false;
    
    // 호버 모드 변수
    private Vector3 fixedPosition; // 고정된 X, Z 위치
    private float groundY; // 땅의 Y 위치
    private float bobbingTime = 0f; // 보빙 애니메이션 시간
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Rigidbody 기본 설정
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.None;
    }
    
    void Start()
    {
        if (autoLaunchOnStart)
        {
            Invoke(nameof(Launch), 0.1f);
        }
    }
    
    public void Launch()
    {
        if (hasLaunched) return;
        
        hasLaunched = true;
        
        // 랜덤 방향
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        Vector3 horizontalDir = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );

        // 발사 속도
        Vector3 launchVelocity = horizontalDir * launchForce + Vector3.up * launchUpwardForce;
        rb.linearVelocity = launchVelocity;
        
        // 회전 토크
        Vector3 torque = new Vector3(
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f)
        );
        rb.angularVelocity = torque;
        
        Debug.Log($"[EnergyCore] Launched! Velocity: {rb.linearVelocity}");
    }
    
    public void LaunchWithDirection(Vector3 direction, float horizontalForce, float upwardForce)
    {
        if (hasLaunched) return;
        
        hasLaunched = true;
        
        Vector3 launchVelocity = direction.normalized * horizontalForce + Vector3.up * upwardForce;
        rb.linearVelocity = launchVelocity;
    }
    
    void FixedUpdate()
    {
        if (!isHovering)
        {
            CheckGroundForHover();
        }
    }
    
    void Update()
    {
        if (isHovering)
        {
            // ★ 호버 모드: 애니메이션으로 Y값 제어
            UpdateHoverAnimation();
            
            // Y축 회전
            transform.Rotate(Vector3.up, hoverRotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    /// <summary>
    /// 땅 근처에 도달했는지 확인
    /// </summary>
    void CheckGroundForHover()
    {
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            float distanceToGround = hit.distance;
            
            // 땅과 가까워지면 호버 모드 활성화
            if (distanceToGround <= activationDistance)
            {
                ActivateHoverMode(hit.point.y);
            }
        }
    }
    
    /// <summary>
    /// 호버 모드 활성화
    /// </summary>
    void ActivateHoverMode(float groundYPosition)
    {
        isHovering = true;
        
        // X, Z 위치 고정
        fixedPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        groundY = groundYPosition;
        
        // Rigidbody를 Kinematic으로 전환 (물리 끄기)
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // 회전을 Y축만 남기고 수평으로
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        
        // 보빙 시작 시간
        bobbingTime = 0f;
        
        Debug.Log($"[EnergyCore] Hover mode activated at ground Y: {groundY}");
    }
    
    /// <summary>
    /// 호버 애니메이션: Y값만 sin 함수로 위아래 움직임
    /// </summary>
    void UpdateHoverAnimation()
    {
        bobbingTime += Time.deltaTime * bobbingSpeed;
        
        // Sin 함수로 부드러운 위아래 움직임
        float yOffset = Mathf.Sin(bobbingTime) * bobbingAmount;
        
        // Y값 = 땅 높이 + 기본 호버 높이 + sin 오프셋
        float targetY = groundY + hoverHeight + yOffset;
        
        // 위치 직접 설정 (X, Z는 고정, Y만 변경)
        transform.position = new Vector3(fixedPosition.x, targetY, fixedPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            CollectItem();
        }
    }

    void CollectItem()
    {
        // Debug.Log("[EnergyCore] Collected!");
        // Destroy(gameObject);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * raycastDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position - Vector3.up * activationDistance, 0.3f);
        
        if (isHovering)
        {
            // 고정된 X, Z 위치 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(fixedPosition.x, groundY, fixedPosition.z),
                new Vector3(fixedPosition.x, groundY + hoverHeight + bobbingAmount, fixedPosition.z)
            );
            
            // 중심 높이 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(fixedPosition.x, groundY + hoverHeight, fixedPosition.z), 0.2f);
        }
    }
}