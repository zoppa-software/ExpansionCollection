using System.Collections;

namespace ExpansionCollection
{
    partial class BPlusTree<T>
    {
        /// <summary>項目参照用、列挙子。</summary>
        private sealed class BPlusEnumerator
            : IBPlusTreeIterator<T>
        {
            #region "fields"

            /// <summary>参照対象のコレクション。</summary>
            private BPlusTree<T> parent;

            /// <summary>現在位置（葉要素）</summary>
            private BLeaf curleaf;

            /// <summary>現在位置（葉要素内のインデックス）</summary>
            private int ptridx;

            /// <summary>初期位置ならば 0。</summary>
            private int started;

            #endregion

            #region "properties"

            /// <summary>列挙子の現在位置にあるコレクション内の要素を取得する。</summary>
            public T Current
            {
                get {
                    return this.curleaf.GetValue(this.ptridx);
                }
            }

            /// <summary>現在要素の位置を取得する。</summary>
            public int CurrentIndex
            {
                get {
                    var res = this.started;
                    var ptr = this.curleaf.PreviewLeaf;
                    while (ptr != null) {
                        res += ptr.Count;
                        ptr = ptr.PreviewLeaf;
                    }
                    return res;
                }
            }

            /// <summary>列挙子の現在位置にあるコレクション内の要素を取得する。</summary>
            object IEnumerator.Current
            {
                get {
                    return this.curleaf.GetValue(this.ptridx);
                }
            }

            #endregion

            #region "constructor"

            /// <summary>コンストラクタ。</summary>
            /// <param name="parent">B+木コレクション。</param>
            public BPlusEnumerator(BPlusTree<T> parent)
            {
                this.parent = parent;
                this.curleaf = parent.start;
                this.ptridx = -1;
                this.started = 0;
            }

            /// <summary>コンストラクタ（列挙を特定の位置より始める場合）</summary>
            /// <param name="parent">B+木コレクション。</param>
            /// <param name="leaf">葉要素。</param>
            /// <param name="index">開始位置。</param>
            public BPlusEnumerator(BPlusTree<T> parent, BLeaf leaf, int index)
            {
                this.parent = parent;
                this.curleaf = leaf;
                this.ptridx = -1;
                this.started = index;
            }

            #endregion

            #region "methods"

            /// <summary>列挙子を次の要素へ進める。</summary>
            /// <returns>進める要素があれば真。</returns>
            public bool MoveNext()
            {
                if (this.ptridx < 0) {
                    this.ptridx = this.started;
                    return (this.ptridx >= 0 && this.ptridx < this.curleaf.Count);
                }
                else if (this.ptridx < this.curleaf.Count - 1) {
                    this.ptridx++;
                    return true;
                }
                else if (this.curleaf.NextLeaf != null) {
                    this.curleaf = this.curleaf.NextLeaf;
                    this.ptridx = 0;
                    return true;
                }
                else {
                    return false;
                }
            }

            /// <summary>列挙子を前の要素へ進める。</summary>
            /// <returns>進める要素があれば真。</returns>
            public bool MovePreviw()
            {
                if (this.ptridx < 0) {
                    this.ptridx = this.started;
                    return (this.ptridx >= 0 && this.ptridx < this.curleaf.Count);
                }
                else if (this.ptridx > 0) {
                    this.ptridx--;
                    return true;
                }
                else if (this.curleaf.PreviewLeaf != null) {
                    this.curleaf = this.curleaf.PreviewLeaf;
                    this.ptridx = this.curleaf.Count - 1;
                    return true;
                }
                else {
                    return false;
                }
            }

            /// <summary>列挙子をコレクションの最初の要素の前に設定する。</summary>
            public void Reset()
            {
                this.curleaf = this.parent.start;
                this.ptridx = -1;
                this.started = 0;
            }

            /// <summary>リソースの解放を行う。</summary>
            public void Dispose()
            {
                // ※ 解放する要素なし
            }

            #endregion
        }
    }
}
