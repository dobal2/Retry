using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class TimeSkip : MonoBehaviour
{
    private Rigidbody rigid;
    private Movement movement;
    private Camera mainCamera;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float cooldownTimer = 1f;
    private float curTimer;
    
    [Header("Lens Distortion Effect")]
    [SerializeField] private float distortionMinValue = -0.5f;
    [SerializeField] private float distortionDownDuration = 0.1f;
    [SerializeField] private float distortionResetDuration = 0.3f;
    
    [Header("Saturation Effect")]
    [SerializeField] private float dashSaturation = -100f;
    [SerializeField] private float normalSaturation = 30f;
    [SerializeField] private float saturationSpeed = 20f;
    [SerializeField] private float saturationRestoreSpeed = 5f;
    
    private Volume volume;
    private LensDistortion _lensDistortion;
    private ColorAdjustments _colorAdjustments;
    
    private Coroutine saturationCoroutine;
    
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        movement = GetComponent<Movement>();
        mainCamera = Camera.main;
        
        if (volume == null)
        {
            GameObject volumeObj = GameObject.FindWithTag("Volume");
            if (volumeObj != null)
            {
                volume = volumeObj.GetComponent<Volume>();
            }
        }
        
        if (volume != null)
        {
            volume.profile.TryGet(out _lensDistortion);
            volume.profile.TryGet(out _colorAdjustments);
        }
    }

    void Update()
    {
        curTimer += Time.deltaTime;
        if (cooldownTimer <= curTimer)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(DashCoroutine());
                curTimer = 0;
            }
        }
    }
    
    private IEnumerator DashCoroutine()
    {
        movement.SetDashing(true);
        
        StartCoroutine(LensDistortionSequence());
        StartCoroutine(SaturationSequence());
        
        Vector3 dashDirection = mainCamera.transform.forward;
        dashDirection.Normalize();
        
        // 레이캐스트로 장애물 체크
        float actualDistance = dashDistance;
        if (Physics.Raycast(transform.position, dashDirection, out RaycastHit hit, dashDistance))
        {
            actualDistance = hit.distance - 0.5f;
            if (actualDistance < 0) actualDistance = 0;
        }
        
        // 물리 상태 저장
        Vector3 originalVelocity = rigid.linearVelocity;
        bool originalGravity = rigid.useGravity;
        
        rigid.linearVelocity = Vector3.zero;
        rigid.useGravity = false;
        rigid.isKinematic = true;
        
        // 부드럽게 이동
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dashDirection * actualDistance;
        float elapsed = 0f;
        
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        transform.position = endPos;
        
        // 물리 복구
        rigid.isKinematic = false;
        rigid.useGravity = originalGravity;
        rigid.linearVelocity = Vector3.zero;
        
        movement.SetDashing(false);
    }
    
    private IEnumerator LensDistortionSequence()
    {
        if (_lensDistortion == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < distortionDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / distortionDownDuration;
            _lensDistortion.intensity.value = Mathf.Lerp(0f, distortionMinValue, t);
            yield return null;
        }
        
        _lensDistortion.intensity.value = distortionMinValue;
        
        elapsed = 0f;
        while (elapsed < distortionResetDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / distortionResetDuration;
            _lensDistortion.intensity.value = Mathf.Lerp(distortionMinValue, 0f, t);
            yield return null;
        }
        
        _lensDistortion.intensity.value = 0f;
    }
    
    private IEnumerator SaturationSequence()
    {
        if (_colorAdjustments == null) yield break;
    
        // 시간이 멈춰있으면 흑백 조정 안함
        if (TimeStopManager.Instance != null && TimeStopManager.Instance.IsTimeStopped)
        {
            yield break;
        }
    
        if (saturationCoroutine != null)
        {
            StopCoroutine(saturationCoroutine);
        }
    
        float currentSaturation = _colorAdjustments.saturation.value;
    
        while (Mathf.Abs(currentSaturation - dashSaturation) > 1f)
        {
            currentSaturation = Mathf.Lerp(currentSaturation, dashSaturation, Time.unscaledDeltaTime * saturationSpeed);
            _colorAdjustments.saturation.value = currentSaturation;
            yield return null;
        }
    
        _colorAdjustments.saturation.value = dashSaturation;
    
        yield return new WaitForSecondsRealtime(0.1f);
    
        while (Mathf.Abs(currentSaturation - normalSaturation) > 1f)
        {
            currentSaturation = Mathf.Lerp(currentSaturation, normalSaturation, Time.unscaledDeltaTime * saturationRestoreSpeed);
            _colorAdjustments.saturation.value = currentSaturation;
            yield return null;
        }
    
        _colorAdjustments.saturation.value = normalSaturation;
        saturationCoroutine = null;
    }
}