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
        [SerializeField] private AudioClip[] audioClipsWalking;
        [SerializeField] private AudioClip[] audioClipsRunning;
        [SerializeField] private AudioClip[] audioClipsLanding;
        [SerializeField] private float walkStepInterval = 0.35f;
        [SerializeField] private float runStepInterval = 0.3f;

        [Header("Speeds")]
        [SerializeField] private float speedWalking = 5.0f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 6.0f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private ShakeData shakeData;
        [SerializeField] private CameraShaker cameraShaker;

        [Header("Swimming Settings")]
        [SerializeField] private LayerMask waterLayer;
        [SerializeField] private float swimSpeed = 3.0f;
        [SerializeField] private float swimVerticalSpeed = 2.0f;
        [SerializeField] private float swimJumpForce = 8.0f;

        [Header("Landing Settings")]
        [SerializeField] private float landingSpeedMultiplier = 0.3f;
        [SerializeField] private float landingRecoveryTime = 0.5f;
        [SerializeField] private float minFallSpeedForLanding = 5f;
        [SerializeField] private float minAirborneTimeForEffect = 0.3f;
        [SerializeField] private float maxSlopeAngleForLanding = 45f;
        [SerializeField] private float minAirborneTime = 0.2f;

        [Header("Landing Camera Effect")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float landingDipDuration = 0.3f;
        [SerializeField] private float maxLandingTiltAmount = 6f;
        [SerializeField] private float maxAirTimeForTilt = 2f;

        [Header("Debug")]
        [SerializeField] private bool showLandingDebug = false;
        
        private bool isDashing = false;

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
            // 수영 중이면 지면 충돌 무시
            if (isSwimming) return;

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
        }

        private void OnCollisionStay(Collision collision)
        {
            // 수영 중이면 지면 충돌 무시
            if (isSwimming) return;

            if (((1 << collision.gameObject.layer) & groundLayer) == 0)
                return;

            grounded = true;
            canJump = true;
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
                return;
            }

            float slopeAngle = 90f;
            if (collision.contactCount > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                slopeAngle = Vector3.Angle(Vector3.up, normal);
            }

            if (slopeAngle > maxSlopeAngleForLanding)
            {
                return;
            }

            OnLanded();
        }

        public void SetDashing(bool dashing)
        {
            isDashing = dashing;
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
            
            if (jumpPressed)
            {
                if (isSwimming)
                {
                    PerformSwimJump();
                }
                else if (CanPerformJump())
                {
                    PerformJump();
                }
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

        private void ExitSwimmingMode()
        {
            if (!isSwimming) return;
    
            isSwimming = false;
            currentWaterCollider = null;
            
            rigidBody.useGravity = true;
            rigidBody.linearDamping = 0f;
    
            Debug.Log("<color=green>[Swimming] 수영 모드 비활성화!</color>");
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
            if (isDashing) return;
            
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            float speedMultiplier = PlayerStats.Instance.GetMoveSpeedMultiplier();
            
            if (playerCharacter.IsRunning())
                movement *= speedWalking * 1.5f * currentSpeedMultiplier * speedMultiplier;
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
            
            // 점프하면서 수영 모드 즉시 해제
            ExitSwimmingMode();
            
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
                // 점프 중이 아닐 때만 수영 모드 해제
                if (!isSwimJumping)
                {
                    ExitSwimmingMode();
                }
            }
        }
        #endregion
    }
}