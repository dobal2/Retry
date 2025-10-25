// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
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
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

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
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;
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
            canJump = true; // 바닥에 닿으면 점프 다시 가능
        }

        protected override void FixedUpdate()
        {
            MoveCharacter();

            if (jumpPressed && canJump && grounded)
                PerformJump();

            // ✅ 착지 감지
            if (!wasGroundedLastFrame && grounded)
            {
                if (cameraShaker != null && shakeData != null)
                    cameraShaker.Shake(shakeData); // 착지 시 카메라 흔들림
            }
            

            wasGroundedLastFrame = grounded; // 다음 프레임용 저장
            grounded = false;
            jumpPressed = false;
        }


        protected override void Update()
        {
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            PlayFootstepSounds();

            // Space 키 입력 체크 (Infima 입력 시스템과 별개로 Unity 기본 Input 사용)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                jumpPressed = true;
        }
        #endregion

        #region METHODS
        private void MoveCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            if (playerCharacter.IsRunning())
                movement *= speedRunning;
            else
                movement *= speedWalking;

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
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }
        #endregion
    }
}
