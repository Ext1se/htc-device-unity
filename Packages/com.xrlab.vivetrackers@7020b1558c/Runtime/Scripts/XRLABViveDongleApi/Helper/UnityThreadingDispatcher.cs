using System.Collections.Generic;
using UnityEngine;

public class UnityDispatcher
{
    static UnityThreadingDispatcher _unityThreadingDispatcher;
    static UnityThreadingDispatcher unityThreadingDispatcher
    {
        get
        {
            return _unityThreadingDispatcher;
        }
    }


    public static void Create(bool dontDestroyOnLoad = true)
    {
        if (_unityThreadingDispatcher == null)
        {
            var go = new GameObject("UnityThreadingDispatcher");
            _unityThreadingDispatcher = go.AddComponent<UnityThreadingDispatcher>();
            if (dontDestroyOnLoad)
                UnityThreadingDispatcher.DontDestroyOnLoad(go);
        }
    }

    public static void Destroy()
    {
        if (_unityThreadingDispatcher != null)
        {
            Object.Destroy(_unityThreadingDispatcher.gameObject);
            _unityThreadingDispatcher = null;
        }
    }

    public static void Invoke(System.Action action)
    {
        if (action != null)
            unityThreadingDispatcher.AddAction(action);
    }

    class UnityThreadingDispatcher : MonoBehaviour
    {
        [SerializeField] int orderMessagesCount;
        Queue<System.Action> queue = new Queue<System.Action>();
        static object queue_lock = new object();
        void Update()
        {
            lock (queue_lock)
            {
                orderMessagesCount = queue.Count;
                while (queue.Count > 0)
                {
                    var action = queue.Dequeue();
                    action?.Invoke();
                }
            }
        }

        internal void AddAction(System.Action action)
        {
            lock (queue_lock)
                queue.Enqueue(action);
        }
    }
}