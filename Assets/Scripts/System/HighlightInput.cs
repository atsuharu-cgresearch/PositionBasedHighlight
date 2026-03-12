using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PositionBasedHighlight;

namespace PositionBasedHighlight
{
    [System.Serializable]
    public struct Transform2D
    {
        public Vector2 pos;
        public float rot;
        public float scale;

        public Transform2D(Vector2 p, float r, float s)
        {
            pos = p;
            rot = r;
            scale = s;
        }
    }

    public enum TextureSize
    {
        TEX_SIZE_128 = 128,
        TEX_SIZE_256 = 256,
        TEX_SIZE_512 = 512,
        TEX_SIZE_1024 = 1024,
        TEX_SIZE_2048 = 2048,
    }

    [System.Serializable]
    public class InputElement
    {
        public HighlightType type = 0;
        public Color color = Color.white;
        public Transform2D transform = new Transform2D(new Vector2(0, 0), 0, 1);
    }

    [System.Serializable]
    public class InputSlot
    {
        public TargetMesh target;
        public Occluder occluder;
        public List<InputElement> elements;
        [Range(0, 1)] public float response = 0.3f;
        [Range(0, 2)] public float curvature = 1.0f;
    }

    /// <summary>
    /// 外部からのパラメータや設定の入力・変更を扱うクラス
    /// </summary>
    public class HighlightInput : MonoBehaviour
    {
        public Transform cameraTransform;
        public Transform lightTransform;
        public PBDSolver.Parameter solverParameter;

        // Systemクラスで値が変更されないようにするため、インターフェースを使って以下のプロパティのみ参照させる
        public bool isActive;
        public List<InputSlot> slots;
        public int depthRTSize;
        public int colliderRTSize;
        public int rendererRTSize;

        private void Start()
        {

        }

        private void Update()
        {
            
        }
    }
}
