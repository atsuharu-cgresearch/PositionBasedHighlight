using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    /// <summary>
    /// 指定されたワールド座標（カメラやライト）が、基準時からどれだけ移動したか（緯度・経度の差分）を
    /// TargetMeshからの相対的なUVオフセットとして計算するクラス
    /// </summary>
    public class DirectionalOffsetCalculator
    {
        private TargetMesh target;

        private Vector3 localDirRef = new Vector3(1, 0, 0); // 基準となるローカル方向

        public DirectionalOffsetCalculator(TargetMesh tgt)
        {
            target = tgt;
        }

        /// <summary>
        /// 現在の位置を基準として記憶する
        /// </summary>
        public void Reset(Vector3 worldPos)
        {
            localDirRef = CalcLocalDir(worldPos);
        }

        /// <summary>
        /// 基準方向からのズレをUV空間のオフセットとして計算する
        /// </summary>
        public Vector2 CalcOffset(Vector3 currentWorldPos)
        {
            Vector3 localDirCurr = CalcLocalDir(currentWorldPos);

            HelperFunction.DirToLatiLong(localDirCurr, out float latiDegCurr, out float longDegCurr);
            HelperFunction.DirToLatiLong(localDirRef, out float latiDegRef, out float longDegRef);

            float diffU = -1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, longDegCurr - longDegRef));
            float diffV = -1 * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, latiDegCurr - latiDegRef));

            return new Vector2(diffU, diffV);
        }

        /// <summary>
        /// TargetMeshから見た、対象座標へのローカル方向ベクトルを計算
        /// </summary>
        private Vector3 CalcLocalDir(Vector3 worldPos)
        {
            Vector3 targetOrigin = target.WorldOriginUVN;
            Quaternion targetRotation = target.WorldRotationUVN;

            return Vector3.Normalize(Quaternion.Inverse(targetRotation) * (worldPos - targetOrigin));
        }
    }
}
