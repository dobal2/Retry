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
    [SerializeField] private float saturationTransitionSpeed = 15f;
    [SerializeField] private float saturationRestoreSpeed = 2f;
    
    [Header("Hue Shift Effect")]
    [SerializeField] private bool useHueShift = true;
    [SerializeField] private float hueShiftDuration = 0.5f;
    [SerializeField] private float hueResetDuration = 0.3f;
    [SerializeField] private float maxHueShift = 180f;
    
    [Header("Lens Distortion Effect")]
    [SerializeField] private bool useLensDistortion = true;
    [SerializeField] private float distortionPeakValue = 0.3f;
    [SerializeField] private float distortionMinValue = -0.7f;
    [SerializeField] private float distortionUpDuration = 0.2f;
    [SerializeField] private float distortionDownDuration = 0.3f;
    [SerializeField] private float distortionResetDuration = 0.3f;
    
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

    [Header("Sound Settings")]
    [SerializeField] private float clockSoundInterval = 1f;
    
    [Header("Volumetric Fog")]
    [SerializeField] private Material volumetricFogMaterial;
    [SerializeField] private float fogColorTransitionSpeed = 5f; 
    
    private Coroutine fogColorCoroutine;

    private Coroutine timeStopCoroutine;
    private Coroutine clockSoundCoroutine;
    private Coroutine saturationCoroutine; // ✅ Saturation 코루틴 추적
    private bool isTimeStopActive = false;

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
        
        if (volumetricFogMaterial != null)
        {
            volumetricFogMaterial.SetFloat("_ColorBlend", 0f);
        }
        else
        {
            Debug.LogWarning("Volumetric Fog Material not assigned!");
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
        SoundManager.Instance.PlaySfx(SoundManager.Sfx.TimeStop);
        curTime = 0f;

        // 카메라 쉐이크
        if (cameraShaker != null && shakeData != null)
        {
            cameraShaker.Shake(shakeData);
        }

        // ✅ 모든 이펙트를 순차적으로 실행
        StartCoroutine(TimeStopEffectSequence());
    }

    IEnumerator TimeStopEffectSequence()
    {
        // ✅ 이전 Saturation 코루틴 중단
        if (saturationCoroutine != null)
        {
            StopCoroutine(saturationCoroutine);
            saturationCoroutine = null;
        }

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
        
        if (volumetricFogMaterial != null)
        {
            if (fogColorCoroutine != null)
            {
                StopCoroutine(fogColorCoroutine);
            }
            fogColorCoroutine = StartCoroutine(TransitionFogColor(1f, fogColorTransitionSpeed));
        }

        // Hue Shift → 흑백 시작
        if (useHueShift)
        {
            hueShiftCoroutine = StartCoroutine(HueShiftSequence());
        }
        else
        {
            saturationCoroutine = StartCoroutine(TransitionSaturation(timeStopSaturation, saturationTransitionSpeed));
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
        isTimeStopActive = true;
        TimeStopManager.Instance.StartTimeStop();
        Debug.Log("[Skill] Time Stop activated!");

        // ✅ 시계 소리 반복 재생 시작
        if (clockSoundCoroutine != null)
        {
            StopCoroutine(clockSoundCoroutine);
        }
        clockSoundCoroutine = StartCoroutine(PlayClockSoundLoop());

        // 지속시간 후 종료
        if (timeStopCoroutine != null)
        {
            StopCoroutine(timeStopCoroutine);
        }
        timeStopCoroutine = StartCoroutine(TimeStopDuration(timeStopDuration));
    }

    IEnumerator PlayClockSoundLoop()
    {
        while (isTimeStopActive)
        {
            SoundManager.Instance.PlaySfx(SoundManager.Sfx.ClockSound);
            
            // 대기 중에도 상태 체크
            float elapsed = 0f;
            while (elapsed < clockSoundInterval && isTimeStopActive)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        
        clockSoundCoroutine = null;
        Debug.Log("[Skill] Clock sound loop ended");
    }

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

    IEnumerator HueShiftSequence()
    {
        if (_colorAdjustments == null) yield break;

        // ✅ 이전 Saturation 코루틴이 있다면 중단
        if (saturationCoroutine != null)
        {
            StopCoroutine(saturationCoroutine);
            saturationCoroutine = null;
        }

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

        // 2단계: Hue Shift 180 → 0 (빠르게 복귀) + 0.2초 전에 흑백 전환 시작
        elapsed = 0f;
        bool saturationStarted = false;
        float saturationStartTime = hueResetDuration - 0.3f; // 끝나기 0.2초 전
    
        if (saturationStartTime < 0f)
            saturationStartTime = 0f;

        while (elapsed < hueResetDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hueResetDuration;
    
            _colorAdjustments.hueShift.value = Mathf.Lerp(maxHueShift, 0f, t);
        
            // ✅ 0.2초 전에 흑백 전환 시작
            if (!saturationStarted && elapsed >= saturationStartTime)
            {
                saturationStarted = true;
                saturationCoroutine = StartCoroutine(TransitionSaturation(timeStopSaturation, saturationTransitionSpeed));
                Debug.Log("[Skill] Saturation transition started (0.2s before hue reset ends)");
            }
        
            yield return null;
        }

        _colorAdjustments.hueShift.value = 0f;
    
        // ✅ 혹시 트랜지션이 시작 안됐으면 여기서라도 시작
        if (!saturationStarted)
        {
            saturationCoroutine = StartCoroutine(TransitionSaturation(timeStopSaturation, saturationTransitionSpeed));
            Debug.Log("[Skill] Saturation transition started (fallback)");
        }
    }

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
        yield return new WaitForSecondsRealtime(duration);
        DeactivateTimeStop();
    }

    private void DeactivateTimeStop()
    {
        // ✅ 먼저 플래그를 false로 설정
        isTimeStopActive = false;
        Debug.Log("[Skill] Time Stop flag set to false");
        
        // ✅ 시계 소리 코루틴 중단
        if (clockSoundCoroutine != null)
        {
            StopCoroutine(clockSoundCoroutine);
            clockSoundCoroutine = null;
        }
        
        // ✅ 재생 중인 모든 시계 소리를 즉시 중단
        SoundManager.Instance.StopAllClockSounds();

        // ✅ 시간정지 종료
        TimeStopManager.Instance.StopTimeStop();

        // ✅ 이전 Saturation 코루틴 중단 후 새로 시작
        if (saturationCoroutine != null)
        {
            StopCoroutine(saturationCoroutine);
        }
        saturationCoroutine = StartCoroutine(TransitionSaturation(normalSaturation, saturationRestoreSpeed));

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

        // Lens Distortion 초기화
        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = 0f;
        }
        
        if (volumetricFogMaterial != null)
        {
            if (fogColorCoroutine != null)
            {
                StopCoroutine(fogColorCoroutine);
            }
            fogColorCoroutine = StartCoroutine(TransitionFogColor(0f, fogColorTransitionSpeed));
        }

        Debug.Log("[Skill] Time Stop ended!");
    }
    
    IEnumerator TransitionFogColor(float targetBlend, float speed)
    {
        if (volumetricFogMaterial == null) yield break;

        float currentBlend = volumetricFogMaterial.GetFloat("_ColorBlend");

        while (Mathf.Abs(currentBlend - targetBlend) > 0.01f)
        {
            currentBlend = Mathf.Lerp(currentBlend, targetBlend, Time.unscaledDeltaTime * speed);
            volumetricFogMaterial.SetFloat("_ColorBlend", currentBlend);
            yield return null;
        }

        volumetricFogMaterial.SetFloat("_ColorBlend", targetBlend);
        fogColorCoroutine = null;
    }

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
        saturationCoroutine = null; // ✅ 완료 시 null로 설정
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