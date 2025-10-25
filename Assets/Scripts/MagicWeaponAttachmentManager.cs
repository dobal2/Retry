// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Magic Weapon Attachment Manager. 줌(Scope) 기능이 없는 마법 무기용
    /// </summary>
    public class MagicWeaponAttachmentManager : WeaponAttachmentManagerBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Muzzle")]

        [Tooltip("Selected Muzzle Index.")]
        [SerializeField]
        private int muzzleIndex;

        [Tooltip("All possible Muzzle Attachments that this Weapon can use!")]
        [SerializeField]
        private MuzzleBehaviour[] muzzleArray;
        
        [Header("Magazine")]

        [Tooltip("Selected Magazine Index (마법 무기는 사용 안함, 호환성 유지용).")]
        [SerializeField]
        private int magazineIndex;

        [Tooltip("All possible Magazine Attachments (마법 무기는 사용 안함, 호환성 유지용).")]
        [SerializeField]
        private Magazine[] magazineArray;

        #endregion

        #region FIELDS

        /// <summary>
        /// Equipped Muzzle.
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;
        
        /// <summary>
        /// Equipped Magazine (호환성 유지용).
        /// </summary>
        private MagazineBehaviour magazineBehaviour;

        #endregion

        #region UNITY FUNCTIONS

        protected override void Awake()
        {
            //Select Muzzle!
            if (muzzleArray != null && muzzleArray.Length > 0)
                muzzleBehaviour = muzzleArray.SelectAndSetActive(muzzleIndex);

            //Select Magazine (호환성 유지용)!
            if (magazineArray != null && magazineArray.Length > 0)
                magazineBehaviour = magazineArray.SelectAndSetActive(magazineIndex);
        }        

        #endregion

        #region GETTERS

        // 줌 기능 완전히 비활성화 - 항상 null 반환
        public override ScopeBehaviour GetEquippedScope() => null;
        public override ScopeBehaviour GetEquippedScopeDefault() => null;

        public override MagazineBehaviour GetEquippedMagazine() => magazineBehaviour;
        public override MuzzleBehaviour GetEquippedMuzzle() => muzzleBehaviour;

        #endregion
    }
}