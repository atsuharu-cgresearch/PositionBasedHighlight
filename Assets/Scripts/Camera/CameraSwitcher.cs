using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    private Camera[] cameraArray;
    private int activeCamIndex = 0;

    public Camera ActiveCam => cameraArray[activeCamIndex];

    private void Start()
    {
        // 自分の子オブジェクトの中から、Cameraコンポーネントをすべて取得
        cameraArray = GetComponentsInChildren<Camera>();

        Switch(0);
    }

    /// <summary>
    /// 
    /// </summary>
    private void Switch(int index)
    {
        activeCamIndex = Mathf.Clamp(index, 0, cameraArray.Length - 1);

        SwitchCam();
    }

    /// <summary>
    /// activeCamIndex番のカメラがシーンのレンダリングに使用されるようにする
    /// </summary>
    private void SwitchCam()
    {
        // 全てのカメラを無効化
        for (int i = 0; i < cameraArray.Length; i++)
        {
            cameraArray[i].gameObject.SetActive(false);
        }

        cameraArray[activeCamIndex].gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Switch(activeCamIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Switch(activeCamIndex - 1);
        }
    }
}
