using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    public class Occluder : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer srcSmr;
        private SkinnedMeshRenderer smr;

        private void Start()
        {
            smr = GetComponent<SkinnedMeshRenderer>();
        }

        private void Update()
        {
            CopyBlendShapes(srcSmr, smr);
        }

        private void CopyBlendShapes(SkinnedMeshRenderer src, SkinnedMeshRenderer dst)
        {
            int count = src.sharedMesh.blendShapeCount;

            for (int i = 0; i < count; i++)
            {
                float weight = src.GetBlendShapeWeight(i);
                dst.SetBlendShapeWeight(i, weight);
            }
        }
    }
}
