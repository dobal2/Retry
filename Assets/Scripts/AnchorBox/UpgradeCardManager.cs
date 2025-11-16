using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class UpgradeCardManager : MonoBehaviour
{
    private static UpgradeCardManager instance;
    public static UpgradeCardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UpgradeCardManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UpgradeCardManager");
                    instance = go.AddComponent<UpgradeCardManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("Card Prefab")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private RectTransform cardParent;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float canvasPlaneDistance = 0.3f;
    
    [Header("Spawn Settings")]
    [SerializeField] private float cardSpacing = 400f;
    [SerializeField] private float cardYOffset = 0f;
    
    [Header("Upgrade Weights")]
    [SerializeField] private UpgradeWeight[] upgradeWeights;
    
    private List<UpgradeCard> activeCards = new List<UpgradeCard>();
    private bool isSelecting = false;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;
    
    [System.Serializable]
    public class UpgradeWeight
    {
        public UpgradeCard.UpgradeType type;
        public float weight = 1f;
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        if (upgradeWeights == null || upgradeWeights.Length == 0)
        {
            InitializeDefaultWeights();
        }
    }
    
    private void Start()
    {
        if (targetCanvas == null)
        {
            targetCanvas = cardParent?.GetComponentInParent<Canvas>();
        }
        
        if (targetCanvas != null)
        {
            StartCoroutine(DelayPlaneDistance(1));
        }
        
        if (cardParent != null)
        {
            cardParent.gameObject.SetActive(false);
        }
    }

    IEnumerator DelayPlaneDistance(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetCanvas.planeDistance = canvasPlaneDistance;
    }
    
    private void InitializeDefaultWeights()
    {
        int typeCount = System.Enum.GetValues(typeof(UpgradeCard.UpgradeType)).Length;
        upgradeWeights = new UpgradeWeight[typeCount];
        
        for (int i = 0; i < typeCount; i++)
        {
            upgradeWeights[i] = new UpgradeWeight
            {
                type = (UpgradeCard.UpgradeType)i,
                weight = 1f
            };
        }
    }
    
    public void SpawnCards(int count)
    {
        if (isSelecting)
        {
            Debug.LogWarning("Already selecting cards!");
            return;
        }
        
        if (cardPrefab == null)
        {
            Debug.LogError("Card prefab not assigned!");
            return;
        }
        
        if (cardParent == null)
        {
            Debug.LogError("Card parent not assigned!");
            return;
        }
        
        ClearActiveCards();
        
        isSelecting = true;
        cardParent.gameObject.SetActive(true);
        
        // 시간 멈추기
        Time.timeScale = 0f;
        
        // 마우스 커서 해제
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        for (int i = 0; i < count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            
            if (cardRect != null)
            {
                float xPos = 0f;
                
                if (count == 3)
                {
                    if (i == 0) xPos = -cardSpacing;
                    else if (i == 1) xPos = 0f;
                    else if (i == 2) xPos = cardSpacing;
                }
                else
                {
                    float startX = -(count - 1) * cardSpacing * 0.5f;
                    xPos = startX + i * cardSpacing;
                }
                
                cardRect.anchoredPosition = new Vector2(xPos, cardYOffset);
            }
            
            UpgradeCard card = cardObj.GetComponent<UpgradeCard>();
            
            if (card != null)
            {
                UpgradeCard.UpgradeType randomType = GetRandomUpgradeType();
                card.Initialize(randomType, this);
                activeCards.Add(card);
            }
        }
        
        Debug.Log($"Spawned {count} upgrade cards");
    }
    
    private UpgradeCard.UpgradeType GetRandomUpgradeType()
    {
        float totalWeight = 0f;
        foreach (var uw in upgradeWeights)
        {
            totalWeight += uw.weight;
        }
        
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        
        foreach (var uw in upgradeWeights)
        {
            cumulative += uw.weight;
            if (random <= cumulative)
            {
                return uw.type;
            }
        }
        
        return upgradeWeights[0].type;
    }
    
    public void OnCardSelected(UpgradeCard selectedCard)
    {
        selectedCard.ApplyUpgrade();
        
        foreach (var card in activeCards)
        {
            if (card != null && card != selectedCard)
            {
                Destroy(card.gameObject);
            }
        }
        
        activeCards.Clear();
        isSelecting = false;
        
        cardParent.gameObject.SetActive(false);
        
        // 시간 복구
        Time.timeScale = 1f;
        
        // 마우스 커서 복구
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
        
        Debug.Log("Card selection complete");
    }
    
    private void ClearActiveCards()
    {
        foreach (var card in activeCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        activeCards.Clear();
    }
    
    public bool IsSelecting() => isSelecting;
    public int GetActiveCardCount() => activeCards.Count;
    
    public void SetUpgradeWeight(UpgradeCard.UpgradeType type, float weight)
    {
        foreach (var uw in upgradeWeights)
        {
            if (uw.type == type)
            {
                uw.weight = Mathf.Max(0f, weight);
                return;
            }
        }
    }
}