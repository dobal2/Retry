using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    private static LoadingUI instance;

    public static LoadingUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LoadingUI>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShowLoading(string status = "Loading...")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        UpdateProgress(0f);
    }

    public void UpdateProgress(float progress, string status = null)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        if (status != null && statusText != null)
        {
            statusText.text = status;
        }
    }

    public void HideLoading()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}