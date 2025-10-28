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
        [SerializeField, Tooltip("점프 가능한 최대 높이 감지 거리")]
        private float groundCheckDistance = 0.3f;
        [SerializeField, Tooltip("지면 레이어")]
        private LayerMask groundLayer;
        
        private bool wasGroundedLastFrame;
        
        [SerializeField] private ShakeData shakeData;
        [SerializeField] private CameraShaker cameraShaker;

        [Header("Landing Settings")]
        [SerializeField, Tooltip("착지 시 속도 감소 비율 (0~1)")]
        private float landingSpeedMultiplier = 0.3f;
        [SerializeField, Tooltip("속도가 회복되는 시간 (초)")]
        private float landingRecoveryTime = 0.5f;
        [SerializeField, Tooltip("착지 판정 최소 낙하 속도 (양수로 입력, 예: 5)")]
        private float minFallSpeedForLanding = 5f;

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

        private readonly RaycastHit[] groundHits = new RaycastHit[8];

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

        // ✅ 착지 판정용 낙하 속도 저장
        private float lastAirborneVelocity = 0f;

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
            audioSource.loop = false; // 배열 재생이므로 loop 끔

            // 카메라 원래 위치 저장
            if (cameraTransform != null)
            {
                originalCameraLocalPos = cameraTransform.localPosition;
                originalCameraLocalRot = cameraTransform.localRotation;
            }
        }

        private void OnCollisionStay()
        {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = extents.x - 0.01f;

            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, groundLayer, QueryTriggerInteraction.Ignore);

            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule))
                return;

            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            grounded = true;
            canJump = true;
        }

        protected override void FixedUpdate()
        {
            MoveCharacter();
            grounded = false;
            jumpPressed = false;
        }
        
        protected override void LateUpdate()
        {
            MoveCharacter();

            // 점프 처리
            if (jumpPressed && canJump && grounded)
                PerformJump();

            float verticalVelocity = rigidBody.linearVelocity.y;

            // ✅ 공중에 있을 때 낙하 속도 기록
            if (!grounded && verticalVelocity < 0)
            {
                lastAirborneVelocity = verticalVelocity;
            }

            // ✅ 착지 판정 및 효과 (절댓값으로 비교)
            if (!wasGroundedLastFrame && grounded)
            {
                float fallSpeed = Mathf.Abs(lastAirborneVelocity);
                
                if (showLandingDebug)
                {
                    Debug.Log($"<color=yellow>[Landing Check] 낙하 속도: {fallSpeed:F2} | 최소 요구: {minFallSpeedForLanding:F2}</color>");
                }

                // 절댓값으로 비교
                if (fallSpeed >= minFallSpeedForLanding)
                {
                    if (showLandingDebug)
                    {
                        Debug.Log($"<color=green>[Landing] 착지 효과 발동! 속도: {fallSpeed:F2}</color>");
                    }
                    OnLanded();
                }
                else
                {
                    if (showLandingDebug)
                    {
                        Debug.Log($"<color=gray>[Landing] 착지 효과 무시 (속도 부족)</color>");
                    }
                }

                // 착지 후 속도 초기화
                lastAirborneVelocity = 0f;
            }

            wasGroundedLastFrame = grounded;
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
        }

        private void PlayFootstepSounds()
        {
            bool isMoving = grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f;

            if (isMoving)
            {
                // 발소리 타이머 업데이트
                stepTimer += Time.deltaTime;

                float currentInterval = playerCharacter.IsRunning() ? runStepInterval : walkStepInterval;

                // 타이머가 간격을 넘으면 발소리 재생
                if (stepTimer >= currentInterval)
                {
                    PlayNextFootstep();
                    stepTimer = 0f;
                }
            }
            else
            {
                // 멈추면 타이머 리셋
                stepTimer = 0f;
            }

            wasMovingLastFrame = isMoving;
        }

        /// <summary>
        /// 다음 발소리 재생
        /// </summary>
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

            // 발소리 재생
            if (clips[currentIndex] != null)
            {
                audioSource.PlayOneShot(clips[currentIndex]);
            }
        }

        /// <summary>
        /// 착지 소리 재생
        /// </summary>
        private void PlayLandingSound()
        {
            if (audioClipsLanding == null || audioClipsLanding.Length == 0)
                return;

            // 랜덤으로 착지 소리 선택
            int randomIndex = Random.Range(0, audioClipsLanding.Length);
            AudioClip landingClip = audioClipsLanding[randomIndex];

            if (landingClip != null)
            {
                audioSource.PlayOneShot(landingClip);
            }
        }

        /// <summary>
        /// 착지 시 호출
        /// </summary>
        private void OnLanded()
        {
            // 착지 소리 재생
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

        /// <summary>
        /// 착지 후 속도 회복
        /// </summary>
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

        /// <summary>
        /// 착지 시 카메라 딥 효과
        /// </summary>
        private IEnumerator LandingCameraDip()
        {
            isLandingDipActive = true;
            float elapsedTime = 0f;

            // ✅ 이미 저장된 lastAirborneVelocity 사용
            float fallSpeed = Mathf.Abs(lastAirborneVelocity);
            float impactStrength = Mathf.Clamp01((fallSpeed - minFallSpeedForLanding) / 10f);
            
            float actualDipAmount = landingDipAmount * (0.5f + impactStrength * 0.5f);
            float actualTiltAmount = landingTiltAmount * (0.5f + impactStrength * 0.5f);

            float downDuration = landingDipDuration * 0.3f;
            while (elapsedTime < downDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / downDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                // 아래로 이동
                Vector3 targetPos = originalCameraLocalPos + Vector3.down * actualDipAmount * easeT;
                cameraTransform.localPosition = targetPos;
                
                // 앞으로 기울임
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