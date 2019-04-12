using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqAndTrie
{
    public interface ITrie<T, V, Alphabet> :
        IDictionary<T, V>,
        IEquatable<ITrie<T, V, Alphabet>> where T : IEnumerable,
        IEnumerable<Alphabet> where V : IEquatable<V>
    {
        // upserts a key-value
        void AddOrReplace(T key, V val);
        // gets all values with a given prefix
        IEnumerable<T> FindAllSuffixesFromPrefix(T key);
        // gets the longest prefix for a key
        int LongestPrefix(T key);
    }
}
