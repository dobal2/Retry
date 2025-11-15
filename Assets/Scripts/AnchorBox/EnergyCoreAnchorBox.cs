using UnityEngine;

public class EnergyCoreAnchorBox : InteractableAnchorBox
{
    protected override void OnInteract()
    {
        if (ForceFieldManager.Instance == null) return;
        
        if (ForceFieldManager.Instance.TryLevelUpWithEnergy())
        {
            Debug.Log("Energy Core upgraded!");
        }
    }
    
    protected override bool CanInteract()
    {
        if (!base.CanInteract()) return false;
        
        if (ForceFieldManager.Instance == null || PlayerStats.Instance == null)
            return false;
        
        int required = ForceFieldManager.Instance.GetRequiredEnergyForNextLevel();
        return PlayerStats.Instance.GetEnergy() >= required;
    }
}