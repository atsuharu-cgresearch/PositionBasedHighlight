using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class ShapeMatchConstraint : ConstraintCommon
    {
        public struct Cluster
        {
            public int pStart;
            public int pCount;
        }

        protected ComputeBuffer restPositionBuffer;

        public ShapeMatchConstraint(ComputeBuffer particleBuffer, int[] indexArray, int[] countArray)
        {
            // バッファの中身を配列にコピーして取得
            ParticleData[] particles = new ParticleData[particleBuffer.count];
            particleBuffer.GetData(particles);

            int numClusters = countArray.Length;

            Cluster[] clusters = new Cluster[numClusters];
            int pOffset = 0;
            Vector2[] restPositions = new Vector2[indexArray.Length];
            for (int i = 0; i < numClusters; i++)
            {
                Cluster cluster = new Cluster();
                cluster.pStart = pOffset;
                cluster.pCount = countArray[i];

                Vector2 restCenter = new Vector2(0, 0);
                for (int j = 0; j < countArray[i]; j++)
                {
                    // Debug.Log(indexArray[cluster.pStart + j]);
                    restCenter += particles[indexArray[cluster.pStart + j]].position;
                }

                restCenter /= (float)countArray[i];

                for (int j = 0; j < countArray[i]; j++)
                {
                    restPositions[cluster.pStart + j] = particles[indexArray[cluster.pStart + j]].position - restCenter;
                }

                clusters[i] = cluster;

                pOffset += countArray[i];
            }

            Initialize("ComputeShader/ShapeMatching", clusters, indexArray, particleBuffer);

            restPositionBuffer = ComputeHelper.CreateStructuredBuffer(restPositions);
            compute.SetBuffer(kCalcDelta, "_RestPositions", restPositionBuffer);
        }

        public override void Release()
        {
            base.Release();

            ComputeHelper.Release(restPositionBuffer);
        }
    }

}