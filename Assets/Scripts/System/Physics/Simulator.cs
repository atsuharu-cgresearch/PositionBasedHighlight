using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class Simulator
    {
        // 
        private readonly int maxLayers = 16;

        private Body body;
        public BodyCreator BodyCreator { get; private set; }
        public ExternalDataPool DataPool { get; private set; }

        private PBDSolver solver;

        private bool isInitialized;

        public Simulator()
        {
            BodyCreator = new BodyCreator();
            DataPool = new ExternalDataPool(maxLayers);
            isInitialized = false;
        }

        public bool Initialize(PBDSolver.Parameter solverParameter, int colliderTexSize)
        {
            // Bodyを初期化
            body = BodyCreator.CreateBody(colliderTexSize);

            if (body != null)
            {
                // ソルバーを初期化
                solver = new PBDSolver(solverParameter, body, DataPool);

                // 生成されたバッファを、DataPoolにセット
                DataPool.SetSimulationOutputs(body.ParticleBuffer, body.ObjToParticles);

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

            // シミュレーションを実行
            solver.Step(dt);
        }

        public void ReleaseBuffers()
        {
            body.ReleaseBuffers();
        }
    }
}