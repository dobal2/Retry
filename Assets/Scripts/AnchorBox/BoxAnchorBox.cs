using UnityEngine;

public class BoxAnchorBox : InteractableAnchorBox
{
    [Header("Box Settings")]
    [SerializeField] private int baseOpenCost = 50;
    [SerializeField] private float costMultiplier = 1.5f;
    
    [Header("Card Spawn")]
    [SerializeField] private int cardsToSpawn = 3;
    
    // 모든 박스가 공유하는 static 변수
    private static int globalTimesOpened = 0;
    
    private int CurrentCost => Mathf.RoundToInt(baseOpenCost * Mathf.Pow(costMultiplier, globalTimesOpened));
    
    protected override void OnInteract()
    {
        if (PlayerStats.Instance == null) return;
        if (UpgradeCardManager.Instance == null) return;
        
        int cost = CurrentCost;
        PlayerStats.Instance.ConsumeEnergy(cost);
        globalTimesOpened++;
        
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
    public static int GetGlobalTimesOpened() => globalTimesOpened;
    
    // 게임 리셋 시 호출
    public static void ResetGlobalCount()
    {
        globalTimesOpened = 0;
    }
}