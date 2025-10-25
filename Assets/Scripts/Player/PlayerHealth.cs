using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHp;
    [SerializeField] private float hp;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    
    private Animator anim;
    public bool died;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
    }

    private void Die()
    {
        died = true;
    }

    private void Update()
    {
        hpText.text = hp+"/"+maxHp;
        hpSlider.value = hp/maxHp;
        
        if(died)
            return;

        if (hp <= 0)
        {
            Die();
        }
    }
}