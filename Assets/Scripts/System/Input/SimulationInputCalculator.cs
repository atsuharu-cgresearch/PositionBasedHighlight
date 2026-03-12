using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class SimulationInputCalculator
    {
        private InputSlot slot;

        // Simulatorの何番目に登録しているか
        public int[] objKeys;
        // 
        public int layerKey;
        // コライダーのオフセット
        public Transform2D textureTransform;

        // 入力のOccluderの有無によって使用するクラスを変更するため、インターフェースを使う
        private IUVMapGenerator uvMapGenerator;

        private SDFCalculator sdfCalculator;

        private Vector3 camDirLocalRef = new Vector3(1, 0, 0);

        private DirectionalOffsetCalculator camDirCalculator;
        private DirectionalOffsetCalculator lightDirCalculator;

        public SimulationInputCalculator(InputSlot slot, int texSize, int index)
        {
            this.slot = slot;

            // 
            layerKey = index;


            // Occluderの有無で、使うモジュールを切り替える
            if (slot.occluder != null)
            {
                uvMapGenerator = new UVMaskGenerator(slot.target, slot.occluder, texSize);
            }
            else
            {
                uvMapGenerator = new UVMapGenerator(slot.target, texSize);
            }


            sdfCalculator = new SDFCalculator(texSize);

            camDirCalculator = new DirectionalOffsetCalculator(slot.target);
            lightDirCalculator = new DirectionalOffsetCalculator(slot.target);
        }

        /// <summary>
        /// シミュレータにオブジェクトを登録し、キーを取得する
        /// </summary>
        public void RegisterForPhysics(BodyCreator bodyCreator)
        {
            objKeys = new int[slot.elements.Count];

            for (int i = 0; i < slot.elements.Count; i++)
            {
                Vector4 initTransform = new Vector4(
                    slot.elements[i].transform.pos.x,
                    slot.elements[i].transform.pos.y,
                    slot.elements[i].transform.scale,
                    slot.elements[i].transform.rot
                    );

                bodyCreator.AddElement(
                    SimulationObjectDatabase.Load(slot.elements[i].type),
                    initTransform,
                    layerKey,
                    out objKeys[i]
                    );
            }
        }

        /// <summary>
        /// 現在のカメラ・ライト・メッシュの状態で、ハイライトが基準の状態になるようにリセットする
        /// </summary>
        public void Reset(Vector3 camPos, Vector3 lightPos)
        {
            camDirCalculator.Reset(camPos);
            lightDirCalculator.Reset(lightPos);
        }

        public void UpdatePhysicsInputs(ExternalDataPool dataPool, Vector3 camPos, Quaternion camRot, Vector3 lightPos)
        {
            // UVマップを生成
            RenderTexture uvMap = uvMapGenerator.Generate(camPos);

            // UVマップからSDFを生成
            RenderTexture sdf = sdfCalculator.Calculate(uvMap);

            // 視点や光源の変化によるオフセットを計算
            float response = slot.response;
            float curvature = slot.curvature;
            Vector2 colliderOffset = response * camDirCalculator.CalcOffset(camPos);
            Vector2 lightOffset = response * lightDirCalculator.CalcOffset(lightPos);


            // コライダー（SDF）をシミュレーターに登録
            Transform2D colliderTransform = new Transform2D(colliderOffset, 0f, curvature);
            dataPool.SetCollider(sdf, colliderTransform, layerKey);

            // 目標位置をシミュレーターに登録
            for (int i = 0; i < objKeys.Length; i++)
            {
                Transform2D inputTransform = slot.elements[i].transform;

                // Transform2Dのコンストラクタ (pos, rot, scale) の順序に注意して生成
                Transform2D targetPosTransform = new Transform2D(
                    new Vector2(inputTransform.pos.x + lightOffset.x, inputTransform.pos.y + lightOffset.y),
                    inputTransform.rot,
                    inputTransform.scale
                );

                dataPool.SetTargetPosOffset(targetPosTransform, objKeys[i]);
            }

            // レンダリングに渡すために保存しておく
            textureTransform = colliderTransform;
        }

        public void Release()
        {
            uvMapGenerator.Release();
            sdfCalculator.Release();
        }
    }
}
