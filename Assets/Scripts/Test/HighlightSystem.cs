using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class HighlightSystem : MonoBehaviour
    {
        private IReadOnlyInput input;

        private Simulator simulator;

        private HighlightComponent[] components;

        private void Start()
        {
            input = GetComponent<IReadOnlyInput>();

            Initialize();
        }

        private void Initialize()
        {
            // コンポーネントにシミュレーターの参照を渡すので先にインスタンス化する必要がある
            simulator = new Simulator();

            components = new HighlightComponent[input.Slots.Count];
            for (int i = 0; i < input.Slots.Count; i++)
            {
                components[i] = new HighlightComponent(
                    input.Slots[i],
                    input.DepthRTSize,
                    input.ColliderRTSize,
                    i
                    );

                components[i].RegisterForPhysics(simulator.BodyCreator);
            }

            // 各コンポーネントがパーティクルの登録などを行った後にこれを実行して初期化する
            simulator.Initialize(input.ReadOnlyParameter, input.ColliderRTSize);

            for (int i = 0; i < input.Slots.Count; i++)
            {
                // パーティクルバッファと参照用のデータを渡すだけなので、インターフェースを使う
                components[i].InitRenderer(simulator, input.RendererRTSize);
            }

            // 現在のカメラや光源の配置を基準とする
            ResetHighlight();
        }

        /// <summary>
        /// 現在のカメラやライトの状態で、ハイライトが基準の状態になるように調整する
        /// </summary>
        private void ResetHighlight()
        {
            for (int i = 0; i < input.Slots.Count; i++)
            {
                components[i].Reset(input.GetActiveCamPos(), input.GetActiveLightPos());
            }
        }

        private void Update()
        {
            if (!input.IsActive) return;

            if (Input.GetKeyDown(KeyCode.R)) ResetHighlight();

            Vector3 camPos = input.GetActiveCamPos();
            Quaternion camRot = input.GetActiveCamRot();
            Vector3 lightPos = input.GetActiveLightPos();
            Quaternion lightRot = input.GetActiveLightRot();

            for (int i = 0; i < input.Slots.Count; i++)
            {
                components[i].SetPhysicsInputs(simulator.DataPool, camPos, camRot, lightPos);
            }

            // シミュレーションを実行
            float dtClamp = Mathf.Clamp(Time.deltaTime, 1 / 240f, 1 / 60f);
            simulator.Execute(0.016f);

            // 結果を取得してレンダリング
            for (int i = 0; i < input.Slots.Count; i++)
            {
                components[i].Render();
            }
        }

        private void OnDestroy()
        {
            simulator.ReleaseBuffers();

            foreach (var component in components)
            {
                component.ReleaseBuffers();
            }
        }
    }
}
