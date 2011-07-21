using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace DozensAPI
{
    public class Dozens
    {
        private JavaScriptSerializer _Serializer;

        private IAPIEndPoint _APIEndPoint;

        private string _DozensUserId;
        
        private string _APIKey;
        
        public string Token { get; set; }

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
            this._APIEndPoint = new DefaultAPIEndPoint();
        }

        [DebuggerDisplay("{auth_token}")]
        internal class AuthResult
        {
            public string auth_token { get; set; }
        }

        private void Auth()
        {
            lock (this._APIEndPoint)
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
            lock (this._APIEndPoint)
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

        protected T CallAPI<T>(string actionName, object target = null, object param = null, string verb = null)
        {
            lock (this._APIEndPoint)
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
                        var errResult = string.IsNullOrWhiteSpace(body) ?
                            new ErrorResult() :
                            this._Serializer.Deserialize<ErrorResult>(body);
                        throw new DozensException(errResult.Code, errResult.Message, e);
                    }
                }
            }
        }

        private IAPIEndPoint GetAPIEndPoint()
        {
            var client = this._APIEndPoint;
            var baseUrl = new Uri(this.BaseURL);
            client.Headers.Clear();
            client.Headers.Add(HttpRequestHeader.Host, baseUrl.Host);
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

        public DozensZone[] GetZones()
        {
            var result = CallAPI<ZoneResult>("zone");
            return result.domain;
        }

        public DozensZone[] CreateZone(string zoneName, bool addGoogleApps = false, string googleAuthorize = null)
        {
            var param = new { 
                name = zoneName,
                add_google_apps = addGoogleApps,
                google_authorize = googleAuthorize ?? ""
            };
            return CallAPI<ZoneResult>("zone/create", param: param).domain;
        }

        public DozensZone[] DeleteZone(int zoneId)
        {
            return CallAPI<ZoneResult>("zone/delete", zoneId, verb: "DELETE").domain;
        }

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

        public DozensRecord[] GetRecords(string zoneName)
        {
            var result = CallAPI<RecoredResult>("record", zoneName);
            return result.record;
        }

        public DozensRecord[] UpdateRecord(int recordId, int? prio, string content, int ttl = 7200)
        {
            var result = CallAPI<RecoredResult>(
                "record/update", 
                recordId,
                new { prio = prio.ToString(), content, ttl });
            return result.record;
        }

        public DozensRecord[] UpdateRecord(string zoneName, string name, int? prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(zoneName, name);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        public DozensRecord[] UpdateRecord(string fqdn, int? prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(fqdn);
            return UpdateRecord(recordId, prio, content, ttl);
        }

        public DozensRecord[] CreateRecord(string zoneName, string name, string type, int? prio, string content, int ttl = 7200)
        {
            var result = CallAPI<RecoredResult>(
                "record/create",
                param: new { 
                    domain = zoneName, 
                    name, 
                    type, 
                    prio = prio.ToString(), 
                    content, 
                    ttl });
            return result.record;
        }

        public DozensRecord[] DeleteRecord(int recordId)
        {
            var result = CallAPI<RecoredResult>("record/delete", recordId, verb: "DELETE");
            return result.record;
        }

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
