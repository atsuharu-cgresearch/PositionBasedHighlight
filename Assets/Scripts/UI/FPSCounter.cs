using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    // 直近10秒の記録用
    [SerializeField] private float averageWindow = 10.0f; // 平均を取る期間（秒）
    private Queue<float> frameTimeSamples = new Queue<float>();
    private Queue<float> timeStamps = new Queue<float>();

    void Update()
    {
        // 前フレームとの差分を蓄積
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float currentFps = 1.0f / deltaTime;
        float currentFrameTimeMs = deltaTime * 1000.0f; // ミリ秒に変換

        // サンプルを追加
        float currentTime = Time.unscaledTime;
        frameTimeSamples.Enqueue(deltaTime);
        timeStamps.Enqueue(currentTime);

        // 古いサンプルを削除（10秒より前のもの）
        while (timeStamps.Count > 0 && currentTime - timeStamps.Peek() > averageWindow)
        {
            timeStamps.Dequeue();
            frameTimeSamples.Dequeue();
        }

        // 平均FPS・処理時間を計算
        float sumFrameTime = 0.0f;
        foreach (float ft in frameTimeSamples)
        {
            sumFrameTime += ft;
        }

        int sampleCount = frameTimeSamples.Count;
        float averageFrameTime = sumFrameTime / sampleCount;
        float averageFps = 1.0f / averageFrameTime;
        float averageFrameTimeMs = averageFrameTime * 1000.0f;

        // 表示（2行）
        fpsText.text = string.Format("{0:0.0} FPS (avg: {1:0.0})\n{2:0.00} ms (avg: {3:0.00})",
            currentFps, averageFps, currentFrameTimeMs, averageFrameTimeMs);
    }

    // リセット機能
    public void ResetAverage()
    {
        frameTimeSamples.Clear();
        timeStamps.Clear();
    }
}
