using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DozensAPI
{
    /// <summary>
    /// Dozens の REST API を呼び出して、ゾーンやレコードの追加・取得・変更・削除を行います。
    /// </summary>
    [ProgId("DozensAPI.Dozens")]
    public class Dozens
    {
        private JavaScriptSerializer _Serializer;

        protected IAPIEndPoint APIEndPoint { get; set; }

        private string _DozensUserId;

        private string _APIKey;

        /// <summary>
        /// Dozens REST API を呼び出すときに使う、認証後のトークンを取得または設定します。
        /// 通常は設定する必要はありません。
        /// コンストラクタまたは Auth メソッドに指定された Dozens ID と API KEY によって自動的に認証され、その結果が設定されます。
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Dozens REST API の URL を取得または設定します。既定では "http://dozens.jp/api" に初期化されます。
        /// </summary>
        public string BaseURL { get; set; }

        /// <summary>
        /// Dozens アカウントの ID と API KEY を指定して、<see cref="Dozens"/> クラスの新しいインスタンスを初期化します。
        /// (ゾーンやレコードの追加/取得/変更/削除を行う前の、明示的な Auth メソッド呼び出しは不要です。引数に渡されたIDとAPIKEYで必要に応じて自動的に認証を行います。)
        /// </summary>
        /// <param name="dozensUserId">Dozensに開設したアカウントのIDを指定します。</param>
        /// <param name="apiKey">そのアカウントの API KEY を指定します。API KEY は Doznes の Web サイトにログインして、プロフィールのページ(https://dozens.jp/profile)から入手できます。</param>
        public Dozens(string dozensUserId, string apiKey)
        {
            Initialize();
            this._DozensUserId = dozensUserId;
            this._APIKey = apiKey;
        }

        /// <summary>
        /// <see cref="Dozens"/> クラスの新しいインスタンスを初期化します。
        /// (ゾーンやレコードの追加/取得/変更/削除を行う前に、Auth メソッドによる認証が必要です。)
        /// </summary>
        public Dozens()
        {
            Initialize();
        }

        private void Initialize()
        {
            this.BaseURL = "http://dozens.jp/api";
            this._Serializer = new JavaScriptSerializer();
            this.APIEndPoint = new DefaultAPIEndPoint();
        }

        [DebuggerDisplay("{auth_token}")]
        internal class AuthResult
        {
            public string auth_token { get; set; }
        }

        protected void Auth()
        {
            lock (this.APIEndPoint)
            {
                this.Token = null;

                var apiEndPoint = GetAPIEndPoint();
                var resultJson = apiEndPoint.DownloadString(this.BaseURL + "/authorize.json");
                var result = this._Serializer.Deserialize<AuthResult>(resultJson);

                this.Token = result.auth_token;
            }
        }

        /// <summary>
        /// Dozens アカウントの ID と API KEY を指定して、認証を行います。
        /// 成功すると、Token プロパティに認証結果のトークンが格納され、以後の操作
        /// (ゾーンやレコードの追加/取得/変更/削除) で使われます。
        /// </summary>
        /// <param name="dozensUserId">Dozensに開設したアカウントのIDを指定します。</param>
        /// <param name="apiKey">そのアカウントの API KEY を指定します。API KEY は Doznes の Web サイトにログインして、プロフィールのページ(https://dozens.jp/profile)から入手できます。</param>
        public void Auth(string dozensUserId, string apiKey)
        {
            lock (this.APIEndPoint)
            {
                this._DozensUserId = dozensUserId;
                this._APIKey = apiKey;
                this.Auth();
            }
        }

        internal class ErrorResult
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// APIエンドポイントを使って、Dozens REST API の呼び出しを実行します。
        /// </summary>
        protected T CallAPI<T>(string actionName, object target = null, object param = null, string verb = null)
        {
            lock (this.APIEndPoint)
            {
                if (string.IsNullOrEmpty(this.Token)) Auth();

                var url = this.BaseURL + "/" + actionName +
                    (target != null ? "/" + target.ToString() : "") +
                    ".json";

                var paramJson = (param != null) ? _Serializer.Serialize(param) : null;

                var apiEndPoint = GetAPIEndPoint();
                try
                {
                    var resultJson = (param != null || verb != null) ?
                        apiEndPoint.UploadString(url, verb ?? "POST", paramJson ?? "") :
                        apiEndPoint.DownloadString(url);

                    var result = this._Serializer.Deserialize<T>(resultJson);
                    return result;
                }
                catch (WebException e)
                {
                    using (var responseBody = new StreamReader(e.Response.GetResponseStream()))
                    {
                        var body = responseBody.ReadToEnd();
                        var errResult = string.IsNullOrEmpty(body) ?
                            new ErrorResult() :
                            this._Serializer.Deserialize<ErrorResult>(body);
                        throw new DozensException(errResult.Code, errResult.Message, e);
                    }
                }
            }
        }

        private IAPIEndPoint GetAPIEndPoint()
        {
            var client = this.APIEndPoint;
            var baseUrl = new Uri(this.BaseURL);
            client.Headers.Clear();
            client.Headers.Add(HttpRequestHeader.Accept, "application/json");
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            if (string.IsNullOrEmpty(this.Token))
            {
                client.Headers["X-Auth-User"] = this._DozensUserId;
                client.Headers["X-Auth-Key"] = this._APIKey;
            }
            else
            {
                client.Headers["X-Auth-Token"] = this.Token;
            }

            return client;
        }

        internal class ZoneResult
        {
            public DozensZone[] domain { get; set; }
        }

        /// <summary>
        /// ゾーン(ドメイン)リストを取得します。
        /// </summary>
        /// <returns>登録されているすべてのゾーンの配列</returns>
        public DozensZone[] GetZones()
        {
            var result = CallAPI<ZoneResult>("zone");
            return result.domain;
        }

        /// <summary>
        /// ゾーンを新規登録します。
        /// </summary>
        /// <param name="zoneName">ゾーンの名前です。(ex."dozens.jp")</param>
        /// <param name="addGoogleApps">GoogleApps のレコードを追加する場合は <c>true</c> を指定します。省略時は false です。</param>
        /// <param name="googleAuthorize">CNAME によって GoogleApps の確認をする場合に設定します。省略時は null です。</param>
        /// <returns>登録されているすべてのゾーンの配列</returns>
        public DozensZone[] CreateZone(string zoneName, bool addGoogleApps = false, string googleAuthorize = null)
        {
            var param = new
            {
                name = zoneName,
                add_google_apps = addGoogleApps,
                google_authorize = googleAuthorize ?? ""
            };
            return CallAPI<ZoneResult>("zone/create", param: param).domain;
        }

        /// <summary>
        /// 引数に指定したゾーンIDのゾーンを削除します。
        /// </summary>
        /// <param name="zoneId">削除対象のゾーンを一意に識別するゾーンID</param>
        /// <returns>登録されているすべてのゾーンの配列</returns>
        public DozensZone[] DeleteZone(int zoneId)
        {
            return CallAPI<ZoneResult>("zone/delete", zoneId, verb: "DELETE").domain;
        }

        /// <summary>
        /// 引数に指定したゾーン名のゾーンを削除します。
        /// </summary>
        /// <param name="zoneName">削除する対象のゾーンの、ゾーン名(ex."dozens.jp")</param>
        /// <returns>登録されているすべてのゾーンの配列</returns>
        public DozensZone[] DeleteZone(string zoneName)
        {
            var query = from zone in GetZones()
                        where zone.Name == zoneName
                        select zone.Id;
            var zoneId = query.Single();
            return CallAPI<ZoneResult>("zone/delete", zoneId, verb: "DELETE").domain;
        }

        internal class RecoredResult
        {
            public DozensRecord[] record { get; set; }
        }

        /// <summary>
        /// 引数に指定したゾーン名のゾーンに登録されているレコードリストを取得します。
        /// </summary>
        /// <param name="zoneName">ゾーンの名前(ex."dozens.jp")</param>
        /// <returns>指定のゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] GetRecords(string zoneName)
        {
            var result = CallAPI<RecoredResult>("record", zoneName);
            return result.record;
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="recordId">変更(更新)する対象のレコードを一意に識別するレコードID</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(int recordId, int prio, string content, int ttl = 7200)
        {
            return UpdateRecord(recordId, (object)prio, content, ttl);
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="recordId">変更(更新)する対象のレコードを一意に識別するレコードID</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(int recordId, object prio, string content, int ttl = 7200)
        {
            if (prio != null && Regex.IsMatch(prio.ToString(), @"^\d{0,7}$") == false)
                throw new ArgumentException("prio には、null、または整数値のみが指定できます。");

            var result = CallAPI<RecoredResult>(
                "record/update",
                recordId,
                new { prio = (prio ?? "").ToString(), content, ttl });
            return result.record;
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="zoneName">変更(更新)対象のレコードを含んでいるゾーンの名前(ex."dozens.jp")</param>
        /// <param name="name">変更(更新)対象のレコードの名前(ex."www", "www.dozens.jp")</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(string zoneName, string name, int prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(zoneName, name);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="zoneName">変更(更新)対象のレコードを含んでいるゾーンの名前(ex."dozens.jp")</param>
        /// <param name="name">変更(更新)対象のレコードの名前(ex."www", "www.dozens.jp")</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(string zoneName, string name, object prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(zoneName, name);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="fqdn">変更(更新)対象のレコードを特定できるFQDN(ex."www.dozens.jp")</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(string fqdn, int prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(fqdn);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        /// <summary>
        /// 指定したレコードの登録内容を変更(更新)します。
        /// </summary>
        /// <param name="fqdn">変更(更新)対象のレコードを特定できるFQDN(ex."www.dozens.jp")</param>
        /// <param name="prio">変更後のプライオリティ</param>
        /// <param name="content">変更後の値</param>
        /// <param name="ttl">変更後のTTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] UpdateRecord(string fqdn, object prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(fqdn);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        /// <summary>
        /// 指定のゾーンにレコードを新規登録します。
        /// </summary>
        /// <param name="zoneName">レコードを新規登録する対象のゾーン名(ex."dozens.jp")</param>
        /// <param name="name">新規登録するレコードの名前(ex."www","www.dozens.jp")</param>
        /// <param name="type">新規登録するレコードのタイプ(ex."A","CNAME","MX","TXT")</param>
        /// <param name="prio">新規登録するレコードのプライオリティ。</param>
        /// <param name="content">新規登録するレコードの値</param>
        /// <param name="ttl">新規登録するレコードの TTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] CreateRecord(string zoneName, string name, string type, int prio, string content, int ttl = 7200)
        {
            return CreateRecord(zoneName, name, type, (object)prio, content, ttl);
        }

        /// <summary>
        /// 指定のゾーンにレコードを新規登録します。
        /// </summary>
        /// <param name="zoneName">レコードを新規登録する対象のゾーン名(ex."dozens.jp")</param>
        /// <param name="name">新規登録するレコードの名前(ex."www","www.dozens.jp")</param>
        /// <param name="type">新規登録するレコードのタイプ(ex."A","CNAME","MX","TXT")</param>
        /// <param name="prio">新規登録するレコードのプライオリティ。null を指定できます。</param>
        /// <param name="content">新規登録するレコードの値</param>
        /// <param name="ttl">新規登録するレコードの TTL。省略時は 7200 です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] CreateRecord(string zoneName, string name, string type, object prio, string content, int ttl = 7200)
        {
            if (prio != null && Regex.IsMatch(prio.ToString(), @"^\d{0,7}$") == false)
                throw new ArgumentException("prio には、null、または整数値のみが指定できます。");

            var result = CallAPI<RecoredResult>(
                "record/create",
                param: new
                {
                    domain = zoneName,
                    name,
                    type,
                    prio = (prio ?? "").ToString(),
                    content,
                    ttl
                });
            return result.record;
        }

        /// <summary>
        /// 引数に指定したレコードIDのレコードを削除します。
        /// </summary>
        /// <param name="recordId">削除対象のレコードを一意に識別するレコードID</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] DeleteRecord(int recordId)
        {
            var result = CallAPI<RecoredResult>("record/delete", recordId, verb: "DELETE");
            return result.record;
        }

        /// <summary>
        /// 指定したゾーン名、レコード名のレコードを削除します。
        /// </summary>
        /// <param name="zoneOrFQDName">削除対象のレコードを含むゾーンの名前(ex."dozens.jp")。あるいは、削除対象のレコードを特定する FQDN (ex."www.dozens.jp")。</param>
        /// <param name="name">削除対象のレコード名(ex."www","www.dozens.jp")。<paramref name="zoneOrFQDName"/> に FQDN(ex."www.dozens.jp") を指定した場合は、null を指定してください。省略時は null です。</param>
        /// <returns>指定のレコードと同じゾーンに登録されているすべてのレコードの配列</returns>
        public DozensRecord[] DeleteRecord(string zoneOrFQDName, string name = null)
        {
            var recordId = RetrieveRecordId(zoneOrFQDName, name);
            return DeleteRecord(recordId);
        }

        private int RetrieveRecordId(string zoneOrFQDName, string name = null)
        {
            var zoneName = name != null ? zoneOrFQDName :
                GetZones()
                .Where(zone => zoneOrFQDName.EndsWith(zone.Name))
                .OrderByDescending(zone => zone.Name.Length)
                .First().Name;

            name = name ?? zoneOrFQDName;

            var query = from record in GetRecords(zoneName)
                        where record.Name == name || record.Name == (name + "." + zoneName)
                        select record.Id;
            var recordId = query.Single();
            return recordId;
        }

    }
}
