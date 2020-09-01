using System.Linq;
using UnityEngine.Assertions;

namespace AillieoUtils
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using UnityEngine.UI;

    [AddComponentMenu("UI/Culling Scroll Rect")]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CullingScrollRect : ScrollRect
    {

        private class ComparerForLeftBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle lh, ChildItemHandle rh) { return lh.Left().CompareTo(rh.Left()); } }
        private class ComparerForRightBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle lh, ChildItemHandle rh) { return lh.Right().CompareTo(rh.Right()); } }
        private class ComparerForTopBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle lh, ChildItemHandle rh) { return lh.Top().CompareTo(rh.Top()); } }
        private class ComparerForBottomBound : IComparer<ChildItemHandle> { public int Compare(ChildItemHandle lh, ChildItemHandle rh) { return lh.Bottom().CompareTo(rh.Bottom()); } }

        private static readonly ComparerForLeftBound comparerForLeftBound = new ComparerForLeftBound();
        private static readonly ComparerForRightBound comparerForRightBound = new ComparerForRightBound();
        private static readonly ComparerForTopBound comparerForTopBound = new ComparerForTopBound();
        private static readonly ComparerForBottomBound comparerForBottomBound = new ComparerForBottomBound();

        private readonly List<ChildItemHandle> sortedForLeft = new List<ChildItemHandle>();
        private readonly List<ChildItemHandle> sortedForRight = new List<ChildItemHandle>();
        private readonly List<ChildItemHandle> sortedForTop = new List<ChildItemHandle>();
        private readonly List<ChildItemHandle> sortedForBottom = new List<ChildItemHandle>();

        private static class DirtyFlags
        {
            public const int ListOrder          = 1 << 1;
            public const int CriticalIndex      = 1 << 2;
            public const int ContentBorderLT    = 1 << 3;
            public const int ContentBorderRB    = 1 << 4;
            public const int ContentSizeExpand  = 1 << 5;
            public const int ContentSizeShrink  = 1 << 6;

            public const int All = ListOrder | CriticalIndex | ContentBorderLT | ContentBorderRB | ContentSizeExpand | ContentSizeShrink;
        }

        private struct CriticalItems
        {
            public int leftToHide;
            public int rightToHide;
            public int topToHide;
            public int bottomToHide;

            public int leftToShow;
            public int rightToShow;
            public int topToShow;
            public int bottomToShow;

            public void Reset()
            {
                leftToHide =    -1;
                rightToHide =   -1;
                topToHide =     -1;
                bottomToHide =  -1;

                leftToShow =    -1;
                rightToShow =   -1;
                topToShow =     -1;
                bottomToShow =  -1;
            }

            public bool AnyInvalid()
            {
                return leftToHide < 0 || rightToHide < 0 || topToHide < 0 || bottomToHide < 0 ||
                    leftToShow < 0 || rightToShow < 0 || topToShow < 0 || bottomToShow < 0;
            }
        }

        private CriticalItems criticalItems;

        private int dirtyFlag = 0;
        private readonly HashSet<ChildItemHandle> newHandles = new HashSet<ChildItemHandle>();
        private static readonly HashSet<ChildItemHandle> dirtyHandles = new HashSet<ChildItemHandle>();

        public Vector2 borderLeftTop
        {
            get => m_BordersLT;
            set
            {
                if (m_BordersLT != value)
                {
                    lastBorderValue.x = m_BordersLT.x;
                    lastBorderValue.y = m_BordersLT.y;
                    m_BordersLT = value;
                    InternalSetDirty(DirtyFlags.ContentBorderLT);
                }
            }
        }

        public Vector2 borderRightBottom
        {
            get => m_BordersRB;
            set
            {
                if (m_BordersRB != value)
                {
                    lastBorderValue.z = m_BordersRB.x;
                    lastBorderValue.w = m_BordersRB.y;
                    m_BordersRB = value;
                    InternalSetDirty(DirtyFlags.ContentBorderRB);
                }
            }
        }

        [SerializeField]
        private Vector2 m_BordersLT;
        [SerializeField]
        private Vector2 m_BordersRB;

        private Vector4 lastBorderValue;

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
            sortedForLeft.Add(newChild);
            sortedForRight.Add(newChild);
            sortedForTop.Add(newChild);
            sortedForBottom.Add(newChild);

            newHandles.Add(newChild);

            InternalSetDirty(DirtyFlags.ContentSizeExpand | DirtyFlags.ListOrder);
        }

        public void RemoveAllChildren()
        {
            foreach (var h in sortedForLeft)
            {
                h.SetVisible(false);
            }
            sortedForLeft.Clear();
            sortedForRight.Clear();
            sortedForTop.Clear();
            sortedForBottom.Clear();

            InternalSetDirty(DirtyFlags.All);
        }

        public bool RemoveChild(ChildItemHandle handle)
        {
            if (handle.owner != this)
            {
                return false;
            }

            handle.SetVisible(false);

            bool remove = sortedForLeft.Remove(handle);
            if (remove)
            {
                sortedForRight.Remove(handle);
                sortedForTop.Remove(handle);
                sortedForBottom.Remove(handle);

                InternalSetDirty(DirtyFlags.ContentSizeShrink | DirtyFlags.CriticalIndex);
            }
            return remove;
        }

        public void ScrollTo(ChildItemHandle handle)
        {
            if (handle.owner != this)
            {
                return;
            }

            Vector2 position = handle.Position;
            Vector2 contentSize = content.sizeDelta;
            this.normalizedPosition = new Vector2(
                position.x / contentSize.x,
                1 - position.y / contentSize.y
                );
            InternalSetDirty(DirtyFlags.CriticalIndex);
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

            lastBorderValue = new Vector4(borderLeftTop.x, borderLeftTop.y, borderRightBottom.x, borderRightBottom.y);

            PerformCulling(false);
            // todo: create internal pools for item template
        }

        private void ExpandContentSize(IEnumerable<ChildItemHandle> newChildItems)
        {
            Rect contentRect = content.rect;
            Vector2 newMax = contentRect.size - borderRightBottom;
            foreach (ChildItemHandle item in newChildItems)
            {
                Rect childRect = item.AABB;
                newMax.x = Mathf.Max(newMax.x, childRect.xMax);
                newMax.y = Mathf.Max(newMax.y, -childRect.yMin);
            }
            content.sizeDelta = borderLeftTop + newMax + borderRightBottom;
        }

        private void UpdateVisibility(IEnumerable<ChildItemHandle> childItems)
        {
            foreach (ChildItemHandle item in childItems)
            {
                item.SetVisible(IsSeen(item));
            }
        }

        private Rect refRect;

        // from UI.ScrollRect
        private Vector2 m_prevPosition;
        private Vector2 m_curDelta;

        public void SetDirty(bool immediateUpdate = false)
        {
            InternalSetDirty(DirtyFlags.All);
            if (immediateUpdate)
            {
                InternalUpdateView();
            }
        }

        private void InternalSetDirty(int flag)
        {
            if (dirtyFlag == 0)
            {
                StartCoroutine(DelayUpdateView());
            }
            dirtyFlag |= flag;
        }

        private IEnumerator DelayUpdateView()
        {
            yield return null;
            InternalUpdateView();
        }

        private void InternalUpdateView()
        {
            if (dirtyFlag == 0)
            {
                return;
            }

            //int f = dirtyFlag;
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            // 依此判断各个flag

            // 1. 更新border
            if((dirtyFlag & DirtyFlags.ContentBorderRB) > 0)
            {
                Vector2 size = content.sizeDelta - new Vector2(lastBorderValue.z, lastBorderValue.w);
                size += borderRightBottom;
                content.sizeDelta = size;
                dirtyFlag &= ~DirtyFlags.ContentBorderRB;
            }

            if((dirtyFlag & DirtyFlags.ContentBorderLT) > 0)
            {
                //Vector2 delta = new Vector2(borderLeftTop.x - lastBorderValue.x, borderLeftTop.y - lastBorderValue.y);
                //foreach (ChildItemHandle handle in sortedForLeft)
                //{
                //    RectTransform item = handle.Item;
                //    if (item != null)
                //    {
                //        item.anchoredPosition += delta;
                //    }
                //}
                dirtyFlag &= ~DirtyFlags.ContentBorderLT;
            }

            // 2. 关于contentSize的更新
            // 三种情况 2-1.有新增有删除 2-2.只有删除 2-3.只有新增
            if ((dirtyFlag & DirtyFlags.ContentSizeShrink) > 0)
            {
                if ((dirtyFlag & DirtyFlags.ContentSizeExpand) > 0)
                {
                    // 2-1
                    newHandles.Clear();
                    dirtyFlag &= ~DirtyFlags.ContentSizeExpand;
                    dirtyFlag |= DirtyFlags.ListOrder;
                    dirtyFlag |= DirtyFlags.CriticalIndex;
                }

                // 2-2
                content.sizeDelta = Vector2.zero;
                ExpandContentSize(sortedForLeft);
                dirtyFlag &= ~DirtyFlags.ContentSizeShrink;
            }
            else if((dirtyFlag & DirtyFlags.ContentSizeExpand) > 0)
            {
                // 2-3
                ExpandContentSize(newHandles);
                newHandles.Clear();
                dirtyFlag &= ~DirtyFlags.ContentSizeExpand;
            }

            // 3. 是否需要重新sort
            if((dirtyFlag & DirtyFlags.ListOrder) > 0)
            {
                sortedForLeft.Sort(comparerForLeftBound);
                sortedForRight.Sort(comparerForRightBound);
                sortedForTop.Sort(comparerForTopBound);
                sortedForBottom.Sort(comparerForBottomBound);
                dirtyFlag &= ~DirtyFlags.ListOrder;
                dirtyFlag &= DirtyFlags.CriticalIndex;
            }

            // 4. 是否需要重新获取critical
            if((dirtyFlag & DirtyFlags.CriticalIndex) > 0)
            {
                FindAllCriticalItems();
                dirtyFlag &= ~DirtyFlags.CriticalIndex;
            }

            // 最后需要更新一次culling
            PerformCulling(false);

            Assert.AreEqual(dirtyFlag, 0);

            //sw.Stop();
            //long ms = sw.ElapsedMilliseconds;
            //Debug.Log($"InternalUpdateView: f=B{ Convert.ToString(f, 2)} ms={ms}");
        }

        private void FindAllCriticalItems()
        {
            // 重新获取关键index
            criticalItems.Reset();

            float left = refRect.x;
            float right = refRect.xMax;
            float top = refRect.yMax;
            float bottom = refRect.y;

            FindCritical(sortedForLeft, left, itemHandle => itemHandle.Left(), ref criticalItems.leftToShow, ref criticalItems.leftToHide);
            FindCritical(sortedForRight, right, itemHandle => itemHandle.Right(), ref criticalItems.rightToHide, ref criticalItems.rightToShow);
            FindCritical(sortedForTop, top, itemHandle => itemHandle.Top(), ref criticalItems.topToHide, ref criticalItems.topToShow);
            FindCritical(sortedForBottom, bottom, itemHandle => itemHandle.Bottom(), ref criticalItems.bottomToShow, ref criticalItems.bottomToHide);
        }

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            base.SetContentAnchoredPosition(position);

            m_curDelta = content.anchoredPosition - m_prevPosition;
            m_prevPosition = content.anchoredPosition;

            PerformCulling(true);
        }

        protected override void SetNormalizedPosition(float value, int axis)
        {
            base.SetNormalizedPosition(value, axis);
            InternalSetDirty(DirtyFlags.CriticalIndex);
        }

        private void PerformCulling(bool criticalItemsOnly)
        {
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            UpdateRefRect();

            if (criticalItemsOnly)
            {
                UpdateAllCriticalItems();
            }
            else
            {
                UpdateVisibility(sortedForLeft);
            }

            //sw.Stop();
            //long ms = sw.ElapsedMilliseconds;
            //Debug.Log($"PerformCulling: ms={ms}");
        }

        private void UpdateAllCriticalItems()
        {
            dirtyHandles.Clear();
            int itemCount = sortedForLeft.Count;
            if (horizontal)
            {
                float left = refRect.x;
                float right = refRect.xMax;
                if (m_curDelta.x > 0)
                {
                    // 向右
                    UpdateCritical(
                        sortedForLeft,
                        sortedForRight,
                        ref criticalItems.leftToShow,
                        ref criticalItems.leftToHide,
                        ref criticalItems.rightToShow,
                        ref criticalItems.rightToHide,
                        r => r.Left(),
                        r => r.Right(),
                        left,
                        right,
                        true,
                        dirtyHandles);
                }
                else if (m_curDelta.x < 0)
                {
                    // 向左
                    UpdateCritical(
                        sortedForLeft,
                        sortedForRight,
                        ref criticalItems.leftToShow,
                        ref criticalItems.leftToHide,
                        ref criticalItems.rightToShow,
                        ref criticalItems.rightToHide,
                        r => r.Left(),
                        r => r.Right(),
                        left,
                        right,
                        false,
                        dirtyHandles);
                }
            }

            if (vertical)
            {
                float top = refRect.yMax;
                float bottom = refRect.y;
                if (m_curDelta.y > 0)
                {
                    // 向上
                    UpdateCritical(
                        sortedForBottom,
                        sortedForTop,
                        ref criticalItems.bottomToShow,
                        ref criticalItems.bottomToHide,
                        ref criticalItems.topToShow,
                        ref criticalItems.topToHide,
                        r => r.Bottom(),
                        r => r.Top(),
                        bottom,
                        top,
                        true,
                        dirtyHandles);
                }
                else if (m_curDelta.y < 0)
                {
                    // 向下
                    UpdateCritical(
                        sortedForBottom,
                        sortedForTop,
                        ref criticalItems.bottomToShow,
                        ref criticalItems.bottomToHide,
                        ref criticalItems.topToShow,
                        ref criticalItems.topToHide,
                        r => r.Bottom(),
                        r => r.Top(),
                        bottom,
                        top,
                        false,
                        dirtyHandles);
                }
            }

            UpdateVisibility(dirtyHandles);
            dirtyHandles.Clear();
        }

        private readonly Vector3[] viewWorldCorners = new Vector3[4];
        private readonly Vector3[] rectCorners = new Vector3[2];

        private void UpdateRefRect()
        {
            // refRect:  viewPort in content-space
            viewRect.GetWorldCorners(viewWorldCorners);

            rectCorners[0] = content.transform.InverseTransformPoint(viewWorldCorners[0]);
            rectCorners[1] = content.transform.InverseTransformPoint(viewWorldCorners[2]);

            // Debug.Log($"{viewWorldCorners[0]}||{viewWorldCorners[2]}||{rectCorners[0]}||{rectCorners[1]}");
            refRect = new Rect(rectCorners[0], rectCorners[1] - rectCorners[0]);
        }

        //protected override void OnDestroy()
        //{
        //    // tod: purge pools
        //}

        private static void FindCritical(List<ChildItemHandle> list, float critical, Func<ChildItemHandle,float> compareValue, ref int lastLess, ref int firstGreater)
        {
            if(list.Count == 0)
            {
                return;
            }

            if(list.Count == 1)
            {
                lastLess = 0;
                firstGreater = 0;
                return;
            }

            // 找到最后一个小于的 和第一个大于的
            for (int i = 0, len = list.Count; i + 1 < len; ++ i)
            {
                if (compareValue(list[i]) <= critical && compareValue(list[i + 1]) > critical)
                {
                    lastLess = i;
                    firstGreater = i + 1;
                    return;
                }
            }

            if(compareValue(list[0]) > critical)
            {
                // 所有都大于
                lastLess = 0;
                firstGreater = 0;
            }
            else
            {
                // 所有都小于
                lastLess = list.Count - 1;
                firstGreater = list.Count - 1;
            }
        }

        private static void UpdateCritical(
            List<ChildItemHandle> sortedForLowBound,
            List<ChildItemHandle> sortedForHighBound,
            ref int lowToShow,
            ref int lowToHide,
            ref int highToShow,
            ref int highToHide,
            Func<ChildItemHandle, float> lowCompareValue,
            Func<ChildItemHandle, float> highCompareValue,
            float lowBound,
            float highBound,
            bool movingForward,
            HashSet<ChildItemHandle> dirtyItemsToFill
            )
        {

            int itemCount = sortedForLowBound.Count;
            if(movingForward)
            {
                while (lowToShow >= 0 && lowToShow < itemCount && highCompareValue(sortedForHighBound[lowToShow]) > lowBound)
                {
                    dirtyItemsToFill.Add(sortedForHighBound[lowToShow]);
                    lowToShow--;
                }
                lowToShow = Mathf.Clamp(lowToShow, 0, itemCount - 1);

                while (highToHide >= 0 && highToHide < itemCount && lowCompareValue(sortedForLowBound[highToHide]) > highBound)
                {
                    dirtyItemsToFill.Add(sortedForLowBound[highToHide]);
                    highToHide--;
                }
                highToHide = Mathf.Clamp(highToHide, 0, itemCount - 1);

                //
                while (highToShow >= 0 && highToShow < itemCount && lowCompareValue(sortedForLowBound[highToShow]) > highBound)
                {
                    highToShow--;
                }
                highToShow = Mathf.Clamp(highToShow, 0, itemCount - 1);

                while (lowToHide >= 0 && lowToHide < itemCount && highCompareValue(sortedForHighBound[lowToHide]) > lowBound)
                {
                    lowToHide--;
                }
                lowToHide = Mathf.Clamp(lowToHide, 0, itemCount - 1);
            }
            else
            {
                while (highToShow >= 0 && highToShow < itemCount && lowCompareValue(sortedForLowBound[highToShow]) < highBound)
                {
                    dirtyHandles.Add(sortedForLowBound[highToShow]);
                    highToShow++;
                }
                highToShow = Mathf.Clamp(highToShow, 0, itemCount - 1);

                while (lowToHide >= 0 && lowToHide < itemCount && highCompareValue(sortedForHighBound[lowToHide]) < lowBound)
                {
                    dirtyHandles.Add(sortedForHighBound[lowToHide]);
                    lowToHide++;
                }
                lowToHide = Mathf.Clamp(lowToHide, 0, itemCount - 1);

                //
                while (lowToShow >= 0 && lowToShow < itemCount && highCompareValue(sortedForHighBound[lowToShow]) < lowBound)
                {
                    lowToShow++;
                }
                lowToShow = Mathf.Clamp(lowToShow, 0, itemCount - 1);

                while (highToHide >= 0 && highToHide < itemCount && lowCompareValue(sortedForLowBound[highToHide]) < highBound)
                {
                    highToHide++;
                }
                highToHide = Mathf.Clamp(highToHide, 0, itemCount - 1);
            }
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color backup = Gizmos.color;

            foreach (var h in sortedForLeft)
            {
                Rect r = h.AABB;

                Gizmos.color = Color.blue;

                if(!criticalItems.AnyInvalid())
                {
                    bool criticalToShow =
                        h == sortedForLeft[criticalItems.leftToShow] ||
                        h == sortedForRight[criticalItems.rightToShow] ||
                        h == sortedForTop[criticalItems.topToShow] ||
                        h == sortedForBottom[criticalItems.bottomToShow];

                    bool criticalToHide =
                        h == sortedForLeft[criticalItems.leftToHide] ||
                        h == sortedForRight[criticalItems.rightToHide] ||
                        h == sortedForTop[criticalItems.topToHide] ||
                        h == sortedForBottom[criticalItems.bottomToHide];

                    if (criticalToShow)
                    {
                        Gizmos.color = Color.yellow;
                    }
                    if (criticalToHide)
                    {
                        Gizmos.color = Color.red;
                    }
                }

                Vector2 p00 = content.TransformPoint(r.min);
                Vector2 p11 = content.TransformPoint(r.max);
                Vector2 p01 = content.TransformPoint(new Vector2(r.x, r.y + r.height));
                Vector2 p10 = content.TransformPoint(new Vector2(r.x + r.width, r.y));

                Gizmos.DrawLine(p00, p01);
                Gizmos.DrawLine(p01, p11);
                Gizmos.DrawLine(p11, p10);
                Gizmos.DrawLine(p10, p00);
            }

            Gizmos.color = backup;
        }

#endif

    }

}
