#if DEVELOPMENT_BUILD || UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System.Diagnostics;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DevProfilerFull : MonoBehaviour
{
    public Font font;

    [Header("VR Settings")]
    public bool isVRMode = false;
    public float vrDistance = 2.0f;
    public float vrHeightOffset = 0.0f;
    public float vrScale = 0.002f;

    private Canvas profilerCanvas;
    private Text fpsText, gpuText, drawText, batchText, vertsText;
    private Text usedMemText, peakMemText, limitMemText, managedMemText, nativeMemText, gcText, apiText, gpuNameText;
    private Image barUsed, barPeak;
    private FrameTiming[] frameTimings = new FrameTiming[1];
    private float deltaTime = 0.0f;
    private float peakMemory = 0;
    private List<Image> frameHistory = new List<Image>();
    private int historyIndex = 0;
    private const int historyLength = 20;

    private float lastTime;
    private float lastTotalProcessorTime;
    private TextMeshProUGUI cpuLabel;

    public enum Language { Turkish, English, Arabic }
    public Language currentLanguage = Language.Turkish;

    private readonly Dictionary<Language, Dictionary<string, string>> texts = new()
    {
        [Language.Turkish] = new()
        {
            ["FPS"] = "FPS",
            ["GPU"] = "GPU",
            ["DrawCalls"] = "Çizim",
            ["Batches"] = "Batch",
            ["Verts"] = "Vertex",
            ["Used"] = "Kullanılan",
            ["Peak"] = "Zirve",
            ["Limit"] = "Limit",
            ["Managed"] = "Yönetilen",
            ["Native"] = "Native",
            ["GC"] = "GC Toplamaları",
            ["API"] = "API",
            ["GPUName"] = "GPU",
            ["CPU"] = "CPU Kullanımı"
        },
        [Language.English] = new()
        {
            ["FPS"] = "FPS",
            ["GPU"] = "GPU",
            ["DrawCalls"] = "Draw Calls",
            ["Batches"] = "Batches",
            ["Verts"] = "Verts",
            ["Used"] = "Used",
            ["Peak"] = "Peak",
            ["Limit"] = "Limit",
            ["Managed"] = "Managed",
            ["Native"] = "Native",
            ["GC"] = "GC Collections",
            ["API"] = "API",
            ["GPUName"] = "GPU",
            ["CPU"] = "CPU Usage"
        },
        [Language.Arabic] = new()
        {
            ["FPS"] = "معدل الإطارات",
            ["GPU"] = "المعالج الرسومي",
            ["DrawCalls"] = "استدعاءات الرسم",
            ["Batches"] = "دفعات",
            ["Verts"] = "رؤوس",
            ["Used"] = "المستخدم",
            ["Peak"] = "الذروة",
            ["Limit"] = "الحد",
            ["Managed"] = "مدار",
            ["Native"] = "أصلي",
            ["GC"] = "تجميع القمامة",
            ["API"] = "واجهة برمجة التطبيقات",
            ["GPUName"] = "المعالج الرسومي",
            ["CPU"] = "استخدام المعالج"
        }
    };

    void Start()
    {
        CreateUI();

        Process process = Process.GetCurrentProcess();
        lastTime = Time.realtimeSinceStartup;
        lastTotalProcessorTime = (float)process.TotalProcessorTime.TotalSeconds;
    }

    void Update()
    {
        if (isVRMode && profilerCanvas != null && profilerCanvas.transform.parent == null)
        {
            AttachToCamera();
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        float ms = 1000f / Mathf.Max(fps, 0.0001f);

        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);
        float gpuTime = count > 0 ? (float)frameTimings[0].gpuFrameTime : -1f;

#if UNITY_EDITOR
        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int batches = UnityEditor.UnityStats.batches;
        int verts = UnityEditor.UnityStats.vertices;
#else
        int drawCalls = -1, batches = -1, verts = -1;
#endif

        float used = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        peakMemory = Mathf.Max(peakMemory, used);
        float limit = SystemInfo.systemMemorySize;
        float managed = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
        float native = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
        int gcCount = System.GC.CollectionCount(0);

        string api = SystemInfo.graphicsDeviceType.ToString();
        string gpuName = SystemInfo.graphicsDeviceName;

        Color fpsColor = fps >= 50 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);

        var t = texts[currentLanguage];

        fpsText.text = $"{t["FPS"]}: {fps:F1} ({ms:F1}ms)";
        fpsText.color = fpsColor;

        gpuText.text = $"{t["GPU"]}: {(gpuTime > 0 ? gpuTime.ToString("F1") + "ms" : "N/A")}";
        drawText.text = $"{t["DrawCalls"]}: {drawCalls}";
        batchText.text = $"{t["Batches"]}: {batches}";
        vertsText.text = $"{t["Verts"]}: {verts / 1000f:F1}k";

        usedMemText.text = $"{t["Used"]}: {used:F1}MB";
        peakMemText.text = $"{t["Peak"]}: {peakMemory:F1}MB";
        limitMemText.text = $"{t["Limit"]}: {limit:F1}MB";
        managedMemText.text = $"{t["Managed"]}: {managed:F1}MB";
        nativeMemText.text = $"{t["Native"]}: {native:F1}MB";
        gcText.text = $"{t["GC"]}: {gcCount}";
        apiText.text = $"{t["API"]}: {api}";
        gpuNameText.text = $"{t["GPUName"]}: {gpuName}";

        UpdateCPUUsageLabel(t["CPU"]);

        barUsed.fillAmount = Mathf.Clamp01(used / limit);
        barPeak.fillAmount = Mathf.Clamp01(peakMemory / limit);

        UpdateFrameHistory(fps);
    }

    void UpdateFrameHistory(float fps)
    {
        if (frameHistory.Count == 0) return;
        Color color = fps >= 50 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
        frameHistory[historyIndex].color = color;
        historyIndex = (historyIndex + 1) % historyLength;
    }

    void UpdateCPUUsageLabel(string label)
    {
        Process process = Process.GetCurrentProcess();
        float currentTime = Time.realtimeSinceStartup;
        float currentTotalProcessorTime = (float)process.TotalProcessorTime.TotalSeconds;

        float cpuUsage = 100f * (currentTotalProcessorTime - lastTotalProcessorTime) / (currentTime - lastTime) / SystemInfo.processorCount;
        cpuLabel.text = $"{label}: %{cpuUsage:F2}";

        lastTime = currentTime;
        lastTotalProcessorTime = currentTotalProcessorTime;
    }

    void CreateUI()
    {
        GameObject canvasObj = new GameObject("ProfilerCanvas");
        profilerCanvas = canvasObj.AddComponent<Canvas>();
        if (isVRMode)
        {
            profilerCanvas.renderMode = RenderMode.WorldSpace;
            AttachToCamera();
        }
        else
        {
            profilerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = CreatePanel(canvasObj.transform, new Vector2(450, 430), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10));

        float y = -10;

        fpsText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 25;
        gpuText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 25;

        CreateFrameHistoryBar(panel.transform, new Vector2(10, y)); y -= 30;

        drawText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        batchText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        vertsText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 30;

        CreateMemoryBar(panel.transform, new Vector2(10, y), 400, 20); y -= 25;

        usedMemText = CreateLabel(panel.transform, new Vector2(10, y));
        peakMemText = CreateLabel(panel.transform, new Vector2(170, y));
        limitMemText = CreateLabel(panel.transform, new Vector2(300, y)); y -= 25;

        managedMemText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        nativeMemText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        gcText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        apiText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;
        gpuNameText = CreateLabel(panel.transform, new Vector2(10, y)); y -= 20;

        cpuLabel = CreateCPULabel(panel.transform, new Vector2(10, y));
    }

    GameObject CreatePanel(Transform parent, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject panel = new GameObject("ProfilerPanel");
        panel.transform.SetParent(parent);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        return panel;
    }

    Text CreateLabel(Transform parent, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(420, 20);

        Text txt = obj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.text = "...";
        return txt;
    }

    TextMeshProUGUI CreateCPULabel(Transform parent, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject("CPULabel", typeof(RectTransform));
        obj.transform.SetParent(parent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 24);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "CPU: ...";
        tmp.fontSize = 18;
        tmp.color = Color.green;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return tmp;
    }

    void CreateMemoryBar(Transform parent, Vector2 pos, float width, float height)
    {
        GameObject bg = new GameObject("MemBG");
        bg.transform.SetParent(parent);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = bgRT.pivot = new Vector2(0, 1);
        bgRT.anchoredPosition = pos;
        bgRT.sizeDelta = new Vector2(width, height);
        bg.AddComponent<Image>().color = Color.gray;

        barUsed = CreateBar(bg.transform, Color.cyan);
        barPeak = CreateBar(bg.transform, new Color(1f, 0.5f, 0f));
    }

    Image CreateBar(Transform parent, Color color)
    {
        GameObject bar = new GameObject("Bar");
        bar.transform.SetParent(parent);
        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image img = bar.AddComponent<Image>();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 0f;
        return img;
    }

    void CreateFrameHistoryBar(Transform parent, Vector2 startPos)
    {
        float size = 12;
        float spacing = 2;
        for (int i = 0; i < historyLength; i++)
        {
            GameObject box = new GameObject("FrameBox");
            box.transform.SetParent(parent);
            RectTransform rt = box.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = startPos + new Vector2(i * (size + spacing), 0);
            rt.sizeDelta = new Vector2(size, size);
            Image img = box.AddComponent<Image>();
            img.color = Color.black;
            frameHistory.Add(img);
        }
    }

    void AttachToCamera()
    {
        Camera cam = Camera.main;
        if (cam != null && profilerCanvas != null)
        {
            Transform canvasTransform = profilerCanvas.transform;
            canvasTransform.SetParent(cam.transform);
            canvasTransform.localPosition = new Vector3(0, vrHeightOffset, vrDistance);
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = new Vector3(vrScale, vrScale, vrScale);
        }
    }
}

#endif