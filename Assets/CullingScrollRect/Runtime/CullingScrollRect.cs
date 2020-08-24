namespace AillieoUtils
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using UnityEngine.UI;

    public class CullingScrollRect : ScrollRect
    {
        //private class ComparerForLeftBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle x, ChildItemHandle y) { return x.Rect.xMin.CompareTo(y.Rect.xMin); } }
        //private class ComparerForRightBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle x, ChildItemHandle y) { return x.Rect.xMax.CompareTo(y.Rect.xMax); } }
        //private class ComparerForTopBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle x, ChildItemHandle y) { return x.Rect.yMax.CompareTo(y.Rect.yMax); } }
        //private class ComparerForBottomBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle x, ChildItemHandle y) { return x.Rect.yMin.CompareTo(y.Rect.yMin); } }

        //private static readonly ComparerForLeftBound comparerForLeftBound = new ComparerForLeftBound();
        //private static readonly ComparerForRightBound comparerForRightBound = new ComparerForRightBound();
        //private static readonly ComparerForTopBound comparerForTopBound = new ComparerForTopBound();
        //private static readonly ComparerForBottomBound comparerForBottomBound = new ComparerForBottomBound();

        //private List<ChildItemHandle> sortedForLeft = new List<ChildItemHandle>();
        //private List<ChildItemHandle> sortedForRight = new List<ChildItemHandle>();
        //private List<ChildItemHandle> sortedForTop = new List<ChildItemHandle>();
        //private List<ChildItemHandle> sortedForBottom = new List<ChildItemHandle>();

        //private int[] criticalIndex = new int[8];
        
        //private static class CriticalItemType
        //{
        //    public const int TopToHide = 0;
        //    public const int BottomToHide = 1;
        //    public const int LeftToHide = 2;
        //    public const int RightToHide = 3;

        //    public const int TopToShow = 4;
        //    public const int BottomToShow = 5;
        //    public const int LeftToShow = 6;
        //    public const int RightToShow = 7;
        //}

        private HashSet<ChildItemHandle> handles = new HashSet<ChildItemHandle>();

        public ChildItemHandle AddChild(Func<RectTransform> createFunc, Action<RectTransform> recycleFunc, Vector2 position, Vector2 size, float rotationZ)
        {
            ChildItemHandle handle = new ChildItemHandle(this, createFunc, recycleFunc, position, size, rotationZ);
            InternalAddChild(handle);
            return handle;
        }

        public ChildItemHandle AddChild(RectTransform template, Vector2 position, Vector2 size, float rotationZ)
        {
            ChildItemHandle handle = new ChildItemHandle(this, template, position, size, rotationZ);
            InternalAddChild(handle);
            return handle;
        }

        private void InternalAddChild(ChildItemHandle newChild)
        {
            handles.Add(newChild);
            newChild.SetVisible(IsSeen(newChild));

            // todo: add dirty flag
            ExpandContentSize(newChild);
        }

        public void RemoveAllChildren()
        {
            foreach (var h in handles)
            {
                h.SetVisible(false);
            }

            handles.Clear();
        }

        public bool RemoveChild(ChildItemHandle handle)
        {
            handle.SetVisible(false);
            return handles.Remove(handle);
        }

        private bool IsSeen(ChildItemHandle childItemHandle)
        {
            Rect viewPortRect = refRect;
            Rect itemRect = childItemHandle.AABB;
            bool overlap = viewPortRect.Overlaps(itemRect);
            //Debug.Log($"{viewPortRect} \n {itemRect}    -->{overlap}");
            return overlap;
        }

        protected override void Start()
        {
            base.Start();

            content.pivot = Vector2.up;

            m_curDelta = content.anchoredPosition - m_prevPosition;
            m_prevPosition = content.anchoredPosition;

            PerformCulling();
            // todo: create internal pools for item template
        }

        void ExpandContentSize(ChildItemHandle newChildItem)
        {
            Rect contentRect = content.rect;
            Rect childRect = newChildItem.AABB;
            Vector2 newMin = Vector2.zero;
            Vector2 newMax = contentRect.size;

            newMin.x = Mathf.Min(newMin.x, childRect.min.x);
            newMin.y = Mathf.Min(newMin.y, -childRect.max.y);
            newMax.x = Mathf.Max(newMax.x, childRect.max.x);
            newMax.y = Mathf.Max(newMax.y, -childRect.min.y);

            content.sizeDelta = newMax;

            UpdateRefRect();
        }

        private Rect refRect;

        // from UI.ScrollRect
        private  Vector2 m_prevPosition;
        private Vector2 m_curDelta;

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            base.SetContentAnchoredPosition(position);

            m_curDelta = content.anchoredPosition - m_prevPosition;
            m_prevPosition = content.anchoredPosition;

            PerformCulling();
        }

        public void PerformCulling()
        {
            UpdateRefRect();

            foreach (var h in handles)
            {
                h.SetVisible(IsSeen(h));
            }
        }

        Vector3[] viewWorldConers = new Vector3[4];
        Vector3[] rectCorners = new Vector3[2];

        private void UpdateRefRect()
        {
            // refRect:  viewPort in content-space
            viewRect.GetWorldCorners(viewWorldConers);

            rectCorners[0] = content.transform.InverseTransformPoint(viewWorldConers[0]);
            rectCorners[1] = content.transform.InverseTransformPoint(viewWorldConers[2]);

            // Debug.Log($"{viewWorldConers[0]}||{viewWorldConers[2]}||{rectCorners[0]}||{rectCorners[1]}");
            refRect = new Rect(rectCorners[0], rectCorners[1] - rectCorners[0]);
        }

        //protected override void OnDestroy()
        //{
        //    // tod: purge pools
        //}

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            foreach (var h in handles)
            {
                Rect r = h.AABB;

                //Vector3 p = this.content.InverseTransformPoint(r.position);
                Vector2 offset = this.content.position;
                r.position = r.position + offset;

                Vector2 p00 = r.min;
                Vector2 p11 = r.max;
                Vector2 p01 = new Vector2(r.x, r.y + r.height);
                Vector2 p10 = new Vector2(r.x + r.width, r.y);

                Gizmos.DrawLine(p00, p01);
                Gizmos.DrawLine(p01, p11);
                Gizmos.DrawLine(p11, p10);
                Gizmos.DrawLine(p10, p00);
            }
        }
#endif
    }

}
