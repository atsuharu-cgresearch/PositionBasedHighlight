using UnityEngine;
using UnityEngine.Rendering;

namespace PositionBasedHighlight
{
    /// <summary>
    /// CommandBufferを使用して、メッシュを直接RenderTextureに描画する
    /// Cameraコンポーネントを使用する方法よりも早い
    /// </summary>
    public class MeshDirectRenderer
    {
        private Mesh[] meshes;
        private Material[] materials;

        private CommandBuffer cmd;

        public MeshDirectRenderer(Mesh[] meshArray, Material[] materialArray)
        {
            meshes = meshArray;

            materials = materialArray;

            cmd = new CommandBuffer { name = "DrawHighlightMeshes" };
        }

        public void DrawMesh(ref RenderTexture rt, Vector2 pos, float rotationZ, float camSize)
        {
            cmd.Clear();

            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.clear);

            // コライダーのTransformをカメラにコピーし、直交投影（Orthographic）でメッシュを描画するイメージ
            // 実際にはCameraコンポーネントは使わず、CommandBufferで直接描画する

            // Projection行列（サイズ）
            // Orthographicカメラの 'Size' は画面中心から上端までの距離のため、
            // 上下左右の範囲は-size～size
            Matrix4x4 ortho = Matrix4x4.Ortho(-camSize, camSize, -camSize, camSize, -100f, 100f);
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(ortho, true);

            // View行列 (位置と回転)
            Quaternion rotation = Quaternion.Euler(0, 0, rotationZ);
            Matrix4x4 cameraTRS = Matrix4x4.TRS(new Vector3(pos.x, pos.y, -1f), rotation, Vector3.one);

            // カメラの逆行列がView行列になる
            Matrix4x4 viewMatrix = cameraTRS.inverse;

            // 行列をセット
            cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            // 描画実行
            for (int i = 0; i < meshes.Length; i++)
            {
                // メッシュの座標は動かさない
                cmd.DrawMesh(meshes[i], Matrix4x4.identity, materials[i], 0, 0);
            }

            Graphics.ExecuteCommandBuffer(cmd);

            cmd.Clear();
        }

        public void Release()
        {
            if (cmd != null)
            {
                cmd.Release();
                cmd = null;
            }
        }
    }
}