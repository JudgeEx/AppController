using System;
using System.Collections.Generic;

interface IKeyValueProvider<TKey, TValue>
{
    void Set(TKey key, TValue value);
    TValue Get(TKey key);
}

class InMemoryKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
    SortedList<DateTime, TKey> expire = new SortedList<DateTime, TKey>();
    void Cleanup()
    {
        var now = DateTime.Now;
        while (expire.Count > 0 && expire.Keys[0] < now)
        {
            dic.Remove(expire.Values[0]);
            expire.RemoveAt(0);
        }
    }
    public void Set(TKey key, TValue value)
    {
        Cleanup();
        dic[key] = value;
    }
    public TValue Get(TKey key)
    {
        Cleanup();
        return dic.ContainsKey(key) ? dic[key] : default(TValue);
    }
}

class RedisKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    public TValue Get(TKey key)
    {
        throw new System.NotImplementedException();
    }

    public void Set(TKey key, TValue value)
    {
        throw new System.NotImplementedException();
    }
}