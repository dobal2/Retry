using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Settings")]
    [SerializeField] private float minDisplayTime = 1.5f;
    [SerializeField] private float completeDelay = 0.5f;
    [SerializeField] private bool debugMode = true; // ★ 디버그 켜기
    
    private static LoadingUI instance;
    private float loadingStartTime;
    private float lastProgressUpdateTime;
    private bool isCurrentlyLoading = false;
    private Coroutine hideCoroutine; // ★ 코루틴 참조 저장

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

    public bool IsLoading => isCurrentlyLoading;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log("[LoadingUI] Instance created");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShowLoading(string status = "Loading...")
    {
        // ★ 이전 숨기기 코루틴 중지
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        loadingStartTime = Time.realtimeSinceStartup;
        lastProgressUpdateTime = Time.realtimeSinceStartup;
        isCurrentlyLoading = true;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        UpdateProgress(0f);
        
        if (debugMode)
        {
            Debug.Log($"[LoadingUI] ShowLoading: {status}");
        }
    }

    public void UpdateProgress(float progress, string status = null)
    {
        lastProgressUpdateTime = Time.realtimeSinceStartup;
        
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
        
        if (debugMode)
        {
            Debug.Log($"[LoadingUI] Progress: {progress:F2} - {status}");
        }
    }

    public void HideLoading()
    {
        if (!isCurrentlyLoading)
        {
            Debug.LogWarning("[LoadingUI] HideLoading called but not loading!");
            return;
        }
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        hideCoroutine = StartCoroutine(HideLoadingCoroutine());
    }
    
    private IEnumerator HideLoadingCoroutine()
    {
        if (debugMode)
        {
            Debug.Log("[LoadingUI] Starting hide sequence...");
        }
        
        // ★ 1. 최소 표시 시간 체크
        float elapsedTime = Time.realtimeSinceStartup - loadingStartTime;
        if (elapsedTime < minDisplayTime)
        {
            float waitTime = minDisplayTime - elapsedTime;
            if (debugMode)
            {
                Debug.Log($"[LoadingUI] Waiting for min display time: {waitTime:F2}s");
            }
            yield return new WaitForSecondsRealtime(waitTime);
        }
        
        // ★ 2. 100% 도달 확인
        int maxWaitFrames = 300;
        int waitFrames = 0;
        
        while (progressBar != null && progressBar.value < 0.99f && waitFrames < maxWaitFrames)
        {
            waitFrames++;
            if (debugMode && waitFrames % 60 == 0)
            {
                Debug.Log($"[LoadingUI] Waiting for 100%... Current: {progressBar.value:F2}");
            }
            yield return null;
        }
        
        // ★ 3. 완료 후 추가 대기
        if (debugMode)
        {
            Debug.Log($"[LoadingUI] Complete delay: {completeDelay}s");
        }
        yield return new WaitForSecondsRealtime(completeDelay);
        
        // ★ 4. 최종 숨기기
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        isCurrentlyLoading = false;
        hideCoroutine = null;
        
        if (debugMode)
        {
            float totalTime = Time.realtimeSinceStartup - loadingStartTime;
            Debug.Log($"[LoadingUI] Hidden! Total time: {totalTime:F2}s");
        }
    }
    
    public void ForceHide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        isCurrentlyLoading = false;
        
        if (debugMode)
        {
            Debug.Log("[LoadingUI] Force hidden!");
        }
    }
    
    private void Update()
    {
        if (isCurrentlyLoading)
        {
            float totalTime = Time.realtimeSinceStartup - loadingStartTime;
            
            if (totalTime > 30f && debugMode)
            {
                Debug.LogWarning($"[LoadingUI] Loading for {totalTime:F0}s - might be stuck!");
            }
            
            if (totalTime > 60f)
            {
                Debug.LogError("[LoadingUI] Loading timeout! Force hiding...");
                ForceHide();
            }
        }
    }
}