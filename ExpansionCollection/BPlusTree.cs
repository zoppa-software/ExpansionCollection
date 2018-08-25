using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ExpansionCollection
{
    /// <summary>独自 B+木コレクション実装。</summary>
    /// <typeparam name="T">対象の型。</typeparam>
    public partial class BPlusTree<T>
        : IEnumerable<T>, IEnumerable, IList<T>
    {
        #region "const"

        /// <summary>ブロックサイズ。</summary>
        private const int DefMSize = 16;

        #endregion

        #region "interface"

        /// <summary>木要素インターフェース。</summary>
        private interface BParts
        {
            /// <summary>検索キーとなる要素を取得する。</summary>
            T HeaderItem { get; }

            /// <summary>データを格納している葉情報の参照を記憶する。</summary>
            BLeaf TraverseLeaf { get; }

            /// <summary>項目数を取得する。</summary>
            int Count { get; }

            /// <summary>木要素に要素を追加する。</summary>
            /// <param name="item">追加する項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>追加したら真。</returns>
            bool Add(T item, BPlusTree<T> parent, ref ManageResult manage);

            /// <summary>指定要素を取得する。</summary>
            /// <param name="item">削除する要素。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="remove">削除状態。</param>
            /// <returns>バランス調整が必要ならば真。</returns>
            bool Remove(T item, BPlusTree<T> parent, ref RemoveResult remove);

            /// <summary>指定位置の要素を取得する。</summary>
            /// <param name="index">削除するインデックス。</param>
            /// <param name="parent">木構造。</param>
            /// <param name="remove">削除状態。</param>
            /// <returns>バランス調整が必要ならば真。</returns>
            bool RemoveAt(int index, BPlusTree<T> parent, ref RemoveResult remove);

            /// <summary>同階層の要素内の項目数のバランスを取る。</summary>
            /// <param name="other">同階層の要素。</param>
            void BalanceParts(BParts other);

            /// <summary>指定要素の項目を取り込む。</summary>
            /// <param name="other">取り込む要素。</param>
            void MargeParts(BParts other);

            /// <summary>リストに指定項目が登録されているか検索し、あれば取得する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="resultvalue">取得結果。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>存在すれば真。</returns>
            bool TryGetValue(T item, out T resultvalue, BPlusTree<T> parent);

            /// <summary>指定した項目が最初に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            int IndexOf(T item, BPlusTree<T> parent);

            /// <summary>指定した項目が最後に見つかったインデックスを取得する。</summary>
            /// <param name="item">検索する要素。</param>
            /// <returns>要素のインデックス。見つからなかったら -1。</returns>
            int LastIndexOf(T item, BPlusTree<T> parent);

            /// <summary>文字列表現（木形式）を取得する。</summary>
            /// <param name="builder">文字列バッファ。</param>
            /// <param name="nest">ネスト文字列。</param>
            void ConvertTxetTree(StringBuilder builder, string nest);
        }

        #endregion

        #region "struct"

        /// <summary>処理状態結果。</summary>
        private struct ManageResult
        {
            /// <summary>交換するデータ。</summary>
            public BParts newParts;

            /// <summary>葉情報が変更されていたら真。</summary>
            public bool changed;
        }

        /// <summary>削除状態結果。</summary>
        private struct RemoveResult
        {
            /// <summary>カウントインデックス。</summary>
            public int countIndex;

            /// <summary>削除されたら真。</summary>
            public bool changed;
        }

        /// <summary>検索結果構造体。</summary>
        private struct SearchResult
        {
            /// <summary>項目を格納している葉要素。</summary>
            public readonly BLeaf leaf;

            /// <summary>葉要素内のインデックス。</summary>
            public readonly int index;

            /// <summary>コンストラクタ。</summary>
            /// <param name="leaf">対象の葉要素。</param>
            /// <param name="idx">インデックス。</param>
            public SearchResult(BLeaf leaf, int idx)
            {
                this.leaf = leaf;
                this.index = idx;
            }
        }

        #endregion

        #region "inner class"

        //--------------------------------------------------------------------
        // 列挙の実装
        //--------------------------------------------------------------------
        /// <summary>項目参照用、列挙子。</summary>
        private sealed class BPlusEnumerator
            : IBPlusTreeIterator<T>
        {
            /// <summary>参照対象のコレクション。</summary>
            private BPlusTree<T> parent;

            /// <summary>現在位置（葉要素）</summary>
            private BLeaf curleaf;

            /// <summary>現在位置（葉要素内のインデックス）</summary>
            private int ptridx;

            /// <summary>初期位置ならば 0。</summary>
            private int started;

            /// <summary>列挙子の現在位置にあるコレクション内の要素を取得する。</summary>
            public T Current
            {
                get {
                    return this.curleaf.GetValue(this.ptridx);
                }
            }

            /// <summary>列挙子の現在位置にあるコレクション内の要素を取得する。</summary>
            object IEnumerator.Current
            {
                get {
                    return this.curleaf.GetValue(this.ptridx);
                }
            }

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
        }

        #endregion

        #region "fields"

        /// <summary>分割サイズ。</summary>
        private readonly int mSize;

        /// <summary>バケットサイズ。</summary>
        private readonly int bracketSize;

        /// <summary>比較処理。</summary>
        private readonly IComparer<T> defComp;

        /// <summary>ルート枝要素。</summary>
        private BBranch root;

        /// <summary>開始葉要素。</summary>
        private BLeaf start;

        #endregion

        #region "properties"

        /// <summary>リストに格納されている要素数を取得する。</summary>
        public int Count
        {
            get;
            private set;
        }

        /// <summary>リストが読み取り専用か真偽値を返す。</summary>
        /// <value>真偽値。</value>
        /// <remarks>
        /// このリストは読み取り専用にできません。
        /// </remarks>
        public bool IsReadOnly => false;

        #endregion

        #region "indexer"

        /// <summary>指定したインデックスにある項目を取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>指定位置の要素。</returns>
        public T this[int index]
        {
            get {
                if (index >= 0 && index < this.Count) {
                    var res = this.SearchLeafAt(index);
                    return res.leaf.GetValue(res.index);
                }
                else {
                    throw new IndexOutOfRangeException("インデックスが要素の範囲に含まれない");
                }
            }
            set {
                throw new NotSupportedException("このリストはオーダー順を持つリストであるため、途中挿入はできません。");
            }
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <param name="bracketSize">バケットサイズを指定。</param>
        /// <param name="comparer">比較処理。</param>
        public BPlusTree(int bracketSize, IComparer<T> comparer)
        {
            this.mSize = bracketSize >> 1;
            this.defComp = comparer;
            this.bracketSize = this.mSize * 2 + 1;
            this.Clear();

            var ll = new List<int>();
            ll.Add(1);
        }

        /// <summary>コンストラクタ。</summary>
        public BPlusTree()
            : this(DefMSize, Comparer<T>.Default)
        { }

        /// <summary>コンストラクタ。</summary>
        /// <param name="collection">コピー元リスト。</param>
        public BPlusTree(IEnumerable<T> collection)
            : this(DefMSize, Comparer<T>.Default)
        {
            this.AddRange(collection);
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="bracketSize">バケットサイズを指定。</param>
        public BPlusTree(int bracketSize)
            : this(bracketSize, Comparer<T>.Default)
        { }

        /// <summary>コンストラクタ。</summary>
        /// <param name="bracketSize">バケットサイズを指定。</param>
        /// <param name="collection">コピー元リスト。</param>
        public BPlusTree(int bracketSize, IEnumerable<T> collection)
            : this(bracketSize, Comparer<T>.Default)
        {
            this.AddRange(collection);
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="comparer">比較式。</param>
        public BPlusTree(IComparer<T> comparer)
            : this(DefMSize, comparer)
        { }

        /// <summary>コンストラクタ。</summary>
        /// <param name="collection">コピー元リスト。</param>
        /// <param name="comparer">比較式。</param>
        public BPlusTree(IEnumerable<T> collection, IComparer<T> comparer)
            : this(DefMSize, comparer)
        {
            this.AddRange(collection);
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="bracketSize">バケットサイズを指定。</param>
        /// <param name="collection">コピー元リスト。</param>
        /// <param name="comparer">比較式。</param>
        public BPlusTree(int bracketSize, IEnumerable<T> collection, IComparer<T> comparer)
            : this(bracketSize, comparer)
        {
            this.AddRange(collection);
        }

        #endregion

        #region "methods"

        /// <summary>リストから全ての項目を削除します。</summary>
        public void Clear()
        {
            this.root = null;
            this.start = new BLeaf(this.bracketSize);
        }

        /// <summary>インスタンスの文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            var res = new StringBuilder();
            if (this.root == null) {
                this.start.ConvertTxetTree(res, "");
            }
            else {
                this.root.ConvertTxetTree(res, "");
            }
            return res.ToString();
        }

        /// <summary>コレクションを配列に変換する。</summary>
        /// <returns>変換結果配列。</returns>
        public T[] ToArray()
        {
            var res = new T[this.Count];
            var ptr = this.start;
            int idx = 0;
            while (ptr != null) {
                Array.Copy(ptr.Values, 0, res, idx, ptr.Count);
                idx += ptr.Count;
                ptr = ptr.NextLeaf;
            }
            return res;
        }

        /// <summary>指定範囲の要素を取得する。</summary>
        /// <param name="index">範囲の開始位置。</param>
        /// <param name="count">範囲の要素数。</param>
        /// <returns>範囲内の要素リスト。</returns>
        public List<T> GetRange(int index, int count)
        {
            var res = new List<T>(count);
            var block = this.SearchLeafAt(index);
            var ptr = block.leaf;
            var idx = block.index;
            var len = 0;

            // ネスト分があればその分ずらして展開
            if (ptr != null && idx > 0 && count > 0) {
                len = count < ptr.Count - idx ? count : ptr.Count - idx;
                for (int i = 0; i < len; ++i) {
                    res.Add(ptr.Values[i + idx]);
                }
                ptr = ptr.NextLeaf;
                count -= len;
            }

            // 残りの部分を展開
            while (ptr != null && count > 0) {
                len = count < ptr.Count ? count : ptr.Count;
                for (int i = 0; i < len; ++i) {
                    res.Add(ptr.Values[i]);
                }
                ptr = ptr.NextLeaf;
                count -= len;
            }
            return res;
        }

        /// <summary>コレクションの各要素に指定された式を適用する。</summary>
        /// <param name="action">実行する式。</param>
        public void ForEach(Action<T> action)
        {
            var ptr = this.start;
            while (ptr != null) {
                for (int i = 0; i < ptr.Count; ++i) {
                    action(ptr.GetValue(i));
                }
            }
        }

        //---------------------------------------------------------------------
        // 追加
        //---------------------------------------------------------------------
        /// <summary>要素を追加する。</summary>
        /// <param name="item">追加する要素。</param>
        public void Add(T item)
        {
            var manage = new ManageResult();
            this.LocalAdd(item, ref manage);
        }

        /// <summary>要素リストを追加する。</summary>
        /// <param name="collection">要素リスト。</param>
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var v in collection) {
                var manage = new ManageResult();
                this.LocalAdd(v, ref manage);
            }
        }

        /// <summary>要素を追加する（内部処理）</summary>
        /// <param name="item">追加する要素。</param>
        /// <param name="manage">処理状態結果。</param>
        private void LocalAdd(T item, ref ManageResult manage)
        {
            if (this.root == null) {
                if (this.start.Add(item, this, ref manage)) {
                    this.root = new BBranch(this.bracketSize, this.start, manage.newParts);
                }
            }
            else {
                if (this.root.Add(item, this, ref manage)) {
                    this.root = new BBranch(this.bracketSize, this.root, manage.newParts);
                }
            }
        }

        /// <summary>リストの指定したインデックスに要素を挿入する。</summary>
        /// <param name="index">インデックス。</param>
        /// <param name="item">挿入する要素。</param>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException("このリストはオーダー順を持つリストであるため、途中挿入はできません。");
        }

        //--------------------------------------------------------------------
        // 列挙機能実装
        //--------------------------------------------------------------------
        /// <summary>指定要素を削除する。</summary>
        /// <param name="item">削除する要素。</param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            var remove = new RemoveResult();
            if (this.root == null) {
                this.start.Remove(item, this, ref remove);
            }
            else {
                if (this.root.Remove(item, this, ref remove)) {
                    this.RelinkRoot();
                }
            }
            return remove.changed;
        }

        /// <summary>指定位置の要素を取得する。</summary>
        /// <param name="index">削除するインデックス。</param>
        public void RemoveAt(int index)
        {
            var remove = new RemoveResult();
            if (this.root == null) {
                this.start.RemoveAt(index, this, ref remove);
            }
            else {
                if (this.root.RemoveAt(index, this, ref remove)) {
                    this.RelinkRoot();
                }
            }
        }

        /// <summary>ルート要素の再設定。</summary>
        private void RelinkRoot()
        {
            if (this.root.Count <= 1) {
                var parts = this.root.FirstParts;
                if (parts is BBranch) {
                    this.root = (BBranch)parts;
                }
                else {
                    this.root = null;
                    this.start = (BLeaf)parts;
                }
            }
        }

        //--------------------------------------------------------------------
        // 列挙機能実装
        //--------------------------------------------------------------------
        /// <summary>コレクションを反復処理する列挙子を取得する。</summary>
        /// <returns>反復処理する列挙子。</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new BPlusEnumerator(this);
        }

        /// <summary>コレクションを反復処理する列挙子を取得する。</summary>
        /// <returns>反復処理する列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BPlusEnumerator(this);
        }

        /// <summary>指定インデックスがどの葉のどの位置かを取得する。</summary>
        /// <param name="index">データインデックス。</param>
        /// <returns>検索結果。</returns>
        private SearchResult SearchLeafAt(int index)
        {
            var cur = this.start;
            var idx = index;

            while (cur.NextLeaf != null) {
                if (idx < cur.Count) {
                    return new SearchResult(cur, idx);
                }
                else {
                    idx -= cur.Count;
                }
                cur = cur.NextLeaf;
            }
            return new SearchResult(cur, idx);
        }

        //--------------------------------------------------------------------
        // 値取得、存在確認
        //--------------------------------------------------------------------
        /// <summary>リストに指定項目が登録されているか検索し、あれば取得する。</summary>
        /// <param name="item">検索項目。</param>
        /// <param name="resultvalue">取得結果。</param>
        /// <returns>存在すれば真。</returns>
        public bool TryGetValue(T item, out T resultvalue)
        {
            if (this.root == null) {
                return this.start.TryGetValue(item, out resultvalue, this);
            }
            else {
                return this.root.TryGetValue(item, out resultvalue, this);
            }
        }

        /// <summary>要素が存在するか確認する。</summary>
        /// <param name="item">確認する要素。</param>
        /// <returns>要素が存在すれば真。</returns>
        public bool Contains(T item)
        {
            T res;
            return this.TryGetValue(item, out res);
        }

        //---------------------------------------------------------------------
        // コピー機能
        //---------------------------------------------------------------------
        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="array">コピー先配列。</param>
        /// <param name="index">コピー先配列の書き込み開始位置。</param>
        public void CopyTo(Array array, int index)
        {
            var ptr = this.start;
            while (ptr != null) {
                for (int i = 0; i < ptr.Count; ++i) {
                    array.SetValue(ptr.GetValue(i), index++);
                }
                ptr = ptr.NextLeaf;
            }
        }

        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="array">コピー先配列。</param>
        public void CopyTo(T[] array)
        {
            this.CopyTo(array, 0);
        }

        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="array">コピー先配列。</param>
        /// <param name="index">コピー先配列の書き込み開始位置。</param>
        public void CopyTo(T[] array, int index)
        {
            var ptr = this.start;
            while (ptr != null) {
                for (int i = 0; i < ptr.Count; ++i) {
                    array[index++] = ptr.GetValue(i);
                }
                ptr = ptr.NextLeaf;
            }
        }

        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="index">コピー元開始位置。</param>
        /// <param name="array">コピー先配列。</param>
        /// <param name="arrayIndex">コピー先配列の書き込み開始位置。</param>
        /// <param name="count">コピーする数。</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            var block = this.SearchLeafAt(index);
            var ptr = block.leaf;
            var idx = block.index;
            var len = 0;

            // ネスト分があればその分ずらして展開
            if (ptr != null && idx > 0 && count > 0) {
                len = count < ptr.Count - idx ? count : ptr.Count - idx;
                for (int i = 0; i < len; ++i) {
                    array[arrayIndex++] = ptr.Values[i + idx];
                }
                ptr = ptr.NextLeaf;
                count -= len;
            }

            // 残りの部分を展開
            while (ptr != null && count > 0) {
                len = count < ptr.Count ? count : ptr.Count;
                for (int i = 0; i < len; ++i) {
                    array[arrayIndex++] = ptr.Values[i];
                }
                ptr = ptr.NextLeaf;
                count -= len;
            }
        }

        //---------------------------------------------------------------------
        // IndexOf／LastIndexOf
        //---------------------------------------------------------------------
        /// <summary>指定した項目が最初に見つかったインデックスを取得する。</summary>
        /// <param name="item">検索する要素。</param>
        /// <returns>要素のインデックス。見つからなかったら -1。</returns>
        public int IndexOf(T item)
        {
            if (this.root == null) {
                return this.start.IndexOf(item, this);
            }
            else {
                return this.root.IndexOf(item, this);
            }
        }

        /// <summary>指定した項目が最後に見つかったインデックスを取得する。</summary>
        /// <param name="item">検索する要素。</param>
        /// <returns>要素のインデックス。見つからなかったら -1。</returns>
        public int LastIndexOf(T item)
        {
            if (this.root == null) {
                return this.start.LastIndexOf(item, this);
            }
            else {
                return this.root.LastIndexOf(item, this);
            }
        }

        #endregion

        //public int RemoveAll(Predicate<T> match);
        //public void RemoveRange(int index, int count);
        //public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter);
        //public bool Exists(Predicate<T> match);
        //public T Find(Predicate<T> match);
        //public List<T> FindAll(Predicate<T> match);
        //public int FindIndex(int startIndex, int count, Predicate<T> match);
        //public int FindIndex(int startIndex, Predicate<T> match);
        //public int FindIndex(Predicate<T> match);
        //public T FindLast(Predicate<T> match);
        //public int FindLastIndex(int startIndex, int count, Predicate<T> match);
        //public int FindLastIndex(int startIndex, Predicate<T> match);
        //public int FindLastIndex(Predicate<T> match);
        //public bool TrueForAll(Predicate<T> match);

    }
}
