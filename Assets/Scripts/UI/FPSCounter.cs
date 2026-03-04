using UnityEngine;
using TMPro; // TextMeshProを使う場合

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    void Update()
    {
        // 前フレームとの差分を蓄積
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float fps = 1.0f / deltaTime;
        fpsText.text = string.Format("{0:0.0} FPS", fps);
    }
}