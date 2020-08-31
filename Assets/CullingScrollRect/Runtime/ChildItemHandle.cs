// #define DEBUG_MODE

namespace AillieoUtils
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using UnityEngine.UI;

    public class ChildItemHandle
    {
        public RectTransform Item { get; private set; } = null;

        public Vector2 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    aabb = null;
                }
            }
        }

        public Vector2 Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    aabb = null;
                }
            }
        }

        public float RotationZ
        {
            get => rotationZ;
            set
            {
                if (rotationZ != value)
                {
                    rotationZ = value;
                    aabb = null;
                }
            }
        }

        private Vector2 position;
        private Vector2 size;
        private float rotationZ;
        public readonly RectTransform template;
        public readonly Func<RectTransform> createFunc;
        public readonly Action<RectTransform> recycleFunc;
        public readonly CullingScrollRect owner;
        private Rect? aabb;

        public Rect AABB
        {
            get
            {
                if (aabb == null)
                {

                    Vector2 p = position;
                    p.y = -p.y;
                    Vector2 s = size;

                    if (rotationZ == 0)
                    {
                    }
                    else
                    {
                        float sin = Mathf.Abs(Mathf.Sin(rotationZ * Mathf.Deg2Rad));
                        float cos = Mathf.Abs(Mathf.Cos(rotationZ * Mathf.Deg2Rad));

                        float xRange = size.x * cos + size.y * sin;
                        float yRange = size.x * sin + size.y * cos;

                        s = new Vector2(xRange, yRange);
                    }

                    p = p - s * 0.5f;
                    aabb = new Rect(p, s);
                }

                return aabb.Value;
            }
        }

        public void SetVisible(bool visible)
        {
#if DEBUG_MODE
            bool backup = visible;
            visible = true;
#endif
            if (visible && Item == null)
            {
                if (createFunc != null)
                {
                    Item = createFunc();
                }
                else
                {
                    Item = GameObject.Instantiate(template.gameObject).GetComponent<RectTransform>();
                }
                Item.SetParent(owner.content.transform, false);
                Vector2 pos = Position;
                pos.y = -pos.y;
                pos = pos + (Item.pivot - 0.5f * Vector2.one) * Size;
                Item.anchoredPosition = pos;
            }
            else if (!visible && Item != null)
            {
                if (recycleFunc != null)
                {
                    recycleFunc(Item);
                }
                else
                {
                    GameObject.Destroy(Item.gameObject);
                }
                Item = null;
            }

#if DEBUG_MODE
            Item.gameObject.SetActive(backup);
#endif
        }

        public ChildItemHandle(CullingScrollRect owner, Func<RectTransform> createFunc, Action<RectTransform> recycleFunc, Vector2 position, Vector2 size, float rotationZ)
        {
            this.owner = owner;
            this.createFunc = createFunc;
            this.recycleFunc = recycleFunc;
            this.position = position;
            this.size = size;
            this.rotationZ = rotationZ;
        }

        public ChildItemHandle(CullingScrollRect owner, RectTransform template, Vector2 position, Vector2 size, float rotationZ)
        {
            this.owner = owner;
            this.template = template;
            this.position = position;
            this.size = size;
            this.rotationZ = rotationZ;
        }

        public float Left()
        {
            return AABB.x;
        }

        public float Right()
        {
            return AABB.xMax;
        }

        public float Top()
        {
            return AABB.yMax;
        }

        public float Bottom()
        {
            return AABB.y;
        }
    }

}
