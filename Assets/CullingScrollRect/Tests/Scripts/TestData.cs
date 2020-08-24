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
