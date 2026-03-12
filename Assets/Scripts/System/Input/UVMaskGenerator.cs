using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class UVMaskGenerator : IUVMapGenerator
    {
        private struct BarycentricData
        {
            public int id0, id1, id2;
            public float b0, b1, b2;
        }

        private TargetMesh target;
        private Occluder occluder;
        private int texSize;

        private ComputeShader compute;

        private int kernelCalcBarycentric;
        private int kernelGenMask;

        private ComputeBuffer targetUVsBuffer;
        private ComputeBuffer targetTrianglesBuffer;
        private ComputeBuffer targetWorldVerticesBuffer;

        private ComputeBuffer occluderTrianglesBuffer;
        private ComputeBuffer occluderWorldVerticesBuffer;

        private ComputeBuffer barycentricDataBuffer;
        private RenderTexture maskTexture;


        public UVMaskGenerator(TargetMesh tgt, Occluder occ, int texSize)
        {
            target = tgt;
            occluder = occ;
            this.texSize = texSize;

            InitCS();

            PrecomputeBarycentricData();
        }

        private void InitCS()
        {
            // compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/UVMask"));
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/Optimized/UVMask"));

            kernelCalcBarycentric = compute.FindKernel("CalcBarycentric");
            kernelGenMask = compute.FindKernel("GenMask");

            // バッファの生成
            // WorldVerticesBufferは、計算させた結果を取得するのでここでは生成しない
            targetUVsBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshUVs);
            targetTrianglesBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshTriangles);
            occluderTrianglesBuffer = ComputeHelper.CreateStructuredBuffer(occluder.MeshTriangles); Debug.Log("TriangleLength: " + occluderTrianglesBuffer.count);
            ComputeHelper.CreateRenderTexture(ref maskTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);
            barycentricDataBuffer = ComputeHelper.CreateStructuredBuffer<BarycentricData>(maskTexture.width * maskTexture.height);

            // バッファと変数のセット
            ComputeHelper.SetBuffer(compute, targetUVsBuffer, "TargetUVs", kernelCalcBarycentric);
            ComputeHelper.SetBuffer(compute, targetTrianglesBuffer, "TargetTriangles", kernelCalcBarycentric);
            ComputeHelper.SetBuffer(compute, occluderTrianglesBuffer, "OccluderTriangles", kernelGenMask);
            ComputeHelper.SetBuffer(compute, barycentricDataBuffer, "BarycentricDataPixels", kernelCalcBarycentric, kernelGenMask);
            ComputeHelper.AssignTexture(compute, maskTexture, "UVMask", kernelGenMask);
            compute.SetInt("_TexSize", maskTexture.width);
            compute.SetInt("_NumTargetTriangles", targetTrianglesBuffer.count / 3);
            compute.SetInt("_NumOccluderTriangles", occluderTrianglesBuffer.count / 3);
        }

        /// <summary>
        /// マスクテクスチャのピクセルごとに、参照するポリゴンの3頂点のインデックスとバリセントリック座標を計算し保持しておく
        /// </summary>
        private void PrecomputeBarycentricData()
        {
            compute.Dispatch(kernelCalcBarycentric, Mathf.CeilToInt(maskTexture.width / 8f), Mathf.CeilToInt(maskTexture.height / 8f), 1);
        }

        public RenderTexture Generate(Vector3 camPos)
        {
            // Targetの頂点のワールド座標を計算して取得する
            targetWorldVerticesBuffer = target.CalculateBlendShapeBoneSelf();

            // Occluderの頂点のワールド座標を計算して取得する
            occluderWorldVerticesBuffer = occluder.CalculateBlendShapeBoneSelf();

            // UVマップの画素ごとに交差判定を実行し、マスク済みUVマップを作成する
            compute.SetBuffer(kernelGenMask, "TargetWorldVertices", targetWorldVerticesBuffer);
            compute.SetBuffer(kernelGenMask, "OccluderWorldVertices", occluderWorldVerticesBuffer);
            compute.SetVector("_ViewPos", camPos);

            compute.Dispatch(kernelGenMask, Mathf.CeilToInt(maskTexture.width / 8f), Mathf.CeilToInt(maskTexture.height / 8f), 1);

            HighlightDebugger.Instance.DebugTexture(maskTexture);

            return maskTexture;
        }

        public void Release()
        {
            ComputeHelper.Release(
                targetUVsBuffer,
                targetTrianglesBuffer,
                occluderTrianglesBuffer,
                barycentricDataBuffer
                );
        }
    }
}
