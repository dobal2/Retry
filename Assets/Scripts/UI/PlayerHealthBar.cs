using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    private Slider hpSlider;
    private TextMeshProUGUI hpText;

    private void Start()
    {
        GameObject healthBarObj = GameObject.Find("HealthBar");
        if (healthBarObj != null)
        {
            hpSlider = healthBarObj.GetComponent<Slider>();
            hpText = healthBarObj.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        float currentHp = PlayerStats.Instance.GetCurrentHealth();
        float maxHp = PlayerStats.Instance.GetMaxHealth();
        
        if (hpText != null)
            hpText.text = $"{currentHp:F0}/{maxHp:F0}";
        
        if (hpSlider != null)
            hpSlider.value = PlayerStats.Instance.GetHealthRatio();
        
    }
    
}