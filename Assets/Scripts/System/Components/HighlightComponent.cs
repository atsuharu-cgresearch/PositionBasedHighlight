using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace PositionBasedHighlight
{
    /// <summary>
    /// ハイライト単位での処理を管理するクラス
    /// </summary>
    public class HighlightComponent
    {
        // Simulatorの何番目に登録されたか
        // Rendererがパーティクルバッファを参照するときに使用する
        private int[] objKeys;
        private int layerKey;
        private InputSlot slot;

        private ColliderGenerator colliderGenerator;
        private LightDirCalculator lightDirCalculator;

        private HighlightRendererMesh rendererMesh;
        private HighlightRendererPIP rendererPIP;

        static readonly ProfilerMarker markerRenderer = new ProfilerMarker("MyMarkerRenderer");
        static readonly ProfilerMarker markerRendererMesh = new ProfilerMarker("MyMarkerRendererMesh");

        // UV空間からシミュレーション空間への変換は、レンダリングに使用するので保持しておく
        private Vector4 textureTransform = new Vector4(0.25f, 0.25f, 1, 0);

        public HighlightComponent(InputSlot slot, int depthTexSize, int reflMapTexSize, int index)
        {
            this.slot = slot;
            layerKey = index;

            colliderGenerator = new ColliderGenerator(slot.target, slot.occluder, depthTexSize, reflMapTexSize);

            lightDirCalculator = new LightDirCalculator(slot.target);
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

        public void InitRenderer(IParticleDataProvider particleDataProvider, int renderSize)
        {
            // 全体のパーティクルバッファと、参照のためのデータを取得
            ComputeBuffer particles = particleDataProvider.GetParticleBuffer();

            ObjectToParticles[] references = new ObjectToParticles[objKeys.Length];
            for (int i = 0; i < objKeys.Length; i++)
            {
                references[i] = particleDataProvider.GetParticleReference(objKeys[i]);
            }

            SimulationObjectDefinition[] defs = new SimulationObjectDefinition[slot.elements.Count];
            for (int i = 0; i < slot.elements.Count; i++)
            {
                defs[i] = SimulationObjectDatabase.Load(slot.elements[i].type);
            }

            rendererPIP = new HighlightRendererPIP(renderSize, defs, particles, references);
            rendererMesh = new HighlightRendererMesh(renderSize, defs, particles, references);
        }

        public void Reset(Vector3 camPos, Vector3 lightPos)
        {
            colliderGenerator.Reset(camPos);

            lightDirCalculator.Reset(lightPos);
        }

        public void SetPhysicsInputs(ExternalDataPool dataPool, Vector3 camPos, Quaternion camRot, Vector3 lightPos)
        {
            // 視点や光源の変化によるハイライトの変化にどれくらい追従させるか
            float response = slot.response;
            // 
            float curvature = slot.curvature;

            // コライダーを更新
            RenderTexture collider = colliderGenerator.DrawColliderMap(camPos, camRot);

            Vector2 colliderOffset = response * colliderGenerator.CalcColliderOffset(camPos);
            float colliderScale = curvature;
            float colliderRot = 0;
            Vector4 colliderTransform = new Vector4(colliderOffset.x, colliderOffset.y, colliderScale, colliderRot);

            dataPool.SetCollider(collider, colliderTransform, layerKey);
            

            Vector2 lightOffset = response * lightDirCalculator.CalcLightDirOffset(lightPos);

            for (int i = 0; i < objKeys.Length; i++)
            {
                Transform2D inputTransform = slot.elements[i].transform;

                Vector4 targetPosTransform = new Vector4(
                    inputTransform.pos.x + lightOffset.x,
                    inputTransform.pos.y + lightOffset.y,
                    inputTransform.scale,
                    inputTransform.rot
                    );
                dataPool.SetTargetPosOffset(targetPosTransform, objKeys[i]);
            }
            

            // レンダリングに使用するので保存しておく
            textureTransform = colliderTransform;
        }

        /// <summary>
        /// シミュレーション後のパーティクルを使って、UVテクスチャを描画する
        /// </summary>
        public void Render()
        {
            /*using (markerRenderer.Auto())
            {
                slot.target.RenderResult(rendererPIP.RenderResult(textureTransform));

            }*/

            
            using (markerRendererMesh.Auto())
            {
                slot.target.RenderResult(rendererMesh.RenderResult(textureTransform));
            }
            
        }

        public void ReleaseBuffers()
        {
            colliderGenerator.ReleaseBuffers();
            rendererPIP.ReleaseBuffers();
        }
    }
}
