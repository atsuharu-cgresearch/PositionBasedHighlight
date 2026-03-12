using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class AreaConstraint
    {
        // --- Shader & Kernels ---
        private ComputeShader compute;
        private int kCalcDelta;
        private int kApplyDelta;

        // --- Buffers ---
        private ComputeBuffer particleBuffer;
        private ComputeBuffer clusterBuffer;
        private ComputeBuffer indexBuffer;
        private ComputeBuffer correctionBuffer;
        private ComputeBuffer referenceBuffer;
        private ComputeBuffer helperBuffer;

        // --- Thread Groups (事前計算用) ---
        private int threadGroupsCalc;
        private int threadGroupsApply;

        // --- Structs ---
        public struct AreaConstCluster
        {
            public float restArea;
            public int pStart;
        }

        public struct Helper
        {
            public int start;
            public int count;
        }

        /// <summary>
        /// コンストラクタ: 必要なデータの生成からシェーダーのセットアップまでを一貫して行う
        /// </summary>
        public AreaConstraint(ComputeBuffer particleBuffer, int[] indexArray)
        {
            this.particleBuffer = particleBuffer;

            // 1. クラスタ（面積の初期状態）の計算
            AreaConstCluster[] clusters = CreateClusters(indexArray);

            // 2. 参照テーブル（頂点ごとの拘束リスト）の作成
            CreateReferenceTables(indexArray, out int[] referenceArray, out Helper[] helperArray);

            // 3. ComputeShaderのロードとカーネル取得
            LoadComputeShader();

            // 4. バッファの生成
            AllocateBuffers(clusters, indexArray, referenceArray, helperArray);

            // 5. シェーダーへのデータバインド
            BindBuffersToShader();

            // 6. 毎フレーム使うスレッドグループ数の事前計算
            CalculateThreadGroups(clusters.Length);
        }

        // =========================================================
        // 初期化サブルーチン群 (処理ごとに分割)
        // =========================================================

        // --- 1. クラスタデータの生成 ---
        private AreaConstCluster[] CreateClusters(int[] indexArray)
        {
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

                clusters[i] = cluster;
                pOffset += 3;
            }
            return clusters;
        }

        // --- 2. 参照テーブルの生成 ---
        // 戻り値ではなく out パラメータを使うことで、この関数内で完結させています
        private void CreateReferenceTables(int[] indexArray, out int[] referenceArray, out Helper[] helperArray)
        {
            int numParticles = particleBuffer.count;
            List<int>[] myCorrectionList = new List<int>[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                myCorrectionList[i] = new List<int>();
            }

            for (int i = 0; i < indexArray.Length; i++)
            {
                int particleID = indexArray[i];
                myCorrectionList[particleID].Add(i);
            }

            List<int> myCorrectionList1D = new List<int>();
            helperArray = new Helper[numParticles];
            int count = 0;

            for (int i = 0; i < numParticles; i++)
            {
                Helper helper = new Helper();
                helper.start = count;
                helper.count = myCorrectionList[i].Count;
                helperArray[i] = helper;

                for (int j = 0; j < myCorrectionList[i].Count; j++)
                {
                    myCorrectionList1D.Add(myCorrectionList[i][j]);
                }
                count += myCorrectionList[i].Count;
            }

            referenceArray = myCorrectionList1D.ToArray();
        }

        // --- 3. ComputeShaderの準備 ---
        private void LoadComputeShader()
        {
            // compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/AreaConstraint"));
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/Optimized/AreaConstraint"));

            kCalcDelta = compute.FindKernel("CS_CalcDelta");
            kApplyDelta = compute.FindKernel("CS_ApplyDelta");
        }

        // --- 4. バッファの確保 ---
        private void AllocateBuffers(AreaConstCluster[] clusters, int[] indexArray, int[] referenceArray, Helper[] helperArray)
        {
            clusterBuffer = ComputeHelper.CreateStructuredBuffer(clusters);
            indexBuffer = ComputeHelper.CreateStructuredBuffer(indexArray);
            correctionBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(indexArray.Length);
            referenceBuffer = ComputeHelper.CreateStructuredBuffer(referenceArray);
            helperBuffer = ComputeHelper.CreateStructuredBuffer(helperArray);
        }

        // --- 5. バッファのバインド ---
        private void BindBuffersToShader()
        {
            // Calc Kernel
            compute.SetBuffer(kCalcDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kCalcDelta, "_Clusters", clusterBuffer);
            compute.SetBuffer(kCalcDelta, "_Indices", indexBuffer);
            compute.SetBuffer(kCalcDelta, "_Corrections", correctionBuffer);

            // Apply Kernel
            compute.SetBuffer(kApplyDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kApplyDelta, "_Corrections", correctionBuffer);
            compute.SetBuffer(kApplyDelta, "_References", referenceBuffer);
            compute.SetBuffer(kApplyDelta, "_Helpers", helperBuffer);

            // 固定値のセット
            compute.SetInt("_NumParticles", particleBuffer.count);
            compute.SetInt("_NumConstraints", clusterBuffer.count);
        }

        // --- 6. スレッドグループの事前計算 ---
        private void CalculateThreadGroups(int numClusters)
        {
            threadGroupsCalc = Mathf.CeilToInt(numClusters / 64f);
            threadGroupsApply = Mathf.CeilToInt(particleBuffer.count / 64f);
        }

        // =========================================================
        // 実行・解放処理
        // =========================================================

        public void ConstrainPositions(float k)
        {
            compute.SetFloat("_K", k);

            // 事前計算したスレッドグループ数を使用してDispatch
            compute.Dispatch(kCalcDelta, threadGroupsCalc, 1, 1);
            compute.Dispatch(kApplyDelta, threadGroupsApply, 1, 1);
        }

        public void Release()
        {
            ComputeHelper.Release(
                clusterBuffer,
                indexBuffer,
                correctionBuffer,
                referenceBuffer,
                helperBuffer
            );
        }
    }
}