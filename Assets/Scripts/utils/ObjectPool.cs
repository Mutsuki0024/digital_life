using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    readonly GameObject _prefab;
    readonly Transform _parent;
    readonly Queue<GameObject> _pool = new Queue<GameObject>();
    readonly bool _expandable;
    int _count;

    public ObjectPool(GameObject prefab, int initialSize, Transform parent = null, bool expandable = true)
    {
        _prefab = prefab;
        _parent = parent;
        _expandable = expandable;
        Prewarm(initialSize);
    }

    void Prewarm(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var go = GameObject.Instantiate(_prefab, _parent);
            go.SetActive(false);
            _pool.Enqueue(go);
            _count++;
        }
    }

    public GameObject Get()
    {
        if (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            go.SetActive(true);

            // 重新启用 Collider
            var col = go.GetComponent<Collider>();
            if (col) col.enabled = true;

            return go;
        }
        if (_expandable)
        {
            _count++;
            return GameObject.Instantiate(_prefab, _parent, false);
        }
        return null; // 或抛异常
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(_parent, false);
        _pool.Enqueue(go);
    }

    public void ReturnAllActive()
    {
        // 把 parent 下的活跃实例全部回收
        if (_parent == null) return;
        var active = new List<Transform>();
        foreach (Transform t in _parent)
            if (t.gameObject.activeSelf) active.Add(t);
        foreach (var t in active)
            Return(t.gameObject);
    }

    public int TotalCreated => _count;
    public int Available => _pool.Count;
}