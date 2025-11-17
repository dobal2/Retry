using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class CameraLook : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 sensitivity = new Vector2(1, 1);
        [SerializeField] private Vector2 yClamp = new Vector2(-60, 60);
        [SerializeField] private bool smooth = true;
        [SerializeField] private float interpolationSpeed = 25f;

        private CharacterBehaviour playerCharacter;
        private Rigidbody playerCharacterRigidbody;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
            playerCharacterRigidbody = playerCharacter.GetComponent<Rigidbody>();
        }

        private void Start()
        {
            Vector3 euler = transform.localRotation.eulerAngles;
            yaw = playerCharacter.transform.localRotation.eulerAngles.y;
            pitch = euler.x;
        }

        private void LateUpdate()
        {
            // 카드 선택 중이면 카메라 회전 안함
            if (UpgradeCardManager.Instance != null && UpgradeCardManager.Instance.IsSelecting())
                return;
            if (PlayerStats.Instance != null && !PlayerStats.Instance.IsAlive())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            
            Vector2 input = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;
            input *= sensitivity;

            yaw += input.x;
            pitch -= input.y;
            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);

            Quaternion targetCameraRotation = Quaternion.Euler(pitch, 0f, 0f);
            Quaternion targetCharacterRotation = Quaternion.Euler(0f, yaw, 0f);

            if (smooth)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetCameraRotation, Time.deltaTime * interpolationSpeed);
                playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(playerCharacterRigidbody.rotation, targetCharacterRotation, Time.deltaTime * interpolationSpeed));
            }
            else
            {
                transform.localRotation = targetCameraRotation;
                playerCharacterRigidbody.MoveRotation(targetCharacterRotation);
            }
        }
    }
}