using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace PositionBasedHighlight
{
    public class UVMaskGenerator
    {
        private TargetMesh target;
        private Occluder occluder;

        private UVMaskCamera maskCam;

        private RenderTexture targetDepthRT;
        private RenderTexture occluderDepthRT;
        private RenderTexture maskRT;

        static readonly ProfilerMarker markerTgtDepth = new ProfilerMarker("MyMarkerTgtDepth");
        static readonly ProfilerMarker markerOccDepth = new ProfilerMarker("MyMarkerOccDepth");
        static readonly ProfilerMarker markerMask = new ProfilerMarker("MyMarkerMask");

        public UVMaskGenerator(TargetMesh tgt, Occluder occ, int depthRTSize, int reflMapRTSize)
        {
            target = tgt;
            occluder = occ;

            GameObject camObj = Object.Instantiate(Resources.Load<GameObject>("Prefab/MaskCamera"));
            camObj.transform.SetParent(target.transform);
            maskCam = camObj.GetComponent<UVMaskCamera>();

            // Cameraコンポーネントは、アクティブ状態でシーンにおいておくとレンダリング処理が実行されてしまうので、非アクティブにしておく
            // 非アクティブ状態でも、Camera.Render()関数は実行可能
            maskCam.gameObject.SetActive(false);

            HelperFunction.CreateCameraTargetDepthRT(ref targetDepthRT, depthRTSize, depthRTSize);
            HelperFunction.CreateCameraTargetDepthRT(ref occluderDepthRT, depthRTSize, depthRTSize);
            HelperFunction.CreateCameraTargetFloat4RT(ref maskRT, reflMapRTSize);
        }

        public RenderTexture DrawMap(Vector3 camPos, Quaternion camRot)
        {
            float radius = 0.4f;
            Vector3 center = target.WorldOriginUVN + target.WorldRotationUVN * (0.5f * Vector3.forward);
            float dist = Vector3.Magnitude(camPos - center);
            Vector3 dir = Vector3.Normalize(camPos - center);
            Vector3 pos = center + dir * Mathf.Min(radius, dist);
            Quaternion rot = Quaternion.LookRotation(target.WorldOriginUVN - pos);

            // maskCam.SetTransform(camPos, camRot, new Vector3(1, 1, 1));
            maskCam.SetTransform(pos, rot, new Vector3(1, 1, 1));

            DrawMaskRT();

            return maskRT;
        }

        private void DrawMaskRT()
        {
            // 遮蔽物ありの場合
            if (occluder != null)
            {
                // Targetの深度テクスチャを描画
                using (markerTgtDepth.Auto())
                {
                    maskCam.DrawDepthRT(ref targetDepthRT, target.gameObject);
                }
                    
                // HighlightDebugger.Instance.DebugTexture(targetDepthRT);

                // Occluderの深度テクスチャを描画
                using (markerOccDepth.Auto())
                {
                    maskCam.DrawDepthRT(ref occluderDepthRT, occluder.gameObject);
                }
                    

                using (markerMask.Auto())
                {
                    maskCam.DrawMaskRT(ref maskRT, targetDepthRT, occluderDepthRT, target.gameObject);
                }
                    
                // HighlightDebugger.Instance.DebugTexture(maskRT);
            }

            // 遮蔽物なしの場合はマスクテクスチャにUVマップを描画
            else
            {
                using (markerMask.Auto())
                {
                    maskCam.DrawUVMap(ref maskRT, target.gameObject);
                    // HighlightDebugger.Instance.DebugTexture(maskRT);
                }
            }
        }
    }
}
