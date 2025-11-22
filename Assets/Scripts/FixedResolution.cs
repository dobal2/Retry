using UnityEngine;
using UnityEngine.UI;

public class FixedResolution : MonoBehaviour
{
    public int targetWidth = 1920;
    public int targetHeight = 1080;
    
    [Header("Screen Space Camera Settings")]
    [SerializeField] private Camera uiCamera;
    [SerializeField] private float planeDistance = 100f;
    
    private Canvas canvas;
    private CanvasScaler scaler;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas != null)
        {
            scaler = canvas.GetComponent<CanvasScaler>();
            
            // Screen Space - Camera 설정
            if (uiCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = planeDistance;
            }
        }
        
        AdjustCanvas();
    }

    private void Update()
    {
        // 해상도 변경 시에만 업데이트
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustCanvas();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    private void AdjustCanvas()
    {
        if (scaler == null) return;
        
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(targetWidth, targetHeight);
        
        float screenAspect = (float)Screen.width / Screen.height;
        float targetAspect = (float)targetWidth / targetHeight;

        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = (screenAspect >= targetAspect) ? 1 : 0;
    }
}