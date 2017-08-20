using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Newtonsoft.Json;
using Xunit;

namespace DozensAPI.Test.MockServer
{
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

    public class MockDozensApiServer : IDisposable
    {
        private static Random Random { get; } = new Random();

        private IWebHost Host { get; set; }

        public string Url { get; protected set; }

        /// <summary>ゾーンのリスト</summary>
        private List<DozensZone> Zones { get; }

        /// <summary>ゾーンIDで区別される、ゾーンごとのレコードのリスト</summary>
        private Dictionary<int, List<DozensRecord>> Records { get; }

        public MockDozensApiServer()
        {
            AssignUrl();

            var zone = new DozensZone { Id = 1, Name = "jsakamoto.info" };
            this.Zones = new List<DozensZone> { zone };
            var records = new List<DozensRecord> {
                new DozensRecord { Id = 2, Name = "www", Prio = 0, Content = "192.168.0.101", Type = "A", TTL = 7200 }
            };
            this.Records = new Dictionary<int, List<DozensRecord>> {
                {zone.Id, records}
            };
        }

        private void AssignUrl()
        {
            lock (Random)
            {
                var port = Random.Next(8000, 9000);
                this.Url = $"http://localhost:{port}";
            }
        }

        public void Start()
        {
            for (; ; )
            {
                try
                {
                    this.Host = new WebHostBuilder()
                            .UseKestrel()
                            .Configure(this.Configure)
                            .Start(this.Url);
                    return;
                }
                catch (IOException e) when (e.InnerException?.InnerException is UvException uv && uv.StatusCode == -4091)
                {
                    this.Host?.Dispose();
                    this.AssignUrl();
                }
            }
        }

        private void Configure(IApplicationBuilder app)
        {
            Map(app, "GET", "/api/authorize", ProcAuth);
            Map(app, "POST", "/api/zone/create", ProcCreateZone);
            Map(app, "GET", "/api/zone", ProcGetZone);
            Map(app, "DELETE", "/api/zone/delete/(?<zoneId>\\d+)", ProcDeleteZone);
            Map(app, "POST", "/api/record/create", ProcCreateRecord);
            Map(app, "GET", "/api/record/(?<zoneName>.+)", ProcGetRecord);
            Map(app, "POST", "/api/record/update/(?<recordId>\\d+)", ProcUpdateRecord);
            Map(app, "DELETE", "/api/record/delete/(?<recordId>\\d+)", ProcDeleteRecord);
        }

        private void Map(IApplicationBuilder app, string method, string pathMatch, Func<HttpContext, Match, Data, string> action)
        {
            app.MapWhen(
                context => context.Request.Method == method && Regex.IsMatch(context.Request.Path, $"^{pathMatch}\\.json$"),
                a =>
                {
                    a.Run(async (context) =>
                    {
                        var pathInfo = Regex.Match(context.Request.Path, $"^{pathMatch}\\.json$");

                        var buff = new byte[context.Request.ContentLength ?? 0];
                        context.Request.Body.Read(buff, 0, buff.Length);
                        var requestJson = Encoding.UTF8.GetString(buff);
                        var data = JsonConvert.DeserializeObject<Data>(requestJson);

                        var responseJson = action(context, pathInfo, data);

                        context.Response.Headers.Clear();
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(responseJson);
                    });
                });
        }

        private string ProcAuth(HttpContext context, Match pathInfo, Data data)
        {
            var headers = context.Request.Headers;
            headers["X-Auth-User"].Is("jsakamoto");
            headers["X-Auth-Key"].Is("a8753098B73131kewt987612004957d89");
            return @"{""auth_token"":""6cfb3debbbac7d144e9eb7b701f79c2225bd6646""}";
        }

        private void ValidateCommonHeaders(HttpContext context)
        {
            var headers = context.Request.Headers;
            headers["X-Auth-Token"].Is("6cfb3debbbac7d144e9eb7b701f79c2225bd6646");

            if (context.Request.ContentLength > 0)
                headers["Content-Type"].Is("application/json");
        }

        private string ProcGetZone(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);
            return GetZonesJson();
        }

        private string GetZonesJson()
        {
            if (!this.Zones.Any()) return "[]";
            return JsonConvert.SerializeObject(new
            {
                domain = this.Zones.Select(z => new { id = z.Id, name = z.Name }).ToArray()
            });
        }

        private string ProcCreateZone(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);
            data.name.IsNotNull();
            data.name.IsNot("");

            if (Regex.IsMatch(data.name, "[,/*_;:+]"))
            {
                context.Response.StatusCode = 400;
                return "{\"code\":400,\"message\":\"There are some incorrect value.\",\"detail\":{\"name\":[\"ドメイン名は正しい形式で入力してください。\"]}}";
            }

            var nextZoneId = this.Zones.DefaultIfEmpty(new DozensZone { Id = 0 }).Max(z => z.Id) + 1;
            this.Zones.Insert(0, new DozensZone { Id = nextZoneId, Name = data.name });
            this.Records.Add(nextZoneId, new List<DozensRecord>());

            return GetZonesJson();
        }

        private string ProcDeleteZone(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);

            var zoneId = int.Parse(pathInfo.Groups["zoneId"].Value);
            var target = this.Zones.Single(z => z.Id == zoneId);
            this.Zones.Remove(target);
            this.Records.Remove(target.Id);

            return GetZonesJson();
        }

        private string ProcGetRecord(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);

            var zoneName = pathInfo.Groups["zoneName"].Value;
            var zone = this.Zones.Single(z => z.Name == zoneName);

            return GetRecordsJson(zone);
        }

        private string GetRecordsJson(DozensZone zone)
        {
            var records = this.Records[zone.Id]
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name == "" ? zone.Name : r.Name + "." + zone.Name,
                    type = r.Type,
                    prio = r.Prio,
                    content = r.Content,
                    ttl = r.TTL
                })
                .ToArray();
            if (!records.Any()) return "[]";
            return JsonConvert.SerializeObject(new { record = records });
        }

        private string ProcUpdateRecord(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);
            data.content.IsNotNull();
            data.content.IsNot("");
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var recordId = int.Parse(pathInfo.Groups["recordId"].Value);

            var target = this.Zones
                .Select(zone => new { zone, record = this.Records[zone.Id].FirstOrDefault(r => r.Id == recordId) })
                .Single(item => item.record != null);

            target.record.Prio = data.prio;
            target.record.Content = data.content;
            target.record.TTL = data.ttl;

            return GetRecordsJson(target.zone);
        }

        private string ProcCreateRecord(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);
            data.domain.Is(s => !string.IsNullOrEmpty(s));
            data.name.IsNotNull();
            data.type.Is(s => new[] { "A", "CNAME", "MX", "TXT" }.Contains(s));
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var zone = this.Zones.Single(z => z.Name == data.domain);
            var records = this.Records[zone.Id];
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

        private string ProcDeleteRecord(HttpContext context, Match pathInfo, Data data)
        {
            ValidateCommonHeaders(context);
            data.IsNull();

            var recordId = int.Parse(pathInfo.Groups["recordId"].Value);

            var target = this.Zones
                .Select(zone => new { zone, record = this.Records[zone.Id].FirstOrDefault(r => r.Id == recordId) })
                .Single(item => item.record != null);

            var records = this.Records[target.zone.Id];
            records.Remove(target.record);

            return GetRecordsJson(target.zone);
        }

        public void Dispose()
        {
            this.Host?.Dispose();
        }
    }
}
