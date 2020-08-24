using AillieoUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestView : MonoBehaviour
{
    public TestData testData;

    public CullingScrollRect scrollRect;

    void Start()
    {
        InitScrollView();
    }

    private void InitScrollView()
    {
        RectTransform itemTemplateLine = testData.itemTemplateLine;
        float lineHeight = itemTemplateLine.rect.height;
        foreach (var edge in testData.edges)
        {
            Vector2 pos0 = testData.nodes[edge[0]];
            Vector2 pos1 = testData.nodes[edge[1]];

            Vector2 pos = 0.5f * (pos0 + pos1);
            float length = Vector2.Distance(pos0, pos1);
            float rot = Vector2.SignedAngle(pos1 - pos0, Vector2.right);
            scrollRect.AddChild(
                () => {
                    GameObject go = GameObject.Instantiate(itemTemplateLine.gameObject, this.scrollRect.content);
                    RectTransform r = go.GetComponent<RectTransform>();
                    Vector2 size = r.rect.size;
                    size.x = length;
                    r.sizeDelta = size;
                    Vector3 angles = r.localEulerAngles;
                    angles.z = rot;
                    r.localEulerAngles = angles;
                    r.SetAsFirstSibling();
                    return r;
                },
                (rect) => { GameObject.Destroy(rect.gameObject); },
                (pos0 + pos1) * 0.5f,
                new Vector2(length, lineHeight),
                rot);
        }

        RectTransform itemTemplateIcon = testData.itemTemplateIcon;
        Vector2 iconSize = itemTemplateIcon.rect.size;
        foreach (var iconPos in testData.nodes)
        {
            scrollRect.AddChild(
                () => {
                    GameObject go = GameObject.Instantiate(itemTemplateIcon.gameObject, this.scrollRect.content);
                    RectTransform r = go.GetComponent<RectTransform>();
                    r.SetAsLastSibling();
                    return r;
                },
                (rect) => { GameObject.Destroy(rect.gameObject); },
                iconPos,
                iconSize,
                0);
        }

        this.scrollRect.normalizedPosition = new Vector2(0, 0.5f);
        scrollRect.content.sizeDelta += new Vector2(100, 200);

        scrollRect.PerformCulling();
    }

}
