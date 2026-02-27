using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class CameraMeshSetController : MonoBehaviour
    {
        private Camera cam;
        private Vector3 initPos;

        private GameObject[] renderTargetObjs;
        private MeshFilter[] meshFilters;
        private MeshRenderer[] meshRenderers;

        [SerializeField] private string tempLayerName = "CameraMeshSetOnly";
        private int tempLayer;

        public void Initialize(Mesh[] meshArray, Material[] materialArray)
        {
            cam = GetComponentInChildren<Camera>();
            initPos = cam.transform.position;

            // このレイヤーのみを対象とするLayerMaskを生成してカメラに設定
            cam.cullingMask = LayerMask.GetMask(tempLayerName);

            // レイヤー名からレイヤー番号を取得
            tempLayer = LayerMask.NameToLayer(tempLayerName);


            // 必要な数のオブジェクトを配置し、MeshFilterとMeshRendererにMeshとMaterialをセット
            renderTargetObjs = new GameObject[meshArray.Length];
            meshFilters = new MeshFilter[meshArray.Length];
            meshRenderers = new MeshRenderer[meshArray.Length];
            for (int i = 0; i < meshArray.Length; i++)
            {
                renderTargetObjs[i] = new GameObject("RenderTarget");
                renderTargetObjs[i].transform.SetParent(transform);

                meshFilters[i] = renderTargetObjs[i].AddComponent<MeshFilter>();
                meshFilters[i].mesh = meshArray[i];

                meshRenderers[i] = renderTargetObjs[i].AddComponent<MeshRenderer>();
                meshRenderers[i].material = materialArray[i];

                // レイヤーを専用のレイヤーに変更する
                renderTargetObjs[i].layer = tempLayer;
            }
        }

        public void UpdateCamera(Vector2 offset, float size)
        {
            cam.transform.position = initPos + new Vector3(offset.x, offset.y, 0);
            cam.orthographicSize = size;
        }

        public void DrawMesh(RenderTexture rt)
        {
            // オブジェクトをアクティブ状態にする
            foreach (var obj in renderTargetObjs)
            {
                obj.SetActive(true);
            }

            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;

            // RenderTextureに描画
            cam.clearFlags = CameraClearFlags.Depth;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            // 非アクティブ状態にする
            foreach (var obj in renderTargetObjs)
            {
                obj.SetActive(false);
            }
        }
    }
}
