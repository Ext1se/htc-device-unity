using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowFPSMobile : MonoBehaviour
{
    // provided by Niter88
    private string fps = "";

    private WaitForSecondsRealtime waitForFrequency;

    GUIStyle style = new GUIStyle();
    Rect rect;

    bool isInicialized = false;


    private void Awake()
    {
        Inicialize(true);
    }

    private IEnumerator FPS()
    {
        int lastFrameCount;
        float lastTime;
        float timeSpan;
        int frameCount;
        for (; ; )
        {
            // Capture frame-per-second
            lastFrameCount = Time.frameCount;
            lastTime = Time.realtimeSinceStartup;
            yield return waitForFrequency;
            timeSpan = Time.realtimeSinceStartup - lastTime;
            frameCount = Time.frameCount - lastFrameCount;

            fps = string.Format("FPS: {0}", Mathf.RoundToInt(frameCount / timeSpan));
        }
    }


    void OnGUI()
    {
        GUI.Label(rect, fps, style);
    }

    private void Inicialize(bool showFps)
    {
        isInicialized = true;

        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = Screen.height * 3 / 100;
        style.normal.textColor = new Color32(0, 200, 0, 255);
        rect = new Rect(0, 5, 0, Screen.height * 2 / 100);

        if (showFps)
            StartCoroutine(FPS());
    }


    public void SetNewConfig(GraphicSettingsMB gSettings)
    {
        Application.targetFrameRate = gSettings.targetFrameRate;

        waitForFrequency = new WaitForSecondsRealtime(gSettings.testFpsFrequency);

        if (!isInicialized) Inicialize(gSettings.showFps);

        if (!gSettings.showFps)
            Destroy(this.gameObject);
    }
}

[SerializeField]
public class GraphicSettingsMB
{
    public byte targetFrameRate = 30;
    public byte testFpsFrequency = 1;
    public bool showFps = false;
}