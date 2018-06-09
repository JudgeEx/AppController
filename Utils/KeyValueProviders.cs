using System;
using System.Linq;
using System.Collections.Generic;

interface IKeyValueProvider<TKey, TValue>
{
    void Set(TKey key, TValue value, TimeSpan expireTime = default);
    TValue Get(TKey key);
}

class InMemoryKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
    SortedDictionary<DateTime, TKey> expire = new SortedDictionary<DateTime, TKey>();
    void Cleanup()
    {
        var now = DateTime.Now;
        while (expire.Count > 0 && expire.First().Key < now)
        {
            dic.Remove(expire.First().Value);
            expire.Remove(expire.First().Key);
        }
    }
    public void Set(TKey key, TValue value, TimeSpan expireTime)
    {
        Cleanup();
        expireTime = expireTime == default ? TimeSpan.FromDays(36500) : expireTime;
        expire[DateTime.Now + expireTime] = key;
        dic[key] = value;
    }
    public TValue Get(TKey key)
    {
        Cleanup();
        TValue ret = default;
        dic.TryGetValue(key, out ret);
        return ret;
    }
}

class RedisKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    public TValue Get(TKey key)
    {
        throw new System.NotImplementedException();
    }

    public void Set(TKey key, TValue value, TimeSpan expireTime)
    {
        throw new System.NotImplementedException();
    }
}