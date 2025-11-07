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
    private Volume volume;  // ★ 메인 Volume
    [SerializeField] private float damageEffectDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private AnimationCurve damageEffectCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private Animator anim;
    public bool died;
    private Coroutine damageEffectCoroutine;
    private Vignette vignette;  // ★ Vignette 컴포넌트
    private float originalVignetteIntensity;  // ★ 원래 비네팅 값

    private void Start()
    {
        anim = GetComponent<Animator>();
        
        if (volume == null)
        {
            volume = FindObjectOfType<Volume>();
        }
        
        // ★ Volume에서 Vignette 가져오기
        if (volume != null && volume.profile.TryGet(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
        }
        else
        {
            Debug.LogWarning("NoVolume For take damage effect");
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
        // 카메라 흔들림
        shaker.Shake(takeDamageShake);
        
        // 데미지 적용
        PlayerStats.Instance.TakeDamage(damage);
        
        // ★ 데미지 화면 효과 재생
        PlayDamageEffect();
    }

    /// <summary>
    /// 데미지 화면 효과 재생
    /// </summary>
    private void PlayDamageEffect()
    {
        if (vignette == null) return;
        
        // 이전 효과가 재생 중이면 중단
        if (damageEffectCoroutine != null)
        {
            StopCoroutine(damageEffectCoroutine);
        }
        
        // 새 효과 시작
        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    /// <summary>
    /// 데미지 효과 코루틴 (Vignette intensity를 시간에 따라 조절)
    /// </summary>
    private IEnumerator DamageEffectCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < damageEffectDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / damageEffectDuration;
            
            // AnimationCurve를 사용해서 부드러운 감쇠
            float curveValue = damageEffectCurve.Evaluate(normalizedTime);
            vignette.intensity.value = originalVignetteIntensity + (maxVignetteIntensity * curveValue);
            
            yield return null;
        }
        
        // 원래 값으로 복원
        vignette.intensity.value = originalVignetteIntensity;
        damageEffectCoroutine = null;
    }

    private void Die()
    {
        died = true;
        //anim.SetTrigger("Die");
    }

    private void Update()
    {
        // PlayerStats에서 HP 값 가져오기
        float currentHp = PlayerStats.Instance.GetCurrentHealth();
        float maxHp = PlayerStats.Instance.GetMaxHealth();
        
        // UI 업데이트
        if (hpText != null)
            hpText.text = $"{currentHp:F0}/{maxHp:F0}";
        
        if (hpSlider != null)
            hpSlider.value = PlayerStats.Instance.GetHealthRatio();
        
        if(died)
            return;

        // 사망 체크
        if (!PlayerStats.Instance.IsAlive())
        {
            Die();
        }
    }

    private void OnDestroy()
    {
        // ★ 오브젝트 파괴 시 원래 값으로 복원
        if (vignette != null)
        {
            vignette.intensity.value = originalVignetteIntensity;
        }
    }
}