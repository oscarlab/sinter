using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqAndTrie
{

    public class Trie<T, V, Alphabet> :
    ITrie<T, V, Alphabet> where T : IEnumerable,
    IEnumerable<Alphabet> where V : IEquatable<V>
    {
        // credit: https://inductivestep.org/2016/06/12/trying-tries-c-idictionary-generic-trie/

        #region Trie<T,V> interface implementation
        private int count;

        private TrieNode Root { get; set; }

        private class TrieNode
        {
            public TrieNode Parent { get; set; }

            public IDictionary<Alphabet, TrieNode> Children { get; set; }

            public V Value { get; set; }

            public bool HasValue { get; set; }

            // we store the full key for nodes that have values; 
            // this is necessary to implement the c# dictionary interface in a generic, 
            // but would be more space-efficient if we only stored the last letter
            public T FullKey { get; set; }

            public Alphabet LastLetter { get; set; }

            public TrieNode(TrieNode parent, Alphabet lastLetter) : this()
            {
                Parent = parent;
                LastLetter = lastLetter;
            }

            public TrieNode()
            {
                HasValue = false;
            }
        }

        public Trie()
        {
            Root = new TrieNode();
            count = 0;
        }

        private bool FindClosestNodeByKey(T key, out TrieNode result, out int indexMatched)
        {
            return FindClosestNodeByKey(key, Root, out result, out indexMatched);
        }

        // loops through tree and finds the closest result (ie longest matching prefix); also returns how many indices were matched (so we dont have to repeat the comparison)		
        // O(k) where k is the number of characters in the key
        private bool FindClosestNodeByKey(T key, TrieNode startNode, out TrieNode result, out int lastIndexMatched)
        {
            var currentNode = startNode;
            lastIndexMatched = -1;
            TrieNode lastNode = null;
            var enumKey = (IEnumerable<Alphabet>)key;
            foreach (var letter in enumKey)
            {
                if (currentNode.Children == null)
                {
                    result = lastNode;
                    return false;
                }

                if (!currentNode.Children.ContainsKey(letter))
                {
                    result = currentNode;
                    return false;
                }

                lastNode = currentNode;
                currentNode = currentNode.Children[letter];
                lastIndexMatched++;
            }

            result = currentNode;
            return true;
        }

        private void BuildNodesForKeyValue(T key, V val, bool allowReplace = true, int startIndex = 0)
        {
            BuildNodesForKeyValue(key, val, Root, allowReplace, startIndex);
        }

        private void BuildNodesForKeyValue(T key, V val, TrieNode startNode, bool allowReplace, int startIndex)
        {
            var currentNode = startNode;
            var enumKey = (IEnumerable<Alphabet>)key;

            var currentIndex = 0;
            foreach (var letter in enumKey)
            {
                // skip characters until our start index  - this is inefficient but only costs us 2x the length of the key
                if (currentIndex < startIndex)
                {
                    currentIndex++;
                    continue;
                }

                if (currentNode.Children == null)
                    currentNode.Children = new Dictionary<Alphabet, TrieNode>();

                if (!currentNode.Children.ContainsKey(letter))
                    currentNode.Children.Add(letter, new TrieNode(currentNode, letter));

                currentNode = currentNode.Children[letter];
                currentIndex++;
            }

            // Code prevents menus from updating
            Console.WriteLine("{0} {1}", currentNode.Value, currentNode.HasValue);
           /* if (currentNode.HasValue)
            {
                if (!allowReplace)
                    throw new ArgumentException();
            }
               */
            currentNode.Value = val;
            currentNode.HasValue = true;
            currentNode.FullKey = key;
            count++;
        }

        public void AddOrReplace(T key, V val)
        {
            BuildNodesForKeyValue(key, val, true);
        }

        public void AddOrReplace(T key, V val, bool allowReplace = true)
        {
            BuildNodesForKeyValue(key, val, allowReplace);
        }

        public int LongestPrefix(T key)
        {
            int matchedIndex = -1;
            var foundExact = FindClosestNodeByKey(key, out TrieNode resultNode, out matchedIndex);
            return matchedIndex;
        }

        public IEnumerable<T> FindAllSuffixesFromPrefix(T key)
        {
            var foundExact = FindClosestNodeByKey(key, out TrieNode resultNode, out int matchedIndex);

            var result = new List<T>();
            if (foundExact == false)
                return result;

            var searchQueue = new Queue<Tuple<List<Alphabet>, TrieNode>>();
            searchQueue.Enqueue(new Tuple<List<Alphabet>, TrieNode>(new List<Alphabet>(), resultNode));
            while (searchQueue.Count > 0)
            {
                var currentNodeTuple = searchQueue.Dequeue();
                if (currentNodeTuple.Item2.Children != null)
                    foreach (var child in currentNodeTuple.Item2.Children)
                    {
                        List<Alphabet> newList = currentNodeTuple.Item1.Select(item => (Alphabet)(item)).ToList<Alphabet>();
                        newList.Add(child.Key);
                        searchQueue.Enqueue(new Tuple<List<Alphabet>, TrieNode>(newList, child.Value));
                    }

                if (currentNodeTuple.Item2.HasValue)
                    result.Add(currentNodeTuple.Item2.FullKey);
            }

            return result;
        }

        public IEnumerable<KeyValuePair<T, V>> FindAllKeyValues()
        {
            // Run BFS traversal
            TrieNode resultNode = Root;
            var searchQueue = new Queue<TrieNode>();
            searchQueue.Enqueue(Root);
            var result = new List<KeyValuePair<T, V>>();
            while (searchQueue.Count > 0)
            {
                var currentNodeTuple = searchQueue.Dequeue();
                if (currentNodeTuple.Children != null)
                    foreach (var child in currentNodeTuple.Children)
                        searchQueue.Enqueue(child.Value);

                if (currentNodeTuple.HasValue)
                    result.Add(new KeyValuePair<T, V>(currentNodeTuple.FullKey, currentNodeTuple.Value));
            }

            return result;
        }

        #endregion
        #region Comparable, ICloneable,  IDictionary<T, V>, IEquatable<Trie<T,V>> interfaces implementations
        public bool ContainsKey(T key)
        {
            var found = FindClosestNodeByKey(key, out TrieNode node, out int indexMatched);
            return found;
        }

        public void Add(T key, V val)
        {
            BuildNodesForKeyValue(key, val, false);
        }

        public bool Contains(KeyValuePair<T, V> kvp)
        {
            var containsKey = TryGetValue(kvp.Key, out V valResult);
            if (!containsKey)
                return false;
            if (valResult == null && kvp.Value == null)
                return true;
            return (valResult.Equals(kvp.Value));
        }

        public void Add(KeyValuePair<T, V> kvp)
        {
            Add(kvp.Key, kvp.Value);
        }

        public bool Remove(KeyValuePair<T, V> kvp)
        {
            //TODO: combine redundant code with Remove(T key)
            var key = kvp.Key;
            var found = FindClosestNodeByKey(key, out TrieNode node, out int indexMatched);
            if (!found)
                return false;

            if (!kvp.Value.Equals(node.Value))
                return false;

            node.HasValue = false;
            node.Value = default(V);
            count--; // need to track number of values in trie
            var currentNode = node;

            // stop when you reach another value, there are other children, or you are the top
            while (!currentNode.HasValue && !(currentNode == Root) && ((currentNode.Children == null) || (currentNode.Children.Count == 0)))
            {
                var parent = currentNode.Parent;
                parent.Children.Remove(node.LastLetter);
                currentNode = parent;
            }

            return true;
        }

        public void CopyTo(KeyValuePair<T, V>[] targetArray, int numToCopy)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T key)
        {
            var found = FindClosestNodeByKey(key, out TrieNode node, out int indexMatched);
            if (!found)
                return false;

            node.HasValue = false;
            node.Value = default(V);
            count--; // need to track number of values in trie
            var currentNode = node;

            // stop when you reach another value, there are other children, or you are the top
            while (!currentNode.HasValue && !(currentNode == Root) && ((currentNode.Children == null) || (currentNode.Children.Count == 0)))
            {
                var parent = currentNode.Parent;
                parent.Children.Remove(node.LastLetter);
                currentNode = parent;
            }

            return true;
        }

        public bool TryGetValue(T key, out V val)
        {
            var found = FindClosestNodeByKey(key, out TrieNode node, out int indexMatched);
            val = default(V);
            if (!found)
                return false;

            val = node.Value;
            return true;
        }

        public void Clear()
        {
            Root = new TrieNode();
            // garbage collection will handle the rest			
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<T, V>> GetEnumerator()
        {
            var allVals = FindAllKeyValues();
            return allVals.GetEnumerator();
        }

        public bool Equals(ITrie<T, V, Alphabet> trie)
        {
            throw new NotImplementedException();
        }

        public V this[T key]
        {
            get
            {
                var hasValue = TryGetValue(key, out V result);
                if (hasValue)
                    return result;

                throw new KeyNotFoundException();
            }
            set
            {
                AddOrReplace(key, value);
            }
        }

        public ICollection<T> Keys
        {
            get
            {
                var kvps = FindAllKeyValues();
                return (
                    from val in kvps
                    select val.Key).ToList();
            }
        }

        public ICollection<V> Values
        {
            get
            {
                var kvps = FindAllKeyValues();
                return (
                    from val in kvps
                    select val.Value).ToList();
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        #endregion
    }
}

