using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class BodyCreator
    {
        public struct ObjectData
        {
            public SimulationObjectDefinition def; // パーティクル配置・拘束条件
            public Vector4 initTransform; // 初期配置
            public int layer; // 何番目のレイヤーに登録するか
        }

        // 全ての登録が終了した後でまとめてバッファを生成するため、それまでここに保存しておく
        private List<ObjectData> objectDataList = new List<ObjectData>();

        /// <summary>
        /// オブジェクトごとに登録していったデータを１次元にまとめるのに使用する
        /// </summary>
        public class AggregateData
        {
            public List<Vector2> positionsInit = new List<Vector2>();
            public List<Vector2> positionsLocal = new List<Vector2>();
            public List<int> distIndices = new List<int>();
            public List<int> areaIndices = new List<int>();
            public List<int> shapeMatchIndices = new List<int>();
            public List<int> shapeMatchCounts = new List<int>();
            public List<int> myObjectIndices = new List<int>();
            public List<int> myLayerIndices = new List<int>();
            public List<ObjectToParticles> objToParticles = new List<ObjectToParticles>();

            public AggregateData(List<ObjectData> objectDataList)
            {
                int pOffset = 0;

                for (int i = 0; i < objectDataList.Count; i++)
                {
                    var def = objectDataList[i].def;

                    // ローカル座標にTransformを適用して初期配置を決める
                    Vector2[] posTransformed = new Vector2[def.particles.Length];

                    for (int j = 0; j < posTransformed.Length; j++)
                    {
                        // posTransformed[j] = def.particles[j];
                        posTransformed[j] = LocalToWorld(def.particles[j], objectDataList[i].initTransform);
                        // Debug.Log(posTransformed[j]);
                    }
                    positionsInit.AddRange(posTransformed);

                    // ローカル座標も別で保存
                    positionsLocal.AddRange(def.particles);

                    // インデックスのオフセット計算と集計
                    if (def.distConstIndices.Length > 0)
                        distIndices.AddRange(ApplyIndexOffset(def.distConstIndices, pOffset));

                    if (def.areaConstIndices.Length > 0)
                        areaIndices.AddRange(ApplyIndexOffset(def.areaConstIndices, pOffset));

                    if (def.shapeMatchIndices.Length > 0)
                    {
                        shapeMatchIndices.AddRange(ApplyIndexOffset(def.shapeMatchIndices, pOffset));
                        shapeMatchCounts.AddRange(def.shapeMatchCounts);
                    }

                    // どのパーティクルがどのオブジェクトか
                    for (int p = 0; p < def.particles.Length; p++) myObjectIndices.Add(i);

                    // パーティクルのレイヤー
                    for (int p = 0; p < def.particles.Length; p++) myLayerIndices.Add(objectDataList[i].layer);

                    objToParticles.Add(new ObjectToParticles(pOffset, def.particles.Length));

                    pOffset += def.particles.Length;
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

                Vector2 pRot;
                pRot.x = c * p.x - s * p.y;
                pRot.y = s * p.x + c * p.y;

                return pRot + origin;
            }

            private int[] ApplyIndexOffset(int[] indices, int offset)
            {
                int[] result = new int[indices.Length];
                for (int i = 0; i < indices.Length; i++) result[i] = indices[i] + offset;
                return result;
            }
        }

        public BodyCreator()
        {

        }

        public void AddElement(SimulationObjectDefinition def, Vector4 initTransform, int layer, out int keyGetParticles)
        {
            ObjectData data = new ObjectData();

            data.def = def;
            data.initTransform = initTransform;
            data.layer = layer;

            objectDataList.Add(data);

            keyGetParticles = objectDataList.Count - 1;
        }


        public ComputeBuffer CreateParticleBuffer(AggregateData aggregate)
        {
            int numParticles = aggregate.positionsInit.Count;

            ParticleData[] particles = new ParticleData[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                particles[i] = new ParticleData(aggregate.positionsInit[i]);
            }

            return ComputeHelper.CreateStructuredBuffer(particles);
        }

        public Body CreateBody(int colliderTexSize)
        {
            if (objectDataList == null || objectDataList.Count == 0)
            {
                Debug.LogWarning("オブジェクトが１つも登録されていないため、Bodyを初期化しません");
                return null;
            }

            // データを１つにまとめる
            var aggregate = new AggregateData(objectDataList);

            ComputeBuffer particleBuffer = CreateParticleBuffer(aggregate);
            ComputeBuffer objIndexBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myObjectIndices.ToArray());
            ComputeBuffer layerBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myLayerIndices.ToArray());
            ComputeBuffer localPosBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.positionsLocal.ToArray());

            var areaConst = aggregate.areaIndices.Count > 0
                ? new AreaConstraint(particleBuffer, aggregate.areaIndices.ToArray()) : null;

            var distConst = aggregate.distIndices.Count > 0
                ? new DistanceConstraint(particleBuffer, aggregate.distIndices.ToArray()) : null;

            var shapeConst = aggregate.shapeMatchIndices.Count > 0
                ? new ShapeMatchConstraint(particleBuffer, aggregate.shapeMatchIndices.ToArray(), aggregate.shapeMatchCounts.ToArray()) : null;

            var collisionConst = new CollisionSolver(colliderTexSize);
            var targetPosConst = new TargetPosSolver();
            var particleCollisionConst = new ParticleCollisionSolver(particleBuffer, objIndexBuffer, layerBuffer);

            return new Body(
                particleBuffer, localPosBuffer, objIndexBuffer, layerBuffer,
                distConst, areaConst, shapeConst,
                collisionConst, targetPosConst, particleCollisionConst,
                aggregate.objToParticles.ToArray()
                );
        }
    }
}
