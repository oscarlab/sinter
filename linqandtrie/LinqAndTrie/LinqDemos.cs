using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Diagnostics;

namespace LinqAndTrie
{
    public static class LinqDemos
    {
        public static void TestTries()
        {
            // loop for different dataset sizes
            for (int n = 1; n <= 7001; n += 1000)
            {
                Console.WriteLine("------------------------------------------------------------------------------------");
                Console.WriteLine("Test for [" + n + "] entries in dictionary");
                var trie = new Trie<string, bool, char>();
                var dictionary = new Dictionary<string, bool>();

                // fill with values
                int i = 0;
                for (i = 1; i < n; i++)
                {
                    trie.Add("V8" + (i * 2).ToString(), true);
                    dictionary.Add("V8" + (i * 2).ToString(), true);
                }

                var reallyLongString = "V886";
                for (i = 1; i <= 20; i++)
                    reallyLongString += "2";

                // test for longest prefix - dictionary
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var longestPrefix = dictionary.Where(p => reallyLongString.StartsWith(p.Key)).OrderByDescending(p => p.Key.Length).FirstOrDefault();
                Console.WriteLine("Dictionary Longest Prefix for:\n" + reallyLongString + "\n is" + longestPrefix);
                var inDictionary = dictionary.ContainsKey(reallyLongString);
                var dictionaryTicks = stopWatch.ElapsedTicks;
                stopWatch.Stop();
                stopWatch.Reset();

                // test for longest prefix - Trie
                stopWatch.Start();
                var longestPrefixIndex = trie.LongestPrefix(reallyLongString);
                Console.WriteLine("Trie Longest Prefix end index for:\n" + reallyLongString + "\n is" + (longestPrefixIndex));
                var trieTicks = stopWatch.ElapsedTicks;
                stopWatch.Stop();
                Console.WriteLine("Trie Ticks for LongestPrefix: " + trieTicks.ToString());
                Console.WriteLine("Dictionary Ticks for LongestPrefix: " + dictionaryTicks.ToString());
                stopWatch.Reset();
                var testSuffixString = "V822";

                // test for findallsuffixes - trie
                stopWatch.Start();
                var endingSuffixes = trie.FindAllSuffixesFromPrefix(testSuffixString);
                var trieTicksSuffixes = stopWatch.ElapsedTicks;
                stopWatch.Stop();
                Console.WriteLine("Found " + endingSuffixes.Count() + " suffixes");
                Console.WriteLine("First 10 Suffix matches for " + testSuffixString + ":");
                i = 0;
                foreach (var suffix in endingSuffixes)
                {
                    Console.Write(suffix + " ");
                    i++;
                    if (i >= 10)
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("Trie Ticks for FindAllSuffixes: " + trieTicksSuffixes.ToString());
                stopWatch.Reset();

                // test for findallsuffixes - dictionary
                stopWatch.Start();
                endingSuffixes =
                    from key in dictionary.Keys
                    where key.StartsWith(testSuffixString)
                    select key;
                var dictTicksSuffixes = stopWatch.ElapsedTicks;
                stopWatch.Stop();
                Console.WriteLine("Found " + endingSuffixes.Count() + " suffixes");
                Console.WriteLine("First 10 Suffix matches for " + testSuffixString + ":");
                i = 0;
                foreach (var suffix in endingSuffixes)
                {
                    Console.Write(suffix + " ");
                    i++;
                    if (i >= 10)
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("Dictionary Ticks for FindAllSuffixes: " + dictTicksSuffixes.ToString());
            }
        }

        public static void TestTriesIntArray()
        {
            /**
             * root = [10, 123]
             *  a = [10, 123, 1]
             *  b = [10, 123, 2]
             *    x = [10, 123, 1, 1]
             *    y = [10, 123, 1, 3]
             *      z = [10, 123, 10, 1]
             */
            Console.WriteLine("------------------------------------------------------------------------------------");

            //var trie = new Trie<int[], AutomationElement, int>();
            var trie = new Trie<int[], string, int>
                {
                    // fill with values
                    { new int[] { 10, 123 }, "root" },
                    { new int[] { 10, 123, 1 }, "a" },
                    { new int[] { 10, 123, 2 }, "b" },
                    { new int[] { 10, 123, 1, 1 }, "x" },
                    { new int[] { 10, 123, 1, 3 }, "y" },
                    { new int[] { 10, 123, 10, 1 }, "z" }
                };

            // test for longest prefix - Trie
            int[] reallyLongString = new int[] { 10, 123, 2 };
            var longestPrefixIndex = trie.LongestPrefix(reallyLongString);
            Console.WriteLine("Trie Longest Prefix end index for :" + string.Join(".", reallyLongString) + ": is " + (longestPrefixIndex.ToString()));

            // test for findallsuffixes - trie
            //int[] testSuffixString = new int[] { 10, 123 };
            //int[] testSuffixString = new int[] { 10, 123, 10 };
            //int[] testSuffixString = new int[] { 10, 123, 10, 1 };
            int[] testSuffixString = new int[] { 10, 123, 10, 11 };

            var endingSuffixes = trie.FindAllSuffixesFromPrefix(testSuffixString);

            Console.WriteLine("Found " + endingSuffixes.Count() + " suffixes");
            Console.WriteLine("First few Suffix matches for " + string.Join(".", testSuffixString) + ":");

            int i = 0;
            foreach (var suffix in endingSuffixes)
            {
                Console.WriteLine(string.Join(".", suffix) + " " + trie[suffix]);
                i++;
                if (i >= 10)
                    break;
            }

            Console.WriteLine();
        }
    }
}
