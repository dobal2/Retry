using InfimaGames.LowPolyShooterPack;
using UnityEngine;


public class TimeSkip : MonoBehaviour
{
    private Rigidbody rigid;
    private Movement movement;
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float cooldownTimer;
    private float curTimer;
    
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        movement = GetComponent<Movement>();
    }

    void Update()
    {
        curTimer += Time.deltaTime;
        if (cooldownTimer <= curTimer)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StartCoroutine(DashCoroutine());
                curTimer = 0;
            }
        }
        
    }
    
    private System.Collections.IEnumerator DashCoroutine()
    {
        movement.SetDashing(true);
        rigid.AddForce(transform.forward * dashForce, ForceMode.VelocityChange);
        
        yield return new WaitForSeconds(dashDuration);
        
        movement.SetDashing(false);
    }
}