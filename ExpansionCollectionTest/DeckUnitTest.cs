using System;
using System.Collections.Generic;
using ExpansionCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpansionCollectionTest
{
    [TestClass]
    public class DeckUnitTest
    {
        [TestMethod]
        public void CopyToTest()
        {
            var deck = new Deck<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 3);
            var list = new List<int>(deck);
            var arr1 = new int[13];
            var arr2 = new int[13];
            deck.CopyTo(arr1);
            list.CopyTo(arr2);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }

            deck.CopyTo(arr1, 3);
            list.CopyTo(arr2, 3);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }

            deck.CopyTo(3, arr1, 2, 6);
            list.CopyTo(3, arr2, 2, 6);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }

        [TestMethod]
        public void SortTest()
        {
            var rnd = new Random();
            var vals = new List<int>();
            for (int i = 0; i < 100; ++i) {
                vals.Add(i);
            }
            for (int i = 0; i < vals.Count - 1; ++i) {
                var idx = rnd.Next(vals.Count - 1 - i);
                var tmp = vals[vals.Count - 1];
                vals[vals.Count - 1] = vals[idx];
                vals[idx] = tmp;
            }

            var deck1 = new Deck<int>(vals, 5);
            deck1.Sort();
            for (int i = 0; i < vals.Count; ++i) {
                Assert.AreEqual(deck1[i], i);
            }

            var deck2 = new Deck<int>(vals, 5);
            deck2.Sort((l, r) => -l.CompareTo(r));
            for (int i = 0; i < vals.Count; ++i) {
                Assert.AreEqual(deck2[i], (vals.Count - 1) - i);
            }
        }

        [TestMethod]
        public void SupportTest()
        {
            // trueForAll
            var deck1 = new Deck<int>(new int[] { 1, 2, 3, 4, 5, 6 }, 5);
            Assert.IsTrue(deck1.TrueForAll((v) => v > 0));
            Assert.IsFalse(deck1.TrueForAll((v) => v < 3));

            // foreach
            var rnd = new Random();
            var vals = new List<int>();
            for (int i = 0; i < 100; ++i) {
                vals.Add(i);
            }
            for (int i = 0; i < vals.Count - 1; ++i) {
                var idx = rnd.Next(vals.Count - 1 - i);
                var tmp = vals[vals.Count - 1];
                vals[vals.Count - 1] = vals[idx];
                vals[idx] = tmp;
            }
            var deck2 = new Deck<int>(vals, 5);
            var deck2_i = 0;
            deck2.ForEach((v) => Assert.AreEqual<int>(vals[deck2_i++], v));
        }

        [TestMethod]
        public void ReverseTest()
        {
            var rnd = new Random();
            var vals = new List<int>();
            for (int i = 0; i < 100; ++i) {
                vals.Add(i);
            }
            for (int i = 0; i < vals.Count - 1; ++i) {
                var idx = rnd.Next(vals.Count - 1 - i);
                var tmp = vals[vals.Count - 1];
                vals[vals.Count - 1] = vals[idx];
                vals[idx] = tmp;
            }

            var deck1 = new Deck<int>(vals, 5);
            deck1.Reverse();
            for (int i = 0, j = vals.Count - 1; i < vals.Count; ++i, --j) {
                Assert.AreEqual(deck1[i], vals[j]);
            }

            var deck2 = new Deck<int>(vals, 5);
            deck2.Reverse(3, 4);
            for (int i = 3, j = 7; i < 7; ++i, --j) {
                Assert.AreEqual(deck2[i], vals[j]);
            }
        }

        [TestMethod]
        public void RemoveTest()
        {
            var vals = new List<int>();
            for (int i = 0; i < 100; ++i) {
                vals.Add(i);
            }

            var deck1 = new Deck<int>(vals, 5);
            deck1.RemoveAll((v) => v >= 3 && v <= 20);
            foreach (var v in deck1) {
                Assert.IsTrue(v < 3 || v > 20);
            }

            var deck2 = new Deck<int>(vals, 5);
            var rnd = new Random(1);
            for (int i = 0; i < deck2.Count - 1; ++i) {
                var idx = rnd.Next(deck2.Count - 1 - i);
                var tmp = deck2[deck2.Count - 1];
                deck2[deck2.Count - 1] = deck2[idx];
                deck2[idx] = tmp;
            }
            var hash = new HashSet<int>(deck2.GetRange(2, 10));
            deck2.Sort();
            foreach (var v in hash) {
                deck2.Remove(v);
            }
            foreach (var v in deck2) {
                Assert.IsFalse(hash.Contains(v));
            }
        }

        [TestMethod]
        public void InsertTest()
        {
            var deck = new Deck<int>(5);
            var tmp = new List<int>();
            var rnd = new Random();
            for (int i = 0; i < 100; ++i) {
                var idx = rnd.Next(deck.Count);
                deck.Insert(idx, i);
                tmp.Insert(idx, i);
            }
            for (int i = 0; i < deck.Count; ++i) {
                Assert.AreEqual(deck[i], tmp[i]);
            }
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var vals = new List<int>();
            for (int i = 0; i < 10; ++i) {
                vals.Add(i);
            }
            var deck = new Deck<int>(vals, 5);
            var tmp = new List<int>(vals);
            deck.InsertRange(3, vals);
            tmp.InsertRange(3, vals);
            for (int i = 0; i < deck.Count; ++i) {
                Assert.AreEqual(deck[i], tmp[i]);
            }
        }

        [TestMethod]
        public void AddSortTest()
        {
            var rnd = new Random(0);
            var vals = new List<int>();
            for (int i = 0; i < 100; ++i) {
                vals.Add(i);
            }
            for (int i = 0; i < vals.Count - 1; ++i) {
                var idx = rnd.Next(vals.Count - 1 - i);
                var tmp = vals[vals.Count - 1];
                vals[vals.Count - 1] = vals[idx];
                vals[idx] = tmp;
            }

            var deck = new Deck<int>(5);
            foreach (var v in vals) {
                deck.AddSort(v);
            }

            for (int i = 0; i < deck.Count; ++i) {
                Assert.AreEqual(deck[i], i);
            }
        }

        [TestMethod]
        public void ContainsTest()
        {
            var rnd = new Random();

            var vals = new List<int>();
            for (int i = 0; i < 50; ++i) {
                vals.Add(i);
            }
            var deck = new Deck<int>(vals, 5);


            for (int i = 0; i < 100; ++i) {
                var v = rnd.Next(vals.Count);
                Assert.AreEqual(deck.Contains(v), vals.Contains(v));
            }
        }

        [TestMethod]
        public void BinarySearchTest()
        {
            var vals = new List<int>();
            for (int i = 0; i < 50; ++i) {
                vals.Add(i);
            }
            var deck = new Deck<int>(vals, 5);

            for (int i = -1; i < 50; ++i) {
                Assert.AreEqual(deck.BinarySearch(i), vals.BinarySearch(i));
            }
        }

        [TestMethod]
        public void IndexOfTest()
        {
            var vals = new List<int>();
            for (int i = 0; i < 50; ++i) {
                vals.Add(i);
                vals.Add(i);
            }
            var deck = new Deck<int>(vals, 5);

            for (int i = -1; i < 50; ++i) {
                Assert.AreEqual(deck.IndexOf(i), vals.IndexOf(i));
            }

            for (int i = -1; i < 50; ++i) {
                Assert.AreEqual(deck.LastIndexOf(i), vals.LastIndexOf(i));
            }
        }
    }
}
