using System.Text;

namespace ExpansionCollection
{
    /// <summary>独自 B+木コレクション実装。</summary>
    /// <typeparam name="T">対象の型。</typeparam>
    public partial class BPlusTree<T>
    {
        /// <summary>木要素インターフェース。</summary>
        private interface IBParts
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
            void BalanceParts(IBParts other);

            /// <summary>指定要素の項目を取り込む。</summary>
            /// <param name="other">取り込む要素。</param>
            void MargeParts(IBParts other);

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

            /// <summary>指定要素以上となるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            SearchResult SearchOfGe(T item, BPlusTree<T> parent);

            /// <summary>指定要素以下となるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            SearchResult SearchOfLe(T item, BPlusTree<T> parent);

            /// <summary>指定要素をこえるインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            SearchResult SearchOfGt(T item, BPlusTree<T> parent);

            /// <summary>指定要素未満のインデックスを検索する。</summary>
            /// <param name="item">検索項目。</param>
            /// <param name="parent">B+木コレクション。</param>
            /// <returns>検索結果。</returns>
            SearchResult SearchOfLt(T item, BPlusTree<T> parent);

            /// <summary>文字列表現（木形式）を取得する。</summary>
            /// <param name="builder">文字列バッファ。</param>
            /// <param name="nest">ネスト文字列。</param>
            void ConvertTxetTree(StringBuilder builder, string nest);
        }
    }
}
