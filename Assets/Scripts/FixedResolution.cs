using System;
using UnityEngine;
using UnityEngine.UI;

public class FixedResolution: MonoBehaviour
{
    public int targetWidth = 1920;
    public int targetHeight = 1080; // 목표 높이

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AdjustCanvas();
    }

    private void Update()
    {
        AdjustCanvas();
    }

    /* Canvas Scaler 설정을 목표 해상도에 맞추어 조정하는 함수 */
    private void AdjustCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Canvas Scaler 설정
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(targetWidth, targetHeight);
                
                // 화면 비율에 따라 맞춤 방식 설정
                float screenAspect = (float)Screen.width / Screen.height;
                float targetAspect = (float)targetWidth / targetHeight;

                // 기기의 화면 비율이 목표 비율보다 더 넓을 때는 너비 기준, 좁을 때는 높이 기준
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = (screenAspect >= targetAspect) ? 1 : 0;
            }
        }
    }
}