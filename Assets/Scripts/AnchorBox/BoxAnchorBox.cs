using UnityEngine;

public class BoxAnchorBox : InteractableAnchorBox
{
    [Header("Box Settings")]
    [SerializeField] private int baseOpenCost = 50;
    [SerializeField] private float costMultiplier = 1.5f;
    [SerializeField] private int timesOpened = 0;
    
    [Header("Card Spawn")]
    [SerializeField] private int cardsToSpawn = 3;
    
    private int CurrentCost => Mathf.RoundToInt(baseOpenCost * Mathf.Pow(costMultiplier, timesOpened));
    
    protected override void OnInteract()
    {
        if (PlayerStats.Instance == null) return;
        if (UpgradeCardManager.Instance == null) return;
        
        int cost = CurrentCost;
        PlayerStats.Instance.ConsumeEnergy(cost);
        timesOpened++;
        
        UpgradeCardManager.Instance.SpawnCards(cardsToSpawn);
        
        Debug.Log($"Box opened! Cost: {cost}, Next cost: {CurrentCost}");
        SoundManager.Instance.PlaySfx(SoundManager.Sfx.OpenChest);
        Destroy(gameObject);
    }
    
    protected override bool CanInteract()
    {
        if (!base.CanInteract()) return false;
        
        if (PlayerStats.Instance == null) return false;
        if (UpgradeCardManager.Instance == null) return false;
        if (UpgradeCardManager.Instance.IsSelecting()) return false;
        
        return PlayerStats.Instance.GetEnergy() >= CurrentCost;
    }
    
    public int GetCurrentCost() => CurrentCost;
    public int GetTimesOpened() => timesOpened;
}