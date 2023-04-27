using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PluginConfig
{
    public static class UnityUtils
    {
        public static void PrintGameobject(GameObject o, int iters = 0)
        {
            string logMessage = "";
            for (int i = 0; i < iters; i++)
                logMessage += '|';
            logMessage += o.name;

            Debug.Log(logMessage);
            foreach (Transform t in o.transform)
                PrintGameobject(t.gameObject, iters + 1);
        }

        public static IEnumerable<Transform> GetChilds(Transform obj)
        {
            int count = obj.childCount;
            for (int i = 0; i < count; i++)
                yield return obj.GetChild(i);
        }

        public static IEnumerable<T> GetComponentsInChildrenRecursively<T>(Transform obj)
        {
            T component;
            foreach (Transform child in obj)
            {
                component = child.gameObject.GetComponent<T>();
                if (component != null)
                    yield return component;
                foreach (T childComp in GetComponentsInChildrenRecursively<T>(child))
                    yield return childComp;
            }

            yield break;
        }

        public static T GetComponentInChildrenRecursively<T>(Transform obj)
        {
            T component;
            foreach (Transform child in obj)
            {
                if (child == obj)
                    continue;

                component = child.gameObject.GetComponent<T>();
                if (component != null)
                    return component;
                component = GetComponentInChildrenRecursively<T>(child);
                if (component != null)
                    return component;
            }

            return default(T);
        }
    }
}
