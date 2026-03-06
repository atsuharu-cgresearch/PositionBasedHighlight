using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// 外部から渡されたデータを保持しておくクラス
    /// </summary>
    public class ExternalDataPool
    {
        public RenderTexture[] SDFArray { get; private set; }
        public Vector4[] ColliderTransforms { get; private set; }
        public Vector4[] TargetPosTransforms { get; private set; }

        public ExternalDataPool(int maxLayers)
        {
            SDFArray = new RenderTexture[maxLayers];

            ColliderTransforms = new Vector4[maxLayers];

            TargetPosTransforms = new Vector4[maxLayers];
        }

        // 
        public void SetCollider(RenderTexture collider, Vector4 transform, int index)
        {
            SDFArray[index] = collider;

            ColliderTransforms[index] = transform;
        }

        public void SetTargetPosOffset(Vector4 transform, int index)
        {
            TargetPosTransforms[index] = transform;
        }
    }
}
