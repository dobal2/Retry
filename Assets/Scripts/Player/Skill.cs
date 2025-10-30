using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Skill : MonoBehaviour
{
    [Header("Post Processing")]
    [SerializeField] private Volume volume;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;
    private Vignette _vignette;
    private LensDistortion _lensDistortion;

    [Header("Skill Settings")]
    [SerializeField] private float coolTime = 5f;
    private float curTime = 0f;
    [SerializeField] private float timeStopDuration = 3f;

    [Header("Visual Effects")]
    [SerializeField] private float normalSaturation = 30f;
    [SerializeField] private float timeStopSaturation = -100f;
    [SerializeField] private float saturationTransitionSpeed = 5f;
    
    [Header("Hue Shift Effect")]
    [SerializeField] private bool useHueShift = true;
    [SerializeField] private float hueShiftDuration = 0.5f; // 0 → 180 올라가는 시간
    [SerializeField] private float hueResetDuration = 0.3f; // 180 → 0 빠르게 돌아오는 시간
    [SerializeField] private float maxHueShift = 180f;
    
    [Header("Lens Distortion Effect")]
    [SerializeField] private bool useLensDistortion = true;
    [SerializeField] private float distortionPeakValue = 0.3f; // 처음 최고점
    [SerializeField] private float distortionMinValue = -0.7f; // 최저점
    [SerializeField] private float distortionUpDuration = 0.2f; // 0 → 0.3 시간
    [SerializeField] private float distortionDownDuration = 0.3f; // 0.3 → -0.7 시간
    [SerializeField] private float distortionResetDuration = 0.3f; // -0.7 → 0 시간
    
    [Header("Chromatic Aberration")]
    [SerializeField] private bool useChromaticAberration = true;
    [SerializeField] private float maxChromaticIntensity = 1f;
    [SerializeField] private float chromaticSpeed = 10f;
    
    [Header("Vignette")]
    [SerializeField] private bool useVignette = true;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private float vignetteSpeed = 5f;

    [Header("Camera Shake")]
    [SerializeField] private ShakeData shakeData;
    [SerializeField] private CameraShaker cameraShaker;

    private Coroutine timeStopCoroutine;

    private void Start()
    {
        // Post Processing 이펙트들 가져오기
        if (volume.profile.TryGet(out _colorAdjustments))
        {
            SetSaturation(normalSaturation);
            _colorAdjustments.hueShift.value = 0f;
        }
        else
        {
            Debug.LogError("No ColorAdjustments found in Volume Profile!");
        }

        // Chromatic Aberration
        if (volume.profile.TryGet(out _chromaticAberration))
        {
            _chromaticAberration.intensity.value = 0f;
        }

        // Vignette
        if (volume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.value = 0f;
        }

        // Lens Distortion
        if (volume.profile.TryGet(out _lensDistortion))
        {
            _lensDistortion.intensity.value = 0f;
        }
        else
        {
            Debug.LogWarning("No LensDistortion found in Volume Profile!");
        }

        // TimeStopManager 체크
        if (TimeStopManager.Instance == null)
        {
            Debug.LogError("TimeStopManager not found in scene!");
        }

        // CameraShaker 체크
        if (cameraShaker == null)
        {
            cameraShaker = FindObjectOfType<CameraShaker>();
            if (cameraShaker == null)
            {
                Debug.LogWarning("CameraShaker not found in scene!");
            }
        }
    }

    void Update()
    {
        // 쿨타임 업데이트 (unscaledDeltaTime 사용)
        if (curTime < coolTime)
        {
            curTime += Time.unscaledDeltaTime;
        }

        // E키로 스킬 발동
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryActivateSkill();
        }
    }

    private void TryActivateSkill()
    {
        // 쿨타임 체크
        if (curTime < coolTime)
        {
            Debug.Log($"[Skill] Cooldown: {coolTime - curTime:F1}s remaining");
            return;
        }

        // 이미 시간정지 중이면 무시
        if (TimeStopManager.Instance.IsTimeStopped)
        {
            Debug.Log("[Skill] Time stop already active!");
            return;
        }

        // 스킬 발동
        ActivateTimeStop();
    }

    private void ActivateTimeStop()
    {
        // 쿨타임 리셋
        curTime = 0f;

        // 카메라 쉐이크
        if (cameraShaker != null && shakeData != null)
        {
            cameraShaker.Shake(shakeData);
        }

        // ✅ 모든 이펙트를 순차적으로 실행
        StartCoroutine(TimeStopEffectSequence());
    }

    /// <summary>
    /// 시간 정지 이펙트 전체 시퀀스
    /// 모든 이펙트 동시 시작 → 모두 끝난 후 시간 정지
    /// </summary>
    /// <summary>
    /// 시간 정지 이펙트 전체 시퀀스
    /// 모든 이펙트 동시 시작 → 렌즈 디스톨션 끝나기 0.1초 전에 시간 정지
    /// </summary>
    IEnumerator TimeStopEffectSequence()
    {
        // ✅ 모든 이펙트를 동시에 시작
        Coroutine lensDistortionCoroutine = null;
        Coroutine hueShiftCoroutine = null;
        Coroutine chromaticCoroutine = null;
        Coroutine vignetteCoroutine = null;

        // Lens Distortion 시작
        if (useLensDistortion && _lensDistortion != null)
        {
            lensDistortionCoroutine = StartCoroutine(LensDistortionSequence());
        }

        // Hue Shift → 흑백 시작
        if (useHueShift)
        {
            hueShiftCoroutine = StartCoroutine(HueShiftSequence());
        }
        else
        {
            hueShiftCoroutine = StartCoroutine(TransitionSaturation(timeStopSaturation, saturationTransitionSpeed));
        }

        // Chromatic Aberration 시작
        if (useChromaticAberration && _chromaticAberration != null)
        {
            chromaticCoroutine = StartCoroutine(ChromaticAberrationPulse());
        }

        // Vignette 시작
        if (useVignette && _vignette != null)
        {
            vignetteCoroutine = StartCoroutine(TransitionVignette(maxVignetteIntensity, vignetteSpeed));
        }

        // ✅ Lens Distortion 끝나기 0.1초 전까지 대기
        float lensDistortionDuration = distortionUpDuration + distortionDownDuration + distortionResetDuration;
        float timeStopTriggerTime = lensDistortionDuration - 0.4f;
    
        if (timeStopTriggerTime < 0f)
            timeStopTriggerTime = 0f;

        yield return new WaitForSecondsRealtime(timeStopTriggerTime);

        // ✅ 시간 정지 시작!
        TimeStopManager.Instance.StartTimeStop();
        Debug.Log("[Skill] Time Stop activated!");

        // 지속시간 후 종료
        if (timeStopCoroutine != null)
        {
            StopCoroutine(timeStopCoroutine);
        }
        timeStopCoroutine = StartCoroutine(TimeStopDuration(timeStopDuration));
    }

    /// <summary>
    /// Lens Distortion: 0 → 0.3 → -0.7 → 0
    /// </summary>
    IEnumerator LensDistortionSequence()
    {
        if (_lensDistortion == null) yield break;

        // 1단계: 0 → 0.3 (팽창)
        float elapsed = 0f;
        while (elapsed < distortionUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / distortionUpDuration;
            
            _lensDistortion.intensity.value = Mathf.Lerp(0f, distortionPeakValue, t);
            yield return null;
        }
        
        _lensDistortion.intensity.value = distortionPeakValue;

        // 2단계: 0.3 → -0.7 (수축)
        elapsed = 0f;
        while (elapsed < distortionDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / distortionDownDuration;
            
            _lensDistortion.intensity.value = Mathf.Lerp(distortionPeakValue, distortionMinValue, t);
            yield return null;
        }
        
        _lensDistortion.intensity.value = distortionMinValue;

        // 3단계: -0.7 → 0 (복귀)
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

    /// <summary>
    /// Hue Shift 0 → 180 → 0 → 흑백 순서
    /// </summary>
    IEnumerator HueShiftSequence()
    {
        if (_colorAdjustments == null) yield break;

        // 1단계: Hue Shift 0 → 180 (무지개 색상 변화)
        float elapsed = 0f;
        while (elapsed < hueShiftDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hueShiftDuration;
            
            _colorAdjustments.hueShift.value = Mathf.Lerp(0f, maxHueShift, t);
            yield return null;
        }
        
        _colorAdjustments.hueShift.value = maxHueShift;
        
        // 2단계: Hue Shift 180 → 0 (빠르게 복귀)
        elapsed = 0f;
        while (elapsed < hueResetDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hueResetDuration;
            
            // 빠르게 복귀
            _colorAdjustments.hueShift.value = Mathf.Lerp(maxHueShift, 0f, t);
            yield return null;
        }
        
        _colorAdjustments.hueShift.value = 0f;
        
        // 3단계: 흑백으로 전환
        yield return StartCoroutine(TransitionSaturation(timeStopSaturation, saturationTransitionSpeed));
    }

    /// <summary>
    /// Chromatic Aberration 펄스 효과
    /// </summary>
    IEnumerator ChromaticAberrationPulse()
    {
        if (_chromaticAberration == null) yield break;

        float elapsed = 0f;
        float pulseDuration = hueShiftDuration + hueResetDuration;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.PingPong(elapsed * chromaticSpeed, 1f);
            _chromaticAberration.intensity.value = Mathf.Lerp(0f, maxChromaticIntensity, t);
            yield return null;
        }

        // 시간정지 중에는 약하게 유지
        _chromaticAberration.intensity.value = maxChromaticIntensity * 0.3f;
    }

    /// <summary>
    /// Vignette 전환
    /// </summary>
    IEnumerator TransitionVignette(float targetIntensity, float speed)
    {
        if (_vignette == null) yield break;

        float currentIntensity = _vignette.intensity.value;

        while (Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.unscaledDeltaTime * speed);
            _vignette.intensity.value = currentIntensity;
            yield return null;
        }

        _vignette.intensity.value = targetIntensity;
    }

    IEnumerator TimeStopDuration(float duration)
    {
        // ✅ WaitForSecondsRealtime 사용 (timeScale 영향 안받음)
        yield return new WaitForSecondsRealtime(duration);

        // 시간정지 해제
        DeactivateTimeStop();
    }

    private void DeactivateTimeStop()
    {
        // 시간정지 종료
        TimeStopManager.Instance.StopTimeStop();

        // 비주얼 복구
        StartCoroutine(TransitionSaturation(normalSaturation, saturationTransitionSpeed));

        // Hue Shift 초기화
        if (_colorAdjustments != null)
        {
            _colorAdjustments.hueShift.value = 0f;
        }

        // Chromatic Aberration 복구
        if (_chromaticAberration != null)
        {
            StartCoroutine(TransitionChromaticAberration(0f, chromaticSpeed));
        }

        // Vignette 복구
        if (_vignette != null)
        {
            StartCoroutine(TransitionVignette(0f, vignetteSpeed));
        }

        // Lens Distortion 초기화 (혹시 모를 잔여값 제거)
        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = 0f;
        }

        Debug.Log("[Skill] Time Stop ended!");
    }

    /// <summary>
    /// Chromatic Aberration 전환
    /// </summary>
    IEnumerator TransitionChromaticAberration(float targetIntensity, float speed)
    {
        if (_chromaticAberration == null) yield break;

        float currentIntensity = _chromaticAberration.intensity.value;

        while (Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.unscaledDeltaTime * speed);
            _chromaticAberration.intensity.value = currentIntensity;
            yield return null;
        }

        _chromaticAberration.intensity.value = targetIntensity;
    }

    /// <summary>
    /// 부드러운 Saturation 전환
    /// </summary>
    IEnumerator TransitionSaturation(float targetSaturation, float speed)
    {
        if (_colorAdjustments == null) yield break;

        float currentSaturation = _colorAdjustments.saturation.value;

        while (Mathf.Abs(currentSaturation - targetSaturation) > 0.1f)
        {
            currentSaturation = Mathf.Lerp(currentSaturation, targetSaturation, Time.unscaledDeltaTime * speed);
            _colorAdjustments.saturation.value = currentSaturation;
            yield return null;
        }

        _colorAdjustments.saturation.value = targetSaturation;
    }

    public void SetSaturation(float newSaturation)
    {
        if (_colorAdjustments != null)
        {
            _colorAdjustments.saturation.value = newSaturation;
        }
    }

    // UI용 쿨타임 프로퍼티
    public float CooldownProgress => Mathf.Clamp01(curTime / coolTime);
    public bool IsOnCooldown => curTime < coolTime;
}