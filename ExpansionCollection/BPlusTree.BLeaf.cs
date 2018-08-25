using System;
using System.Collections.Generic;
using System.Text;

namespace ExpansionCollection
{
    partial class BPlusTree<T>
    {
        /// <summary>木要素、葉クラス。</summary>
        private sealed class BLeaf
            : IBParts
        {
            #region "fields"

            /// <summary>項目リスト。</summary>
            private T[] value;

            #endregion

            #region "properteis"

            /// <summary>項目数を取得する。</summary>
            public int Count
            {
                get;
                private set;
            }

            /// <summary>項目リストを取得する。</summary>
            public T[] Values => this.value;

            /// <summary>データを格納している葉情報の参照を記憶する。</summary>
            public BLeaf TraverseLeaf => this;

            /// <summary>検索キーとなる要素を取得する。</summary>
            public T HeaderItem => this.value[0];

            /// <summary>次の葉要素を取得する。</summary>
            public BLeaf PreviewLeaf
            {
                get;
                private set;
            }

            /// <summary>次の葉要素を取得する。</summary>
            public BLeaf NextLeaf
            {
                get;
                private set;
            }

            #endregion

            #region "constructor"

            /// <summary>コンストラクタ。</summary>
            /// <param name="bracketSize">領域サイズ。</param>
            public BLeaf(int bracketSize)
            {
                this.Count = 0;
                this.PreviewLeaf = null;
                this.NextLeaf = null;
                this.value = new T[bracketSize];
            }

            #endregion

            #region "methods"

            //---------------------------------------------------------------------
            // 追加
            //---------------------------------------------------------------------
            /// <summary>木要素に要素を追加する。</summary>
            /// <param name="item">追加する項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <param name="manage">処理結果。</param>
            /// <returns>追加できたならば真。</returns>
            public bool Add(T item, BPlusTree<T> parent, ref ManageResult manage)
            {
                if (this.Count <= 0) {
                    // 要素が一つもなければ先頭に追加する
                    this.value[0] = item;
                    this.Count++;
                    parent.Count++;
                    manage.changed = true;
                    return false;
                }
                else {
                    // 挿入位置を検索する
                    int idx = BinarySearchOfAdd(item, parent.defComp);

                    if (this.Count < parent.bracketSize) {
                        // 挿入位置に値を挿入
                        if (this.Count > idx) {
                            Array.Copy(this.value, idx, this.value, idx + 1, this.Count - idx);
                        }
                        this.value[idx] = item;

                        this.Count++;
                        parent.Count++;
                        manage.changed = true;
                        return false;
                    }
                    else {
                        // 領域サイズを超えたので分割する
                        this.Split(item, idx, parent, ref manage);
                        parent.Count++;
                        manage.changed = true;
                        return true;
                    }
                }
            }

            /// <summary>挿入位置の検索を行う（指定値を超える、追加用）</summary>
            /// <param name="item">挿入する項目。</param>
            /// <param name="comparer">比較処理。</param>
            /// <returns>挿入位置。</returns>
            private int BinarySearchOfAdd(T item, IComparer<T> comparer)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (comparer.Compare(this.value[md], item) <= 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                return lf;
            }

            /// <summary>葉要素の分割を行う。</summary>
            /// <param name="item">追加する要素。</param>
            /// <param name="idx">追加位置。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="manage">処理状態。</param>
            private void Split(T item, int idx, BPlusTree<T> parent, ref ManageResult manage)
            {
                var tmpNext = new BLeaf(parent.bracketSize);

                if (idx < parent.mSize + 1) {
                    // 後半部分はコピー
                    tmpNext.Count = parent.mSize + 1;
                    Array.Copy(this.value, parent.mSize, tmpNext.value, 0, parent.mSize + 1);

                    // 前半部は挿入
#if DEBUG
                    Array.Clear(this.value, parent.mSize, this.value.Length - parent.mSize);
#endif
                    this.Count = parent.mSize + 1;
                    if (parent.mSize + 1 > idx) {
                        Array.Copy(this.value, idx, this.value, idx + 1, (parent.mSize + 1) - idx);
                    }
                    this.value[idx] = item;
                }
                else {
                    // 後半部分に挿入
                    int ptr = idx - (parent.mSize + 1);
                    tmpNext.Count = parent.mSize + 1;
                    if (ptr > 0) {
                        Array.Copy(this.value, parent.mSize + 1, tmpNext.value, 0, ptr);
                    }
                    Array.Copy(this.value, idx, tmpNext.value, ptr + 1, parent.bracketSize - idx);
                    tmpNext.value[ptr] = item;

                    // 前半部分は変更なし
#if DEBUG
                    Array.Clear(this.value, parent.mSize + 1, this.value.Length - (parent.mSize + 1));
#endif
                    this.Count = parent.mSize + 1;
                }

                // 前後のリンクを設定
                tmpNext.NextLeaf = this.NextLeaf;
                if (this.NextLeaf != null) {
                    this.NextLeaf.PreviewLeaf = tmpNext;
                }
                this.NextLeaf = tmpNext;
                tmpNext.PreviewLeaf = this;

                // 新しく生成した要素を返す
                manage.newParts = tmpNext;
            }

            //---------------------------------------------------------------------
            // 削除
            //---------------------------------------------------------------------
            /// <summary>指定要素を取得する。</summary>
            /// <param name="item">削除する要素。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="remove">削除状態。</param>
            /// <returns>バランス調整が必要ならば真。</returns>
            public bool Remove(T item, BPlusTree<T> parent, ref RemoveResult remove)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (parent.defComp.Compare(this.value[md], item) < 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                if (lf < this.Count &&
                    parent.defComp.Compare(this.value[lf], item) == 0) {
                    if (lf < this.Count - 1) {
                        Array.Copy(this.value, lf + 1, this.value, lf, this.Count - (lf + 1));
                    }
#if DEBUG
                    Array.Clear(this.value, this.Count - 1, 1);
#endif
                    // 項目数を減少
                    this.Count--;
                    parent.Count--;
                    remove.changed = true;
                    return true;
                }
                else {
                    return false;
                }
            }

            /// <summary>指定位置の要素を取得する。</summary>
            /// <param name="index">削除するインデックス。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="remove">削除状態。</param>
            /// <returns>バランス調整が必要ならば真。</returns>
            public bool RemoveAt(int index, BPlusTree<T> parent, ref RemoveResult remove)
            {
                if (remove.countIndex <= index && index < remove.countIndex + this.Count) {
                    // 項目を削除する
                    var i = index - remove.countIndex;
                    if (i < this.Count - 1) {
                        Array.Copy(this.value, i + 1, this.value, i, this.Count - (i + 1));
                    }
#if DEBUG
                    Array.Clear(this.value, this.Count - 1, 1);
#endif
                    // 項目数を減少
                    this.Count--;
                    parent.Count--;

                    return true;
                }
                else {
                    remove.countIndex += this.Count;
                    return false;
                }
            }

            /// <summary>同階層の要素内の項目数のバランスを取る。</summary>
            /// <param name="other">同階層の要素。</param>
            public void BalanceParts(IBParts other)
            {
                var leaf = other as BPlusTree<T>.BLeaf;

                // 全体の項目数を取得
                int allCnt = this.Count + leaf.Count;
                int hfcnt = allCnt / 2;
                int spncnt;

                if (this.Count < hfcnt) {
                    // 左辺の項目が少ない
                    spncnt = hfcnt - this.Count;
                    Array.Copy(leaf.value, 0, this.value, this.Count, spncnt);
                    Array.Copy(leaf.value, spncnt, leaf.value, 0, leaf.Count - spncnt);
                    this.Count = hfcnt;
                    leaf.Count = allCnt - hfcnt;
#if DEBUG
                    for (int i = leaf.Count; i < leaf.value.Length; ++i) {
                        leaf.value[i] = default(T);
                    }
#endif
                }
                else if (this.Count > hfcnt) {
                    // 右辺の項目が少ない
                    spncnt = this.Count - hfcnt;
                    Array.Copy(leaf.value, 0, leaf.value, spncnt, leaf.Count);
                    Array.Copy(this.value, hfcnt, leaf.value, 0, spncnt);
                    this.Count = hfcnt;
                    leaf.Count = allCnt - hfcnt;
#if DEBUG
                    for (int i = this.Count; i < this.value.Length; ++i) {
                        this.value[i] = default(T);
                    }
#endif
                }
            }

            /// <summary>指定要素の項目を取り込む。</summary>
            /// <param name="other">取り込む要素。</param>
            public void MargeParts(IBParts other)
            {
                var leaf = other as BPlusTree<T>.BLeaf;
                Array.Copy(leaf.value, 0, this.value, this.Count, leaf.Count);
                this.Count += leaf.Count;
                this.NextLeaf = leaf.NextLeaf;
                if (leaf.NextLeaf != null) {
                    leaf.NextLeaf.PreviewLeaf = this;
                }
            }

            //---------------------------------------------------------------------
            // 検索機能
            //---------------------------------------------------------------------
            /// <summary>指定要素以上となるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            public SearchResult SearchOfGe(T item, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (parent.defComp.Compare(this.value[md], item) < 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                // 検索結果を返す
                if (lf < this.Count) {
                    return new SearchResult(this, lf);
                }
                else if (this.NextLeaf != null) {
                    return new SearchResult(this.NextLeaf, 0);
                }
                else {
                    return new SearchResult(this, this.Count);
                }
            }

