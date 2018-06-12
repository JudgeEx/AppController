using System;
using System.Collections.Generic;

interface IKeyValueProvider<TKey, TValue>
{
    bool Set(TKey key, TValue value, TimeSpan expireTime = default);
    TValue Get(TKey key);
    bool Remove(TKey key);
}

class KeyValuePairComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
{
    private readonly Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, int> __comparer;
    public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => __comparer(x, y);
    public KeyValuePairComparer(Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, int> Comparer) => __comparer = Comparer;
}

class InMemoryKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    private Dictionary<TKey, (TValue, DateTime)> Dictionary = new Dictionary<TKey, (TValue, DateTime)>();
    private SortedSet<(DateTime, TKey)> SortedSet = new SortedSet<(DateTime, TKey)>();

    bool Cleanup()
    {
        bool ret = true;
        var now = DateTime.Now;
        while (SortedSet.Count > 0 && SortedSet.Min.Item1 < now)
            ret = ret && (Dictionary.Remove(SortedSet.Min.Item2) || SortedSet.Remove(SortedSet.Min));
        return ret;
    }

    public TValue Get(TKey key)
    {
        Cleanup();
        Dictionary.TryGetValue(key, out var ret);
        return ret.Item1;
    }

    public bool Remove(TKey key)
    {
        Cleanup();
        return Dictionary.TryGetValue(key, out var record)
            && Dictionary.Remove(key)
            && SortedSet.Remove((record.Item2, key));
    }

    public bool Set(TKey key, TValue value, TimeSpan expireTime = default)
    {
        Cleanup();
        Remove(key);
        var now = DateTime.Now;
        var targetTime = expireTime == default || expireTime > DateTime.MaxValue - now ? DateTime.MaxValue : now + expireTime;
        return Dictionary.TryAdd(key, (value, targetTime)) && SortedSet.Add((targetTime, key));
    }
}

class RedisKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
{
    public TValue Get(TKey key)
    {
        throw new System.NotImplementedException();
    }

    public bool Remove(TKey key)
    {
        throw new NotImplementedException();
    }

    public bool Set(TKey key, TValue value, TimeSpan expireTime)
    {
        throw new System.NotImplementedException();
    }
}


//class InMemoryKVProvider<TKey, TValue> : IKeyValueProvider<TKey, TValue>
//{
//    Dictionary<TKey, KeyValuePair<TValue, DateTime>> dic = new Dictionary<TKey, KeyValuePair<TValue, DateTime>>();
//    SortedSet<KeyValuePair<DateTime, TKey>> expire = new SortedSet<KeyValuePair<DateTime, TKey>>(new KeyValuePairComparer<DateTime, TKey>((x, y) => DateTime.Compare(x.Key, y.Key)));
//    void Cleanup()
//    {
//        var now = DateTime.Now;
//        while (expire.Count > 0 && expire.Min.Key < now)
//        {
//            dic.Remove(expire.Min.Value);
//            expire.Remove(expire.Min);
//        }
//    }

//    public void Set(TKey key, TValue value, TimeSpan expireTime)
//    {
//        Cleanup();
//        Remove(key);
//        var now = DateTime.Now;
//        var targetTime = expireTime == default || expireTime > DateTime.MaxValue - now ? DateTime.MaxValue : now + expireTime;
//        expire.Add(new KeyValuePair<DateTime, TKey>(targetTime, key));
//        dic[key] = new KeyValuePair<TValue, DateTime>(value, targetTime);
//    }

//    public TValue Get(TKey key)
//    {
//        Cleanup();
//        dic.TryGetValue(key, out var ret);
//        return ret.Key;
//    }

//    public bool Remove(TKey key)
//    {
//        return dic.TryGetValue(key, out var record)
//            && dic.Remove(key)
//            && expire.Remove(new KeyValuePair<DateTime, TKey>(record.Value, key));
//    }
//}
