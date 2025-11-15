using UnityEngine;

public class ForceFieldAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Material forceFieldMaterial;
    
    [Header("Settings")]
    [SerializeField] private bool updateInEditor = false;
    
    private static readonly int AnimationTimeID = Shader.PropertyToID("_AnimationTime");
    
    private float currentAnimationTime = 0f;
    private float frozenTime = 0f; // 시간정지 시 멈춘 시간
    private bool wasFrozen = false;
    
    private void Start()
    {
        // Material 자동 찾기
        if (forceFieldMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                forceFieldMaterial = renderer.material; // 인스턴스 생성
                Debug.Log($"[ForceFieldAnimation] Material found: {forceFieldMaterial.name}");
            }
            else
            {
                Debug.LogError("[ForceFieldAnimation] Renderer not found!");
            }
        }
    }
    
    private void Update()
    {
        if (forceFieldMaterial == null) return;
        
        bool isTimeStopped = TimeStopManager.Instance != null && TimeStopManager.Instance.IsTimeStopped;
        
        if (isTimeStopped)
        {
            // ★ 시간정지 상태 - 시간을 고정
            if (!wasFrozen)
            {
                frozenTime = currentAnimationTime;
                wasFrozen = true;
                Debug.Log($"[ForceFieldAnimation] Time frozen at {frozenTime:F2}");
            }
            
            // 고정된 시간 사용
            forceFieldMaterial.SetFloat(AnimationTimeID, frozenTime);
        }
        else
        {
            // ★ 정상 상태 - 시간 진행
            if (wasFrozen)
            {
                Debug.Log($"[ForceFieldAnimation] Time resumed from {frozenTime:F2}");
                wasFrozen = false;
            }
            
            currentAnimationTime += Time.deltaTime;
            forceFieldMaterial.SetFloat(AnimationTimeID, currentAnimationTime);
        }
    }
    
    private void OnValidate()
    {
        if (updateInEditor && forceFieldMaterial != null)
        {
            forceFieldMaterial.SetFloat(AnimationTimeID, Time.realtimeSinceStartup);
        }
    }
    
    private void OnDestroy()
    {
        // 인스턴스 Material 정리
        if (forceFieldMaterial != null && Application.isPlaying)
        {
            Destroy(forceFieldMaterial);
        }
    }
}