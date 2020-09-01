using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGameObjectPool
{
    private static GameObject poolRoot;

    private GameObject template;
    private int maxSize;
    private Stack<GameObject> stack = new Stack<GameObject>();

    public SimpleGameObjectPool(GameObject template, int maxSize = 10)
    {
        this.template = template;
        this.maxSize = maxSize;
    }

    public GameObject Get()
    {
        if (stack.Count > 0)
        {
            return stack.Pop();
        }
        return GameObject.Instantiate(template);
    }

    public void Recycle(GameObject gameObject)
    {
        if (stack.Count < maxSize)
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("[GameObjectPool]");
                GameObject.DontDestroyOnLoad(poolRoot);
                poolRoot.SetActive(false);
            }

            stack.Push(gameObject);
            gameObject.transform.SetParent(poolRoot.transform, false);
        }
        else
        {
            GameObject.Destroy(gameObject);
        }
    }

}
