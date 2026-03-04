using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using PositionBasedHighlight;

public class BodyGeneratorFromMesh : MonoBehaviour
{
    public Mesh mesh;
    public GameObject vertexPrefab;
    public float offsetDist = 0.1f;
    public int shapeMatchSize = 3;

    public string fileNameIncludeDotJson = "Body.json";

    public string objName = "None";

    public bool drawAreaConst = true;
    public int drawingAreaKey = 0;
    public bool drawShapeMatch = true;
    public int drawingShapeMatchKey = 0;

    private Vector3[] verticesUnique;
    private List<int> selectedIndexList = new List<int>();

    private List<Vector2> positionList = new List<Vector2>();
    private List<int> areaConstIndexList = new List<int>();
    private List<int> shapeMatchIndexList = new List<int>();
    private List<int> shapeMatchCountList = new List<int>();

    // レンダリング用
    private List<int> edgeIndexList = new List<int>();

    private Vector3[] meshVertices;
    private int[] meshTriangles;
    private List<int> vToPReferenceList = new List<int>();

    private void Start()
    {
        List<Vector3> uniqueList = new List<Vector3>();

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < uniqueList.Count; j++)
            {
                float sqrDist = Vector3.SqrMagnitude(mesh.vertices[i] - uniqueList[j]);
                if (sqrDist < 1e-6f)
                {
                    vToPReferenceList.Add(j);

                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                vToPReferenceList.Add(uniqueList.Count);
                uniqueList.Add(mesh.vertices[i]);
            }
        }

        verticesUnique = uniqueList.ToArray();
        Debug.Log(verticesUnique.Length);

        for (int i = 0; i < verticesUnique.Length; i++)
        {
            GameObject vertexObj = Instantiate(vertexPrefab, verticesUnique[i], Quaternion.identity);
            vertexObj.GetComponent<VertexCollider>().index = i;
        }

        meshVertices = (Vector3[])mesh.vertices.Clone();
        meshTriangles = (int[])mesh.triangles.Clone();

    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                int index = hit.collider.gameObject.GetComponent<VertexCollider>().index;

