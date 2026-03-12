using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// このシステムを管理する最上位クラス
    /// 
    /// </summary>
    public class HighlightSystem : MonoBehaviour
    {
        // パラメータ・設定の入力
        private HighlightInput input;

        // 物理シミュレーション
        private Simulator simulator;

        // シミュレーターへの入力とシミュレーション結果の描画
        // これらはハイライトごとに計算する
        private SimulationInputCalculator[] simInputCalculators;
        private SimulationResultRenderer[] simResultRenderers;

        private int numSlots;

        private void Start()
        {
            // 入力担当のコンポーネントを取得
            input = GetComponent<HighlightInput>();

            // 各コンポーネントの初期化
            InitComponents();

            // 現在のカメラや光源の配置を基準とする
            ResetHighlight();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitComponents()
        {
            // コンポーネントにシミュレーターの参照を渡すので先にインスタンス化する必要がある
            simulator = new Simulator();

            numSlots = input.slots.Count;

            // シミュレーターへの入力を計算するクラスを初期化
            // シミュレーターに登録する
            simInputCalculators = new SimulationInputCalculator[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                simInputCalculators[i] = new SimulationInputCalculator(input.slots[i], input.colliderRTSize, i);

                // 
                simInputCalculators[i].RegisterForPhysics(simulator.BodyCreator);
            }


            // シミュレーターを初期化する
            // inputCalculatorがシミュレーターに登録した後
            simulator.Initialize(input.solverParameter, input.colliderRTSize);


            // 
            simResultRenderers = new SimulationResultRenderer[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                simResultRenderers[i] = new SimulationResultRenderer();

                int[] keys = simInputCalculators[i].objKeys;
                ParticleRange[] references = new ParticleRange[keys.Length];
                for (int j = 0; j < keys.Length; j++)
                {
                    references[j] = simulator.DataPool.GetParticleReference(keys[j]);
                }

                simResultRenderers[i].InitRenderer(input.slots[i], simulator.DataPool.ParticleBuffer, references, input.rendererRTSize);
            }
        }

        /// <summary>
        /// 現在のカメラやライトの状態で、ハイライトが基準の状態になるように調整する
        /// </summary>
        private void ResetHighlight()
        {
            for (int i = 0; i < input.slots.Count; i++)
            {
                simInputCalculators[i].Reset(input.cameraTransform.position, input.lightTransform.position);
            }
        }

        private void Update()
        {
            if (!input.isActive) return;

            if (Input.GetKeyDown(KeyCode.R)) ResetHighlight();

            Vector3 camPos = input.cameraTransform.position;
            Quaternion camRot = input.cameraTransform.rotation;
            Vector3 lightPos = input.lightTransform.position;
            Quaternion lightRot = input.lightTransform.rotation;


            for (int i = 0; i < input.slots.Count; i++)
            {
                simInputCalculators[i].UpdatePhysicsInputs(simulator.DataPool, camPos, camRot, lightPos);
            }

            // シミュレーションを実行
            // deltaTimeに0に近い値が入ったり、極端に大きな値が入るとシミュレーションが破綻するため、Clampする
            float dtClamp = Mathf.Clamp(Time.deltaTime, 1 / 300f, 1 / 30f);
            simulator.Execute(dtClamp);


            // 結果を取得してレンダリング
            for (int i = 0; i < input.slots.Count; i++)
            {
                // 
                Transform2D textureTransform = simInputCalculators[i].textureTransform;

                // 
                simResultRenderers[i].Render(textureTransform);
            }
        }

        private void OnDestroy()
        {
            simulator.ReleaseBuffers();

            for (int i = 0; i < numSlots; i++)
            {
                simInputCalculators[i].Release();

                simResultRenderers[i].Release();
            }
        }
    }
}
