using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HS_ProjectileHolder : MonoBehaviour
{
    [Header("Visual Effects")] 
    [SerializeField] protected GameObject flash;
    [SerializeField] protected ParticleSystem projectilePS;
    [SerializeField] protected Light lightSource;
    
    [Header("Settings")]
    [SerializeField] protected bool autoPlayOnEnable = true;
    [SerializeField] protected bool lightEnabled = true;

    protected bool startChecker = false;

    protected virtual void Start()
    {
        if (!startChecker)
        {
            // Flash 이펙트가 있으면 부모에서 분리
            if (flash != null)
            {
                flash.transform.parent = null;
            }

            // 라이트 초기 설정
            if (lightSource != null)
            {
                lightSource.enabled = lightEnabled;
            }

            // 파티클 자동 재생
            if (autoPlayOnEnable && projectilePS != null)
            {
                projectilePS.Play();
            }
        }
        startChecker = true;
    }

    protected virtual void OnEnable()
    {
        if (startChecker)
        {
            // Flash 이펙트 재설정
            if (flash != null)
            {
                flash.transform.parent = null;
            }

            // 라이트 활성화
            if (lightSource != null)
            {
                lightSource.enabled = lightEnabled;
            }

            // 파티클 재생
            if (autoPlayOnEnable && projectilePS != null)
            {
                projectilePS.Play();
            }
        }
    }

    protected virtual void OnDisable()
    {
        // 파티클 정지
        if (projectilePS != null)
        {
            projectilePS.Stop();
        }

        // 라이트 비활성화
        if (lightSource != null)
        {
            lightSource.enabled = false;
        }
    }

    // 수동으로 이펙트 재생
    public virtual void PlayEffect()
    {
        if (projectilePS != null)
        {
            projectilePS.Play();
        }

        if (lightSource != null)
        {
            lightSource.enabled = true;
        }
    }

    // 수동으로 이펙트 정지
    public virtual void StopEffect()
    {
        if (projectilePS != null)
        {
            projectilePS.Stop();
        }

        if (lightSource != null)
        {
            lightSource.enabled = false;
        }
    }

    // 라이트 토글
    public virtual void ToggleLight(bool enable)
    {
        lightEnabled = enable;
        if (lightSource != null)
        {
            lightSource.enabled = enable;
        }
    }
}