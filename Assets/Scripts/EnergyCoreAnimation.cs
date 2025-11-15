using UnityEngine;

public class EnergyCoreAnimation : MonoBehaviour
{
    [Header("Float Animation")]
    [SerializeField] private float floatAmplitude = 0.5f; // 위아래 움직임 범위
    [SerializeField] private float floatSpeed = 1f; // 위아래 움직임 속도
    
    [Header("Rotation")]
    [SerializeField] private Transform coreObject; // 코어 (중앙)
    [SerializeField] private Transform ring1; // 테두리 1
    [SerializeField] private Transform ring2; // 테두리 2
    
    [SerializeField] private float coreRotationSpeed = 30f; // 코어 회전 속도
    [SerializeField] private float ring1RotationSpeed = 50f; // 테두리1 회전 속도
    [SerializeField] private float ring2RotationSpeed = -40f; // 테두리2 회전 속도 (반대 방향)
    
    private Vector3 startPosition;
    private float timeOffset;
    
    private void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 100f); // 각 코어마다 다른 시작 위치
    }
    
    private void Update()
    {
        if(TimeStopManager.Instance.IsTimeStopped)
            return;
        
        // ★ Y축 위아래 움직임
        float yOffset = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;
        
        // ★ 코어 Y축 회전
        if (coreObject != null)
        {
            coreObject.Rotate(Vector3.up, coreRotationSpeed * Time.deltaTime, Space.Self);
        }
        
        // ★ 테두리1 Y축 회전
        if (ring1 != null)
        {
            ring1.Rotate(Vector3.up, ring1RotationSpeed * Time.deltaTime, Space.Self);
        }
        
        // ★ 테두리2 Y축 회전 (반대 방향)
        if (ring2 != null)
        {
            ring2.Rotate(Vector3.up, ring2RotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
