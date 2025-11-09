using System;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviour
{
    private Slider hpSlider;
    private TextMeshProUGUI hpText;
    [SerializeField] private ShakeData takeDamageShake;
    [SerializeField] private CameraShaker shaker;
    
    [Header("Damage Visual Effect")]
    private Volume volume;
    [SerializeField] private float damageEffectDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private AnimationCurve damageEffectCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Material Damage Effect")]
    [SerializeField] private Material damageMaterial; // ★ 데미지 효과 적용할 메터리얼
    private float originalBlurOffset = 0.0025f;
    private float maxBlurOffset = 0.005f;
    private float originalIntensity = 0.4f;
    private float maxIntensity = 1f;
    
    private Animator anim;
    public bool died;
    private Coroutine damageEffectCoroutine;
    private Vignette vignette;
    private float originalVignetteIntensity;

    private void Start()
    {
        anim = GetComponent<Animator>();
        
        if (volume == null)
        {
            volume = FindObjectOfType<Volume>();
        }
        
        if (volume != null && volume.profile.TryGet(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
        }
        else
        {
            Debug.LogWarning("NoVolume For take damage effect");
        }
        
        // ★ Material 초기값 설정
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
        
        GameObject healthBarObj = GameObject.Find("HealthBar");
        if (healthBarObj != null)
        {
            hpSlider = healthBarObj.GetComponent<Slider>();
            hpText = healthBarObj.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void TakeDamage(float damage)
    {
        shaker.Shake(takeDamageShake);
        PlayerStats.Instance.TakeDamage(damage);
        PlayDamageEffect();
    }

    private void PlayDamageEffect()
    {
        if (damageEffectCoroutine != null)
        {
            StopCoroutine(damageEffectCoroutine);
        }
        
        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    private IEnumerator DamageEffectCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < damageEffectDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / damageEffectDuration;
            float curveValue = damageEffectCurve.Evaluate(normalizedTime);
            
            // ★ Vignette 효과
            if (vignette != null)
            {
                vignette.intensity.value = originalVignetteIntensity + (maxVignetteIntensity * curveValue);
            }
            
            // ★ Material 프로퍼티 애니메이션
            if (damageMaterial != null)
            {
                // BlurOffset: 0.0025 -> 0.005 -> 0.0025
                if (damageMaterial.HasProperty("_BlurOffset"))
                {
                    float blurOffset = Mathf.Lerp(originalBlurOffset, maxBlurOffset, curveValue);
                    damageMaterial.SetFloat("_BlurOffset", blurOffset);
                }
                
                // Intensity: 0.5 -> 1.0 -> 0.5
                if (damageMaterial.HasProperty("_Intensity"))
                {
                    float intensity = Mathf.Lerp(originalIntensity, maxIntensity, curveValue);
                    damageMaterial.SetFloat("_Intensity", intensity);
                }
            }
            
            yield return null;
        }
        
        // ★ 원래 값으로 복원
        if (vignette != null)
            vignette.intensity.value = originalVignetteIntensity;
            
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
        
        damageEffectCoroutine = null;
    }

    private void Die()
    {
        died = true;
    }

    private void Update()
    {
        float currentHp = PlayerStats.Instance.GetCurrentHealth();
        float maxHp = PlayerStats.Instance.GetMaxHealth();
        
        if (hpText != null)
            hpText.text = $"{currentHp:F0}/{maxHp:F0}";
        
        if (hpSlider != null)
            hpSlider.value = PlayerStats.Instance.GetHealthRatio();
        
        if(died)
            return;

        if (!PlayerStats.Instance.IsAlive())
        {
            Die();
        }
    }

    private void OnDestroy()
    {
        if (vignette != null)
        {
            vignette.intensity.value = originalVignetteIntensity;
        }
        
        // ★ Material도 원래 값으로 복원
        if (damageMaterial != null)
        {
            if (damageMaterial.HasProperty("_BlurOffset"))
                damageMaterial.SetFloat("_BlurOffset", originalBlurOffset);
            if (damageMaterial.HasProperty("_Intensity"))
                damageMaterial.SetFloat("_Intensity", originalIntensity);
        }
    }
}