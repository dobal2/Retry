using UnityEngine;

public class MiniMapCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;  // 회전 속도
    
    private void LateUpdate()
    {
        if (PlayerStats.Instance != null) 
        {
            // 위치는 즉시 따라감
            Vector3 targetPosition = PlayerStats.Instance.transform.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            
            // 회전은 부드럽게 따라감
            float playerYRotation = PlayerStats.Instance.transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(90f, 0f, playerYRotation);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}