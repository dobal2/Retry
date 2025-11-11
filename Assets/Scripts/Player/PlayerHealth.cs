using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private Slider hpSlider;
    private TextMeshProUGUI hpText;
    public bool died;

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
        
        if (died)
            return;

        if (!PlayerStats.Instance.IsAlive())
        {
            Die();
        }
    }

    private void Die()
    {
        died = true;
    }
}