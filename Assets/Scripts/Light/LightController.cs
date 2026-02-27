using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightController : MonoBehaviour
{
    private VirtualLight[] lightArray;
    private int activeIndex = 0;

    public VirtualLight ActiveLight => lightArray[activeIndex];

    private void Start()
    {
        // 自分の子オブジェクトの中から、VirtualLightコンポーネントをすべて取得
        lightArray = GetComponentsInChildren<VirtualLight>();

        Switch(0);
    }

    /// <summary>
    /// 
    /// </summary>
    private void Switch(int index)
    {
        activeIndex = Mathf.Clamp(index, 0, lightArray.Length - 1);

        SwitchCam();
    }

    /// <summary>
    /// activeCamIndex番のカメラがシーンのレンダリングに使用されるようにする
    /// </summary>
    private void SwitchCam()
    {
        // 全てのカメラを無効化
        for (int i = 0; i < lightArray.Length; i++)
        {
            lightArray[i].gameObject.SetActive(false);
        }

        lightArray[activeIndex].gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Switch(activeIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Switch(activeIndex - 1);
        }
    }
}
