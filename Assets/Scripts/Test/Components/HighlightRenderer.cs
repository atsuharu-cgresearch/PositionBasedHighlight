using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// シミュレーション結果をもとにUVテクスチャを生成し、TargetMeshに渡す
    /// </summary>
    public class HighlightRenderer
    {
        private ComputeShader compute;
        private int kMain;

        private ComputeBuffer particleBuffer;
        private ComputeBuffer edgeIndexBuffer;
        private RenderTexture resultRT;

        public HighlightRenderer(int texSize, SimulationObjectDefinition[] defs, ComputeBuffer particles, ObjectToParticles[] references)
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/ParticleToHighlight"));
            kMain = compute.FindKernel("CS_Main");

            
            List<int> edgeIndexListAll = new List<int>();
            for (int j = 0; j < defs.Length; j++)
            {
                for (int i = 0; i < defs[j].edgeIndices.Length; i++)
                {
                    edgeIndexListAll.Add(defs[j].edgeIndices[i] + references[j].pStart);
                }
                edgeIndexListAll.Add(-1);
                edgeIndexListAll.Add(-1);
            }
            
            edgeIndexBuffer = ComputeHelper.CreateStructuredBuffer(edgeIndexListAll.ToArray());
            ComputeHelper.CreateRenderTexture(ref resultRT, texSize, texSize, RenderTextureFormat.ARGBFloat);
            particleBuffer = particles;

            compute.SetBuffer(kMain, "_Particles", particleBuffer);
            compute.SetBuffer(kMain, "_EdgeIndices", edgeIndexBuffer);
            compute.SetTexture(kMain, "_ResultTexture", resultRT);
            compute.SetInt("_TexSize", resultRT.width);
            compute.SetInt("_NumEdges", edgeIndexBuffer.count / 2);
        }

        public RenderTexture RenderResult(Vector4 textureTransform)
        {
            compute.SetVector("_Transform", textureTransform);

            compute.Dispatch(kMain, Mathf.CeilToInt(resultRT.width / 8f), Mathf.CeilToInt(resultRT.width / 8f), 1);

            return resultRT;
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(edgeIndexBuffer);
        }
    }
}