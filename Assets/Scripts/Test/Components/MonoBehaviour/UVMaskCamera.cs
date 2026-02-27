using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class UVMaskCamera : MonoBehaviour
    {
        [SerializeField] private string tempLayerName = "ReflMapperCamOnly";
        private int tempLayer;

        private Camera cam;

        private Material depthMat;
        private Material maskMat;
        private Material uvMat;

        public int CamPixWidth => cam.pixelWidth;
        public int CamPixHeight => cam.pixelHeight;

        private void Awake()
        {
            // cam = gameObject.AddComponent<Camera>();
            cam = GetComponent<Camera>();

            // このレイヤーのみを対象とするLayerMaskを生成してカメラに設定
            cam.cullingMask = LayerMask.GetMask(tempLayerName);

            // スクリーンに描画されないように、メインカメラよりDepthを小さくしておく
            cam.depth = -2;

            // レイヤー名からレイヤー番号を取得
            tempLayer = LayerMask.NameToLayer(tempLayerName);
            if (tempLayer < 0)
            {
                Debug.Log("専用のレイヤーが見つかりませんでした");
            }

            // マテリアルのインスタンスを作成
            depthMat = new Material(Resources.Load<Shader>("Shader/DepthOnly"));
            maskMat = new Material(Resources.Load<Shader>("Shader/UVMask"));
            uvMat = new Material(Resources.Load<Shader>("Shader/UVMap"));
        }

        public void SetTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;

            cam.nearClipPlane = 0.01f;

            // シーンの描画範囲を完全に覆う正方形領域を描画するように設定する

            cam.aspect = 16 / 9f;

        }

        /// <summary>
        /// 目標のオブジェクトのみ・深度情報のみをRenderTextureに描画
        /// </summary>
        public void DrawDepthRT(ref RenderTexture depthRT, GameObject targetObj)
        {
            // 描画対象のレイヤーを一時的に変更
            int oldLayer = targetObj.layer;
            targetObj.layer = tempLayer;

            // RenderTextureに深度を描画
            cam.clearFlags = CameraClearFlags.Depth;
            cam.targetTexture = depthRT;
            cam.Render();
            cam.targetTexture = null;

            // レイヤーをもとに戻す
            targetObj.layer = oldLayer;
        }

        /// <summary>
        /// 深度テクスチャを比較して、遮蔽部分を判定する
        /// </summary>
        public void DrawMaskRT(ref RenderTexture maskRT, RenderTexture targetDepthRT, RenderTexture occluderDepthRT, GameObject targetObj)
        {
            // 描画対象のレイヤーを一時的に変更
            int oldLayer = targetObj.layer;
            targetObj.layer = tempLayer;

            Renderer renderer = targetObj.GetComponent<Renderer>();

            // 一時的にマスク用のマテリアルに切り替える
            Material oldMat = renderer.material;
            renderer.material = maskMat;

            maskMat.SetTexture("_TargetDepthTex", targetDepthRT);
            maskMat.SetTexture("_OccluderDepthTex", occluderDepthRT);

            // 描画実行
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = maskRT;
            cam.Render();
            cam.targetTexture = null;

            // もとに戻す
            targetObj.layer = oldLayer;
            renderer.material = oldMat;
        }

        public void DrawUVMap(ref RenderTexture rt, GameObject targetObj)
        {
            // 描画対象のレイヤーを一時的に変更
            int oldLayer = targetObj.layer;
            targetObj.layer = tempLayer;

            Renderer renderer = targetObj.GetComponent<Renderer>();

            // 一時的にマスク用のマテリアルに切り替える
            Material oldMat = renderer.material;
            renderer.material = uvMat;

            // 描画実行
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            // もとに戻す
            targetObj.layer = oldLayer;
            renderer.material = oldMat;
        }
    }
}
