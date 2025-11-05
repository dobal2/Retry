// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        [Tooltip("걷기 발소리 배열 (순서대로 재생)")]
        [SerializeField]
        private AudioClip[] audioClipsWalking;

        [Tooltip("달리기 발소리 배열 (순서대로 재생)")]
        [SerializeField]
        private AudioClip[] audioClipsRunning;

        [Tooltip("착지 소리 배열 (랜덤 재생)")]
        [SerializeField]
        private AudioClip[] audioClipsLanding;

        [Tooltip("발소리 재생 간격 (걷기)")]
        [SerializeField]
        private float walkStepInterval = 0.35f;

        [Tooltip("발소리 재생 간격 (달리기)")]
        [SerializeField]
        private float runStepInterval = 0.3f;

        [Header("Speeds")]
        [SerializeField] private float speedWalking = 5.0f;
        [SerializeField, Tooltip("How fast the player moves while running.")]
        private float speedRunning = 9.0f;

        [Header("Jump Settings")]
        [SerializeField, Tooltip("점프 힘 세기")]
        private float jumpForce = 6.0f;
        [SerializeField, Tooltip("지면 레이어")]
        private LayerMask groundLayer;
        
        [SerializeField] private ShakeData shakeData;
        [SerializeField] private CameraShaker cameraShaker;

        [Header("Swimming Settings")]
        [SerializeField, Tooltip("물 레이어")]
        private LayerMask waterLayer;
        [SerializeField, Tooltip("수영 속도")]
        private float swimSpeed = 3.0f;
        [SerializeField, Tooltip("수영 시 상승/하강 속도")]
        private float swimVerticalSpeed = 2.0f;
        [SerializeField, Tooltip("수면에서 점프하여 나갈 수 있는 힘")]
        private float swimJumpForce = 8.0f;
        [SerializeField, Tooltip("경사로로 물 밖으로 나갈 수 있는 최소 높이 (수면 기준)")]
        private float exitWaterHeightThreshold = 0.3f;

        [Header("Landing Settings")]
        [SerializeField, Tooltip("착지 시 속도 감소 비율 (0~1)")]
        private float landingSpeedMultiplier = 0.3f;
        [SerializeField, Tooltip("속도가 회복되는 시간 (초)")]
        private float landingRecoveryTime = 0.5f;
        [SerializeField, Tooltip("착지 판정 최소 낙하 속도 (양수로 입력)")]
        private float minFallSpeedForLanding = 5f;
        
        [SerializeField, Tooltip("착지 효과를 위한 최소 공중 시간 (초)")]
        private float minAirborneTimeForEffect = 0.3f;
        [SerializeField, Tooltip("착지 효과가 발동되는 최대 경사각 (도)")]
        private float maxSlopeAngleForLanding = 45f;
        
        [SerializeField, Tooltip("착지 효과를 위한 최소 공중 시간 (초)")]
        private float minAirborneTime = 0.2f;

        [Header("Landing Camera Effect")]
        [SerializeField, Tooltip("카메라 Transform (Main Camera)")]
        private Transform cameraTransform;
        [SerializeField, Tooltip("착지 효과 지속 시간")]
        private float landingDipDuration = 0.3f;
        [SerializeField, Tooltip("최대 카메라 기울기 (도)")]
        private float maxLandingTiltAmount = 6f;
        [SerializeField, Tooltip("최대 기울기에 도달하는 공중 시간 (초)")]
        private float maxAirTimeForTilt = 2f;

        [Header("Debug")]
        [SerializeField, Tooltip("착지 디버그 정보 표시")]
        private bool showLandingDebug = false;

        #endregion

        #region PROPERTIES
        private Vector3 Velocity
        {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }
        #endregion

        #region FIELDS
        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSource;

        private bool grounded;
        private bool jumpPressed;
        private bool canJump = true;
        
        private int currentJumpCount = 0;

        private bool isSwimming = false;
        private float waterSurfaceY = 0f;
        private Collider currentWaterCollider;
        
        private bool isSwimJumping = false;
        private float swimJumpTimer = 0f;
        private const float swimJumpDuration = 0.5f;

        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;

        private bool isLandingDipActive = false;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;
        private Coroutine landingDipCoroutine;

        private float currentSpeedMultiplier = 1f;
        private Coroutine speedRecoveryCoroutine;
        
        [SerializeField] private float minImpactVelocity = 2f;

        private int currentWalkStepIndex = 0;
        private int currentRunStepIndex = 0;
        private float stepTimer = 0f;
        private bool wasMovingLastFrame = false;

        private float velocityBeforeLanding = 0f;
        private bool wasGroundedLastFrame = true;
        private float airborneTimer = 0f;

        #endregion

        #region UNITY FUNCTIONS
        protected override void Awake()
        {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        protected override void Start()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

            capsule = GetComponent<CapsuleCollider>();
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;

            if (cameraTransform != null)
            {
                originalCameraLocalPos = cameraTransform.localPosition;
                originalCameraLocalRot = cameraTransform.localRotation;
            }


        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isSwimming)
            {
                CheckWaterExit();
                return;
            }

            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = true;
            canJump = true;
            
            currentJumpCount = 0;

            velocityBeforeLanding = rigidBody.linearVelocity.y;

            if (!wasGroundedLastFrame && airborneTimer >= minAirborneTime)
            {
                CheckAndTriggerLanding(collision);
            }
            else if (showLandingDebug)
            {
                Debug.Log($"<color=gray>[Landing] 착지 무시 - 공중시간 부족: {airborneTimer:F2}초 (최소: {minAirborneTime}초)</color>");
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = true;
            canJump = true;
            
            if (isSwimming)
            {
                CheckWaterExit();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = false;
        }

        private void CheckAndTriggerLanding(Collision collision)
        {
            if (airborneTimer < minAirborneTimeForEffect)
            {
                if (showLandingDebug)
                {
                    Debug.Log($"<color=gray>[Landing] 착지 무시 - 공중시간 부족: {airborneTimer:F2}초</color>");
                }
                return;
            }

            float slopeAngle = 90f;
            if (collision.contactCount > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                slopeAngle = Vector3.Angle(Vector3.up, normal);

                if (showLandingDebug)
                {
                    Debug.Log($"<color=cyan>[Landing Check]</color> 공중시간: {airborneTimer:F2}초 | 경사각: {slopeAngle:F1}°");
                }
            }

            if (slopeAngle > maxSlopeAngleForLanding)
            {
                if (showLandingDebug)
                {
                    Debug.Log($"<color=yellow>[Landing] 착지 무시 - 경사각 초과: {slopeAngle:F1}°</color>");
                }
                return;
            }

            if (showLandingDebug)
            {
                Debug.Log($"<color=green>[Landing] 착지 효과 발동! 공중시간: {airborneTimer:F2}초, 경사각: {slopeAngle:F1}°</color>");
            }
            OnLanded();
        }

        protected override void FixedUpdate()
        {
            if (isSwimming)
            {
                SwimCharacter();
                
                if (isSwimJumping)
                {
                    swimJumpTimer -= Time.fixedDeltaTime;
                    if (swimJumpTimer <= 0f)
                    {
                        isSwimJumping = false;
                    }
                }
            }
            else
            {
                MoveCharacter();
            }
            
            if (!grounded && !isSwimming)
            {
                airborneTimer += Time.fixedDeltaTime;
            }
            else
            {
                airborneTimer = 0f;
            }
            
            jumpPressed = false;
        }
        
        protected override void LateUpdate()
        {
            wasGroundedLastFrame = grounded;
            
            if (jumpPressed && isSwimming)
            {
                PerformSwimJump();
            }
            else if (jumpPressed && !isSwimming && CanPerformJump())
            {
                PerformJump();
            }
        }

        protected override void Update()
        {
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
    
            if (!isSwimming)
            {
                PlayFootstepSounds();
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                jumpPressed = true;
        }

       

        private void CheckWaterExit()
        {
            if (!isSwimming || currentWaterCollider == null)
                return;

            float playerY = transform.position.y;
            
            if (grounded && playerY > waterSurfaceY - exitWaterHeightThreshold)
            {
                ExitSwimmingMode();
                Debug.Log($"<color=green>[Swimming] 경사로를 통해 물 밖으로 탈출! (높이: {playerY:F2})</color>");
            }
        }

        private void ExitSwimmingMode()
        {
            if (!isSwimming) return;
    
            isSwimming = false;
            isSwimJumping = false;
            swimJumpTimer = 0f;
            currentWaterCollider = null;
            
            // ★ 중력 다시 켜기
            rigidBody.useGravity = true;
            rigidBody.linearDamping = 0f;
    
            Vector3 vel = rigidBody.linearVelocity;
            if (vel.y < 0)
            {
                vel.y = 0;
            }
            rigidBody.linearVelocity = vel;
    
            Debug.Log("<color=green>[Swimming] 수영 모드 비활성화! (중력 ON)</color>");
        }
        #endregion

        #region METHODS
        
        private bool CanPerformJump()
        {
            int maxJumps = PlayerStats.Instance.GetMaxJumpCount();
            
            if (currentJumpCount == 0)
                return grounded && canJump;
            
            return currentJumpCount < maxJumps;
        }
        
        private void MoveCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            float speedMultiplier = PlayerStats.Instance.GetMoveSpeedMultiplier();
            
            if (playerCharacter.IsRunning())
                movement *= speedRunning * currentSpeedMultiplier * speedMultiplier;
            else
                movement *= speedWalking * currentSpeedMultiplier * speedMultiplier;

            movement = transform.TransformDirection(movement);

            Vector3 vel = rigidBody.linearVelocity;
            vel.x = movement.x;
            vel.z = movement.z;
            rigidBody.linearVelocity = vel;
        }

        private void SwimCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            movement *= swimSpeed;
            movement = transform.TransformDirection(movement);

            Vector3 vel = rigidBody.linearVelocity;
            vel.x = movement.x;
            vel.z = movement.z;
            
            // ★ 점프 중이 아닐 때만 Y축 속도를 천천히 0으로
            if (!isSwimJumping)
            {
                vel.y = Mathf.Lerp(vel.y, 0, Time.fixedDeltaTime * 3f);
            }
            
            rigidBody.linearVelocity = vel;
        }

        private void PerformSwimJump()
        {
            Vector3 vel = rigidBody.linearVelocity;
            vel.y = swimJumpForce;
            rigidBody.linearVelocity = vel;
            
            isSwimJumping = true;
            swimJumpTimer = swimJumpDuration;
            
            Debug.Log("<color=cyan>[Swimming] 수영 점프 실행!</color>");
        }

        private void PerformJump()
        {
            canJump = false;
            grounded = false;

            Vector3 vel = rigidBody.linearVelocity;
            vel.y = 0;
            rigidBody.linearVelocity = vel;

            float jumpMultiplier = PlayerStats.Instance.GetJumpForceMultiplier();
            rigidBody.AddForce(Vector3.up * jumpForce * jumpMultiplier, ForceMode.VelocityChange);
            
            currentJumpCount++;
            
            airborneTimer = 0f;
            
            if (showLandingDebug)
            {
                Debug.Log($"<color=cyan>[Jump] {currentJumpCount}단 점프 실행! (최대: {PlayerStats.Instance.GetMaxJumpCount()})</color>");
            }
        }

        private void PlayFootstepSounds()
        {
            bool isMoving = grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f;

            if (isMoving)
            {
                stepTimer += Time.deltaTime;
                float currentInterval = playerCharacter.IsRunning() ? runStepInterval : walkStepInterval;

                if (stepTimer >= currentInterval)
                {
                    PlayNextFootstep();
                    stepTimer = 0f;
                }
            }
            else
            {
                stepTimer = 0f;
            }

            wasMovingLastFrame = isMoving;
        }

        private void PlayNextFootstep()
        {
            AudioClip[] clips;
            int currentIndex;

            if (playerCharacter.IsRunning())
            {
                if (audioClipsRunning == null || audioClipsRunning.Length == 0)
                    return;

                clips = audioClipsRunning;
                currentIndex = currentRunStepIndex;
                currentRunStepIndex = (currentRunStepIndex + 1) % audioClipsRunning.Length;
            }
            else
            {
                if (audioClipsWalking == null || audioClipsWalking.Length == 0)
                    return;

                clips = audioClipsWalking;
                currentIndex = currentWalkStepIndex;
                currentWalkStepIndex = (currentWalkStepIndex + 1) % audioClipsWalking.Length;
            }

            if (clips[currentIndex] != null)
            {
                audioSource.PlayOneShot(clips[currentIndex]);
            }
        }

        private void PlayLandingSound()
        {
            if (audioClipsLanding == null || audioClipsLanding.Length == 0)
                return;

            int randomIndex = Random.Range(0, audioClipsLanding.Length);
            AudioClip landingClip = audioClipsLanding[randomIndex];

            if (landingClip != null)
            {
                audioSource.PlayOneShot(landingClip);
            }
        }

        private void OnLanded()
        {
            PlayLandingSound();

            if (speedRecoveryCoroutine != null)
                StopCoroutine(speedRecoveryCoroutine);
            speedRecoveryCoroutine = StartCoroutine(RecoverSpeedAfterLanding());

            if (cameraTransform != null)
            {
                if (landingDipCoroutine != null)
                    StopCoroutine(landingDipCoroutine);
                landingDipCoroutine = StartCoroutine(LandingCameraDip());
            }
        }

        private IEnumerator RecoverSpeedAfterLanding()
        {
            currentSpeedMultiplier = landingSpeedMultiplier;
            float elapsedTime = 0f;
            
            while (elapsedTime < landingRecoveryTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / landingRecoveryTime;
                currentSpeedMultiplier = Mathf.Lerp(landingSpeedMultiplier, 1f, t);
                yield return null;
            }
            
            currentSpeedMultiplier = 1f;
            speedRecoveryCoroutine = null;
        }

        private IEnumerator LandingCameraDip()
        {
            isLandingDipActive = true;
            float elapsedTime = 0f;

            float tiltRatio = Mathf.Clamp01(airborneTimer / maxAirTimeForTilt);
            float actualTiltAmount = maxLandingTiltAmount * tiltRatio;
    
            if (showLandingDebug)
            {
                Debug.Log($"<color=cyan>[Landing Tilt]</color> 공중시간: {airborneTimer:F2}초, 기울기: {actualTiltAmount:F2}도");
            }

            float downDuration = landingDipDuration * 0.3f;
            while (elapsedTime < downDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / downDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
        
                Quaternion targetRot = originalCameraLocalRot * Quaternion.Euler(actualTiltAmount * easeT, 0, 0);
                cameraTransform.localRotation = targetRot;
        
                yield return null;
            }

            float upDuration = landingDipDuration * 0.7f;
            elapsedTime = 0f;
    
            while (elapsedTime < upDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / upDuration;
                float easeT = t < 0.5f 
                    ? 4f * t * t * t 
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        
                Quaternion currentRot = originalCameraLocalRot * Quaternion.Euler(actualTiltAmount * (1f - easeT), 0, 0);
                cameraTransform.localRotation = currentRot;
        
                yield return null;
            }

            cameraTransform.localRotation = originalCameraLocalRot;
    
            isLandingDipActive = false;
            landingDipCoroutine = null;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterLayer) != 0)
            {
                isSwimming = true;
                currentWaterCollider = other;
                waterSurfaceY = other.bounds.max.y;
        
                rigidBody.useGravity = false;
                rigidBody.linearDamping = 3f;
                
                Vector3 vel = rigidBody.linearVelocity;
                vel.y = 0;
                rigidBody.linearVelocity = vel;
        
                Debug.Log($"<color=cyan>[Swimming] 수영 모드 활성화!</color>");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterLayer) != 0)
            {
                ExitSwimmingMode();
            }
        }
        #endregion
    }
    
    
}