using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DozensAPI
{
    /// <summary>
    /// Dozens REST API へのアクセス手段を表すインターフェースです。
    /// </summary>
    public interface IAPIEndPoint
    {
        /// <summary>
        /// 指定したメソッドを使用して、指定したリソースに指定した文字列をアップロードします。
        /// </summary>
        /// <param name="address">ファイルを受信するリソースの URI。この URI は、method パラメーターに指定されたメソッドを使用して送信される要求を受け入れることができるリソースを識別するものであることが必要です。</param>
        /// <param name="method">リソースに文字列を送信するために使用する HTTP メソッド。null の場合、http の既定値は POST、ftp の既定値は STOR です。</param>
        /// <param name="data">アップロードする文字列。</param>
        /// <returns>サーバーが送信した応答を格納している System.String。</returns>
        string UploadString(string address, string method, string data);

        /// <summary>
        /// 要求されたリソースを System.String としてダウンロードします。ダウンロードするリソースは、URI を含む System.String として指定します。
        /// </summary>
        /// <param name="address">ダウンロードする URI を格納している System.String。</param>
        /// <returns>要求されたリソースを格納する System.String。</returns>
        string DownloadString(string address);

        /// <summary>
        /// 要求に関連付けられているヘッダーの名前/値ペアのコレクションを取得します。
        /// </summary>
        WebHeaderCollection Headers { get; }
    }
}
