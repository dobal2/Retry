using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class TimeSkip : MonoBehaviour
{
    private Movement movement;
    
    [Header("Time Skip Settings")]
    [SerializeField] private float timeScaleMultiplier = 3f;
    [SerializeField] private float skipDuration = 0.5f;
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
    private bool isSkipping = false;
    
    void Start()
    {
        movement = GetComponent<Movement>();
        
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
        curTimer += Time.unscaledDeltaTime;
        if (cooldownTimer <= curTimer && !isSkipping)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(TimeSkipCoroutine());
                curTimer = 0;
            }
        }
    }
    
    private IEnumerator TimeSkipCoroutine()
    {
        isSkipping = true;
        
        StartCoroutine(LensDistortionSequence());
        StartCoroutine(SaturationSequence());
        
        float originalTimeScale = Time.timeScale;
        float originalFixedDeltaTime = Time.fixedDeltaTime;
        
        Time.timeScale = timeScaleMultiplier;
        Time.fixedDeltaTime = 0.02f * timeScaleMultiplier;
        
        float elapsed = 0f;
        while (elapsed < skipDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        
        isSkipping = false;
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