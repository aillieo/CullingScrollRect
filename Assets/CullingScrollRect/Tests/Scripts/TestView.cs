using AillieoUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestView : MonoBehaviour
{
    public TestData testData;

    public CullingScrollRect scrollRect;

    private List<ChildItemHandle> edgeHandles = new List<ChildItemHandle>();
    private List<ChildItemHandle> iconHandles = new List<ChildItemHandle>();

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
            var handle = scrollRect.AddChild(
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
            edgeHandles.Add(handle);
        }

        RectTransform itemTemplateIcon = testData.itemTemplateIcon;
        Vector2 iconSize = itemTemplateIcon.rect.size;
        foreach (var iconPos in testData.nodes)
        {
            var handle = scrollRect.AddChild(
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
            iconHandles.Add(handle);
        }

        scrollRect.SetDirty(true);

        scrollRect.normalizedPosition = new Vector2(0, 0.5f);

    }

    public void AddRandom()
    {
        RectTransform itemTemplateIcon = testData.itemTemplateIcon;
        Vector2 iconSize = itemTemplateIcon.rect.size;
        Vector2 iconPos = new Vector2(
            Random.value * scrollRect.content.sizeDelta.x,
            Random.value * scrollRect.content.sizeDelta.y
            );
        var handle = scrollRect.AddChild(
                () => {
                    GameObject go = GameObject.Instantiate(testData.itemTemplateIcon.gameObject, this.scrollRect.content);
                    RectTransform r = go.GetComponent<RectTransform>();
                    r.SetAsLastSibling();
                    return r;
                },
                (rect) => { GameObject.Destroy(rect.gameObject); },
                iconPos,
                iconSize,
                0);
        iconHandles.Add(handle);
    }

    public void RemoveRandom()
    {
        if (iconHandles.Count == 0)
        {
            return;
        }
        int index = Random.Range(0, iconHandles.Count);
        ChildItemHandle handle = iconHandles[index];
        scrollRect.RemoveChild(handle);
        iconHandles.Remove(handle);
    }

    public void FocusRandom()
    {
        if(iconHandles.Count == 0)
        {
            return;
        }
        int index = Random.Range(0, iconHandles.Count);
        ChildItemHandle handle = iconHandles[index];
        scrollRect.ScrollTo(handle);
    }
}
