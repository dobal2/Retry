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
    
    [Header("Entity Type")]
    [SerializeField] private EntityType entityType = EntityType.Enemy; // ★ 적과 같이 멈춤
    
    private Rigidbody rb;
    private bool isHovering = false;
    private bool hasLaunched = false;
    private bool hasLanded = false;
    private bool isFrozen = false; // ★ 추가
    
    private Vector3 fixedPosition;
    private float groundY;
    private float bobbingTime = 0f;
    
    // ★ 시간 정지용 캐싱
    private Vector3 frozenVelocity;
    private Vector3 frozenAngularVelocity;
    
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
    
    public void LaunchWithDirection(Vector3 direction, float horizontalForce, float upwardForce)
    {
        hasLaunched = true;
        hasLanded = false;
        
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
        if (isFrozen) return; // ★ 멈췄으면 물리 업데이트 안함
        
        if (!isHovering && hasLaunched && !hasLanded)
        {
            CheckGroundContact();
        }
    }
    
    void Update()
    {
        CheckTimeStopState(); // ★ 시간 정지 체크
        
        if (isFrozen) return; // ★ 멈췄으면 업데이트 안함
        
        if (isHovering)
        {
            UpdateHoverAnimation();
            transform.Rotate(Vector3.up, hoverRotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    // ★ 시간 정지 상태 체크
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
    
    // ★ 엔티티 정지
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
    
    // ★ 엔티티 정지 해제
    private void UnfreezeEntity()
    {
        isFrozen = false;
        
        if (rb != null && !isHovering) // 호버링 중이면 이미 kinematic
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
        PlayerStats.Instance.AddEnergy(1);
        Destroy(gameObject);
    }
}