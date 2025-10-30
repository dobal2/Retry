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

        [Header("Landing Settings")]
        [SerializeField, Tooltip("착지 시 속도 감소 비율 (0~1)")]
        private float landingSpeedMultiplier = 0.3f;
        [SerializeField, Tooltip("속도가 회복되는 시간 (초)")]
        private float landingRecoveryTime = 0.5f;
        [SerializeField, Tooltip("착지 판정 최소 낙하 속도 (양수로 입력)")]
        private float minFallSpeedForLanding = 5f;
        
        [SerializeField, Tooltip("착지 효과가 발동되는 최대 경사각 (도)")]
        private float maxSlopeAngleForLanding = 45f;
        
        // ✅ 공중에 있어야 하는 최소 시간
        [SerializeField, Tooltip("착지 효과를 위한 최소 공중 시간 (초)")]
        private float minAirborneTime = 0.2f;

        [Header("Landing Camera Effect")]
        [SerializeField, Tooltip("카메라 Transform (Main Camera)")]
        private Transform cameraTransform;
        [SerializeField, Tooltip("착지 시 카메라가 내려가는 거리")]
        private float landingDipAmount = 0.15f;
        [SerializeField, Tooltip("착지 효과 지속 시간")]
        private float landingDipDuration = 0.3f;
        [SerializeField, Tooltip("착지 시 카메라 기울기 (도)")]
        private float landingTiltAmount = 2f;

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

        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;

        // 카메라 착지 효과
        private bool isLandingDipActive = false;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;
        private Coroutine landingDipCoroutine;

        // 착지 후 속도 관련
        private float currentSpeedMultiplier = 1f;
        private Coroutine speedRecoveryCoroutine;
        
        [SerializeField] private float minImpactVelocity = 2f;

        // 발소리 관련
        private int currentWalkStepIndex = 0;
        private int currentRunStepIndex = 0;
        private float stepTimer = 0f;
        private bool wasMovingLastFrame = false;

        // ✅ 착지 판정 개선 변수들
        private float velocityBeforeLanding = 0f;
        private bool wasGroundedLastFrame = true; // 이전 프레임에 지면에 있었는지
        private float airborneTimer = 0f; // 공중에 있던 시간

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

            // 카메라 원래 위치 저장
            if (cameraTransform != null)
            {
                originalCameraLocalPos = cameraTransform.localPosition;
                originalCameraLocalRot = cameraTransform.localRotation;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 지면 레이어 체크
            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = true;
            canJump = true;

            // ✅ 착지 순간의 속도 저장
            velocityBeforeLanding = rigidBody.linearVelocity.y;

            // ✅ 이전 프레임에 공중에 있었고, 충분히 오래 공중에 있었을 때만 착지 판정
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
            // 지면 레이어 체크
            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = true;
            canJump = true;
        }

        private void OnCollisionExit(Collision collision)
        {
            // 지면 레이어 체크
            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = false;
        }

        private void CheckAndTriggerLanding(Collision collision)
        {
            float fallSpeed = Mathf.Abs(velocityBeforeLanding);

            // 낙하 속도 체크
            if (fallSpeed < minFallSpeedForLanding)
            {
                if (showLandingDebug)
                {
                    Debug.Log($"<color=gray>[Landing] 착지 무시 - 속도 부족: {fallSpeed:F2}</color>");
                }
                return;
            }

            // ✅ 경사각 체크
            float slopeAngle = 90f;
            if (collision.contactCount > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                slopeAngle = Vector3.Angle(Vector3.up, normal);

                if (showLandingDebug)
                {
                    Debug.Log($"<color=cyan>[Landing Check]</color> " +
                              $"낙하속도: {fallSpeed:F2} | " +
                              $"경사각: {slopeAngle:F1}° | " +
                              $"공중시간: {airborneTimer:F2}초");
                }
            }

            // 경사각이 너무 크면 착지 효과 무시
            if (slopeAngle > maxSlopeAngleForLanding)
            {
                if (showLandingDebug)
                {
                    Debug.Log($"<color=yellow>[Landing] 착지 무시 - 경사각 초과: {slopeAngle:F1}°</color>");
                }
                return;
            }

            // ✅ 모든 조건 통과 - 착지 효과 발동!
            if (showLandingDebug)
            {
                Debug.Log($"<color=green>[Landing] 착지 효과 발동! 속도: {fallSpeed:F2}, 경사각: {slopeAngle:F1}°, 공중시간: {airborneTimer:F2}초</color>");
            }
            OnLanded();
        }

        protected override void FixedUpdate()
        {
            MoveCharacter();
            
            // ✅ 공중에 있으면 타이머 증가
            if (!grounded)
            {
                airborneTimer += Time.fixedDeltaTime;
            }
            else
            {
                // 지면에 있으면 타이머 리셋
                airborneTimer = 0f;
            }
            
            jumpPressed = false;
        }
        
        protected override void LateUpdate()
        {
            // ✅ 이전 프레임 상태 저장 (다음 프레임 판정용)
            wasGroundedLastFrame = grounded;
            
            // 점프 처리
            if (jumpPressed && canJump && grounded)
                PerformJump();
        }

        protected override void Update()
        {
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            PlayFootstepSounds();

            // Space 키 입력 체크
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                jumpPressed = true;
        }
        #endregion

        #region METHODS
        private void MoveCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            // 속도 배율 적용
            if (playerCharacter.IsRunning())
                movement *= speedRunning * currentSpeedMultiplier;
            else
                movement *= speedWalking * currentSpeedMultiplier;

            movement = transform.TransformDirection(movement);

            // 수평속도 유지, y속도는 기존 그대로
            Vector3 vel = rigidBody.linearVelocity;
            vel.x = movement.x;
            vel.z = movement.z;
            rigidBody.linearVelocity = vel;
        }

        private void PerformJump()
        {
            canJump = false;
            grounded = false;

            // 수직 속도 초기화 후 점프력 적용
            Vector3 vel = rigidBody.linearVelocity;
            vel.y = 0;
            rigidBody.linearVelocity = vel;

            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            
            // ✅ 점프 시작 시 공중 타이머 리셋
            airborneTimer = 0f;
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

            // 속도 회복
            if (speedRecoveryCoroutine != null)
                StopCoroutine(speedRecoveryCoroutine);
            speedRecoveryCoroutine = StartCoroutine(RecoverSpeedAfterLanding());

            // 카메라 착지 효과
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

            float fallSpeed = Mathf.Abs(velocityBeforeLanding);
            float impactStrength = Mathf.Clamp01((fallSpeed - minFallSpeedForLanding) / 10f);
            
            float actualDipAmount = landingDipAmount * (0.5f + impactStrength * 0.5f);
            float actualTiltAmount = landingTiltAmount * (0.5f + impactStrength * 0.5f);

            float downDuration = landingDipDuration * 0.3f;
            while (elapsedTime < downDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / downDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                Vector3 targetPos = originalCameraLocalPos + Vector3.down * actualDipAmount * easeT;
                cameraTransform.localPosition = targetPos;
                
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
                
                Vector3 currentPos = originalCameraLocalPos + Vector3.down * actualDipAmount * (1f - easeT);
                cameraTransform.localPosition = currentPos;
                
                Quaternion currentRot = originalCameraLocalRot * Quaternion.Euler(actualTiltAmount * (1f - easeT), 0, 0);
                cameraTransform.localRotation = currentRot;
                
                yield return null;
            }

            cameraTransform.localPosition = originalCameraLocalPos;
            cameraTransform.localRotation = originalCameraLocalRot;
            
            isLandingDipActive = false;
            landingDipCoroutine = null;
        }
        #endregion
    }
}