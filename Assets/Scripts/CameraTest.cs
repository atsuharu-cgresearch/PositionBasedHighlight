using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTest : MonoBehaviour
{
    private void Start()
    {
        Camera cam = GetComponent<Camera>();

        cam.aspect = 16 / 9f;
    }
}
