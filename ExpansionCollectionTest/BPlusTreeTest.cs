using System;
using System.Collections.Generic;
using System.Linq;
using ExpansionCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpansionCollectionTest
{
    [TestClass]
    public class BPlusTreeTest
    {
        [TestMethod]
        public void ToArrayTest()
        {
            int limit = 100;

            var rnd = new Random();
            var vals = new List<int>();
            for (int i = 0; i < limit; ++i) {
                vals.Add(rnd.Next(limit));
            }

            var tree = new BPlusTree<int>(4);
            foreach (var v in vals) {
                tree.Add(v);
            }

            var arr = tree.ToArray();

            for (int i = 0; i < arr.Length; ++i) {
                Assert.AreEqual(tree[i], arr[i]);
            }
            Assert.AreEqual(tree.Count, arr.Length);
        }

        [TestMethod]
        public void GatRangeTest()
        {
            int limit = 100;

            var rnd = new Random();
            var vals = new List<int>();
            for (int i = 0; i < limit; ++i) {
                vals.Add(rnd.Next(limit));
            }

            var tree = new BPlusTree<int>(4);
            foreach (var v in vals) {
                tree.Add(v);
            }

            var arr = tree.ToArray();

            var len = rnd.Next(tree.Count - 1);
            var str = rnd.Next(tree.Count - len);

            var v1 = tree.GetRange(str, len);
            for (int i = 0; i < len; ++i) {
                Assert.AreEqual(v1[i], arr[str + i]);
            }
        }

        [TestMethod]
        public void CopyToTest()
        {
            var tree = new BPlusTree<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var list = new List<int>(tree);
            var arr1 = new int[13];
            var arr2 = new int[13];
            tree.CopyTo(arr1);
            list.CopyTo(arr2);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }

            tree.CopyTo(arr1, 3);
            list.CopyTo(arr2, 3);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }

            tree.CopyTo(3, arr1, 2, 6);
            list.CopyTo(3, arr2, 2, 6);
            for (int i = 0; i < arr1.Length; ++i) {
                Assert.AreEqual(arr1[i], arr2[i]);
            }
        }

        [TestMethod]
        public void IndexOfTest()
        {
            var tree = new BPlusTree<int>(3);
            for (int j = 0; j < 10; ++j) {
                for (int i = 0; i < 6; ++i) {
                    tree.Add(j);
                }
            }

            for (int j = 0; j < 10; ++j) {
                Assert.AreEqual(tree.IndexOf(j), j * 6);
            }
            Assert.AreEqual(tree.IndexOf(10), -1);
        }

        [TestMethod]
        public void LastIndexOfTest()
        {
            var tree = new BPlusTree<int>(3);
            for (int j = 0; j < 10; ++j) {
                for (int i = 0; i < 5; ++i) {
                    tree.Add(j);
                }
            }

            for (int j = 0; j < 10; ++j) {
                Assert.AreEqual(tree.LastIndexOf(j), j * 5 + 4);
            }
            Assert.AreEqual(tree.LastIndexOf(10), -1);
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            int limit = 1000;

            var rnd = new Random();
            var tree = new BPlusTree<int>(4);
            for (int i = 0; i < limit; ++i) {
                tree.Add(i);
            }
            var tmp = new List<int>(tree);

            for (int i = 0; i < limit; ++i) {
                var del = rnd.Next(tree.Count);
                tree.RemoveAt(del);
                tmp.RemoveAt(del);

                for (int j = 0; j < tree.Count; ++j) {
                    Assert.AreEqual(tree[j], tmp[j]);
                }
            }
        }

        [TestMethod]
        public void SearchTest()
        {
            var vals = new int[] { 3, 4, 6, 6, 7, 9, 12, 17, 18, 18,
                                   22, 22, 22, 24, 26, 27, 29, 31, 32, 33, 36 };
            var tree = new BPlusTree<int>(4, vals);

            var vals_gt = new int[] { 0, 0, 0, 1, 2, 2, 4, 5, 5, 6,
                                      6, 6, 7, 7, 7, 7, 7, 8, 10, 10,
                                      10, 10, 13, 13, 14, 14, 15, 16, 16, 17,
                                      17, 18, 19, 20, 20, 20, 21, 21, 21, 21};
            for (int i = 0; i < vals_gt.Length; ++i) {
                var v = tree.SearchOfGreter(i);
                Assert.AreEqual(v.CurrentIndex, vals_gt[i]);
            }

            var vals_ge = new int[] { 0, 0, 0, 0, 1, 2, 2, 4, 5, 5,
                                      6, 6, 6, 7, 7, 7, 7, 7, 8, 10,
                                      10, 10, 10, 13, 13, 14, 14, 15, 16, 16,
                                      17, 17, 18, 19, 20, 20, 20, 21, 21, 21};
            for (int i = 0; i < vals_gt.Length; ++i) {
                var v = tree.SearchOfGreterEqual(i);
                Assert.AreEqual(v.CurrentIndex, vals_ge[i]);
            }

            var vals_lt = new int[] { -1, -1, -1, -1, 0, 1, 1, 3, 4, 4,
                                      5, 5, 5, 6, 6, 6, 6, 6, 7, 9,
                                      9, 9, 9, 12, 12, 13, 13, 14, 15, 15,
                                      16, 16, 17, 18, 19, 19, 19, 20, 20, 20};
            for (int i = 0; i < vals_gt.Length; ++i) {
                var v = tree.SearchOfLess(i);
                Assert.AreEqual(v.CurrentIndex, vals_lt[i]);
            }

            var vals_le = new int[] { -1, -1, -1, 0, 1, 1, 3, 4, 4, 5,
                                      5, 5, 6, 6, 6, 6, 6, 7, 9, 9,
                                      9, 9, 12, 12, 13, 13, 14, 15, 15, 16,
                                      16, 17, 18, 19, 19, 19, 20, 20, 20, 20};
            for (int i = 0; i < vals_gt.Length; ++i) {
                var v = tree.SearchOfLessEqual(i);
                Assert.AreEqual(v.CurrentIndex, vals_le[i]);
            }
        }

        [TestMethod]
        public void SearchTest2()
        {
            var rnd = new Random();
            var tree = new BPlusTree<int>(2);
            var list = new List<int>();
            var limit = 40;

            for (int i = 0; i < limit; ++i) {
                var v = rnd.Next(limit / 2);
                tree.Add(v);
                list.Add(v);
            }
            list.Sort();

            for (int i = 0; i < limit; ++i) {
                var gt = tree.SearchOfGreter(i).CurrentIndex;
                Assert.AreEqual(gt, this.SearchGt(list, i));

                var ge = tree.SearchOfGreterEqual(i).CurrentIndex;
                Assert.AreEqual(ge, this.SearchGe(list, i));

                var lt = tree.SearchOfLess(i).CurrentIndex;
                Assert.AreEqual(lt, this.SearchLt(list, i));

                var le = tree.SearchOfLessEqual(i).CurrentIndex;
                Assert.AreEqual(le, this.SearchLe(list, i));
            }
        }

        private int SearchGt(List<int> list, int v)
        {
            for (int i = 0; i < list.Count; ++i) {
                if (list[i] > v) {
                    return i;
                }
            }
            return list.Count;
        }

        private int SearchGe(List<int> list, int v)
        {
            for (int i = 0; i < list.Count; ++i) {
                if (list[i] >= v) {
                    return i;
                }
            }
            return list.Count;
        }

        private int SearchLt(List<int> list, int v)
        {
            for (int i = list.Count - 1; i >= 0; --i) {
                if (list[i] < v) {
                    return i;
                }
            }
            return -1;
        }

        private int SearchLe(List<int> list, int v)
        {
            for (int i = list.Count - 1; i >= 0; --i) {
                if (list[i] <= v) {
                    return i;
                }
            }
            return -1;
        }
    }
}
