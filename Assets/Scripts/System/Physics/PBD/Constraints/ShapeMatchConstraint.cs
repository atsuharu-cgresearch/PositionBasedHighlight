using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class ShapeMatchConstraint
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

        // --- Shape Match 固有のバッファ ---
        private ComputeBuffer restPositionBuffer;

        // --- Thread Groups (事前計算用) ---
        private int threadGroupsCalc;
        private int threadGroupsApply;

        // --- Structs ---
        public struct Cluster
        {
            public int pStart;
            public int pCount;
        }

        public struct Helper
        {
            public int start;
            public int count;
        }

        /// <summary>
        /// コンストラクタ: 必要なデータの生成からシェーダーのセットアップまでを一貫して行う
        /// </summary>
        public ShapeMatchConstraint(ComputeBuffer particleBuffer, int[] indexArray, int[] countArray)
        {
            this.particleBuffer = particleBuffer;

            // 1. クラスタと初期相対座標（Rest Positions）の計算
            Cluster[] clusters = CreateClustersAndRestPositions(indexArray, countArray, out Vector2[] restPositions);

            // 2. 参照テーブル（頂点ごとの拘束リスト）の作成
            CreateReferenceTables(indexArray, out int[] referenceArray, out Helper[] helperArray);

            // 3. ComputeShaderのロードとカーネル取得
            LoadComputeShader();

            // 4. バッファの生成
            AllocateBuffers(clusters, indexArray, restPositions, referenceArray, helperArray);

            // 5. シェーダーへのデータバインド
            BindBuffersToShader();

            // 6. 毎フレーム使うスレッドグループ数の事前計算
            CalculateThreadGroups(clusters.Length);
        }

        // =========================================================
        // 初期化サブルーチン群 (処理ごとに分割)
        // =========================================================

        // --- 1. クラスタデータと初期相対座標の生成 ---
        private Cluster[] CreateClustersAndRestPositions(int[] indexArray, int[] countArray, out Vector2[] restPositions)
        {
            ParticleData[] particles = new ParticleData[particleBuffer.count];
            particleBuffer.GetData(particles);

            int numClusters = countArray.Length;
            Cluster[] clusters = new Cluster[numClusters];
            restPositions = new Vector2[indexArray.Length];

            int pOffset = 0;

            for (int i = 0; i < numClusters; i++)
            {
                Cluster cluster = new Cluster();
                cluster.pStart = pOffset;
                cluster.pCount = countArray[i];

                // クラスタ内の重心（Rest Center）を計算
                Vector2 restCenter = Vector2.zero;
                for (int j = 0; j < countArray[i]; j++)
                {
                    restCenter += particles[indexArray[cluster.pStart + j]].position;
                }
                restCenter /= (float)countArray[i];

                // 重心からの相対位置を保存
                for (int j = 0; j < countArray[i]; j++)
                {
                    restPositions[cluster.pStart + j] = particles[indexArray[cluster.pStart + j]].position - restCenter;
                }

                clusters[i] = cluster;
                pOffset += countArray[i];
            }

            return clusters;
        }

        // --- 2. 参照テーブルの生成 ---
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
            // compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/ShapeMatching"));
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Physics/Optimized/ShapeMatching"));

            kCalcDelta = compute.FindKernel("CS_CalcDelta");
            kApplyDelta = compute.FindKernel("CS_ApplyDelta");
        }

        // --- 4. バッファの確保 ---
        private void AllocateBuffers(Cluster[] clusters, int[] indexArray, Vector2[] restPositions, int[] referenceArray, Helper[] helperArray)
        {
            clusterBuffer = ComputeHelper.CreateStructuredBuffer(clusters);
            indexBuffer = ComputeHelper.CreateStructuredBuffer(indexArray);
            correctionBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(indexArray.Length);
            referenceBuffer = ComputeHelper.CreateStructuredBuffer(referenceArray);
            helperBuffer = ComputeHelper.CreateStructuredBuffer(helperArray);

            // 固有バッファ
            restPositionBuffer = ComputeHelper.CreateStructuredBuffer(restPositions);
        }

        // --- 5. バッファのバインド ---
        private void BindBuffersToShader()
        {
            // Calc Kernel
            compute.SetBuffer(kCalcDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kCalcDelta, "_Clusters", clusterBuffer);
            compute.SetBuffer(kCalcDelta, "_Indices", indexBuffer);
            compute.SetBuffer(kCalcDelta, "_Corrections", correctionBuffer);

            // 固有バッファのバインド
            compute.SetBuffer(kCalcDelta, "_RestPositions", restPositionBuffer);

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
                helperBuffer,
                restPositionBuffer // 固有バッファも解放
            );
        }
    }
}