                if (!selectedIndexList.Contains(index))
                {
                    selectedIndexList.Add(index);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddBody(positionList.Count);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Export();
        }
    }

    private void AddBody(int indexOffset)
    {
        Vector2[] positionsOuter = new Vector2[selectedIndexList.Count];

        for (int i = 0; i < selectedIndexList.Count; i++)
        {
            positionsOuter[i] = new Vector2(
                verticesUnique[selectedIndexList[i]].x, 
                verticesUnique[selectedIndexList[i]].y
                );
        }

        // 内側に押し出して複製
        Vector2[] positionsInner = new Vector2[selectedIndexList.Count];

        for (int i = 0; i < selectedIndexList.Count; i++)
        {
            Vector2 posPrev = positionsOuter[(i - 1 < 0) ? selectedIndexList.Count - 1 : i - 1];
            Vector2 posNext = positionsOuter[(i + 1 <= selectedIndexList.Count - 1) ? i + 1 : 0];

            Vector2 offset = offsetDist * new Vector2(posPrev.y - posNext.y, posNext.x - posPrev.x).normalized;

            positionsInner[i] = positionsOuter[i] + offset;
        }

        List<Vector2> positionList = new List<Vector2>();
        positionList.AddRange(positionsOuter);
        positionList.AddRange(positionsInner);

        // 面積拘束を追加
        List<int> areaConstIndexList = new List<int>();

        for (int i = 0; i < positionList.Count / 2; i++)
        {
            // 基準のパーティクル
            int id0 = i;

            int skip = 1;
            while (true)
            {
                if (skip > (positionList.Count / 2) / 3) break;

                int id1 = (id0 + skip + positionList.Count / 2) % (positionList.Count / 2);
                int id2 = (id0 + skip * 2 + positionList.Count / 2) % (positionList.Count / 2);

                areaConstIndexList.Add(id0);
                areaConstIndexList.Add(id1);
                areaConstIndexList.Add(id2);

                skip++;
            }
        }

        // Offset
        for (int i = 0; i < areaConstIndexList.Count; i++)
        {
            areaConstIndexList[i] += indexOffset;
        }


        // ShapeMatching拘束を追加
        List<int> shapeMatchIndexList = new List<int>();
        List<int> shapeMatchCountList = new List<int>();

        for (int i = 0; i < positionList.Count / 2; i++)
        {
            List<int> indexList = new List<int>();
            for (int j = 0; j < shapeMatchSize; j++)
            {
                if (i + j < positionList.Count / 2) indexList.Add(i + j);
                else indexList.Add(i + j - positionList.Count / 2);

                if (i + j + positionList.Count / 2 < positionList.Count) indexList.Add(i + j + positionList.Count / 2);
                else indexList.Add(i + j + positionList.Count / 2 - positionList.Count / 2);
            }

            shapeMatchIndexList.AddRange(indexList);
            shapeMatchCountList.Add(indexList.Count);
        }

        // Offset
        for (int i = 0; i < shapeMatchIndexList.Count; i++)
        {
            shapeMatchIndexList[i] += indexOffset;
        }

        // 描画用
        List<int> edgeIndexList = new List<int>();
        for (int i = 0; i < positionList.Count / 2; i++)
        {
            edgeIndexList.Add(i);
            edgeIndexList.Add((i + 1 < positionList.Count / 2) ? i + 1 : 0);
        }

        // Offset
        for (int i = 0; i < edgeIndexList.Count; i++)
        {
            edgeIndexList[i] += indexOffset;
        }

        this.positionList.AddRange(positionList);
        this.areaConstIndexList.AddRange(areaConstIndexList);
        this.shapeMatchIndexList.AddRange(shapeMatchIndexList);
        this.shapeMatchCountList.AddRange(shapeMatchCountList);
        this.edgeIndexList.AddRange(edgeIndexList);

        selectedIndexList.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawSelectedVertices();

        DrawParticles();

        if (drawAreaConst) DrawAreaConstraints();

        if (drawShapeMatch) DrawShapeMatch();
    }

    private void DrawSelectedVertices()
    {
        for (int i = 0; i < selectedIndexList.Count; i++)
        {
            Handles.Label(mesh.vertices[selectedIndexList[i]] + Vector3.up * 0.1f, i.ToString());
        }
    }

    private void DrawParticles()
    {
        Gizmos.color = Color.green;

        for (int i = 0; i < positionList.Count; i++)
        {
            Gizmos.DrawSphere(positionList[i], 0.035f);
        }
    }

    private void DrawAreaConstraints()
    {
        Gizmos.color = Color.red;

        for (int i = 0; i < areaConstIndexList.Count; i += 3)
        {
            if (drawingAreaKey < 0 || i == drawingAreaKey * 3)
            {
                Vector3 p0 = positionList[areaConstIndexList[i + 0]];
                Vector3 p1 = positionList[areaConstIndexList[i + 1]];
                Vector3 p2 = positionList[areaConstIndexList[i + 2]];

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p0);
            }
        }
    }

    private void DrawShapeMatch()
    {
        Gizmos.color = Color.blue;
        int pOffset = 0;
        for (int i = 0; i < shapeMatchCountList.Count; i++)
        {
            int count = shapeMatchCountList[i];

            if (drawingShapeMatchKey < 0 || i == drawingShapeMatchKey)
            {
                for (int j = 0; j < count; j++)
                {
                    Vector3 p0 = positionList[shapeMatchIndexList[pOffset + j]];
                    Vector3 p1 = positionList[shapeMatchIndexList[(j <= count - 2) ? pOffset + j + 1 : pOffset]];

                    Gizmos.DrawLine(p0, p1);
                }
            }

            pOffset += count;
        }
    }
#endif

    private void Export()
    {
        // Wrapperを生成
        var wrapper = new SimulationObjectDefinition();
        wrapper.type = objName;
        wrapper.particles = positionList.ToArray();
        // wrapper.distConstIndices = distConstIndices;
        wrapper.areaConstIndices = areaConstIndexList.ToArray();
        wrapper.shapeMatchIndices = shapeMatchIndexList.ToArray();
        wrapper.shapeMatchCounts = shapeMatchCountList.ToArray();

        wrapper.edgeIndices = edgeIndexList.ToArray();

        wrapper.meshVertices = meshVertices;
        wrapper.meshTriangles = meshTriangles;
        wrapper.vToPReferences = vToPReferenceList.ToArray();

        // jsonに変換
        string json = JsonUtility.ToJson(wrapper, true); // trueで改行

        // 書き出し実行
        string allPath = Path.Combine(Application.dataPath + "/" + fileNameIncludeDotJson);
        File.WriteAllText(allPath, json);
        Debug.Log("書き出し完了。出力先：" + allPath);
    }
}
