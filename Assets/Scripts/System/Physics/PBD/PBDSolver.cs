using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// Position Based DynamicsのSubstep法によるソルバー
    /// 実行ロジックと外部データの取得を全てここに集約する
    /// </summary>
    public class PBDSolver
    {
        private Body body;
        private ExternalDataPool dataPool; // 追加: 外部データへのアクセス権

        private ComputeShader compute;

        private int kPredict;
        private int kUpdate;

        private Parameter parameter;

        [System.Serializable]
        public class Parameter
        {
            public int numIterations = 3;
            public int numSubsteps = 3;
            public float damping = 0.99f;
            public Vector3 gravity = Physics.gravity; // デバッグ用

            public float stiffnessArea = 0.03f;
            public float stiffnessShapeMatch = 0.03f;
            public float stiffnessCollision = 0.1f;
            public float stiffnessTargetPos = 0.001f;
        }

        // 引数に ExternalDataPool を追加
        public PBDSolver(Parameter parameter, Body body, ExternalDataPool dataPool)
        {
            this.body = body;
            this.parameter = parameter;
            this.dataPool = dataPool;

            InitComputeShader();
        }

        private void InitComputeShader()
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/Physics/PBDSolver");

            kPredict = compute.FindKernel("CS_Predict");
            kUpdate = compute.FindKernel("CS_Update");

            ComputeHelper.SetBuffer(compute, body.ParticleBuffer, "_Particles", kPredict, kUpdate);
        }

        public void Step(float dt)
        {
            float subDt = dt / parameter.numSubsteps;

            // =========================================================
            // 【変更点】 DataPoolからの外部データの適用をPBDSolverが行う
            // =========================================================
            if (body.CollisionSolver != null)
            {
                body.CollisionSolver.SetSDFArray(dataPool.SDFArray);
                body.CollisionSolver.SetColliderTransforms(dataPool.ColliderTransforms);
            }
            if (body.TargetPosSolver != null)
            {
                body.TargetPosSolver.SetOffsets(dataPool.TargetPosTransforms);
            }
            // =========================================================

            // プロパティ変数とSDFをシェーダーに渡す
            compute.SetInt("_NumParticles", body.ParticleBuffer.count);
            compute.SetFloat("_Dt", subDt);
            compute.SetFloat("_Damping", parameter.damping);
            compute.SetVector("_Gravity", parameter.gravity);

            // 各種ソルバーへのバッファのバインド
            body.CollisionSolver.Bind(body.ParticleBuffer, body.LayerBuffer);
            body.TargetPosSolver.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer);
            body.TargetPosForce.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer, dt);

            // Substepに分割して反復実行
            for (int i = 0; i < parameter.numSubsteps; i++)
            {
                Substep();
            }
        }

        private void Substep()
        {
            int threadGroups = Mathf.CeilToInt(body.ParticleBuffer.count / 64f);

            // 外力の適用
            float k_TargetPosForce = parameter.stiffnessTargetPos;
            body.TargetPosForce.ApplyForce(k_TargetPosForce);

            // 現在の速度から推定位置を計算
            compute.Dispatch(kPredict, threadGroups, 1, 1);

            // 拘束条件を解いて位置を修正
            for (int i = 0; i < parameter.numIterations; i++)
            {
                if (body.AreaConstraint != null)
                {
                    float k_Area = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessArea), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.AreaConstraint.ConstrainPositions(k_Area);
                }

                if (body.ShapeMatchConstraint != null)
                {
                    float k_ShapeMatch = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessShapeMatch), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.ShapeMatchConstraint.ConstrainPositions(k_ShapeMatch);
                }

                if (body.CollisionSolver != null)
                {
                    float k_Collision = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessCollision), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.CollisionSolver.ConstrainPositions(k_Collision);
                }
            }

            // 修正結果をもとに位置と速度を更新
            compute.Dispatch(kUpdate, threadGroups, 1, 1);
        }
    }
}