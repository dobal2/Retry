// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Magic Weapon. 재장전 없고 줌도 없는 마법 무기
    /// </summary>
    public class MagicWeapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Firing")]

        [Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
        [SerializeField] 
        private bool automatic = true;
        
        [Tooltip("How fast the projectiles are.")]
        [SerializeField]
        private float projectileSpeed = 400.0f;

        [Tooltip("Amount of shots this weapon can shoot in a minute.")]
        [SerializeField] 
        private int roundsPerMinutes = 200;

        [Tooltip("Mask of things recognized when firing.")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("Maximum distance at which this weapon can fire accurately.")]
        [SerializeField]
        private float maximumDistance = 500.0f;

        [Header("Projectile Spawn")]
        
        [Tooltip("프로젝타일이 발사되는 위치. 설정하지 않으면 Muzzle Socket을 사용합니다.")]
        [SerializeField]
        private Transform projectileSpawnPoint;

        [Header("Resources")]
        
        [Tooltip("Projectile Prefab.")]
        [SerializeField]
        private GameObject prefabProjectile;
        
        [Tooltip("The AnimatorController a player character needs to use while wielding this weapon.")]
        [SerializeField] 
        public RuntimeAnimatorController controller;

        [Tooltip("Weapon Body Texture.")]
        [SerializeField]
        private Sprite spriteBody;
        
        [Header("Audio Clips Holster")]

        [Tooltip("Holster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipHolster;

        [Tooltip("Unholster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipUnholster;
        
        [Header("Audio Clips Other")]

        [Tooltip("AudioClip played when this weapon is fired.")]
        [SerializeField]
        private AudioClip audioClipFire;

        [Tooltip("AudioClip played when trying to fire without ammunition.")]
        [SerializeField]
        private AudioClip audioClipFireEmpty;

        #endregion

        #region FIELDS

        /// <summary>
        /// Weapon Animator.
        /// </summary>
        private Animator animator;
        
        /// <summary>
        /// Attachment Manager.
        /// </summary>
        private WeaponAttachmentManagerBehaviour attachmentManager;

        /// <summary>
        /// Equipped Muzzle Reference.
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;
        
        /// <summary>
        /// Magazine (호환성 유지용).
        /// </summary>
        private MagazineBehaviour magazineBehaviour;

        /// <summary>
        /// The GameModeService used in this game!
        /// </summary>
        private IGameModeService gameModeService;
        
        /// <summary>
        /// The main player character behaviour component.
        /// </summary>
        private CharacterBehaviour characterBehaviour;

        /// <summary>
        /// The player character's camera.
        /// </summary>
        private Transform playerCamera;
        
        #endregion

        #region UNITY
        
        protected override void Awake()
        {
            //Get Animator.
            //animator = GetComponent<Animator>();
            
            //Get Attachment Manager.
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            //Cache the game mode service.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            //Cache the player character.
            characterBehaviour = gameModeService.GetPlayerCharacter();
            //Cache the world camera.
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }
        
        protected override void Start()
        {
            //Get Muzzle.
            if (attachmentManager != null)
            {
                muzzleBehaviour = attachmentManager.GetEquippedMuzzle();
                magazineBehaviour = attachmentManager.GetEquippedMagazine();
            }
        }

        #endregion

        #region GETTERS

        public override Animator GetAnimator() => animator;
        
        public override Sprite GetSpriteBody() => spriteBody;

        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;

        // 재장전 관련은 null 반환 (마법 무기는 재장전 없음)
        public override AudioClip GetAudioClipReload() => null;
        public override AudioClip GetAudioClipReloadEmpty() => null;

        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
        
        public override AudioClip GetAudioClipFire() => muzzleBehaviour != null ? muzzleBehaviour.GetAudioClipFire() : audioClipFire;
        
        // 현재 마나를 탄약으로 반환 (재장전을 막기 위해 항상 최대값과 동일하게)
        public override int GetAmmunitionCurrent() => GetAmmunitionTotal();

        // 최대 마나를 총 탄약으로 반환
        public override int GetAmmunitionTotal() => 999;

        public override bool IsAutomatic() => automatic;
        
        // ★ 공격속도에 PlayerStats 적용!
        public override float GetRateOfFire()
        {
            float baseRate = roundsPerMinutes;
            float multiplier = PlayerStats.Instance.GetAttackSpeedMultiplier();
            return baseRate * multiplier;
        }
        
        // 항상 가득 찬 상태로 반환해서 재장전 시도를 막음
        public override bool IsFull() => true;
        
        // 항상 탄약이 있는 것으로 반환
        public override bool HasAmmunition() => true;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;
        
        // 마법 무기는 재장전 불가!
        public override bool CanReload() => false;
        
        // 마법 무기는 줌 불가!
        public override bool CanAim() => false;

        #endregion

        #region METHODS

        // 마법 무기는 재장전 불가
        public override void Reload()
        {
            // 아무것도 하지 않음
        }
        
        public override void Fire(float spreadMultiplier = 1.0f)
        {
            //Make sure that we have a camera cached.
            if (playerCamera == null)
                return;

            // 프로젝타일 발사 위치 결정
            Transform spawnPoint = projectileSpawnPoint;
            
            if (spawnPoint == null && muzzleBehaviour != null)
            {
                spawnPoint = muzzleBehaviour.GetSocket();
            }
            
            // 발사 위치가 없으면 리턴
            if (spawnPoint == null)
                return;
            
            //Play the firing animation.
            const string stateName = "Fire";
            //animator.Play(stateName, 0, 0.0f);

            //Play all muzzle effects.
            if (muzzleBehaviour != null)
                muzzleBehaviour.Effect();
            
            //Determine the rotation that we want to shoot our projectile in.
            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - spawnPoint.position);
            
            //If there's something blocking, aim directly at that thing.
            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - spawnPoint.position);
                
            //프로젝타일 생성 - spawnPoint.position에서 발사됩니다!
            GameObject projectile = Instantiate(prefabProjectile, spawnPoint.position, rotation);
            
            // ★ PlayerStats에서 프로젝타일 크기 적용!
            float sizeMultiplier = PlayerStats.Instance.GetProjectileSizeMultiplier();
            projectile.transform.localScale *= sizeMultiplier;
            

            if (projectile.TryGetComponent<HS_ProjectileMover>(out var mover))
            {
                float speedMultiplier = PlayerStats.Instance.GetProjectileSpeedMultiplier();
                mover.SetProjectileSpeed(projectileSpeed * speedMultiplier);
            }
        }

        public override void FillAmmunition(int amount)
        {
            // 마법 무기는 탄약 충전이 필요 없음
        }

        public override void EjectCasing()
        {
            // 마법 무기는 탄피가 없음
        }

        #endregion
    }
}