using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    public enum UpgradeType
    {
        MaxHealth,
        AttackSpeed,
        ProjectileDamage,
        ProjectileSpeed,
        ProjectileSize,
        MoveSpeed,
        JumpForce,
        ExtraJump
    }
    
    [Header("Upgrade Info")]
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private float upgradeValue;
    [SerializeField] private string upgradeName;
    [SerializeField] private string upgradeDescription;
    
    [Header("Visual")]
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    
    [Header("Button")]
    [SerializeField] private Button selectButton;
    
    [Header("Upgrade Ranges")]
    [SerializeField] private float minHealthBonus = 25f;
    [SerializeField] private float maxHealthBonus = 40f;
    [SerializeField] private float minStatBonus = 0.25f;
    [SerializeField] private float maxStatBonus = 0.4f;
    
    private UpgradeCardManager manager;
    private bool isApplied = false;
    
    private void Awake()
    {
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }
        
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnButtonClicked);
        }
    }
    
    private void OnButtonClicked()
    {
        if (manager != null)
        {
            manager.OnCardSelected(this);
        }
        else
        {
            ApplyUpgrade();
            Destroy(gameObject);
        }
    }
    
    public void Initialize(UpgradeType type, UpgradeCardManager cardManager)
    {
        upgradeType = type;
        manager = cardManager;
        
        GenerateUpgradeValues();
        UpdateUI();
    }
    
    private void GenerateUpgradeValues()
    {
        switch (upgradeType)
        {
            case UpgradeType.MaxHealth:
                upgradeValue = Random.Range(minHealthBonus, maxHealthBonus);
                upgradeName = "Health Boost";
                upgradeDescription = $"Max Health +{upgradeValue:F0}";
                break;
                
            case UpgradeType.AttackSpeed:
                upgradeValue = Random.Range(minStatBonus, maxStatBonus);
                upgradeName = "Attack Speed";
                upgradeDescription = $"Attack Speed +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.ProjectileDamage:
                upgradeValue = Random.Range(minStatBonus, maxStatBonus);
                upgradeName = "Projectile Damage";
                upgradeDescription = $"Projectile Damage +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.ProjectileSpeed:
                upgradeValue = Random.Range(minStatBonus, maxStatBonus);
                upgradeName = "Projectile Speed";
                upgradeDescription = $"Projectile Speed +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.ProjectileSize:
                upgradeValue = Random.Range(minStatBonus*1.3f, maxStatBonus*1.3f);
                upgradeName = "Projectile Size";
                upgradeDescription = $"Projectile Size +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.MoveSpeed:
                upgradeValue = Random.Range(minStatBonus, maxStatBonus);
                upgradeName = "Move Speed";
                upgradeDescription = $"Move Speed +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.JumpForce:
                upgradeValue = Random.Range(minStatBonus, maxStatBonus);
                upgradeName = "Jump Force";
                upgradeDescription = $"Jump Force +{upgradeValue * 100:F0}%";
                break;
                
            case UpgradeType.ExtraJump:
                upgradeValue = 1;
                upgradeName = "Extra Jump";
                upgradeDescription = "Max Jump Count +1";
                break;
        }
    }
    
    private void UpdateUI()
    {
        if (nameText != null) nameText.text = upgradeName;
        if (descriptionText != null) descriptionText.text = upgradeDescription;
    }
    
    public void ApplyUpgrade()
    {
        if (isApplied || PlayerStats.Instance == null) return;
        
        switch (upgradeType)
        {
            case UpgradeType.MaxHealth:
                float newMax = PlayerStats.Instance.GetMaxHealth() + upgradeValue;
                PlayerStats.Instance.SetMaxHealth(newMax);
                PlayerStats.Instance.Heal(upgradeValue);
                break;
                
            case UpgradeType.AttackSpeed:
                PlayerStats.Instance.AddAttackSpeed(upgradeValue);
                break;
                
            case UpgradeType.ProjectileDamage:
                PlayerStats.Instance.AddProjectileDamage(upgradeValue);
                break;
                
            case UpgradeType.ProjectileSpeed:
                PlayerStats.Instance.AddProjectileSpeed(upgradeValue);
                break;
                
            case UpgradeType.ProjectileSize:
                PlayerStats.Instance.AddProjectileSize(upgradeValue);
                break;
                
            case UpgradeType.MoveSpeed:
                PlayerStats.Instance.AddMoveSpeed(upgradeValue);
                break;
                
            case UpgradeType.JumpForce:
                PlayerStats.Instance.AddJumpForce(upgradeValue);
                break;
                
            case UpgradeType.ExtraJump:
                PlayerStats.Instance.AddJumpCount((int)upgradeValue);
                break;
        }
        
        isApplied = true;
        Debug.Log($"Applied upgrade: {upgradeName} - {upgradeDescription}");
    }
    
    public UpgradeType GetUpgradeType() => upgradeType;
    public float GetUpgradeValue() => upgradeValue;
    public string GetUpgradeName() => upgradeName;
    public string GetUpgradeDescription() => upgradeDescription;
}