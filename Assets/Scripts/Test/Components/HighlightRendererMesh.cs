using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// シミュレーション結果をもとにUVテクスチャを生成し、TargetMeshに渡す
    /// </summary>
    public class HighlightRendererMesh
    {
        private ComputeBuffer particleBuffer;
        private RenderTexture resultRT;

        private CameraMeshSetController rendererSet;

        public HighlightRendererMesh(int texSize, SimulationObjectDefinition[] defs, ComputeBuffer particles, ObjectToParticles[] references)
        {
            GameObject rendererSetObj = Object.Instantiate(Resources.Load<GameObject>("Prefab/CameraMeshSet"));
            rendererSet = rendererSetObj.GetComponent<CameraMeshSetController>();

            Mesh[] meshArray = new Mesh[defs.Length];
            Material[] materialArray = new Material[defs.Length];

            for (int i = 0; i < defs.Length; i++)
            {
                Mesh mesh = new Mesh();

                mesh.SetVertices(defs[i].meshVertices);
                mesh.SetTriangles(defs[i].meshTriangles, 0);

                // 頂点がどのパーティクルを参照するかの情報をUV2に入れておく
                Vector4[] uv2 = new Vector4[defs[i].vToPReferences.Length];
                for (int j = 0; j < uv2.Length; j++)
                {
                    uv2[j] = new Vector4(defs[i].vToPReferences[j], 0, 0, 0);
                }
                mesh.SetUVs(1, uv2);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                meshArray[i] = mesh;


                Material mat = new Material(Resources.Load<Shader>("Shader/ParticleRendererMesh"));
                mat.SetBuffer("_Particles", particles);
                mat.SetInt("_Start", references[i].pStart);

                materialArray[i] = mat;
            }
            
            rendererSet.Initialize(meshArray, materialArray);

            particleBuffer = particles;
            HelperFunction.CreateCameraTargetFloat4RT(ref resultRT, texSize);
        }

        public RenderTexture RenderResult(Vector4 textureTransform)
        {
            Vector3 camOffset = new Vector3(textureTransform.x, textureTransform.y, 0);
            float camSize = textureTransform.z / 2;
            float camRot = textureTransform.w;

            rendererSet.UpdateCamera(camOffset, camSize);

            rendererSet.DrawMesh(resultRT);

            return resultRT;
        }

        public void ReleaseBuffers()
        {
            
        }
    }
}