using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class ColliderGenerator
    {
        private UVMaskGenerator uvMaskGenerator;
        private SDFCalculator sdfCalculator;

        private TargetMesh target;

        private Vector3 camDirLocalRef = new Vector3(1, 0, 0);

        public ColliderGenerator(TargetMesh tgt, Occluder occ, int depthRTSize, int colliderRTSize)
        {
            uvMaskGenerator = new UVMaskGenerator(tgt, occ, depthRTSize, colliderRTSize);

            sdfCalculator = new SDFCalculator(colliderRTSize);

            this.target = tgt;
        }

        public void Reset(Vector3 camPos)
        {
            camDirLocalRef = CalcCamDirLocal(camPos);
        }



        public RenderTexture DrawColliderMap(Vector3 camPos, Quaternion camRot)
        {
            RenderTexture maskRT = uvMaskGenerator.DrawMap(camPos, camRot);

            return sdfCalculator.Calculate(maskRT);
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
    }
}
