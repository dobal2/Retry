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
    
    private Rigidbody rb;
    private bool isHovering = false;
    private bool hasLaunched = false;
    private bool hasLanded = false;
    
    private Vector3 fixedPosition;
    private float groundY;
    private float bobbingTime = 0f;
    
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
        if (!isHovering && hasLaunched && !hasLanded)
        {
            CheckGroundContact();
        }
    }
    
    void Update()
    {
        if (isHovering)
        {
            UpdateHoverAnimation();
            transform.Rotate(Vector3.up, hoverRotationSpeed * Time.deltaTime, Space.World);
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