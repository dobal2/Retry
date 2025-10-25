using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using FirstGearGames.SmoothCameraShaker;
using StarterAssets;
using TMPro;
using UnityEngine.InputSystem;

public class ThirdPersonShooterController : MonoBehaviour {

    [SerializeField] private CinemachineVirtualCamera normalVirtualCamera;
    //[SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    //[SerializeField] private Transform debugTransform;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawnBulletPosition;
    [SerializeField] private float shootCoolTime;
    private float shootCoolTimer;
    //[SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private int maxAmmo;
    [SerializeField] private int currentAmmo;
    //[SerializeField] private CameraShaker cameraShaker;
    //[SerializeField] private ShakeData fireShakeData;


    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    //private Animator animator;
    
    //[SerializeField] private Transform spineBone;
    [SerializeField] private Transform cameraTransform;
    
    private PlayerHealth playerHealth;

    private void Awake() {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        //animator = GetComponent<Animator>();
        currentAmmo = maxAmmo;
        playerHealth = GetComponent<PlayerHealth>();
    }
    
    private void LateUpdate() {
        
        // if (spineBone != null) {
        //     // 카메라 pitch(X 회전값) 추출
        //     float cameraPitch = cameraTransform.eulerAngles.x;
        //
        //     // 0~360 → -180~180 보정
        //     if (cameraPitch > 180f) cameraPitch -= 360f;
        //
        //     // 회전 제한
        //     float clampedPitch = Mathf.Clamp(cameraPitch, -60f, 45f);
        //
        //     // 현재 애니메이션의 회전을 기준으로
        //     Quaternion baseRotation = spineBone.rotation;
        //
        //     // 회전할 축 기준 (월드가 아니라 로컬의 X축 기준)
        //     Vector3 spineRight = spineBone.right;
        //
        //     // 추가 회전 계산 (Pitch 보정)
        //     Quaternion addedRotation = Quaternion.AngleAxis(clampedPitch, spineRight);
        //
        //     // 누적 적용
        //     spineBone.rotation = addedRotation * baseRotation;
        // }
    }


    private void Update() {
    
    if(playerHealth.died)
        return;
    
    Vector3 mouseWorldPosition = Vector3.zero;

    Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
    Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
    Transform hitTransform = null;
    
    if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask)) {
        // 뭔가 맞으면 그 지점
        mouseWorldPosition = raycastHit.point;
        hitTransform = raycastHit.transform;
    } else {
        // 아무것도 안 맞으면 카메라 방향으로 멀리
        mouseWorldPosition = ray.origin + ray.direction * 999f;
    }
    
    //ammoText.text = "Ammo : "+ currentAmmo.ToString();

    if (starterAssetsInputs.aim) {
        //animator.SetBool("Aiming",true);
        //aimVirtualCamera.gameObject.SetActive(true);
        //thirdPersonController.SetSensitivity(aimSensitivity);
        thirdPersonController.SetRotateOnMove(false);
       // animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));

        Vector3 worldAimTarget = mouseWorldPosition;
        worldAimTarget.y = transform.position.y;
        Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

        transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
    } else {
        //animator.SetBool("Aiming",false);
        //aimVirtualCamera.gameObject.SetActive(false);
        //thirdPersonController.SetSensitivity(normalSensitivity);
        thirdPersonController.SetRotateOnMove(true);
        //animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
    }

    shootCoolTimer += Time.deltaTime;

    if (starterAssetsInputs.reload)
    {
        starterAssetsInputs.reload = false;
        currentAmmo = maxAmmo;
    }

    if (starterAssetsInputs.shoot)
    {
        if (currentAmmo > 0 && shootCoolTime <= shootCoolTimer)
        {
            //animator.SetTrigger("Fire");
            //AudioManager.Instance.PlaySfx(AudioManager.Sfx.LaserSound);
            currentAmmo--;
            shootCoolTimer = 0;
            // Projectile Shoot
            Vector3 aimDir = (mouseWorldPosition - spawnBulletPosition.position).normalized;
            Instantiate(pfBulletProjectile, spawnBulletPosition.position, Quaternion.LookRotation(aimDir, Vector3.up));
            
            // if (cameraShaker != null && fireShakeData != null)
            //     cameraShaker.Shake(fireShakeData);
        }
    }

}

}