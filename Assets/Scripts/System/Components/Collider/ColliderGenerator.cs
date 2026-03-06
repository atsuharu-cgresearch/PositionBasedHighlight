using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace PositionBasedHighlight
{
    /// <summary>
    /// 
    /// </summary>
    public class ColliderGenerator
    {
        private UVMaskGenerator uvMaskGenerator; // カメラを使う方法

        private UVMaskGeneratorIntersect uvMaskGeneratorIntersect; // ComputeShaderでレイキャストする方法
        private UVMapGenerator uvMapGenerator;

        private SDFCalculator sdfCalculator;

        private TargetMesh target;
        private Occluder occluder;

        private Vector3 camDirLocalRef = new Vector3(1, 0, 0);

        static readonly ProfilerMarker markerUVMaskOriginal = new ProfilerMarker("MyMarkerUVMaskOriginal");
        static readonly ProfilerMarker markerUVMaskIntersect = new ProfilerMarker("MyMarkerUVMaskIntersect");

        public ColliderGenerator(TargetMesh tgt, Occluder occ, int depthRTSize, int colliderRTSize)
        {
            uvMaskGenerator = new UVMaskGenerator(tgt, occ, depthRTSize, colliderRTSize);

            if (occ != null) uvMaskGeneratorIntersect = new UVMaskGeneratorIntersect(tgt, occ, colliderRTSize);
            else
            {
                uvMapGenerator = new UVMapGenerator(tgt, colliderRTSize);
                uvMapGenerator.Generate();
            }
            

            sdfCalculator = new SDFCalculator(colliderRTSize);

            target = tgt;
            occluder = occ;
        }

        public void Reset(Vector3 camPos)
        {
            camDirLocalRef = CalcCamDirLocal(camPos);
        }


        /// <summary>
        /// Occluderありの場合
        /// UVMaskGeneratorを使ってマスク済みUVマップから、SDFを生成する
        /// </summary>
        public RenderTexture DrawColliderMap(Vector3 camPos, Quaternion camRot)
        {
            RenderTexture uvMap;

            /*using (markerUVMaskOriginal.Auto())
            {
                uvMap = uvMaskGenerator.DrawMap(camPos, camRot);
            }*/
            

            if (occluder != null)
            {
                using (markerUVMaskIntersect.Auto())
                {
                    uvMap = uvMaskGeneratorIntersect.Generate(camPos);
                }
            }

            else
            {
                uvMap = uvMapGenerator.GetUVMap();
            }

            HighlightDebugger.Instance.DebugTexture(uvMap);

            // return sdfCalculator.Calculate(maskRT);
            return sdfCalculator.Calculate(uvMap);

        }


        public Vector2 CalcColliderOffset(Vector3 camPos)
        {
            Vector3 camDirLocalCurr = CalcCamDirLocal(camPos);

            HelperFunction.DirToLatiLong(camDirLocalCurr, out float latiDegCurr, out float longDegCurr);
            HelperFunction.DirToLatiLong(camDirLocalRef, out float latiDegRef, out float longDegRef);

            // Debug.Log("lati: " + latiDegCurr + "long: " + longDegCurr);

            float diffU = -1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, longDegCurr - longDegRef));
            float diffV = -1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, latiDegCurr - latiDegRef));

            return new Vector2(diffU, diffV);
        }

        private Vector3 CalcCamDirLocal(Vector3 camPos)
        {
            // ワールド座標系でのTargetMeshの現在の位置と回転を取得
            Vector3 targetOrigin = target.WorldOriginUVN;
            Quaternion targetRotation = target.WorldRotationUVN;

            // カメラ方向をローカル座標系に変換する
            return Vector3.Normalize(Quaternion.Inverse(targetRotation) * (camPos - targetOrigin));
        }

        public void ReleaseBuffers()
        {
            if (uvMaskGeneratorIntersect != null) uvMaskGeneratorIntersect.ReleaseBuffers();
            if (uvMapGenerator != null) uvMapGenerator.ReleaseBuffers();
        }
    }
}
