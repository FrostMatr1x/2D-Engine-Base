using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Blockhand;

public static class CoroutineRunner
{
    private static Dictionary<int, IEnumerator> _coroutines = new Dictionary<int, IEnumerator>();
    private static Stack<int> _freeId = new Stack<int>();
    private static int _nextId = 0;

    public static int StartCoroutine(IEnumerator coroutine)
    {
        int id = _freeId.Count > 0 ? _freeId.Pop() : _nextId++;
        
        _coroutines.Add(id, coroutine);
        return id;
    }

    public static void StopCoroutine(int id)
    {
        if (_coroutines.ContainsKey(id))
        {
            _coroutines.Remove(id);
            _freeId.Push(id);
        }
    }

    public static void Update()
    {
        var keys = _coroutines.Keys;
        
        foreach (var id in keys)
        {
            if (!_coroutines.ContainsKey(id)) continue;
            
            if (!_coroutines[id].MoveNext())
            {
                StopCoroutine(id);
            }
        }
    }
}