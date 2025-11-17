using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Energy : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverHeight = 1.5f;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Bobbing Animation")]
    [SerializeField] private float bobbingSpeed = 2f;
    [SerializeField] private float bobbingAmount = 0.3f;
    
    [Header("Rotation")]
    [SerializeField] private float hoverRotationSpeed = 100f;
    
    [Header("Collection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float magnetRange = 5f; // ★ 자석 범위
    [SerializeField] private float magnetSpeed = 10f; // ★ 끌려가는 속도
    [SerializeField] private float magnetAcceleration = 15f; // ★ 가속도
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Enemy;
    
    private Rigidbody rb;
    private bool isHovering = false;
    private bool hasLaunched = false;
    private bool hasLanded = false;
    private bool isFrozen = false;
    private bool isMagnetized = false; // ★ 자석 모드
    
    private Vector3 fixedPosition;
    private float groundY;
    private float bobbingTime = 0f;
    
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
    private Transform playerTransform; // ★ 플레이어 캐싱
    private float currentMagnetSpeed = 0f; // ★ 현재 자석 속도
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.mass = 0.3f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.None;
    }
    
    void Start()
    {
        // ★ 플레이어 찾기
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }
    
    public void LaunchWithDirection(Vector3 direction, float horizontalForce, float upwardForce)
    {
        hasLaunched = true;
        hasLanded = false;
        isMagnetized = false;
        currentMagnetSpeed = 0f;
        
        Vector3 launchVelocity = direction.normalized * horizontalForce + Vector3.up * upwardForce;
        rb.linearVelocity = launchVelocity;
        
        Vector3 torque = new Vector3(
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f)
        );
        rb.angularVelocity = torque;
    }
    
    void FixedUpdate()
    {
        if (isFrozen) return;
        
        if (!isHovering && hasLaunched && !hasLanded)
        {
            CheckGroundContact();
        }
    }
    
    void Update()
    {
        CheckTimeStopState();
        
        if (isFrozen) return;
        
        // ★ 자석 효과 체크
        CheckMagnetEffect();
        
        if (isMagnetized)
        {
            UpdateMagnetMovement();
        }
        else if (isHovering)
        {
            UpdateHoverAnimation();
            transform.Rotate(Vector3.up, hoverRotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    // ★ 자석 효과 체크
    private void CheckMagnetEffect()
    {
        if (playerTransform == null) return;
        if (!hasLanded && !isHovering) return; // 아직 날아가는 중이면 무시
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distance <= magnetRange && !isMagnetized)
        {
            ActivateMagnet();
        }
    }
    
    // ★ 자석 모드 활성화
    private void ActivateMagnet()
    {
        isMagnetized = true;
        isHovering = false;
        currentMagnetSpeed = magnetSpeed * 0.5f; // 초기 속도
        
        // 물리 비활성화
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    // ★ 자석 움직임 업데이트
    private void UpdateMagnetMovement()
    {
        if (playerTransform == null) return;
        
        // 가속
        currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
        currentMagnetSpeed = Mathf.Min(currentMagnetSpeed, magnetSpeed * 3f); // 최대 속도 제한
        
        // 플레이어 방향으로 이동
        Vector3 direction = (playerTransform.position+new Vector3(0,1.5f,0) - transform.position).normalized;
        transform.position += direction * currentMagnetSpeed * Time.deltaTime;
        
        // 회전 (더 빠르게)
        transform.Rotate(Vector3.up, hoverRotationSpeed * 2f * Time.deltaTime, Space.World);
    }
    
    private void CheckTimeStopState()
    {
        if (TimeStopManager.Instance == null) return;
        
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

        if (rb != null && !rb.isKinematic)
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
        
        if (rb != null && !isHovering && !isMagnetized)
        {
            rb.isKinematic = false;
            rb.linearVelocity = frozenVelocity;
            rb.angularVelocity = frozenAngularVelocity;
        }
    }
    
    void CheckGroundContact()
    {
        if (rb.linearVelocity.y > 0) return;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            hasLanded = true;
            ActivateHoverMode(hit.point.y);
        }
    }
    
    void ActivateHoverMode(float groundYPosition)
    {
        isHovering = true;
        
        fixedPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        groundY = groundYPosition;
        
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        
        bobbingTime = 0f;
    }
    
    void UpdateHoverAnimation()
    {
        bobbingTime += Time.deltaTime * bobbingSpeed;
        
        float yOffset = Mathf.Sin(bobbingTime) * bobbingAmount;
        float targetY = groundY + hoverHeight + yOffset;
        
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
        SoundManager.Instance.PlaySfx(SoundManager.Sfx.EnergyCollect);
        PlayerStats.Instance.AddEnergy(1);
        Destroy(gameObject);
    }
}