using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class HighlightDebugger : MonoBehaviour
    {
        public static HighlightDebugger Instance { get; private set; }

        private HighlightSystem system;

        private Material mat;

        public Vector4 textureTransform = new Vector4(0, 0, 1, 0);
        public Vector4 lightOffset = new Vector4(0, 0, 1, 0);


        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            system = FindObjectOfType<HighlightSystem>();

            mat = GetComponentInChildren<MeshRenderer>().material;
        }

        public void DebugTexture(RenderTexture rt)
        {
            if (mat == null) return;

            mat.SetTexture("_MainTex", rt);
        }

        private void OnDrawGizmos()
        {
            DrawAxis();

            DebugTexTransform();

            DebugLightOffset();

            /*if (system != null && system.simulator != null)
            {
                simulator.GetParticles(0, out ComputeBuffer particleBuffer, out ObjectToParticles objToParticles);

                if (particleBuffer != null)
                {
                    DebugParticles(particleBuffer);
                }
            }*/
        }

        private void DrawAxis()
        {
            float size = 1.0f;

            Gizmos.color = Color.red;
            Vector3 dirX = transform.right;
            Gizmos.DrawLine(transform.position, size * (transform.position + dirX));

            Gizmos.color = Color.green;
            Vector3 dirY = transform.up;
            Gizmos.DrawLine(transform.position, size * (transform.position + dirY));
        }

        Vector2 LocalToWorld(Vector2 pLocal, Vector2 pos, float scale, float angleRad)
        {
            // scale
            Vector2 p = pLocal * scale;

            float c = Mathf.Cos(angleRad);
            float s = Mathf.Sin(angleRad);

            // rotation
            Vector2 pRot = new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);

            // translation
            return pRot + pos;
        }

        private void DebugTexTransform()
        {
            Gizmos.color = Color.cyan;

            Vector2 pos = new Vector2(textureTransform.x, textureTransform.y);
            float scale = textureTransform.z;
            float angle = textureTransform.w;

            Vector2 p0 = new Vector2(-0.5f, -0.5f);
            Vector2 p1 = new Vector2(0.5f, -0.5f);
            Vector2 p2 = new Vector2(0.5f, 0.5f);
            Vector2 p3 = new Vector2(-0.5f, 0.5f);

            Vector3 w0 = LocalToWorld(p0, pos, scale, angle);
            Vector3 w1 = LocalToWorld(p1, pos, scale, angle);
            Vector3 w2 = LocalToWorld(p2, pos, scale, angle);
            Vector3 w3 = LocalToWorld(p3, pos, scale, angle);

            Gizmos.DrawLine(w0, w1);
            Gizmos.DrawLine(w1, w2);
            Gizmos.DrawLine(w2, w3);
            Gizmos.DrawLine(w3, w0);
        }

        private void DebugLightOffset()
        {
            Gizmos.color = Color.yellow;

            Vector2 pos = new Vector2(lightOffset.x, lightOffset.y);

            Gizmos.DrawSphere(pos, 0.03f);
        }

        private void DebugParticles(ComputeBuffer particleBuffer, int start, int count)
        {
            ParticleData[] particleDataArray = new ParticleData[particleBuffer.count];
            particleBuffer.GetData(particleDataArray);
            Gizmos.color = Color.green;
            for (int i = 0; i < particleDataArray.Length; i++)
            {
                Gizmos.DrawSphere(particleDataArray[i].position, 0.01f);
            }
        }
    }
}