            /// <summary>指定要素以下となるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            public SearchResult SearchOfLe(T item, BPlusTree<T> parent)
            {
                int lf = -1;
                int rt = this.Count - 1;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md], item) > 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 検索結果を返す
                if (lf >= 0) {
                    return new SearchResult(this, lf);
                }
                else if (this.PreviewLeaf != null) {
                    return new SearchResult(this.PreviewLeaf, 0);
                }
                else {
                    return new SearchResult(this, -1);
                }
            }

            /// <summary>指定要素をこえるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            public SearchResult SearchOfGt(T item, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (parent.defComp.Compare(this.value[md], item) <= 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                // 検索結果を返す
                if (lf < this.Count) {
                    return new SearchResult(this, lf);
                }
                else if (this.NextLeaf != null) {
                    return new SearchResult(this.NextLeaf, 0);
                }
                else {
                    return new SearchResult(this, this.Count);
                }
            }

            /// <summary>指定要素未満となるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            public SearchResult SearchOfLt(T item, BPlusTree<T> parent)
            {
                int lf = -1;
                int rt = this.Count - 1;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md], item) >= 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 検索結果を返す
                if (lf >= 0) {
                    return new SearchResult(this, lf);
                }
                else if (this.PreviewLeaf != null) {
                    return new SearchResult(this.PreviewLeaf, 0);
                }
                else {
                    return new SearchResult(this, -1);
                }
            }

            //---------------------------------------------------------------------
            // IndexOf／LastIndexOf
            //---------------------------------------------------------------------
            /// <summary>指定した項目が最初に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            public int IndexOf(T item, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (parent.defComp.Compare(this.value[md], item) < 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                if (lf < this.Count &&
                    parent.defComp.Compare(this.value[lf], item) == 0) {
                    var ptr = this.PreviewLeaf;
                    while (ptr != null) {
                        lf += ptr.Count;
                        ptr = ptr.PreviewLeaf;
                    }
                    return lf;
                }
                else if (this.NextLeaf != null &&
                         parent.defComp.Compare(this.NextLeaf.value[0], item) == 0) {
                    var ptr = this.PreviewLeaf;
                    while (ptr != null) {
                        lf += ptr.Count;
                        ptr = ptr.PreviewLeaf;
                    }
                    return lf;
                }
                else {
                    return -1;
                }
            }

            /// <summary>指定した項目が最後に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            public int LastIndexOf(T item, BPlusTree<T> parent)
            {
                int lf = -1;
                int rt = this.Count - 1;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md], item) > 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                if (lf >= 0 &&
                    parent.defComp.Compare(this.value[lf], item) == 0) {
                    // 自リスト内で一致を確認
                    var ptr = this.PreviewLeaf;
                    while (ptr != null) {
                        lf += ptr.Count;
                        ptr = ptr.PreviewLeaf;
                    }
                    return lf;
                }
                else if (this.PreviewLeaf != null &&
                         parent.defComp.Compare(this.PreviewLeaf.value[this.PreviewLeaf.Count - 1], item) == 0) {
                    // 次リスト内で一致を確認
                    var ptr = this.PreviewLeaf;
                    while (ptr != null) {
                        lf += ptr.Count;
                        ptr = ptr.PreviewLeaf;
                    }
                    return lf;
                }
                else {
                    return -1;
                }
            }

            //---------------------------------------------------------------------
            // その他
            //---------------------------------------------------------------------
            /// <summary>値を取得する。</summary>
            /// <param name="index">値のインデックス。</param>
            public T GetValue(int index)
            {
                return this.value[index];
            }

            /// <summary>リストに指定項目が登録されているか検索し、あれば取得する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="resultvalue">取得結果。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>存在すれば真。</returns>
            public bool TryGetValue(T item, out T resultvalue, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count;
                int md;

                // 位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (parent.defComp.Compare(this.value[md], item) < 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                if (lf < this.Count &&
                    parent.defComp.Compare(this.value[lf], item) == 0) {
                    resultvalue = this.value[lf];
                    return true;
                }
                else if (this.NextLeaf != null &&
                         parent.defComp.Compare(this.NextLeaf.value[0], item) == 0) {
                    resultvalue = this.NextLeaf.value[0];
                    return true;
                }
                else {
                    resultvalue = default(T);
                    return false;
                }
            }

            /// <summary>文字列表現（木形式）を取得する。</summary>
            /// <param name="builder">文字列バッファ。</param>
            /// <param name="nest">ネスト文字列。</param>
            public void ConvertTxetTree(StringBuilder builder, string nest)
            {
                for (int i = 0; i < this.Count; ++i) {
                    builder.Append(nest);
                    builder.Append(this.value[i]);
                    builder.Append(",");
                }
            }

            #endregion
        }
    }
}
