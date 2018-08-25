using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ExpansionCollection
{
    /// <summary>可変長配列（ブロックアルゴリズム）</summary>
    /// <typeparam name="T">リスト内の要素の型。</typeparam>
    public class Deck<T>
        : IList<T>, IList, ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        #region "const"

        /// <summary>デフォルトサイズ。</summary>
        private const int DEFAULT_SIZE = 128;

        #endregion

        #region "inner class"

        /// <summary>ブロック検索の結果を表現する。</summary>
        private struct BlockInfo
        {
            /// <summary>対象ブロックの参照。</summary>
            public readonly Block block;

            /// <summary>対象ブロックの位置。</summary>
            public readonly int index;

            /// <summary>コンストラクタ。</summary>
            /// <param name="block">対象ブロック。</param>
            /// <param name="index">対象ブロックの位置。</param>
            public BlockInfo(Block block, int index)
            {
                this.block = block;
                this.index = index;
            }
        }

        /// <summary>データブロック。</summary>
        private sealed class Block
        {
            /// <summary>データリスト。</summary>
            public readonly T[] items;

            /// <summary>データ個数。</summary>
            public int count;

            /// <summary>ブロックの先頭位置インデックス。</summary>
            public int Index
            {
                get;
                private set;
            }

            /// <summary>コンストラクタ。</summary>
            /// <param name="prev">前ブロック参照。</param>
            /// <param name="size">ブロックサイズ。</param>
            public Block(Block prev, int size)
            {
                this.items = new T[size];
                this.UpdateIndex(prev);
            }

            /// <summary>先頭位置インデックスを更新する。</summary>
            /// <param name="preview">前ブロック。</param>
            public void UpdateIndex(Block preview)
            {
                this.Index = (preview != null ? preview.Index + preview.count : 0);
            }

            /// <summary>インスタンスの文字列表現を取得する。</summary>
            /// <returns>文字列表現。</returns>
            public override string ToString()
            {
                var buf = new StringBuilder();
                if (this.count > 0) {
                    buf.AppendFormat("{0}:{1}", this.Index, this.items[0].ToString());
                    for (int i = 1; i < this.count; ++i) {
                        buf.AppendFormat(",{0}", this.items[i].ToString());
                    }
                }
                return buf.ToString();
            }
        }

        #endregion

        #region "fields"

        /// <summary>ブロックサイズ。</summary>
        private readonly int helfBlock;

        /// <summary>ブロックリスト。</summary>
        private readonly List<Block> blocks;

        #endregion

        #region "properties"

        /// <summary>ブロックサイズを取得する。</summary>
        private int BlockSize
        {
            get {
                return (this.helfBlock << 1) + 1;
            }
        }

        /// <summary>コレクションに格納されている要素数を取得する。</summary>
        public int Count
        {
            get {
                var bck = this.blocks[this.blocks.Count - 1];
                return bck.Index + bck.count;
            }
        }

        /// <summary>IList が読み取り専用かどうかを示す値を取得する。falseのみ。</summary>
        public bool IsReadOnly => false;

        /// <summary>ICollection へのアクセスが同期されているかどうかを示す値を取得します。</summary>
        public bool IsSynchronized => false;

        /// <summary>ICollection へのアクセスを同期するために使用できるオブジェクトを取得します。</summary>
        public object SyncRoot => throw new NotSupportedException();

        /// <summary>IList が固定サイズかどうかを示す値を取得します。</summary>
        public bool IsFixedSize => false;

        #endregion

        #region "indexer"

        /// <summary>インテグサ。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public T this[int index]
        {
            get {
                return this.GetIndexValue(index);
            }
            set {
                this.SetIndexValue(index, value);
            }
        }

        /// <summary>インテグサ。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        object IList.this[int index]
        {
            get {
                return this.GetIndexValue(index);
            }
            set {
                if (value is T) {
                    this.SetIndexValue(index, (T)value);
                }
                else {
                    throw new InvalidCastException("格納データの型が異なります");
                }
            }
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <remarks>デフォルトのブロックサイズでインスタンスを生成する。</remarks>
        public Deck()
            : this(DEFAULT_SIZE)
        {
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="blockSize">ブロックサイズ。</param>
        /// <remarks>指定されたブロックサイズでインスタンスを生成する。</remarks>
        public Deck(int blockSize)
        {
            this.helfBlock = blockSize >> 1;
            if (this.helfBlock <= 0) {
                this.helfBlock = 1;
            }
            this.blocks = new List<Block>();
            this.blocks.Add(new Block(null, this.BlockSize));
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="collection">元としたコレクション。</param>
        public Deck(ICollection<T> collection)
            : this(collection, DEFAULT_SIZE)
        {
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="collection">元としたコレクション。</param>
        /// <param name="bucketSize">ブロックサイズ。</param>
        public Deck(ICollection<T> collection, int bucketSize)
            : this(bucketSize)
        {
            this.AddRange(collection);
        }

        #endregion

        #region "methods"

        /// <summary>指定インデックスが指すバケットを取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>バケット。</returns>
        private BlockInfo SearchBucket(int index)
        {
            int lf = 0;
            int rt = this.blocks.Count - 1;
            int md;

            // 位置の検索
            while (lf < rt) {
                md = lf + (rt - lf) / 2 + 1;

                if (this.blocks[md].Index.CompareTo(index) > 0) {
                    rt = md - 1;
                }
                else {
                    lf = md;
                }
            }

            return new BlockInfo(this.blocks[lf], lf);
        }

        /// <summary>コレクションを反復処理する列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var bck in this.blocks) {
                for (int i = 0; i < bck.count; ++i) {
                    yield return bck.items[i];
                }
            }
        }

        /// <summary>コレクションを反復処理する列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var bck in this.blocks) {
                for (int i = 0; i < bck.count; ++i) {
                    yield return bck.items[i];
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
            var last = this.blocks[this.blocks.Count - 1];

            if (last.count < last.items.Length) {
                last.items[last.count++] = item;
            }
            else {
                last = new Block(last, this.BlockSize);
                this.blocks.Add(last);
                last.items[last.count++] = item;
            }
        }

        /// <summary>要素を追加する。</summary>
        /// <param name="value">追加する要素。</param>
        /// <returns></returns>
        public int Add(object value)
        {
            if (value is T) {
                this.Add((T)value);
                return this.Count - 1;
            }
            else {
                throw new ArgumentException("item の型が、IList に割り当てることができない型です");
            }
        }

        /// <summary>要素リストを追加する。</summary>
        /// <param name="item">要素リスト。</param>
        public void AddRange(IEnumerable<T> item)
        {
            var last = this.blocks[this.blocks.Count - 1];

            var ptr = item.GetEnumerator();
            while (ptr.MoveNext()) {
                if (last.count < last.items.Length) {
                    last.items[last.count++] = ptr.Current;
                }
                else {
                    last = new Block(last, this.BlockSize);
                    this.blocks.Add(last);
                    last.items[last.count++] = ptr.Current;
                }
            }
        }

        /// <summary>要素の順序を維持した位置に要素を挿入する。</summary>
        /// <param name="item">挿入する要素。</param>
        public void AddSort(T item)
        {
            this.AddSort(item, Comparer<T>.Default);
        }

        /// <summary>要素の順序を維持した位置に要素を挿入する。</summary>
        /// <param name="item">挿入する要素。</param>
        /// <param name="compare">比較処理。</param>
        public void AddSort(T item, IComparer<T> compare)
        {
            if (this.Count > 0) {
                int lf = 0;
                int rt = this.blocks.Count - 1;
                int md;

                // 挿入位置の検索
                while (lf < rt) {
                    md = lf + (rt - lf) / 2;

                    if (md + 1 < this.blocks.Count &&
                        compare.Compare(this.blocks[md + 1].items[0], item) <= 0) {
                        lf = md + 1;
                    }
                    else {
                        rt = md;
                    }
                }

                // 要素を挿入する
                this.InnerAddSort(item, lf, compare);
            }
            else {
                // 最終位置に挿入は追加とする
                this.Add(item);
            }
        }

        /// <summary>要素を挿入する。</summary>
        /// <param name="item">挿入する要素。</param>
        /// <param name="blkIndex">対象ブロックの位置。</param>
        /// <param name="compare">比較処理。</param>
        private void InnerAddSort(T item, int blkIndex, IComparer<T> compare)
        {
            var block = this.blocks[blkIndex];
            int lf = 0;
            int rt = block.count;
            int md;

            // 挿入位置の検索
            while (lf < rt) {
                md = lf + (rt - lf) / 2;

                if (compare.Compare(block.items[md], item) <= 0) {
                    lf = md + 1;
                }
                else {
                    rt = md;
                }
            }

            // ブロックを挿入する
            if (block.count < block.items.Length) {
                this.InsertItem(block, lf, item);
            }
            else {
                this.InsertBucket(block, blkIndex, lf, item);
            }

            // インデックスを更新する
            for (int i = blkIndex + 1; i < this.blocks.Count; ++i) {
                this.blocks[i].UpdateIndex(this.blocks[i - 1]);
            }
        }

        //---------------------------------------------------------------------
        // 挿入
        //---------------------------------------------------------------------
        /// <summary>指定した位置に要素を追加する。</summary>
        /// <param name="index">指定位置。</param>
        /// <param name="item">挿入する要素。</param>
        public void Insert(int index, T item)
        {
            if (index < this.Count) {
                // 挿入位置は 0以上とする
                if (index < 0) {
                    index = 0;
                }

                // 挿入ブロックを検索して追加する
                var bck = this.SearchBucket(index);
                if (bck.block.count < bck.block.items.Length) {
                    this.InsertItem(bck.block, index - bck.block.Index, item);
                }
                else {
                    this.InsertBucket(bck.block, bck.index, index - bck.block.Index, item);
                }

                // インデックスを更新する
                for (int i = bck.index + 1; i < this.blocks.Count; ++i) {
                    this.blocks[i].UpdateIndex(this.blocks[i - 1]);
                }
            }
            else {
                // 最終位置に挿入は追加とする
                this.Add(item);
            }
        }

        /// <summary>指定位置に要素を挿入する。</summary>
        /// <param name="index">挿入位置。</param>
        /// <param name="value">挿入する値。</param>
        public void Insert(int index, object value)
        {
            if (value is T) {
                this.Insert(index, (T)value);
            }
            else {
                throw new ArgumentException("item の型が、IList に割り当てることができない型です");
            }
        }

        /// <summary>指定位置に要素リストを挿入する。</summary>
        /// <param name="index">挿入位置。</param>
        /// <param name="collection">要素リスト。</param>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index < this.Count) {
                // 挿入位置は 0以上とする
                if (index < 0) {
                    index = 0;
                }

                // 挿入ブロックを検索して追加する
                var bckinfo = this.SearchBucket(index);
                var block = bckinfo.block;
                var bcidx = bckinfo.index;
                var inidx = index - block.Index;
                foreach (var item in collection) {
                    if (block.count < block.items.Length) {
                        this.InsertItem(block, inidx++, item);
                    }
                    else {
                        var tmp = this.InsertBucket(block, bcidx++, inidx++, item);
                        inidx -= tmp.count;
                        block = tmp;
                    }
                }

                // インデックスを更新する
                for (int i = bckinfo.index + 1; i < this.blocks.Count; ++i) {
                    this.blocks[i].UpdateIndex(this.blocks[i - 1]);
                }
            }
            else {
                // 最終位置に挿入は追加とする
                this.AddRange(collection);
            }
        }

        /// <summary>ブロックに要素を挿入する。</summary>
        /// <param name="bucket">対象ブロック。</param>
        /// <param name="idx">ブロック内挿入位置。</param>
        /// <param name="item">挿入する要素。</param>
        private void InsertItem(Block bucket, int idx, T item)
        {
            Array.Copy(bucket.items, idx, bucket.items, idx + 1, bucket.items.Length - 1 - idx);
            bucket.items[idx] = item;
            bucket.count++;
        }

        /// <summary>ブロックに要素を追加し、ブロック分割を行う。。</summary>
        /// <param name="bucket">対象ブロック。</param>
        /// <param name="bckIndex">対象ブロックの位置。</param>
        /// <param name="index">挿入位置。</param>
        /// <param name="item">挿入要素。</param>
        /// <returns>追加されたブロック。</returns>
        private Block InsertBucket(Block bucket, int bckIndex, int index, T item)
        {
            // 新しいブロックを作成
            var newbck = new Block(bucket, this.BlockSize);
            this.blocks.Insert(bckIndex + 1, newbck);

            // 挿入先ブロックを選択して配置する
            //
            // 1. 前方ブロックに挿入
            // 2. 後方ブロックに挿入
            var len = (this.BlockSize + 1) >> 1;
            if (index < len) {
                var frnidx = len - 1;           // 1
                Array.Copy(bucket.items, frnidx, newbck.items, 0, bucket.count - frnidx);
                if (len > index + 1) {
                    var bckidx = index + 1;
                    Array.Copy(bucket.items, index, bucket.items, bckidx, len - bckidx);
                }
                bucket.items[index] = item;
            }
            else {
                var insidx = index - len;       // 2
                if (insidx > 0) {
                    Array.Copy(bucket.items, len, newbck.items, 0, insidx);
                }
                newbck.items[insidx] = item;
                if (bucket.count > index) {
                    Array.Copy(bucket.items, index, newbck.items, insidx + 1, bucket.count - index);
                }
            }

            // 要素数の更新
            bucket.count = len;
            newbck.count = len;

            // インデックスを更新する
            newbck.UpdateIndex(bucket);
            return newbck;
        }

        //---------------------------------------------------------------------
        // 削除
        //---------------------------------------------------------------------
        /// <summary>コレクションから全ての項目を削除します。</summary>
        public void Clear()
        {
            this.blocks.Clear();
            this.blocks.Add(new Block(null, this.BlockSize));
        }

        /// <summary>指定された要素を削除する。</summary>
        /// <param name="value">削除する要素。</param>
        public void Remove(object value)
        {
            if (value is T) {
                this.Remove((T)value);
            }
            else {
                throw new ArgumentException("item の型が、IList に割り当てることができない型です");
            }
        }

        /// <summary>指定された要素を削除する。</summary>
        /// <param name="item">削除する要素。</param>
        /// <returns>要素が削除されたならば真。</returns>
        public bool Remove(T item)
        {
            for (int j = 0; j < this.blocks.Count; ++j) {
                var bck = this.blocks[j];
                for (int i = 0; i < bck.count; ++i) {
                    if (item.Equals(bck.items[i])) {
                        this.RemoveItem(bck, j, i);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>指定位置の要素を削除する。</summary>
        /// <param name="index">削除する要素の位置。</param>
        public void RemoveAt(int index)
        {
            var bck = this.SearchBucket(index);
            this.RemoveItem(bck.block, bck.index, index - bck.block.Index);
        }

        /// <summary>範囲を指定して要素を削除する。</summary>
        /// <param name="index">削除範囲開始位置。</param>
        /// <param name="count">削除範囲要素数。</param>
        public void RemoveRange(int index, int count)
        {
            for (int i = index + count - 1; i >= index; --i) {
                this.RemoveAt(i);
            }
        }

        /// <summary>要素を削除する（内部処理用）</summary>
        /// <param name="bck">対象ブロック。</param>
        /// <param name="bckIndex">対象ブロックの位置。</param>
        /// <param name="idx">ブロック内のインデックス。</param>
        private void RemoveItem(Block bck, int bckIndex, int idx)
        {
            // 要素を移動する
            Array.Copy(bck.items, idx + 1, bck.items, idx, bck.count - 1 - idx);
            bck.count--;

            // 閾値をこえたらブロックのマージを行う
            if (bck.count <= this.helfBlock) {
                if (bckIndex > 0 &&
                    this.blocks[bckIndex - 1].count + bck.count <= this.BlockSize) {
                    var other = this.blocks[bckIndex - 1];
                    Array.Copy(bck.items, 0, other.items, other.count, bck.count);
                    other.count += bck.count;
                    this.blocks.RemoveAt(bckIndex);
                }
                else if (bckIndex < this.blocks.Count - 1 &&
                         this.blocks[bckIndex + 1].count + bck.count <= this.BlockSize) {
                    var other = this.blocks[bckIndex + 1];
                    Array.Copy(other.items, 0, bck.items, bck.count, other.count);
                    bck.count += other.count;
                    this.blocks.RemoveAt(bckIndex + 1);
                }
                else if (bckIndex > 0) {
                    this.SplitBuckets(this.blocks[bckIndex - 1], bck);
                }
                else if (bckIndex < this.blocks.Count - 1) {
                    this.SplitBuckets(bck, this.blocks[bckIndex + 1]);
                }
            }

            // インデックスを張り替える
            for (int i = bckIndex; i < this.blocks.Count; ++i) {
                this.blocks[i].UpdateIndex(i > 0 ? this.blocks[i - 1] : null);
            }
        }

        /// <summary>指定式で定義される条件に一致するすべての要素を削除する。</summary>
        /// <param name="match">判定式。</param>
        /// <returns>削除された要素数。</returns>
        public int RemoveAll(Predicate<T> match)
        {
            int res = 0;
            for (int j = this.blocks.Count - 1; j >= 0; --j) {
                var bck = this.blocks[j];
                for (int i = bck.count - 1; i >= 0; --i) {
                    if (match(bck.items[i])) {
                        this.RemoveItem(bck, j, i);
                        res++;
                    }
                }
            }
            return res;
        }

        /// <summary>ブロック間のバランスを取る。</summary>
        /// <param name="prev">前ブロック。</param>
        /// <param name="next">後ブロック。</param>
        private void SplitBuckets(Block prev, Block next)
        {
            if (prev.count < next.count) {
                // 後ろの要素を前のブロックに追加
                var len = (next.count - prev.count) / 2;
                Array.Copy(next.items, 0, prev.items, prev.count, len);
                Array.Copy(next.items, len, next.items, 0, next.count - len);
                prev.count += len;
                next.count -= len;
            }
            else {
                // 前の要素を後のブロックに追加。
                var len = (prev.count - next.count) / 2;
                Array.Copy(next.items, 0, next.items, len, next.count);
                Array.Copy(prev.items, prev.count - len, next.items, 0, len);
                prev.count -= len;
                next.count += len;
            }
        }

        //---------------------------------------------------------------------
        // インデクサ機能
        //---------------------------------------------------------------------
        /// <summary>指定インデックスの要素を取得する。</summary>
        /// <param name="index">指定インデックス。</param>
        /// <returns>取得した要素。</returns>
        private T GetIndexValue(int index)
        {
            if (index >= 0 && index < this.Count) {
                var bck = this.SearchBucket(index);
                return bck.block.items[index - bck.block.Index];
            }
            else {
                throw new IndexOutOfRangeException("添え字が範囲を超えている");
            }
        }

        /// <summary>指定インデックスに要素を設定する。</summary>
        /// <param name="index">指定インデックス。</param>
        /// <param name="value">設定する要素。</param>
        private void SetIndexValue(int index, T value)
        {
            if (index >= 0 && index < this.Count) {
                var bck = this.SearchBucket(index);
                bck.block.items[index - bck.block.Index] = value;
            }
            else {
                throw new IndexOutOfRangeException("添え字が範囲を超えている");
            }
        }

        /// <summary>指定範囲の要素を取得する。</summary>
        /// <param name="index">範囲の開始位置。</param>
        /// <param name="count">範囲の要素数。</param>
        /// <returns>範囲内の要素リスト。</returns>
        public List<T> GetRange(int index, int count)
        {
            var res = new List<T>();

            // データ取得開始位置を取得
            var bck = this.SearchBucket(index);
            var j = bck.index;
            var i = index - bck.block.Index;

            // データをコピー
            var c = 0;
            while (c < this.Count && c < count) {
                if (i >= this.blocks[j].count) {
                    j++;
                    i = 0;
                }
                res.Add(this.blocks[j].items[i++]);
                c++;
            }

            return res;
        }

        //---------------------------------------------------------------------
        // コピー機能
        //---------------------------------------------------------------------
        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="array">コピー先配列。</param>
        /// <param name="index">コピー先配列の書き込み開始位置。</param>
        public void CopyTo(Array array, int index)
        {
            int j = 0, i = 0, c = 0;
            while (c < this.Count) {
                if (i >= this.blocks[j].count) {
                    j++;
                    i = 0;
                }
                array.SetValue(this.blocks[j].items[i++], index++);
                c++;
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
            int j = 0, i = 0, c = 0;
            while (c < this.Count) {
                if (i >= this.blocks[j].count) {
                    j++;
                    i = 0;
                }
                array[index++] = this.blocks[j].items[i++];
                c++;
            }
        }

        /// <summary>配列にデータをコピーする。</summary>
        /// <param name="index">コピー元開始位置。</param>
        /// <param name="array">コピー先配列。</param>
        /// <param name="arrayIndex">コピー先配列の書き込み開始位置。</param>
        /// <param name="count">コピーする数。</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            // データ取得開始位置を取得
            var bck = this.SearchBucket(index);
            var j = bck.index;
            var i = index - bck.block.Index;

            // データをコピー
            var c = 0;
            while (c < this.Count && c < count) {
                if (i >= this.blocks[j].count) {
                    j++;
                    i = 0;
                }
                array[arrayIndex++] = this.blocks[j].items[i++];
                c++;
            }
        }

        //---------------------------------------------------------------------
        // ソート機能
        //---------------------------------------------------------------------
        /// <summary>標準の比較メソッドにてソートを行う。</summary>
        public void Sort()
        {
            var tmp = this.ToArray();
            Array.Sort(tmp);
            this.FromArray(tmp);
        }

        /// <summary>比較式を指定して、ソートを行う。</summary>
        /// <param name="comparison">比較式。</param>
        public void Sort(Comparison<T> comparison)
        {
            var tmp = this.ToArray();
            Array.Sort(tmp, comparison);
            this.FromArray(tmp);
        }

        /// <summary>比較クラスを指定して、ソートを行う。</summary>
        /// <param name="comparer">比較クラス。</param>
        public void Sort(IComparer<T> comparer)
        {
            var tmp = this.ToArray();
            Array.Sort(tmp, comparer);
            this.FromArray(tmp);
        }

        /// <summary>ソート範囲を指定して、ソートを行う。</summary>
        /// <param name="index">ソート開始位置。</param>
        /// <param name="count">ソート要素数。</param>
        /// <param name="comparer">比較式。</param>
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            var tmp = this.ToArray();
            Array.Sort(tmp, index, count, comparer);
            this.FromArray(tmp);
        }

        /// <summary>コレクションを配列に変換する。</summary>
        /// <returns>配列。</returns>
        public T[] ToArray()
        {
            var tmp = new T[this.Count];
            for (int i = 0, c = 0; i < this.blocks.Count; ++i) {
                for (int j = 0; j < this.blocks[i].count; ++j) {
                    tmp[c++] = this.blocks[i].items[j];
                }
            }
            return tmp;
        }

        /// <summary>配列の値をインスタンスに展開する。</summary>
        /// <param name="tmp">元となる配列。</param>
        private void FromArray(T[] tmp)
        {
            for (int i = 0, c = 0; i < this.blocks.Count; ++i) {
                for (int j = 0; j < this.blocks[i].count; ++j) {
                    this.blocks[i].items[j] = tmp[c++];
                }
            }
        }

        //---------------------------------------------------------------------
        // 反転
        //---------------------------------------------------------------------
        /// <summary>全体の要素の逆転を行う。</summary>
        public void Reverse()
        {
            int si = 0, sj = 0;
            int ei = this.blocks[this.blocks.Count - 1].count - 1, ej = this.blocks.Count - 1;
            this.InnerReverse(si, sj, ei, ej);
        }

        /// <summary>指定範囲の要素の逆転を行う。</summary>
        /// <param name="index">範囲の開始位置。</param>
        /// <param name="count">範囲の要素数。</param>
        public void Reverse(int index, int count)
        {
            var st = this.SearchBucket(index);
            int si = index - st.block.Index, sj = st.index;

            var et = this.SearchBucket(index + count);
            int ei = (index + count) - et.block.Index, ej = et.index;

            this.InnerReverse(si, sj, ei, ej);
        }

        /// <summary>要素の入れ替えを行う。</summary>
        /// <param name="si">開始ブロックの要素位置。</param>
        /// <param name="sj">開始ブロック位置。</param>
        /// <param name="ei">終了ブロックの要素位置。</param>
        /// <param name="ej">終了ブロック位置。</param>
        private void InnerReverse(int si, int sj, int ei, int ej)
        {
            while (sj < ej || (sj == ej && si < ei)) {
                // 値を入れ替える
                var tmp = this.blocks[sj].items[si];
                this.blocks[sj].items[si] = this.blocks[ej].items[ei];
                this.blocks[ej].items[ei] = tmp;

                // 範囲を狭める
                si++;
                if (si >= this.blocks[sj].count) {
                    si = 0;
                    sj++;
                }

                ei--;
                if (ei < 0) {
                    ej--;
                    ei = this.blocks[ej].count - 1;
                }
            }
        }

        //---------------------------------------------------------------------
        // 便利機能
        //---------------------------------------------------------------------
        /// <summary>コレクション内の全ての要素が式で定義される条件に一致するか調べる。</summary>
        /// <param name="match">条件式。</param>
        /// <returns>全て一致したら真。</returns>
        public bool TrueForAll(Predicate<T> match)
        {
            for (int j = 0; j < this.blocks.Count; ++j) {
                var bck = this.blocks[j];
                for (int i = 0; i < bck.count; ++i) {
                    if (!match(bck.items[i])) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>コレクションの各要素に指定された式を適用する。</summary>
        /// <param name="action">実行する式。</param>
        public void ForEach(Action<T> action)
        {
            for (int j = 0; j < this.blocks.Count; ++j) {
                var bck = this.blocks[j];
                for (int i = 0; i < bck.count; ++i) {
                    action(bck.items[i]);
                }
            }
        }

        //---------------------------------------------------------------------
        // 追加
        //---------------------------------------------------------------------

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {


            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        //        int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        //        int BinarySearch(T item);
        //        int BinarySearch(T item, IComparer<T> comparer);
        //        bool Contains(T item);
        //        List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter);
        //        bool Exists(Predicate<T> match);
        //        T Find(Predicate<T> match);
        //        List<T> FindAll(Predicate<T> match);
        //        int FindIndex(Predicate<T> match);
        //        int FindIndex(int startIndex, Predicate<T> match);
        //        int FindIndex(int startIndex, int count, Predicate<T> match);
        //        T FindLast(Predicate<T> match);
        //        int FindLastIndex(Predicate<T> match);
        //        int FindLastIndex(int startIndex, Predicate<T> match);
        //        int FindLastIndex(int startIndex, int count, Predicate<T> match);
        //        int IndexOf(T item, int index, int count);
        //        int IndexOf(T item, int index);
        //        int IndexOf(T item);
        //        int LastIndexOf(T item);
        //        int LastIndexOf(T item, int index);
        //        int LastIndexOf(T item, int index, int count);
        //        void TrimExcess();

        #endregion
    }
}
