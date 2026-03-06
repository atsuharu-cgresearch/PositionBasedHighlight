using UnityEngine;
using UnityEngine.Rendering;

namespace PositionBasedHighlight
{
    public class CameraMeshSetController2
    {
        private Mesh[] meshes;
        private Material[] materials;
        private CommandBuffer cmd;

        // UpdateCameraで受け取ったサイズを保持
        private float currentCamSize;

        public void Initialize(Mesh[] meshArray, Material[] materialArray)
        {
            meshes = meshArray;
            materials = materialArray;

            cmd = new CommandBuffer { name = "DrawHighlightMeshes" };
        }

        public void UpdateCamera(Vector2 offset, float size)
        {
            // サイズだけ保持しておき、計算はDrawMeshで行います
            currentCamSize = size;
        }

        public void DrawMesh(RenderTexture rt, Vector2 offset)
        {
            cmd.Clear();

            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.clear);

            // 1. 直交投影（Orthographic）の範囲に、カメラのオフセットを直接組み込む
            float left = offset.x - currentCamSize;
            float right = offset.x + currentCamSize;
            float bottom = offset.y - currentCamSize;
            float top = offset.y + currentCamSize;

            // Zの範囲は -100 ～ 100 など広く取っておきます。
            // これでオブジェクト(Z=0)が確実にクリッピング範囲に入ります。
            Matrix4x4 ortho = Matrix4x4.Ortho(left, right, bottom, top, -100f, 100f);

            // RenderTexture描画用のプラットフォーム差異（上下反転など）を吸収
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(ortho, true);

            // 2. View行列は位置をProjectionに組み込んだため「単位行列（移動・回転なし）」でOK！
            Matrix4x4 viewMatrix = Matrix4x4.identity;

            // 行列をセット
            cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            // 3. メッシュを描画
            for (int i = 0; i < meshes.Length; i++)
            {
                cmd.DrawMesh(meshes[i], Matrix4x4.identity, materials[i], 0, 0);
            }

            Graphics.ExecuteCommandBuffer(cmd);
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