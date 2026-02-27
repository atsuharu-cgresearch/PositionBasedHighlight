using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class LightDirCalculator
    {
        private TargetMesh target;

        private Vector3 lightDirLocalRef;

        public LightDirCalculator(TargetMesh tgt)
        {
            target = tgt;
        }

        public void Reset(Vector3 lightPos)
        {
            lightDirLocalRef = CalcLightDirLocal(lightPos);
        }


        public Vector2 CalcLightDirOffset(Vector3 lightPos)
        {
            Vector3 camDirLocalCurr = CalcLightDirLocal(lightPos);

            HelperFunction.DirToLatiLong(camDirLocalCurr, out float latiDegCurr, out float longDegCurr);
            HelperFunction.DirToLatiLong(lightDirLocalRef, out float latiDegRef, out float longDegRef);

            float diffU = 1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, longDegCurr - longDegRef));
            float diffV = 1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, latiDegCurr - latiDegRef));

            return new Vector2(diffU, diffV);
        }

        private Vector3 CalcLightDirLocal(Vector3 camPos)
        {
            // ワールド座標系でのTargetMeshの現在の位置と回転を取得
            Vector3 targetOrigin = target.WorldOriginUVN;
            Quaternion targetRotation = target.WorldRotationUVN;

            // カメラ方向をローカル座標系に変換する
            return Vector3.Normalize(Quaternion.Inverse(targetRotation) * (camPos - targetOrigin));
        }
    }
}
