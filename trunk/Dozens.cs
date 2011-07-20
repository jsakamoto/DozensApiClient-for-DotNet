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
        /// <see cref="Dozens"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="dozensUserId">Dozensに開設したアカウントのIDを指定します。</param>
        /// <param name="apiKey">そのアカウントの API KEY を指定します。API KEY は Doznes の Web サイトにログインして、プロフィールのページ(https://dozens.jp/profile)から入手できます。</param>
        public Dozens(string dozensUserId, string apiKey)
        {
            this._DozensUserId = dozensUserId;
            this._APIKey = apiKey;
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

        public void UpdateRecord(int recordId, int prio, string content, int ttl = 7200)
        {
            CallAPI<RecoredResult>("record/update", recordId, new { prio, content, ttl });
        }

        public void UpdateRecord(string zoneName, string name, int prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(zoneName, name);
            CallAPI<RecoredResult>("record/update", recordId, new { prio, content, ttl });
        }

        public void UpdateRecord(string fqdn, int prio, string content, int ttl = 7200)
        {
            var recordId = RetrieveRecordId(fqdn);
            CallAPI<RecoredResult>("record/update", recordId, new { prio, content, ttl });
        }

        public void CreateRecord(string zoneName, string name, string type, int prio, string content, int ttl = 7200)
        {
            CallAPI<RecoredResult>("record/create", null,
                new { domain = zoneName, name, type, prio, content, ttl});
        }

        public void DeleteRecord(int recordId)
        {
            CallAPI<RecoredResult>("record/delete", recordId, verb: "DELETE");
        }

        public void DeleteRecord(string zoneOrFQDName, string name = null)
        {
            var recordId = RetrieveRecordId(zoneOrFQDName, name);
            DeleteRecord(recordId);
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
