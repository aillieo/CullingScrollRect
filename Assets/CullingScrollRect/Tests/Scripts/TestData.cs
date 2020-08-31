using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "TestData", menuName = "TestData")]
public class TestData : ScriptableObject
{
    public RectTransform itemTemplateIcon;
    public RectTransform itemTemplateLine;

    public Vector2[] nodes;
    public Vector2Int[] edges;
}


#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(TestData))]
public class TestDataEditor : UnityEditor.Editor
{
    int levelCount = 10;
    float splitFactor = 0.86f;
    float xInterval = 300;
    float yInterval = 250;

    public override void OnInspectorGUI()
    {
        GUILayout.BeginVertical("box");
        levelCount = UnityEditor.EditorGUILayout.IntField("Level Count", levelCount);
        splitFactor = UnityEditor.EditorGUILayout.FloatField("Split Factor", splitFactor);
        xInterval = UnityEditor.EditorGUILayout.FloatField("X Interval", xInterval);
        yInterval = UnityEditor.EditorGUILayout.FloatField("Y Interval", yInterval);
        if (GUILayout.Button("Generate"))
        {
            GenerateData();
        }
        GUILayout.EndVertical();

        base.OnInspectorGUI();
    }

    private void GenerateData()
    {
        List<List<int>> levels = new List<List<int>>();
        List<Vector2Int> edges = new List<Vector2Int>();
        int nodeCountInLevel = 1;
        int nodeIndex = 0;
        for (int i = 0; i < levelCount; ++i)
        {
            bool lastLevel = (i == levelCount - 1);

            List<int> nodes = new List<int>();
            levels.Add(nodes);

            int splitTimes = 0;
            for (int j = 0; j < nodeCountInLevel; ++j)
            {
                nodes.Add(nodeIndex);

                if (!lastLevel)
                {
                    edges.Add(new Vector2Int(nodeIndex, nodeIndex + nodeCountInLevel + splitTimes));

                    if (Random.value < splitFactor / i)
                    {
                        splitTimes++;
                        edges.Add(new Vector2Int(nodeIndex, nodeIndex + nodeCountInLevel + splitTimes));
                    }
                }

                nodeIndex++;
            }

            nodeCountInLevel += splitTimes;
        }

        float height = yInterval * (levels[levelCount - 1].Count - 1);
        UnityEditor.SerializedProperty spNodes = serializedObject.FindProperty("nodes");
        spNodes.arraySize = nodeIndex;
        for (int i = 0; i < levels.Count; ++i)
        {
            List<int> nodes = levels[i];
            float yStart = height * 0.5f - (nodes.Count - 1) * yInterval * 0.5f;
            for (int j = 0; j < nodes.Count; ++j)
            {
                spNodes.GetArrayElementAtIndex(nodes[j]).vector2Value = new Vector2(xInterval * i + j, yStart + yInterval * j);
            }
        }

        UnityEditor.SerializedProperty spEdges = serializedObject.FindProperty("edges");
        spEdges.arraySize = edges.Count;
        for (int i = 0; i < edges.Count; ++i)
        {
            spEdges.GetArrayElementAtIndex(i).vector2IntValue = edges[i];
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
