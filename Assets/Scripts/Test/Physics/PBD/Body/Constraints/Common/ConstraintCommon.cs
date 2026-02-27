using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// PBD拘束条件の共通部分（１つのクラスタが複数のパーティクルを持つ場合）
    /// 各拘束条件クラスはこれを継承して実装する
    /// </summary>
    public class ConstraintCommon
    {
        protected ComputeShader compute;

        protected int kCalcDelta;
        protected int kApplyDelta;

        protected int threadGroupsCalcDelta;
        protected int threadGroupsApplyDelta;

        protected ComputeBuffer particleBuffer;
        // 拘束条件のクラスタごとに持つデータ
        protected ComputeBuffer clusterBuffer;
        // クラスタの持つパーティクルのインデックスを１次元に並べたデータ
        protected ComputeBuffer indexBuffer;
        // クラスタごとに計算される、各パーティクルの修正ベクトルを保持するバッファ
        protected ComputeBuffer correctionBuffer;
        // CorrectionBufferの参照
        protected ComputeBuffer referenceBuffer;
        // ReferenceBufferを参照するのに使用する
        protected ComputeBuffer helperBuffer;

        public struct Helper
        {
            public int start;
            public int count;
        }

        // 初期化処理のテンプレート
        protected virtual void Initialize<T>(string shaderPath, T[] clusters, int[] indexArray, ComputeBuffer particleBuffer)
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>(shaderPath));
            if (compute == null)
            {
                Debug.LogError("ComputeShaderがありません");
            }

            kCalcDelta = compute.FindKernel("CS_CalcDelta");
            kApplyDelta = compute.FindKernel("CS_ApplyDelta");

            CreateBuffers(clusters, indexArray, particleBuffer);

            BindData();

            threadGroupsCalcDelta = Mathf.CeilToInt(clusters.Length / 64f);
            threadGroupsApplyDelta = Mathf.CeilToInt(particleBuffer.count / 64f);
        }

        protected virtual void CreateBuffers<T>(T[] clusters, int[] indexArray, ComputeBuffer particleBuffer)
        {
            this.particleBuffer = particleBuffer;

            clusterBuffer = ComputeHelper.CreateStructuredBuffer(clusters);

            indexBuffer = ComputeHelper.CreateStructuredBuffer(indexArray);

            correctionBuffer = ComputeHelper.CreateStructuredBuffer<Vector2>(indexArray.Length);

            CreateReferenceTables(indexArray, particleBuffer.count);
        }

        private void CreateReferenceTables(int[] indexArray, int numParticles)
        {
            // 各パーティクルが、constIndicesの何番目に登録されているか
            List<int>[] myCorrectionList = new List<int>[numParticles];
            for (int i = 0; i < numParticles; i++) { myCorrectionList[i] = new List<int>(); }

            for (int i = 0; i < indexArray.Length; i++)
            {
                int particleID = indexArray[i];
                myCorrectionList[particleID].Add(i);
            }

            // myCorrectionListを1次元の配列にしつつ、Helperを作成する
            List<int> myCorrectionList1D = new List<int>();
            Helper[] helpers = new Helper[numParticles];
            int count = 0;
            for (int i = 0; i < numParticles; i++)
            {
                Helper helper = new Helper();
                helper.start = count;
                helper.count = myCorrectionList[i].Count;
                helpers[i] = helper;

                for (int j = 0; j < myCorrectionList[i].Count; j++)
                {
                    myCorrectionList1D.Add(myCorrectionList[i][j]);
                }

                count += myCorrectionList[i].Count;
            }

            referenceBuffer = ComputeHelper.CreateStructuredBuffer(myCorrectionList1D.ToArray());
            helperBuffer = ComputeHelper.CreateStructuredBuffer(helpers);
        }

        protected virtual void BindData()
        {
            compute.SetBuffer(kCalcDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kCalcDelta, "_Clusters", clusterBuffer);
            compute.SetBuffer(kCalcDelta, "_Indices", indexBuffer);
            compute.SetBuffer(kCalcDelta, "_Corrections", correctionBuffer);

            compute.SetBuffer(kApplyDelta, "_Particles", particleBuffer);
            compute.SetBuffer(kApplyDelta, "_Corrections", correctionBuffer);
            compute.SetBuffer(kApplyDelta, "_References", referenceBuffer);
            compute.SetBuffer(kApplyDelta, "_Helpers", helperBuffer);

            compute.SetInt("_NumParticles", particleBuffer.count);
            compute.SetInt("_NumConstraints", clusterBuffer.count);
        }


        public void ConstrainPositions(float k)
        {
            // 動的に変更されるパラメータは引数で受け取ってここで設定する
            compute.SetFloat("_K", k);

            // クラスタごとに拘束条件を解いて、修正ベクトルを保存
            compute.Dispatch(kCalcDelta, threadGroupsCalcDelta, 1, 1);
            // パーティクルごとに修正ベクトルを平均して、位置を修正
            compute.Dispatch(kApplyDelta, threadGroupsApplyDelta, 1, 1);
        }

        public virtual void Release()
        {
            ComputeHelper.Release(
                clusterBuffer,
                indexBuffer,
                correctionBuffer,
                referenceBuffer,
                helperBuffer);
        }
    }
}
