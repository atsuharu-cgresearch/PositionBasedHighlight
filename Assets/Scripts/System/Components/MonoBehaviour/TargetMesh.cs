using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// ハイライトを入れるメッシュにこのコンポーネントをアタッチする
    /// 主な役割↓
    /// ・メッシュの現在の状態の計算、保持
    /// ・マスク済みUVマップ生成のため、ブレンドシェイプとボーンの適用結果を自分で計算
    /// ・UVテクスチャを受け取り、マテリアルにセットして描画
    /// </summary>
    public class TargetMesh : MonoBehaviour
    {
        // ローカル座標系（U-V-N）
        [SerializeField] private Vector3 originUVN = Vector3.zero;
        [SerializeField] private Quaternion rotationUVN = Quaternion.identity;
        // [SerializeField] private Vector3 scale;

        // シェイプキーのコピー元
        [SerializeField] private SkinnedMeshRenderer srcSmr;
        [SerializeField] private Transform myBone;
        private SkinnedMeshRenderer smr;
        private BlendShapeBonesSelf blendShapeBoneSelf;

        private Material mat;

        public Vector3 WorldOriginUVN => transform.localToWorldMatrix.MultiplyPoint3x4(originUVN);
        public Quaternion WorldRotationUVN => transform.rotation * rotationUVN;

        public Vector2[] MeshUVs { get; private set; }
        public int[] MeshTriangles { get; private set; }

        private void Awake()
        {
            smr = GetComponent<SkinnedMeshRenderer>();

            CopyBlendShapes(srcSmr, smr);

            MeshUVs = (Vector2[])smr.sharedMesh.uv.Clone();
            MeshTriangles = (int[])smr.sharedMesh.triangles.Clone();

            blendShapeBoneSelf = new BlendShapeBonesSelf(smr, myBone);

            mat = GetComponent<Renderer>().material;
        }

        /// <summary>
        /// もともとStartメソッドで実行していたが、
        /// UVMaskGenerator初期化時にBlendShapeBoneSelfがすでに初期化されている必要があり、Startメソッドの中に書くと実行順序によってはエラーになる。
        /// なので、UVMaskGeneratorが、必要なタイミングでこれを実行する。
        /// </summary>
        public void SetupBlendShapeBoneSelf()
        {

        }

        private void Update()
        {
            CopyBlendShapes(srcSmr, smr);
        }

        /// <summary>
        /// VRMの管理対称の範囲外なので、手動でSkinnedMeshRendererのパラメータをコピーする
        /// </summary>
        private void CopyBlendShapes(SkinnedMeshRenderer src, SkinnedMeshRenderer dst)
        {
            int count = src.sharedMesh.blendShapeCount;

            for (int i = 0; i < count; i++)
            {
                float weight = src.GetBlendShapeWeight(i);
                dst.SetBlendShapeWeight(i, weight);
            }
        }

        /// <summary>
        /// ブレンドシェイプとボーンによる変形後のメッシュのデータを高速に取得する方法がないので、自分で計算する
        /// </summary>
        public ComputeBuffer CalculateBlendShapeBoneSelf()
        {
            return blendShapeBoneSelf.Calculate();
        }

        public void RenderResult(RenderTexture rt)
        {
            mat.SetTexture("_MainTex", rt);
        }

        private void OnDestroy()
        {
            blendShapeBoneSelf.ReleaseBuffers();
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
