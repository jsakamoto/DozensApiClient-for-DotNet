using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;

namespace DozensAPI.Test
{
    public delegate string CommandProc(Match pathInfo, MockEndPoint.Data data);

    public class Command
    {
        public string PathPattern { get; set; }
        public string HttpMethod { get; set; }
        public CommandProc Procedure { get; set; }
        public Command(string pattern, string method, CommandProc proc)
        {
            this.PathPattern = pattern;
            this.HttpMethod = method;
            this.Procedure = proc;
        }
    }

    public class MockEndPoint : IAPIEndPoint
    {
        /// <summary>ゾーンのリスト</summary>
        List<DozensZone> _Zones;

        /// <summary>ゾーンIDで区別される、ゾーンごとのレコードのリスト</summary>
        Dictionary<int, List<DozensRecord>> _Records;

        /// <summary>コンストラクタ。ゾーン情報やレコードの初期設定もここで行ってます。</summary>
        public MockEndPoint()
        {
            this.Headers = new WebHeaderCollection();
            var zone = new DozensZone { Id = 1, Name = "jsakamoto.info" };
            this._Zones = new List<DozensZone> { zone };
            var records = new List<DozensRecord> {
                new DozensRecord { Id = 2, Name = "www", Prio = 0, Content = "192.168.0.101", Type = "A", TTL = 7200 }
            };
            this._Records = new Dictionary<int, List<DozensRecord>> {
                {zone.Id, records}
            };
        }

        #region IAPIEndPoint メンバー

        public WebHeaderCollection Headers { get; set; }

        public string DownloadString(string address)
        {
            return this.UploadString(address, "GET", "");
        }

        public string UploadString(string address, string method, string data)
        {
            var url = new Uri(address);
            url.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped).Is("http://dozens.jp");
            //Headers["Host"].Is("dozens.jp");
            Headers["Accept"].Is("application/json");

            var commands = new[]{
                new Command("/api/authorize", "GET", ProcAuth),
                new Command("/api/zone", "GET", ProcGetZone),
                new Command("/api/zone/create", "POST", ProcCreateZone),
                new Command("/api/zone/delete/(?<zoneId>\\d+)", "DELETE", ProcDeleteZone),
                new Command("/api/record/(?<zoneName>.+)", "GET", ProcGetRecord),
                new Command("/api/record/update/(?<recordId>\\d+)", "POST", ProcUpdateRecord),
                new Command("/api/record/create", "POST", ProcCreateRecord),
                new Command("/api/record/delete/(?<recordId>\\d+)", "DELETE", ProcDeleteRecord),
            };

            var target = (from cmd in commands
                          let pathInfo = Regex.Match(url.AbsolutePath, "^" + cmd.PathPattern + "\\.json$")
                          where pathInfo.Success && cmd.HttpMethod == method
                          select new { cmd, pathInfo }).Single();
            return target.cmd.Procedure(target.pathInfo, DeserializeJson(data));
        }

        #endregion

        public class Data
        {
            public string name { get; set; }
            public bool add_google_apps { get; set; }
            public string google_authorize { get; set; }
            public string domain { get; set; }
            public string type { get; set; }
            public int? prio { get; set; }
            public string content { get; set; }
            public int ttl { get; set; }
        }

        public Data DeserializeJson(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            return JsonConvert.DeserializeObject<Data>(data);
        }

        public string ProcAuth(Match pathInfo, Data data)
        {
            Headers["X-Auth-User"].Is("jsakamoto");
            Headers["X-Auth-Key"].Is("a8753098B73131kewt987612004957d89");
            return @"{""auth_token"":""6cfb3debbbac7d144e9eb7b701f79c2225bd6646""}";
        }

        private void ValidateCommonHeaders()
        {
            Headers["Content-Type"].Is("application/json");
            Headers["X-Auth-Token"].Is("6cfb3debbbac7d144e9eb7b701f79c2225bd6646");
        }

        public string ProcGetZone(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();

            return GetZonesJson();
        }

        private string GetZonesJson()
        {
            if (!this._Zones.Any())
                return "[]";
            else
                return
                    @"{""domain"":[" +
                    string.Join(
                        ",",
                        this._Zones.Select(z => string.Format(@"{{""id"":""{0}"",""name"":""{1}""}}", z.Id, z.Name))
                    ) + "]}";
        }

        public string ProcCreateZone(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();
            data.name.IsNotNull();
            data.name.IsNot("");

            var nextZoneId = _Zones.DefaultIfEmpty(new DozensZone { Id = 0 }).Max(z => z.Id) + 1;
            _Zones.Insert(0, new DozensZone { Id = nextZoneId, Name = data.name });
            _Records.Add(nextZoneId, new List<DozensRecord>());

            return GetZonesJson();
        }

        public string ProcDeleteZone(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();

            var zoneId = int.Parse(pathInfo.Groups["zoneId"].Value);
            var target = _Zones.Single(z => z.Id == zoneId);
            _Zones.Remove(target);
            _Records.Remove(target.Id);

            return GetZonesJson();
        }

        public string ProcGetRecord(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();

            var zoneName = pathInfo.Groups["zoneName"].Value;
            var zone = _Zones.Single(z => z.Name == zoneName);

            return GetRecordsJson(zone);
        }

        private string GetRecordsJson(DozensZone zone)
        {
            var records = _Records[zone.Id]
                .Select(r => string.Format(
                    @"{{""id"":""{0}"",""name"":""{1}"",""type"":""{2}"",""prio"":""{3}"",""content"":""{4}"",""ttl"":""{5}""}}",
                    r.Id,
                    r.Name == "" ? zone.Name : r.Name + "." + zone.Name,
                    r.Type, r.Prio, r.Content, r.TTL)
                )
                .ToArray();
            if (!records.Any())
                return "[]";
            else
                return @"{""record"":[" + string.Join(",", records) + "]}";
        }

        public string ProcUpdateRecord(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();
            data.content.IsNotNull();
            data.content.IsNot("");
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var recordId = int.Parse(pathInfo.Groups["recordId"].Value);

            var target = (from zone in _Zones
                          from record in _Records[zone.Id]
                          where record.Id == recordId
                          select new { zone, record }).Single();

            target.record.Prio = data.prio;
            target.record.Content = data.content;
            target.record.TTL = data.ttl;

            return GetRecordsJson(target.zone);
        }

        public string ProcCreateRecord(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();
            data.domain.Is(s => !string.IsNullOrEmpty(s));
            data.name.IsNotNull();
            data.type.Is(s => new[] { "A", "CNAME", "MX", "TXT" }.Contains(s));
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var zone = _Zones.Single(z => z.Name == data.domain);
            var records = _Records[zone.Id];
            records.Add(new DozensRecord
            {
                Name = data.name,
                Type = data.type,
                Prio = data.prio,
                Content = data.content,
                TTL = data.ttl
            });

            return GetRecordsJson(zone);
        }

        public string ProcDeleteRecord(Match pathInfo, Data data)
        {
            ValidateCommonHeaders();
            data.IsNull();

            var recordId = int.Parse(pathInfo.Groups["recordId"].Value);

            var target = (from zone in _Zones
                          from record in _Records[zone.Id]
                          where record.Id == recordId
                          select new { zone, record }).Single();

            var records = _Records[target.zone.Id];
            records.Remove(target.record);

            return GetRecordsJson(target.zone);
        }
    }
}
