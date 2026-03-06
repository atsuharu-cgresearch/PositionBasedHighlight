using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// Position Based DynamicsのSubstep法によるソルバー
    /// </summary>
    public class PBDSolver
    {
        private Body body;

        private ComputeShader compute;

        private int kPredict;
        private int kUpdate;

        private IReadOnlyParameter parameter;

        [System.Serializable]
        public class Parameter : IReadOnlyParameter
        {
            // Inspectorから編集できるようにする
            [SerializeField] private int numIterations = 3;
            public int numSubsteps = 3;
            public float damping = 0.99f;
            public Vector3 gravity = Physics.gravity;

            public float sDistance = 0.7f;
            public float sArea = 0.03f;
            public float sShapeMatch = 0.03f;
            public float sCollision = 0.1f;
            public float sTargetPos = 0.001f;

            // ソルバー内部で変更されたくないので、インターフェースを使用してこの部分のみを渡す
            public int NumIterations => numIterations;
            public int NumSubsteps => numSubsteps;
            public float Damping => damping;
            public Vector3 Gravity => gravity;

            public float SDistance => sDistance;
            public float SArea => sArea;
            public float SShapeMatch => sShapeMatch;
            public float SCollision => sCollision;
            public float STargetPos => sTargetPos;

            // UIなどから値を変更する場合はこれを使う
            public int NumIteraionsSet { set => numIterations = value; }
            public int NumSubstepsSet { set => numSubsteps = value; }
            public float DampingSet { set => damping = value; }
            public Vector3 GravitySet { set => gravity = value; }

            public float SDistanceSet { set => sDistance = value; }
            public float SAreaSet { set => sArea = value; }
            public float SShapeMatchSet { set => sShapeMatch = value; }
            public float SCollisionSet { set => sCollision = value; }
            public float STargetPosSet { set => sTargetPos = value; }
        }

        public interface IReadOnlyParameter
        {
            int NumIterations { get; }
            int NumSubsteps { get; }
            float Damping { get; }
            Vector3 Gravity { get; }

            float SDistance { get; }
            float SArea { get; }
            float SShapeMatch { get; }
            float SCollision { get; }
            float STargetPos { get; }
        }

        public PBDSolver(IReadOnlyParameter parameter, Body body)
        {
            this.body = body;
            this.parameter = parameter;

            InitComputeShader();
        }

        private void InitComputeShader()
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/PBDSolver");

            kPredict = compute.FindKernel("CS_Predict");
            kUpdate = compute.FindKernel("CS_Update");

            ComputeHelper.SetBuffer(compute, body.ParticleBuffer, "_Particles", kPredict, kUpdate);
        }

        public void Step(float dt)
        {
            float subDt = dt / parameter.NumSubsteps;

            // プロパティ変数とSDFをシェーダーに渡す
            compute.SetInt("_NumParticles", body.ParticleBuffer.count);
            compute.SetFloat("_Dt", subDt);
            compute.SetFloat("_Damping", parameter.Damping);
            compute.SetVector("_Gravity", parameter.Gravity);

            body.CollisionSolver.Bind(body.ParticleBuffer, body.LayerBuffer);
            body.TargetPosSolver.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer);
            body.TargetPosForce.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer, dt);

            // Substepに分割して反復実行
            for (int i = 0; i < parameter.NumSubsteps; i++)
            {
                Substep();
            }
        }

        private void Substep()
        {
            int threadGroups = Mathf.CeilToInt(body.ParticleBuffer.count / 64f);

            // 外力の適用
            float k_TargetPosForce = parameter.STargetPos;
            body.TargetPosForce.ApplyForce(k_TargetPosForce);

            // 現在の速度から推定位置を計算
            compute.Dispatch(kPredict, threadGroups, 1, 1);

            // 拘束条件を解く
            for (int i = 0; i < parameter.NumIterations; i++)
            {
                /*if (body.DistanceConstraint != null)
                {
                    float k_Dist = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.SDistance), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.DistanceConstraint.ConstrainPositions(k_Dist);
                }*/

                if (body.AreaConstraint != null)
                {
                    float k_Area = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.SArea), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.AreaConstraint.ConstrainPositions(k_Area);
                }

                if (body.ShapeMatchConstraint != null)
                {
                    float k_ShapeMatch = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.SShapeMatch), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.ShapeMatchConstraint.ConstrainPositions(k_ShapeMatch);
                }

                /*if (body.TargetPosSolver != null)
                {
                    float k_TargetPos = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.STargetPos), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.TargetPosSolver.ConstrainPositions(k_TargetPos);
                }*/

                if (body.CollisionSolver != null)
                {
                    float k_Collision = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.SCollision), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.CollisionSolver.ConstrainPositions(k_Collision);
                }

                /*if (body.ParticleCollisionSolver != null)
                {
                    float k_PCollision = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.SCollision), 1f / (parameter.NumIterations * parameter.NumSubsteps));
                    body.ParticleCollisionSolver.ConstrainPositions(k_PCollision, 0.03f);
                }*/
            }

            // 修正結果をもとに位置と速度を更新
            compute.Dispatch(kUpdate, threadGroups, 1, 1);
        }
    }
}
