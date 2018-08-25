using System;
using System.Text;

namespace ExpansionCollection
{
    partial class BPlusTree<T>
    {
        //--------------------------------------------------------------------
        // 枝クラス実装
        //--------------------------------------------------------------------
        /// <summary>木要素、枝クラス。</summary>
        private sealed class BBranch
            : BParts
        {
            /// <summary>項目リスト。</summary>
            private BParts[] value;

            /// <summary>先頭の葉。</summary>
            private BLeaf headerLeaf;

            /// <summary>項目数を取得する。</summary>
            public int Count
            {
                get;
                private set;
            }

            /// <summary>検索キーとなる要素を取得する。</summary>
            public T HeaderItem => this.headerLeaf.HeaderItem;

            /// <summary>最初の要素を取得する。</summary>
            /// <returns>最初の要素。</returns>
            public BParts FirstParts => this.value[0];

            /// <summary>データを格納している葉情報の参照を記憶する。</summary>
            public BLeaf TraverseLeaf => this.value[0].TraverseLeaf;

            /// <summary>コンストラクタ。</summary>
            /// <param name="bracket">領域サイズ。</param>
            public BBranch(int bracket)
            {
                this.value = new BParts[bracket];
                this.Count = 0;
            }

            /// <summary>コンストラクタ。</summary>
            /// <param name="bracket">領域サイズ。</param>
            /// <param name="leftItem">左辺値。</param>
            /// <param name="rightItem">右辺値。</param>
            public BBranch(int bracket, BParts leftItem, BParts rightItem)
            {
                this.value = new BParts[bracket];
                this.Count = 2;
                this.value[0] = leftItem;
                this.value[1] = rightItem;
                this.headerLeaf = this.TraverseLeaf;
            }

            /// <summary>木要素に要素を追加する。</summary>
            /// <param name="item">追加する項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <param name="manage">処理結果。</param>
            /// <returns>追加したら真。</returns>
            public bool Add(T item, BPlusTree<T> parent, ref ManageResult manage)
            {
                int lf = 0;
                int rt = this.Count - 1;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (md + 1 < this.Count &&
                        parent.defComp.Compare(this.value[md + 1].HeaderItem, item) <= 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                // 要素を挿入する
                if (this.value[lf].Add(item, parent, ref manage)) {
                    if (this.Count < parent.bracketSize) {
                        // 挿入位置に値を挿入
                        if (this.Count > lf + 1) {
                            Array.Copy(this.value, lf + 1, this.value, lf + 2, this.Count - (lf + 1));
                        }
                        this.value[lf + 1] = manage.newParts;
                        this.Count++;
                        return false;
                    }
                    else {
                        // 領域サイズを超えたので分割する
                        this.Split(manage.newParts, lf + 1, parent, ref manage);
                        return true;
                    }
                }
                else {
                    return false;
                }
            }

            /// <summary>枝要素の分割を行う。</summary>
            /// <param name="element">追加する要素。</param>
            /// <param name="idx">追加位置。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="manage">処理結果。</param>
            private void Split(BParts element, int idx, BPlusTree<T> parent, ref ManageResult manage)
            {
                var tmpNext = new BBranch(parent.bracketSize);

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
                    this.value[idx] = element;
                }
                else {
                    // 後半部分に挿入
                    int ptr = idx - (parent.mSize + 1);
                    tmpNext.Count = parent.mSize + 1;
                    if (ptr > 0) {
                        Array.Copy(this.value, parent.mSize + 1, tmpNext.value, 0, ptr);
                    }
                    Array.Copy(this.value, idx, tmpNext.value, ptr + 1, parent.bracketSize - idx);
                    tmpNext.value[ptr] = element;

                    // 前半部分は変更なし
#if DEBUG
                    Array.Clear(this.value, parent.mSize + 1, this.value.Length - (parent.mSize + 1));
#endif
                    this.Count = parent.mSize + 1;
                }

                // 検索キー参照変更
                tmpNext.headerLeaf = tmpNext.TraverseLeaf;

                // 新しく生成した要素を返す
                manage.newParts = tmpNext;
            }

            /// <summary>指定要素を取得する。</summary>
            /// <param name="item">削除する要素。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="remove">削除状態。</param>
            /// <returns>バランス調整が必要ならば真。</returns>
            public bool Remove(T item, BPlusTree<T> parent, ref RemoveResult remove)
            {
                int lf = 0;
                int rt = this.Count - 1;
                int md;

                // 削除要素の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md].HeaderItem, item) >= 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 要素を削除する
                if (this.value[lf].Remove(item, parent, ref remove)) {
                    if (this.value[lf].Count <= parent.mSize) {
                        this.BalanceChangeOfBParts(parent, lf);
                    }
                    return true;
                }
                else if (lf + 1 < this.Count &&
                         parent.defComp.Compare(this.value[lf + 1].HeaderItem, item) == 0 &&
                         this.value[lf + 1].Remove(item, parent, ref remove)) {
                    if (this.value[lf + 1].Count <= parent.mSize) {
                        this.BalanceChangeOfBParts(parent, lf + 1);
                    }
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
                for (int i = 0; i < this.Count; ++i) {
                    if (this.value[i].RemoveAt(index, parent, ref remove)) {
                        if (this.value[i].Count <= parent.mSize) {
                            this.BalanceChangeOfBParts(parent, i);
                        }
                        return true;
                    }
                }
                return false;
            }

            /// <summary>枝、葉のバランスを調整する。</summary>
            /// <param name="parent">B+木コレクション。</param>
            /// <param name="idx">要素のインデックス。</param>
            private void BalanceChangeOfBParts(BPlusTree<T> parent, int idx)
            {
                if (idx > 0 && this.value[idx - 1].Count > parent.mSize + 1) {
                    this.value[idx - 1].BalanceParts(this.value[idx]);
                }
                else if (idx < this.Count - 1 && this.value[idx + 1].Count > parent.mSize + 1) {
                    this.value[idx].BalanceParts(this.value[idx + 1]);
                }
                else if (idx > 0) {
                    this.value[idx - 1].MargeParts(this.value[idx]);
                    Array.Copy(this.value, idx + 1, this.value, idx, this.Count - 1 - idx);
#if DEBUG
                    Array.Clear(this.value, this.Count - 1, 1);
#endif
                    this.Count--;
                }
                else {
                    this.value[0].MargeParts(this.value[1]);
                    Array.Copy(this.value, 2, this.value, 1, this.Count - 2);
#if DEBUG
                    Array.Clear(this.value, this.Count - 1, 1);
#endif
                    this.Count--;
                }
            }

            /// <summary>同階層の要素内の項目数のバランスを取る。</summary>
            /// <param name="other">同階層の要素。</param>
            public void BalanceParts(BParts other)
            {
                var branch = (BPlusTree<T>.BBranch)other;

                // 全体の項目数を取得
                int allCnt = this.Count + branch.Count;
                int hfcnt = allCnt / 2;
                int spncnt;

                if (this.Count < hfcnt) {
                    // 左辺の項目が少ない
                    spncnt = hfcnt - this.Count;
                    Array.Copy(branch.value, 0, this.value, this.Count, spncnt);
                    Array.Copy(branch.value, spncnt, branch.value, 0, branch.Count - spncnt);
                    this.Count = hfcnt;
                    branch.Count = allCnt - hfcnt;

                    // 検索キー参照変更
                    branch.headerLeaf = branch.TraverseLeaf;
                }
                else if (this.Count > hfcnt) {
                    // 右辺の項目が少ない
                    spncnt = this.Count - hfcnt;
                    Array.Copy(branch.value, 0, branch.value, spncnt, branch.Count);
                    Array.Copy(this.value, hfcnt, branch.value, 0, spncnt);
                    this.Count = hfcnt;
                    branch.Count = allCnt - hfcnt;

                    // 検索キー参照変更
                    branch.headerLeaf = branch.TraverseLeaf;
                }
            }

            /// <summary>指定要素の項目を取り込む。</summary>
            /// <param name="other">取り込む要素。</param>
            public void MargeParts(BParts other)
            {
                var branch = (BPlusTree<T>.BBranch)other;
                Array.Copy(branch.value, 0, this.value, this.Count, branch.Count);
                this.Count += branch.Count;
            }


            //            /// <summary>木要素より項目を削除する。</summary>
            //            /// <param name="item">削除する項目。</param>
            //            /// <param name="parent">B+木コレクション。</param>
            //            /// <returns>項目が残っていれば真。</returns>
            //            public bool Remove(T item, FZZ01_2000_BPlusTree<T> parent)
            //            {
            //                // 削除項目を検索
            //                int idx = BinarySearchOfBPartsLt(item, this.value, this.branchCount);

            //                // 前のグループから削除を行う
            //                var balance = this.value[idx].Remove(item, parent);
            //                if (!parent.changed && idx + 1 < this.branchCount) {
            //                    idx++;
            //                    balance = this.value[idx].Remove(item, parent);
            //                }

            //                // 枝、葉のバランスの調整
            //                if (balance) {
            //                    return this.BalanceChangeOfBParts(idx, parent);
            //                }
            //                else {
            //                    return false;
            //                }
            //            }

            //            /// <summary>指定要素以上となるインデックスを検索する。</summary>
            //            /// <param name="item">検索項目。</param>
            //            /// <param name="parent">B+木コレクション。</param>
            //            /// <returns>検索結果。</returns>
            //            public SearchResult SearchOfGe(T item, FZZ01_2000_BPlusTree<T> parent)
            //            {
            //                // 項目を検索
            //                int idx = BinarySearchOfBPartsLt(item, this.value, this.branchCount);

            //                // 前のグループから検索を行う
            //                var answer = this.value[idx].SearchOfGe(item, parent);
            //                if (answer.index >= answer.leaf.Count && idx + 1 < this.branchCount) {
            //                    var nxtans = this.value[idx + 1].SearchOfGe(item, parent);
            //                    if (nxtans.index < nxtans.leaf.Count) {
            //                        answer = nxtans;
            //                    }
            //                }

            //                return answer;
            //            }

            //            /// <summary>指定要素以下となるインデックスを検索する。</summary>
            //            /// <param name="item">検索項目。</param>
            //            /// <param name="parent">B+木コレクション。</param>
            //            /// <returns>検索結果。</returns>
            //            public SearchResult SearchOfLe(T item, FZZ01_2000_BPlusTree<T> parent)
            //            {
            //                // 項目を検索
            //                int idx = BinarySearchOfBPartsGt(item, this.value, this.branchCount);

            //                // 前のグループから検索を行う
            //                var answer = this.value[idx].SearchOfLe(item, parent);
            //                if (answer.index < 0 && idx > 0) {
            //                    var nxtans = this.value[idx - 1].SearchOfLe(item, parent);
            //                    if (nxtans.index >= 0) {
            //                        answer = nxtans;
            //                    }
            //                }

            //                return answer;
            //            }

            //            /// <summary>指定要素をこえるインデックスを検索する。</summary>
            //            /// <param name="item">検索項目。</param>
            //            /// <param name="parent">B+木コレクション。</param>
            //            /// <returns>検索結果。</returns>
            //            public SearchResult SearchOfGt(T item, FZZ01_2000_BPlusTree<T> parent)
            //            {
            //                // 項目を検索
            //                int idx = BinarySearchOfBPartsLe(item, this.value, this.branchCount);

            //                // 前のグループから検索を行う
            //                var answer = this.value[idx].SearchOfGt(item, parent);
            //                if (answer.index >= answer.leaf.Count && idx + 1 < this.branchCount) {
            //                    var nxtans = this.value[idx + 1].SearchOfGt(item, parent);
            //                    if (nxtans.index < nxtans.leaf.Count) {
            //                        answer = nxtans;
            //                    }
            //                }

            //                return answer;
            //            }

            //            /// <summary>指定要素未満のインデックスを検索する。</summary>
            //            /// <param name="item">検索項目。</param>
            //            /// <param name="parent">B+木コレクション。</param>
            //            /// <returns>検索結果。</returns>
            //            public SearchResult SearchOfLt(T item, FZZ01_2000_BPlusTree<T> parent)
            //            {
            //                // 項目を検索
            //                int idx = BinarySearchOfBPartsGe(item, this.value, this.branchCount);

            //                // 前のグループから検索を行う
            //                var answer = this.value[idx].SearchOfLt(item, parent);
            //                if (answer.index < 0 && idx > 0) {
            //                    var nxtans = this.value[idx - 1].SearchOfLt(item, parent);
            //                    if (nxtans.index >= 0) {
            //                        answer = nxtans;
            //                    }
            //                }

            //                return answer;
            //            }

            /// <summary>リストに指定項目が登録されているか検索し、あれば取得する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="resultvalue">取得結果。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>存在すれば真。</returns>
            public bool TryGetValue(T item, out T resultvalue, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count - 1;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md].HeaderItem, item) >= 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 存在を確認する
                return this.value[lf].TryGetValue(item, out resultvalue, parent);
            }

            /// <summary>指定した項目が最初に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            public int IndexOf(T item, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count - 1;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md].HeaderItem, item) >= 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 存在を確認する
                return this.value[lf].IndexOf(item, parent);
            }

            /// <summary>指定した項目が最後に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            public int LastIndexOf(T item, BPlusTree<T> parent)
            {
                int lf = 0;
                int rt = this.Count - 1;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2 + 1;

                    if (parent.defComp.Compare(this.value[md].HeaderItem, item) > 0) {
                        rt = md - 1;
                    }
                    else {
                        lf = md;
                    }
                }

                // 存在を確認する
                return this.value[lf].LastIndexOf(item, parent);
            }

            /// <summary>文字列表現（木形式）を取得する。</summary>
            /// <param name="builder">文字列バッファ。</param>
            /// <param name="nest">ネスト文字列。</param>
            public void ConvertTxetTree(StringBuilder builder, string nest)
            {
                builder.AppendFormat("{0}・{1}({2})", nest, this.headerLeaf.HeaderItem, this.Count);
                builder.AppendLine();
                for (int i = 0; i < this.Count; ++i) {
                    this.value[i].ConvertTxetTree(builder, nest + " ");
                    builder.AppendLine();
                }
            }
        }
    }
}
