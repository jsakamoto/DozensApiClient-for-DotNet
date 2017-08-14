using System;
using System.Text;
using System.Net.Http;

namespace DozensAPI
{
    /// <summary>
    /// System.Net.WebClient をラップした、IAPIEndPoint の実装です。
    /// </summary>
    internal class DefaultAPIEndPoint : IAPIEndPoint
    {
        protected HttpClient HttpClient { get; set; }

        public DefaultAPIEndPoint()
        {
            this.HttpClient = new HttpClient();
            this.Headers = new WebHeaderCollection();
        }

        #region IAPIEndPoint メンバー

        /// <summary>
        /// 指定したメソッドを使用して、指定したリソースに指定した文字列をアップロードします。
        /// </summary>
        /// <param name="address">ファイルを受信するリソースの URI。この URI は、method パラメーターに指定されたメソッドを使用して送信される要求を受け入れることができるリソースを識別するものであることが必要です。</param>
        /// <param name="method">リソースに文字列を送信するために使用する HTTP メソッド。null の場合の既定値は POST です。</param>
        /// <param name="data">アップロードする文字列。</param>
        /// <returns>
        /// サーバーが送信した応答を格納している System.String。
        /// </returns>
        public string UploadString(string address, string method, string data)
        {
            var request = new HttpRequestMessage(new HttpMethod(method ?? "POST"), address);

            var contentType = this.Headers.TryGetValue(HttpRequestHeader.ContentType, out var value) ? value : "text/plain";
            request.Content = new StringContent(data);
            request.Content.Headers.Clear();
            request.Content.Headers.Add("Content-Type", contentType);

            SetupDefaultRequestHeaders();
            var res = this.HttpClient.SendAsync(request).Result;
            return res.Content.ReadAsStringAsync().Result;
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
            var request = new HttpRequestMessage(HttpMethod.Get, address);
            SetupDefaultRequestHeaders();
            var res = this.HttpClient.SendAsync(request).Result;
            return res.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// 要求に関連付けられているヘッダーの名前/値ペアのコレクションを取得します。
        /// </summary>
        public WebHeaderCollection Headers { get; protected set; }

        #endregion

        private void SetupDefaultRequestHeaders()
        {
            var defaultRequestHeaders = this.HttpClient.DefaultRequestHeaders;
            defaultRequestHeaders.Clear();
            foreach (var header in this.Headers)
            {
                if (header.Key == HttpRequestHeader.ContentType) continue;
                defaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
