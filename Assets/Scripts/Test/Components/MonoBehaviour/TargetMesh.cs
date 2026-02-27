using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class TargetMesh : MonoBehaviour
    {
        // ローカル座標系（U-V-N）
        [SerializeField] private Vector3 originUVN = Vector3.zero;
        [SerializeField] private Quaternion rotationUVN = Quaternion.identity;
        // [SerializeField] private Vector3 scale;

        // シェイプキーのコピー元
        [SerializeField] private SkinnedMeshRenderer srcSmr;

        private SkinnedMeshRenderer smr;
        private Material mat;

        public Vector3 WorldOriginUVN => transform.localToWorldMatrix.MultiplyPoint3x4(originUVN);
        public Quaternion WorldRotationUVN => transform.rotation * rotationUVN;

        private void Start()
        {
            smr = GetComponent<SkinnedMeshRenderer>();
            mat = GetComponent<Renderer>().material;
        }

        private void Update()
        {
            CopyBlendShapes(srcSmr, smr);
        }

        private void CopyBlendShapes(SkinnedMeshRenderer src, SkinnedMeshRenderer dst)
        {
            int count = src.sharedMesh.blendShapeCount;

            for (int i = 0; i < count; i++)
            {
                float weight = src.GetBlendShapeWeight(i);
                dst.SetBlendShapeWeight(i, weight);
            }
        }

        public void RenderResult(RenderTexture rt)
        {
            mat.SetTexture("_MainTex", rt);
        }

        private void OnDrawGizmos()
        {
            DrawTransform();
        }

        private void DrawTransform()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(WorldOriginUVN, 0.001f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.right * 0.05f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.up * 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.forward * 0.05f);
        }
    }
}
