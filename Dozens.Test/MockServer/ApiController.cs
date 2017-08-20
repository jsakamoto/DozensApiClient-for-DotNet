using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DozensAPI.Test.MockServer
{
    public class ApiController : Controller
    {
        private MockDozensApiServer Server { get; }

        public ApiController(MockDozensApiServer server)
        {
            this.Server = server;
        }

        [HttpGet, Route("/api/authorize.json")]
        public IActionResult Authentication()
        {
            var headers = this.Request.Headers;
            headers["X-Auth-User"].Is("jsakamoto");
            headers["X-Auth-Key"].Is("a8753098B73131kewt987612004957d89");
            return Content(@"{""auth_token"":""6cfb3debbbac7d144e9eb7b701f79c2225bd6646""}", "application/json");
        }

        private void ValidateCommonHeaders()
        {
            var headers = this.Request.Headers;
            headers["X-Auth-Token"].Is("6cfb3debbbac7d144e9eb7b701f79c2225bd6646");

            if (this.Request.ContentLength > 0)
                headers["Content-Type"].Is("application/json");
        }


        [HttpGet, Route("/api/zone.json")]
        public object GetZones()
        {
            ValidateCommonHeaders();
            if (!Server.Zones.Any()) return new object[0];
            return new
            {
                domain = Server.Zones.Select(z => new { id = z.Id, name = z.Name }).ToArray()
            };
        }

        [HttpPost, Route("/api/zone/create.json")]
        public object CreateZone([FromBody]Data data)
        {
            ValidateCommonHeaders();
            data.name.IsNotNull();
            data.name.IsNot("");

            if (Regex.IsMatch(data.name, "[,/*_;:+]"))
            {
                return StatusCode(400, new { code = 400, message = "There are some incorrect value.", detail = new { name = new[] { "ドメイン名は正しい形式で入力してください。" } } });
            }

            var nextZoneId = Server.Zones.DefaultIfEmpty(new DozensZone { Id = 0 }).Max(z => z.Id) + 1;
            Server.Zones.Insert(0, new DozensZone { Id = nextZoneId, Name = data.name });
            Server.Records.Add(nextZoneId, new List<DozensRecord>());

            return GetZones();
        }

        [HttpDelete, Route("/api/zone/delete/{zoneId}.json")]
        public object DeleteZone(int zoneId)
        {
            ValidateCommonHeaders();

            var target = Server.Zones.Single(z => z.Id == zoneId);
            Server.Zones.Remove(target);
            Server.Records.Remove(target.Id);

            return GetZones();
        }

        [HttpGet, Route("/api/record/{zoneName}.json")]
        public object GetRecords(string zoneName)
        {
            ValidateCommonHeaders();
            var zone = Server.Zones.Single(z => z.Name == zoneName);

            var records = Server.Records[zone.Id]
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
            if (!records.Any()) return new object[0];
            return new { record = records };
        }

        [HttpPost, Route("/api/record/update/{recordId}.json")]
        public object UpdateRecord(int recordId, [FromBody]Data data)
        {
            ValidateCommonHeaders();
            data.content.IsNotNull();
            data.content.IsNot("");
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var target = Server.Zones
                .Select(zone => new { zone, record = Server.Records[zone.Id].FirstOrDefault(r => r.Id == recordId) })
                .Single(item => item.record != null);

            target.record.Prio = data.prio;
            target.record.Content = data.content;
            target.record.TTL = data.ttl;

            return GetRecords(target.zone.Name);
        }

        [HttpPost, Route("/api/record/create.json")]
        public object CreateRecord([FromBody]Data data)
        {
            ValidateCommonHeaders();
            data.domain.Is(s => !string.IsNullOrEmpty(s));
            data.name.IsNotNull();
            data.type.Is(s => new[] { "A", "CNAME", "MX", "TXT" }.Contains(s));
            data.ttl.Is(t => new[] { 60, 3600, 7200, 86400 }.Contains(t));

            var zone = Server.Zones.Single(z => z.Name == data.domain);
            var records = Server.Records[zone.Id];
            records.Add(new DozensRecord
            {
                Name = data.name,
                Type = data.type,
                Prio = data.prio,
                Content = data.content,
                TTL = data.ttl
            });

            return GetRecords(zone.Name);
        }

        [HttpDelete, Route("/api/record/delete/{recordId}.json")]
        public object DeleteRecord(int recordId, [FromBody]Data data)
        {
            ValidateCommonHeaders();
            data.IsNull();

            var target = Server.Zones
                .Select(zone => new { zone, record = Server.Records[zone.Id].FirstOrDefault(r => r.Id == recordId) })
                .Single(item => item.record != null);

            var records = Server.Records[target.zone.Id];
            records.Remove(target.record);

            return GetRecords(target.zone.Name);
        }
    }
}
