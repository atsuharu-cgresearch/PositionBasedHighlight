using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class BodyCreator
    {
        public struct ObjectData
        {
            public SimulationObjectDefinition def; // パーティクル配置・拘束条件
            public Vector4 initTransform;          // 初期配置
            public int layer;                      // 何番目のレイヤーに登録するか
        }

        private List<ObjectData> objectDataList = new List<ObjectData>();

        /// <summary>
        /// オブジェクトごとに登録していったデータを１次元にまとめるクラス
        /// </summary>
        public class AggregateData
        {
            public List<Vector2> positionsInit = new List<Vector2>();
            public List<Vector2> positionsLocal = new List<Vector2>();
            public List<int> areaIndices = new List<int>();
            public List<int> shapeMatchIndices = new List<int>();
            public List<int> shapeMatchCounts = new List<int>();
            public List<int> myObjectIndices = new List<int>();
            public List<int> myLayerIndices = new List<int>();
            public List<ParticleRange> objToParticles = new List<ParticleRange>();

            public AggregateData(List<ObjectData> objectDataList)
            {
                int pOffset = 0;

                foreach (var data in objectDataList)
                {
                    var def = data.def;
                    int pCount = def.particles.Length;

                    // 【改善1】 一時的な配列(new Vector2[])を作らず、直接Listに追加する
                    for (int j = 0; j < pCount; j++)
                    {
                        positionsInit.Add(LocalToWorld(def.particles[j], data.initTransform));
                        positionsLocal.Add(def.particles[j]);
                        myObjectIndices.Add(data.layer); // オブジェクトのインデックス
                        myLayerIndices.Add(data.layer);
                    }

                    // 【改善2】 ApplyIndexOffsetメソッドを廃止し、直接オフセットを足してAddする
                    if (def.areaConstIndices.Length > 0)
                    {
                        foreach (int idx in def.areaConstIndices)
                            areaIndices.Add(idx + pOffset);
                    }

                    if (def.shapeMatchIndices.Length > 0)
                    {
                        foreach (int idx in def.shapeMatchIndices)
                            shapeMatchIndices.Add(idx + pOffset);

                        shapeMatchCounts.AddRange(def.shapeMatchCounts);
                    }

                    objToParticles.Add(new ParticleRange(pOffset, pCount));
                    pOffset += pCount;
                }
            }

            private Vector2 LocalToWorld(Vector2 pLocal, Vector4 transform)
            {
                Vector2 origin = new Vector2(transform.x, transform.y);
                float scale = transform.z;
                float angle = transform.w;

                Vector2 p = pLocal * scale;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);

                return new Vector2(
                    c * p.x - s * p.y + origin.x,
                    s * p.x + c * p.y + origin.y
                );
            }
        }

        public void AddElement(SimulationObjectDefinition def, Vector4 initTransform, int layer, out int keyGetParticles)
        {
            objectDataList.Add(new ObjectData
            {
                def = def,
                initTransform = initTransform,
                layer = layer
            });

            keyGetParticles = objectDataList.Count - 1;
        }

        public Body CreateBody(int colliderTexSize)
        {
            if (objectDataList.Count == 0)
            {
                Debug.LogWarning("オブジェクトが１つも登録されていないため、Bodyを初期化しません");
                return null;
            }

            // データを１つにまとめる
            var aggregate = new AggregateData(objectDataList);

            // パーティクルデータの生成
            int numParticles = aggregate.positionsInit.Count;
            ParticleData[] particles = new ParticleData[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                particles[i] = new ParticleData(aggregate.positionsInit[i]);
            }

            // バッファの生成
            ComputeBuffer particleBuffer = ComputeHelper.CreateStructuredBuffer(particles);
            ComputeBuffer objIndexBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myObjectIndices.ToArray());
            ComputeBuffer layerBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myLayerIndices.ToArray());
            ComputeBuffer localPosBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.positionsLocal.ToArray());

            // 【改善3】 最新のクラス名（純粋なデータコンテナ）に合わせて生成
            var areaConstraint = aggregate.areaIndices.Count > 0
                ? new AreaConstraint(particleBuffer, aggregate.areaIndices.ToArray()) : null;

            var shapeConstraint = aggregate.shapeMatchIndices.Count > 0
                ? new ShapeMatchConstraint(particleBuffer, aggregate.shapeMatchIndices.ToArray(), aggregate.shapeMatchCounts.ToArray()) : null;

            var collisionConstraint = new CollisionSolver(colliderTexSize);

            // （TargetPos系もデータクラス化されていればここで生成）
            var targetPosConst = new TargetPosSolver();
            var targetPosForce = new TargetPosForce();

            return new Body(
                particleBuffer, localPosBuffer, objIndexBuffer, layerBuffer,
                areaConstraint, shapeConstraint,
                collisionConstraint, targetPosConst, targetPosForce,
                aggregate.objToParticles.ToArray()
            );
        }
    }
}