using System;
using UnityEngine;
using UnityEngine.UI;

public class TimeStopSkillCoolDownUi : MonoBehaviour
{
    [SerializeField] private Image coolDownImage;
    

    private void Update()
    {
        if (PlayerStats.Instance != null)
        {
            var timestopSkill = PlayerStats.Instance.gameObject.GetComponent<TimeStopSkill>();
            coolDownImage.fillAmount = timestopSkill.CooldownProgress;
        }
    }
}
