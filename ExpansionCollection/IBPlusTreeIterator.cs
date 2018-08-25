using System.Collections.Generic;

namespace ExpansionCollection
{
    /// <summary>前後要素にシーケンシャルアクセスするためのインターフェイス。</summary>
    /// <typeparam name="T">対象型情報。</typeparam>
    /// <remarks>
    /// IEnumeratorを継承し、前方向へ遡る MovePreviwを追加したインターフェイス。
    /// </remarks>
    public interface IBPlusTreeIterator<T>
        : IEnumerator<T>
    {
        #region "methods"

        /// <summary>現在要素の位置を取得する。</summary>
        int CurrentIndex { get; }

        /// <summary>列挙子を前の要素へ進める。</summary>
        /// <returns>進める要素があれば真。</returns>
        bool MovePreviw();

        #endregion
    }
}
