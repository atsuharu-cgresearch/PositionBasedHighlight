using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace PositionBasedHighlight
{
    public class SimulationBodyGenerator : MonoBehaviour
    {
        public int particleCount = 32;
        public float radius = 0.1f;
        public string fileNameIncludeDotJson = "data.json";

        public string objName = "None";

        private Vector2[] positions;

        private int[] distConstIndices;

        private int[] areaConstIndices;

        private int[] shapeMatchIndices; // 0,1,2, 0,2,3,4, ,,,
        private int[] shapeMatchCounts; // 3, 4, ,,,


        private void Start()
        {
            CreateBody();
        }

        private void CreateBody()
        {
            CreateCircleBody();

            // jsonファイルで書き出し
            ExportJson();
        }

        #region CircleBody
        /// <summary>
        /// 円形にパーティクルを配置し、距離拘束と面積拘束を設定する
        /// </summary>
        private void CreateCircleBody()
        {
            // パーティクルを配置
            CreateParticlesCircle();

            // 拘束条件を設定
            SetDistConstOuterEdge();
            SetAreaConstOuterEdge();
        }

        /// <summary>
        /// 円形にパーティクルを配置
        /// </summary>
        private void CreateParticlesCircle()
        {
            positions = new Vector2[particleCount];
            for (int i = 0; i < particleCount; i++)
            {
                float angleRad = ((float)i / particleCount) * Mathf.PI * 2;
                float x = radius * Mathf.Cos(angleRad);
                float y = radius * Mathf.Sin(angleRad);
                positions[i] = new Vector2(x, y);
            }
        }

        /// <summary>
        /// 外周部分に隣同士の距離拘束を設定
        /// </summary>
        private void SetDistConstOuterEdge()
        {
            List<int> distConstIndexList = new List<int>();

            for (int i = 0; i < particleCount; i++)
            {
                distConstIndexList.Add(i);
                distConstIndexList.Add((i < particleCount - 1) ? i + 1 : 0);
            }

            distConstIndices = distConstIndexList.ToArray();
        }

        /// <summary>
        /// 外周のパーティクルを使って面積拘束を設定
        /// </summary>
        /*private void SetAreaConstOuterEdge()
        {
            // 各パーティクル、それと最もインデックスが離れたパーティクル、それらの間の2個のパーティクルを選んで2つの三角形の面積拘束を設定する

            List<int> areaConstIndexList = new List<int>();

            for (int i = 0; i < positions.Length; i++)
            {
                // 基準のパーティクル
                int id0 = i;

                // 最もインデックスが離れたパーティクル
                int id1 = (id0 + positions.Length / 2) % positions.Length;

                // 中間のパーティクル
                int id2_a = (id1 + positions.Length / 4) % positions.Length;
                int id2_b = (id1 + 3 * positions.Length / 4) % positions.Length;

                // 外積を使って求めた面積が正の値になるよう、三角形の頂点のインデックスを反時計回りにする
                areaConstIndexList.Add(id0);
                areaConstIndexList.Add(id1);
                areaConstIndexList.Add(id2_a);

                //areaConstIndexList.Add(id0);
                //areaConstIndexList.Add(id2_b);
                //areaConstIndexList.Add(id1);
            }

            areaConstIndices = areaConstIndexList.ToArray();
        }*/

        /*private void SetAreaConstOuterEdge()
        {
            // 各パーティクル、それと最もインデックスが離れたパーティクル、それらの間の2個のパーティクルを選んで2つの三角形の面積拘束を設定する

            List<int> areaConstIndexList = new List<int>();

            for (int i = 0; i < positions.Length; i++)
            {
                // 基準のパーティクル
                int id0 = i;

                int skip = 1;
                while (true)
                {
                    int id1 = id0 + skip;
                    int id2 = id0 + skip * 2;

                    if (id2 == positions.Length) id2 = 0;
                    else if (id2 > positions.Length) break;

                    areaConstIndexList.Add(id0);
                    areaConstIndexList.Add(id2);
                    areaConstIndexList.Add(id1);

                    skip++;
                }
            }

            areaConstIndices = areaConstIndexList.ToArray();
        }*/

        private void SetAreaConstOuterEdge()
        {
            // 各パーティクル、それと最もインデックスが離れたパーティクル、それらの間の2個のパーティクルを選んで2つの三角形の面積拘束を設定する

            List<int> areaConstIndexList = new List<int>();

            for (int i = 0; i < positions.Length; i++)
            {
                // 基準のパーティクル
                int id0 = i;

                int skip = 1;
                while (true)
                {
                    // if (skip >= 2) break;

                    int id1 = id0 + skip;
                    int id2 = id0 + skip * 2;

                    if (id2 >= positions.Length) break;

                    areaConstIndexList.Add(id0);
                    areaConstIndexList.Add(id1);
                    areaConstIndexList.Add(id2);

                    skip++;
                }
            }

            areaConstIndices = areaConstIndexList.ToArray();
        }
        #endregion

        #region ShapeMatching
        private void CreateBodyFromMesh()
        {

        }

        // private void 
        #endregion

        private void ExportJson()
        {
            // Wrapperを生成
            var wrapper = new SimulationObjectDefinition();
            wrapper.type = objName;
            wrapper.particles = positions;
            wrapper.distConstIndices = distConstIndices;
            wrapper.areaConstIndices = areaConstIndices;

            // jsonに変換
            string json = JsonUtility.ToJson(wrapper, true); // trueで改行

            // 書き出し実行
            string allPath = Path.Combine(Application.dataPath + "/" +  fileNameIncludeDotJson);
            File.WriteAllText(allPath, json);
            Debug.Log("書き出し完了。出力先：" + allPath);
        }
    }
}
