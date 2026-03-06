using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// 目以外の、遮蔽物を使用しないメッシュに使用し、メッシュのUVマップを生成する
    /// </summary>
    public class UVMapGenerator
    {
        private TargetMesh target;

        private ComputeShader compute;

        private int kernelGenMap;

        private ComputeBuffer uvsBuffer;
        private ComputeBuffer trianglesBuffer;
        private RenderTexture mapTexture;

        public UVMapGenerator(TargetMesh tgt, int texSize)
        {
            target = tgt;

            InitCS(texSize);
        }

        private void InitCS(int texSize)
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/UVMap"));

            kernelGenMap = compute.FindKernel("GenMap");

            // バッファの生成
            uvsBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshUVs);
            trianglesBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshTriangles);
            ComputeHelper.CreateRenderTexture(ref mapTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);

            // バッファと変数のセット
            ComputeHelper.SetBuffer(compute, uvsBuffer, "UVs", kernelGenMap);
            ComputeHelper.SetBuffer(compute, trianglesBuffer, "Triangles", kernelGenMap);
            ComputeHelper.AssignTexture(compute, mapTexture, "UVMap", kernelGenMap);
            compute.SetInt("_TexSize", mapTexture.width);
            compute.SetInt("_NumTriangles", trianglesBuffer.count / 3);
        }

        public RenderTexture Generate()
        {
            compute.Dispatch(kernelGenMap, Mathf.CeilToInt(mapTexture.width / 8f), Mathf.CeilToInt(mapTexture.width / 8f), 1);
            
            return mapTexture;
        }

        public RenderTexture GetUVMap()
        {
            return mapTexture;
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(uvsBuffer, trianglesBuffer);
        }
    }
}
