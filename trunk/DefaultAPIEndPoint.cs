using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DozensAPI
{
    /// <summary>
    /// System.Net.WebClient をラップした、IAPIEndPoint の実装です。
    /// </summary>
    internal class DefaultAPIEndPoint : IAPIEndPoint
    {
        protected WebClient _WebClient;

        public DefaultAPIEndPoint()
        {
            this._WebClient = new WebClient();
        }

        #region IAPIEndPoint メンバー

        /// <summary>
        /// 指定したメソッドを使用して、指定したリソースに指定した文字列をアップロードします。
        /// </summary>
        /// <param name="address">ファイルを受信するリソースの URI。この URI は、method パラメーターに指定されたメソッドを使用して送信される要求を受け入れることができるリソースを識別するものであることが必要です。</param>
        /// <param name="method">リソースに文字列を送信するために使用する HTTP メソッド。null の場合、http の既定値は POST、ftp の既定値は STOR です。</param>
        /// <param name="data">アップロードする文字列。</param>
        /// <returns>
        /// サーバーが送信した応答を格納している System.String。
        /// </returns>
        public string UploadString(string address, string method, string data)
        {
            return this._WebClient.UploadString(address, method, data);
        }

        /// <summary>
        /// 要求されたリソースを System.String としてダウンロードします。ダウンロードするリソースは、URI を含む System.String として指定します。
        /// </summary>
        /// <param name="address">ダウンロードする URI を格納している System.String。</param>
        /// <returns>
        /// 要求されたリソースを格納する System.String。
        /// </returns>
        public string DownloadString(string address)
        {
            return this._WebClient.DownloadString(address);
        }

        /// <summary>
        /// 要求に関連付けられているヘッダーの名前/値ペアのコレクションを取得します。
        /// </summary>
        public WebHeaderCollection Headers
        {
            get { return this._WebClient.Headers; }
        }

        #endregion
    }
}
