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
    public class InputSlot : IReadOnlySlot
    {
        public TargetMesh target;
        public Occluder occluder;
        public List<InputElement> elements;
        [Range(0, 1)] public float response = 0.3f;
        [Range(0, 2)] public float curvature = 1.0f;
    }

    public interface IReadOnlySlot
    {

    }

    public interface IReadOnlyInput
    {
        bool IsActive { get; }
        List<InputSlot> Slots { get; }
        int DepthRTSize { get; }
        int ColliderRTSize { get; }
        int RendererRTSize { get; }
        PBDSolver.IReadOnlyParameter ReadOnlyParameter { get; }

        Vector3 GetActiveCamPos();
        Quaternion GetActiveCamRot();
        Vector3 GetActiveLightPos();
        Quaternion GetActiveLightRot();
    }

    public class HighlightInput : MonoBehaviour, IReadOnlyInput
    {
        [SerializeField] private bool isActive = true;

        [SerializeField] private List<InputSlot> slots = new List<InputSlot>();

        [SerializeField] private TextureSize depthRTSize = TextureSize.TEX_SIZE_512;
        [SerializeField] private TextureSize colliderSize = TextureSize.TEX_SIZE_512;
        [SerializeField] private TextureSize renderSize = TextureSize.TEX_SIZE_1024;

        [SerializeField] private CameraSwitcher camSwitcher;
        [SerializeField] private LightController lightController;

        [SerializeField] private PBDSolver.Parameter solverParameter;

        // Systemクラスで値が変更されないようにするため、インターフェースを使って以下のプロパティのみ参照させる
        public bool IsActive { get { return isActive; } }
        public List<InputSlot> Slots { get { return slots; } }
        public int DepthRTSize { get { return (int)depthRTSize; } }
        public int ColliderRTSize { get { return (int)colliderSize; } }
        public int RendererRTSize { get { return (int)renderSize; } }
        public PBDSolver.IReadOnlyParameter ReadOnlyParameter => solverParameter;

        public Vector3 GetActiveCamPos()
        {
            return camSwitcher.ActiveCam.transform.position;
        }

        public Quaternion GetActiveCamRot()
        {
            return camSwitcher.ActiveCam.transform.rotation;
        }

        public Vector3 GetActiveLightPos()
        {
            return lightController.ActiveLight.transform.position;
        }

        public Quaternion GetActiveLightRot()
        {
            return lightController.ActiveLight.transform.rotation;
        }

        private void Start()
        {

        }

        private void Update()
        {
            // カメラ、仮想ライト、シミュレーション用パラメータの変更を常に監視しておく
        }
    }
}
