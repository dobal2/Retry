using TMPro;
using UnityEngine;

public class EnergyText : MonoBehaviour
{
    private TextMeshProUGUI energyText;

    private void Start()
    {
        GameObject energyTextObj = GameObject.Find("EnergyText");
        if (energyTextObj != null)
        {
            energyText = energyTextObj.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        float currentEnergy = PlayerStats.Instance.GetEnergy();
        
        if (energyText != null)
            energyText.text = $"Energy: {currentEnergy}";
        
    }
}
