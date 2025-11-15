using UnityEngine;

public class Box : MonoBehaviour
{
    public int requiredEnergy = 50;
    
    public void OpenBox()
    {
        if (PlayerStats.Instance.GetEnergy() >= requiredEnergy)
        {
            PlayerStats.Instance.ConsumeEnergy(requiredEnergy);
            Debug.Log("Box opened!");
        }
    }
}