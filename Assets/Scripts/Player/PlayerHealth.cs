using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private Slider hpSlider;
    private TextMeshProUGUI hpText;
    
    private Animator anim;
    public bool died;

    private void Start()
    {
        anim = GetComponent<Animator>();
        
        // ★ Canvas의 HealthBar에서 Slider 찾기
        GameObject healthBarObj = GameObject.Find("HealthBar");
        if (healthBarObj != null)
        {
            hpSlider = healthBarObj.GetComponent<Slider>();
            
            // ★ HealthBar의 자식에서 TextMeshProUGUI 찾기
            hpText = healthBarObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (hpSlider == null)
                Debug.LogWarning("<color=yellow>[PlayerHealth] HealthBar에 Slider 컴포넌트가 없습니다!</color>");
            
            if (hpText == null)
                Debug.LogWarning("<color=yellow>[PlayerHealth] HealthBar에 TextMeshProUGUI가 없습니다!</color>");
        }
        else
        {
            Debug.LogError("<color=red>[PlayerHealth] HealthBar 오브젝트를 찾을 수 없습니다!</color>");
        }
    }

    public void TakeDamage(float damage)
    {
        // ★ PlayerStats의 TakeDamage 사용
        PlayerStats.Instance.TakeDamage(damage);
    }

    private void Die()
    {
        died = true;
        //anim.SetTrigger("Die"); // 사망 애니메이션 트리거 (있다면)
        Debug.Log("<color=red>[PlayerHealth] 플레이어 사망!</color>");
    }

    private void Update()
    {
        // ★ PlayerStats에서 HP 값 가져오기
        float currentHp = PlayerStats.Instance.GetCurrentHealth();
        float maxHp = PlayerStats.Instance.GetMaxHealth();
        
        // UI 업데이트 (null 체크)
        if (hpText != null)
            hpText.text = $"{currentHp:F0}/{maxHp:F0}";
        
        if (hpSlider != null)
            hpSlider.value = PlayerStats.Instance.GetHealthRatio();
        
        if(died)
            return;

        // ★ PlayerStats의 IsAlive() 체크
        if (!PlayerStats.Instance.IsAlive())
        {
            Die();
        }
    }
}