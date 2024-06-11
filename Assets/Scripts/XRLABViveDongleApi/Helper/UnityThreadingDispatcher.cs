using Meta.WitAi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityDispatcher
{
    static UnityThreadingDispatcher _unityThreadingDispatcher;
    static UnityThreadingDispatcher unityThreadingDispatcher
    {
        get
        {
            if(_unityThreadingDispatcher == null)
            {
                var go = new GameObject("UnityThreadingDispatcher");
                _unityThreadingDispatcher = go.AddComponent<UnityThreadingDispatcher>();
            }
            return _unityThreadingDispatcher;
        }
    }

    public static void Create()
    {
        if (_unityThreadingDispatcher == null)
        {
            var go = new GameObject("UnityThreadingDispatcher");
            _unityThreadingDispatcher = go.AddComponent<UnityThreadingDispatcher>();
        }
    }

    public static void Invoke(System.Action action)
    {
        if (action != null)
            unityThreadingDispatcher.queue.Enqueue(action);
    }

    class UnityThreadingDispatcher : MonoBehaviour
    {
        [SerializeField] int orderMessagesCount;
        internal Queue<System.Action> queue = new Queue<System.Action>();
        void Update()
        {
            orderMessagesCount = queue.Count;
            while (queue.Count > 0)
            {
                var action = queue.Dequeue();
                action?.Invoke();
            }
        }
    }
}