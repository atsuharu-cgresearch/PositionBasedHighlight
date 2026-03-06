using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class DistanceConstraint : ConstraintCommon
    {
        public struct DistConstCluster
        {
            public float restDist;
            public int pStart;
        }

        public DistanceConstraint(ComputeBuffer particleBuffer, int[] indexArray)
        {
            DistConstCluster[] clusters = CreateClusters(particleBuffer, indexArray);

            Initialize("ComputeShader/DistanceConstraint", clusters, indexArray, particleBuffer);
        }

        private DistConstCluster[] CreateClusters(ComputeBuffer particleBuffer, int[] indexArray)
        {
            // バッファの中身を配列にコピーして取得
            ParticleData[] particles = new ParticleData[particleBuffer.count];
            particleBuffer.GetData(particles);

            int numClusters = indexArray.Length / 2;

            DistConstCluster[] clusters = new DistConstCluster[numClusters];
            int pOffset = 0;
            for (int i = 0; i < numClusters; i++)
            {
                DistConstCluster cluster = new DistConstCluster();
                cluster.pStart = pOffset;

                int id0 = indexArray[cluster.pStart];
                int id1 = indexArray[cluster.pStart + 1];
                cluster.restDist = Vector2.Distance(particles[id0].position, particles[id1].position);

                clusters[i] = cluster;

                pOffset += 2;
            }

            return clusters;
        }

    }
}
