#if DEVELOPMENT_BUILD || UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using TMPro;
using System.Diagnostics;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdvancedVRProfiler : MonoBehaviour
{
    public Transform wristTarget;
    public Vector3 wristOffset = new Vector3(0, 0.05f, 0);
    public Vector3 wristRotationOffset = new Vector3(45, 0, 0);
    public float uiScale = 0.001f;
    
    [Space(10)]
    public int maxDrawCalls = 200;
    public int maxTriangles = 500000;
    public int targetFramerate = 72;

    private Canvas canvas;
    private TextMeshProUGUI statsText;
    private FrameTiming[] timings = new FrameTiming[1];

    private float deltaTime = 0.0f;
    private float peakMem = 0f;

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        GameObject canvasGo = new GameObject("VRProfilerCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<GraphicRaycaster>();
        
        RectTransform rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 350);
        
        GameObject textGo = new GameObject("StatsText");
        textGo.transform.SetParent(canvasGo.transform, false);
        
        statsText = textGo.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 18;
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.color = Color.white;
        
        RectTransform textRt = statsText.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        ApplyWristAttachment();
    }

    void Update()
    {
        if (wristTarget != null && canvas.transform.parent != wristTarget)
        {
            ApplyWristAttachment();
        }

        UpdateStats();
    }

    void ApplyWristAttachment()
    {
        if (wristTarget == null) return;
        
        Transform t = canvas.transform;
        t.SetParent(wristTarget);
        t.localPosition = wristOffset;
        t.localEulerAngles = wristRotationOffset;
        t.localScale = new Vector3(uiScale, uiScale, uiScale);
    }

    void UpdateStats()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        float ms = 1000f / Mathf.Max(fps, 0.0001f);

        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, timings);
        float gpuTime = count > 0 ? (float)timings[0].gpuFrameTime : -1f;

        int drawCalls = -1;
        int verts = -1;

#if UNITY_EDITOR
        drawCalls = UnityEditor.UnityStats.drawCalls;
        verts = UnityEditor.UnityStats.vertices;
#endif
        
        float usedMem = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        peakMem = Mathf.Max(peakMem, usedMem);
        
        float batLevel = SystemInfo.batteryLevel;
        string batStatus = SystemInfo.batteryStatus.ToString();
        
        bool isSPI = false;
        if (UnityEngine.XR.XRSettings.enabled)
        {
            isSPI = UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced;
        }

        string fpsColor = fps >= targetFramerate ? "green" : (fps >= targetFramerate - 10 ? "yellow" : "red");
        string dcColor = drawCalls < maxDrawCalls ? "white" : (drawCalls < maxDrawCalls * 1.5f ? "yellow" : "red");
        string vertsColor = verts < maxTriangles ? "white" : (verts < maxTriangles * 1.5f ? "yellow" : "red");
        string spiColor = isSPI ? "green" : "red";
        string batColor = batLevel > 0.2f ? "white" : "red";

        if (batLevel < 0) batLevel = 1.0f; 

        statsText.text = 
            $"<color={fpsColor}>FPS: {fps:F1} ({ms:F1}ms)</color>\n" +
            $"GPU Time: {(gpuTime > 0 ? gpuTime.ToString("F1") + "ms" : "N/A")}\n" +
            $"<color={dcColor}>DrawCalls: {drawCalls}</color>\n" +
            $"<color={vertsColor}>Verts: {(verts > 0 ? (verts / 1000f).ToString("F1") + "k" : "N/A")}</color>\n" +
            $"Mem Used: {usedMem:F1}MB | Peak: {peakMem:F1}MB\n" +
            $"<color={batColor}>Battery: {(batLevel * 100):F0}% [{batStatus}]</color>\n" +
            $"<color={spiColor}>Stereo Mode: {(isSPI ? "SPI" : "Multi-Pass")}</color>";
    }
}

#endif
