using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DozensAPI
{
    /// <summary>
    /// Dozens REST API からエラーが返された場合に発生する例外です。
    /// </summary>
    public class DozensException : Exception
    {
        /// <summary>Dozens REST API から返却されたエラーコード値を取得します。</summary>
        public int Code { get; protected set; }

        /// <summary>
        /// Dozens REST API から返されたコードとメッセージで <see cref="DozensException"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="code">Dozens REST API から返されたエラーコード</param>
        /// <param name="message">Dozens REST API から返されたエラーメッセージ</param>
        /// <param name="innerException">現在の例外の原因である例外。内部例外が指定されていない場合は、null 参照 (Visual Basic の場合は Nothing)。</param>
        public DozensException(int code, string message, Exception innerException)
            : base(string.Format("{1}({0})", code, message), innerException)
        {
            this.Code = code;
        }
    }
}
