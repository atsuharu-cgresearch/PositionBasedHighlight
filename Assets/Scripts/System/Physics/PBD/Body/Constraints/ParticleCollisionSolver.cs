using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class ParticleCollisionSolver
    {
        private ComputeShader compute;
        private int kMain;
        private int threadGroups;

        private ComputeBuffer particleBuffer;
        private ComputeBuffer objectIndexBuffer;
        private ComputeBuffer layerBuffer;
        private ComputeBuffer objectParticleRangeBuffer;
        private ComputeBuffer layerParticleRangeBuffer;

        public struct ParticleRange
        {
            public int start;
            public int count;

            public ParticleRange(int start, int count)
            {
                this.start = start;
                this.count = count;
            }
        }

        public ParticleCollisionSolver(ComputeBuffer particleBuffer, ComputeBuffer objectIndexBuffer, ComputeBuffer layerBuffer)
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/ParticleCollision");
            if (compute == null)
            {
                Debug.LogError("ComputeShaderがありません");
            }
            kMain = compute.FindKernel("CS_Main");

            this.particleBuffer = particleBuffer;
            this.objectIndexBuffer = objectIndexBuffer;
            this.layerBuffer = layerBuffer;

            CreateRangeBuffers(objectIndexBuffer, layerBuffer);

            compute.SetBuffer(kMain, "_Particles", particleBuffer);
            compute.SetBuffer(kMain, "_ObjectIndices", objectIndexBuffer);
            compute.SetBuffer(kMain, "_Layers", layerBuffer);
            compute.SetBuffer(kMain, "_ObjectParticleRanges", objectParticleRangeBuffer);
            compute.SetBuffer(kMain, "_LayerParticleRanges", layerParticleRangeBuffer);

            compute.SetInt("_NumParticles", particleBuffer.count);

            threadGroups = Mathf.CeilToInt(particleBuffer.count / 64f);
        }

        private void CreateRangeBuffers(ComputeBuffer objectIndexBuffer, ComputeBuffer layerBuffer)
        {
            int[] objectIndices = new int[objectIndexBuffer.count];
            int[] layers = new int[layerBuffer.count];
            objectIndexBuffer.GetData(objectIndices);
            layerBuffer.GetData(layers);

            List<ParticleRange> objectParticleRangeList = new List<ParticleRange>();
            List<ParticleRange> layerParticleRangeList = new List<ParticleRange>();

            int prevObj = -1;
            int prevLayer = -1;
            int objStart = 0;
            int layerStart = 0;
            for (int i = 0; i < particleBuffer.count; i++)
            {
                int obj = objectIndices[i];
                int layer = layers[i];

                if (prevObj != obj)
                {
                    objStart = i;
                }

                if (i == particleBuffer.count - 1 || objectIndices[i + 1] != obj)
                {
                    objectParticleRangeList.Add(new ParticleRange(objStart, i - objStart + 1));
                }

                if (prevLayer != layer)
                {
                    layerStart = i;
                }

                if (i == particleBuffer.count - 1 || layers[i + 1] != layer)
                {
                    layerParticleRangeList.Add(new ParticleRange(layerStart, i - layerStart + 1));
                }

                prevObj = obj;
                prevLayer = layer;
            }

            objectParticleRangeBuffer = ComputeHelper.CreateStructuredBuffer(objectParticleRangeList.ToArray());
            layerParticleRangeBuffer = ComputeHelper.CreateStructuredBuffer(layerParticleRangeList.ToArray());
        }

        public void ConstrainPositions(float k, float radius)
        {
            // 動的に変更されるパラメータは引数で受け取ってここで設定する
            compute.SetFloat("_K", k);
            compute.SetFloat("_Radius", radius);

            // 拘束条件を解いて、パーティクルの位置を修正
            compute.Dispatch(kMain, threadGroups, 1, 1);
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(
                objectParticleRangeBuffer,
                layerParticleRangeBuffer
                );
        }
    }
}
