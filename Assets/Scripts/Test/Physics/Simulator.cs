using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    #region Interface Definition
    public interface IParticleDataProvider
    {
        ComputeBuffer GetParticleBuffer();
        ObjectToParticles GetParticleReference(int key);
    }
    #endregion

    public class Simulator : IParticleDataProvider
    {
        private readonly int maxLayers = 16;

        // 
        private Body body;
        public BodyCreator BodyCreator { get; private set; }

        public ExternalDataPool DataPool { get; private set; }

        private PBDSolver pbdSolver;
        private CollisionSolver collisionSolver;
        private TargetPosSolver targetPosSolver;
        private TargetPosForce targetPosForce;
        private ParticleCollisionSolver particleCollisionSolver;

        private bool isInitialized;

        public Simulator()
        {
            BodyCreator = new BodyCreator();
            DataPool = new ExternalDataPool(maxLayers);

            isInitialized = false;
        }

        public bool Initialize(PBDSolver.IReadOnlyParameter solverParameter, int colliderTexSize)
        {
            // Bodyを初期化
            body = BodyCreator.CreateBody(colliderTexSize);

            if (body != null)
            {
                // ソルバーを初期化
                pbdSolver = new PBDSolver(solverParameter, body);

                isInitialized = true;
                return true;
            }

            Debug.Log("Bodyの初期化ができませんでした");
            return false;
        }

        public void Execute(float dt)
        {
            if (!isInitialized)
            {
                Debug.LogError("初期化前に実行関数が呼ばれました。");
                return;
            }

            // 外部からの変更を適用
            body.ApplyExternalData(DataPool);

            // シミュレーションを実行
            pbdSolver.Step(dt);
        }

        public void ReleaseBuffers()
        {
            body.ReleaseBuffers();
        }

        #region Interface Function
        public ComputeBuffer GetParticleBuffer()
        {
            return body.ParticleBuffer;
        }

        public ObjectToParticles GetParticleReference(int key)
        {
            return body.ObjToParticles[key];
        }
        #endregion
    }
}
