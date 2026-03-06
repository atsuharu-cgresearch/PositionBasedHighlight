using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class AreaConstraint : ConstraintCommon
    {
        public struct AreaConstCluster
        {
            public float restArea;
            public int pStart;
        }

        public AreaConstraint(ComputeBuffer particleBuffer, int[] indexArray)
        {
            AreaConstCluster[] clusters = CreateClusters(particleBuffer, indexArray);

            Initialize("ComputeShader/AreaConstraint", clusters, indexArray, particleBuffer);
        }

        private AreaConstCluster[] CreateClusters(ComputeBuffer particleBuffer, int[] indexArray)
        {
            // バッファの中身を配列にコピーして取得
            ParticleData[] particles = new ParticleData[particleBuffer.count];
            particleBuffer.GetData(particles);

            int numClusters = indexArray.Length / 3;

            AreaConstCluster[] clusters = new AreaConstCluster[numClusters];
            int pOffset = 0;
            for (int i = 0; i < numClusters; i++)
            {
                AreaConstCluster cluster = new AreaConstCluster();
                cluster.pStart = pOffset;

                int id0 = indexArray[cluster.pStart];
                int id1 = indexArray[cluster.pStart + 1];
                int id2 = indexArray[cluster.pStart + 2];
                Vector2 v1 = particles[id1].position - particles[id0].position;
                Vector2 v2 = particles[id2].position - particles[id0].position;
                cluster.restArea = 0.5f * (v1.x * v2.y - v1.y * v2.x);

                clusters[i] = cluster; // Debug.Log(cluster.restArea);

                pOffset += 3;
            }

            return clusters;
        }
    }
}
