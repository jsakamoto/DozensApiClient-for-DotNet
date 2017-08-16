using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DozensAPI.Test
{
    public class DozensTest
    {
        private AppSettings AppSettings { get; set; }

        public DozensTest()
        {
            this.AppSettings = AppSettings.Load();
        }

        Dozens CreateTarget()
        {
            var target = new Dozens_TestAdaptor(this.AppSettings.DozensId, this.AppSettings.APIKey);
            ModAPIEndPointIfUseMock(target);
            return target;
        }

        private void ModAPIEndPointIfUseMock(Dozens_TestAdaptor target)
        {
            if (this.AppSettings.UseMock)
            {
                target.SetAPIEndPoint(new MockEndPoint());
            }
        }

        static bool RegexIsMatch(string expectedPattern, string actual)
        {
            return Regex.IsMatch(expectedPattern, actual);
        }

        [Fact]
        public void AuthTest()
        {
            var target = this.CreateTarget();
            target.Token.IsNull();

            (target as Dozens_TestAdaptor).Auth();
            target.Token.IsNotNull();
        }

        [Fact]
        public void Auth2Test()
        {
            var target = new Dozens_TestAdaptor();
            ModAPIEndPointIfUseMock(target);
            target.Token.IsNull();

            target.Auth(this.AppSettings.DozensId, this.AppSettings.APIKey);
            target.Token.IsNotNull();
        }

        [Fact]
        public void GetZonesTest()
        {
            var target = this.CreateTarget();

            VerifyInitialZonez(target);
        }

        [Fact]
        public void GetZonesWithEmptyTest()
        {
            var target = this.CreateTarget();
            target.DeleteZone("jsakamoto.info");
            var zones = target.GetZones();
            zones.Length.Is(0);

            // 原状復旧
            target.CreateZone("jsakamoto.info");
            target.CreateRecord("jsakamoto.info", "www", "A", 0, "192.168.0.101");
        }

        public void VerifyInitialZonez(Dozens target)
        {
            var zones = target
                .GetZones()
                .Select(zone => zone.ToString())
                .ToArray();
            zones.Is(new[] { @"{Id = \d+, Name = ""jsakamoto\.info""}" }, RegexIsMatch);
        }

        [Fact]
        public void CreateAndDeleteZoneByIdTest()
        {
            var target = this.CreateTarget();
            var zones = CreateZoneTest(target);

            var zoneId = zones.First(z => z.Name == "jsakamoto2.info").Id;
            target.DeleteZone(zoneId);
            VerifyInitialZonez(target);
        }

        private DozensZone[] CreateZoneTest(Dozens target)
        {
            VerifyInitialZonez(target);

            var zones = target.CreateZone("jsakamoto2.info");
            zones
                .OrderBy(z => z.Name)
                .Select(z => z.ToString())
                .Is(new[] {
                @"{Id = \d+, Name = ""jsakamoto\.info""}",
                @"{Id = \d+, Name = ""jsakamoto2\.info""}",
                }, RegexIsMatch);
            return zones;
        }

        [Fact]
        public void CreateAndDeleteZoneByNameTest()
        {
            var target = this.CreateTarget();
            CreateZoneTest(target);

            target.DeleteZone("jsakamoto2.info");
            VerifyInitialZonez(target);
        }

        [Fact]
        public void GetRecordsTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);
        }

        [Fact]
        public void GetRecordsWithEmptyTest()
        {
            var target = this.CreateTarget();
            target.DeleteRecord("jsakamoto.info", "www");
            var records = target.GetRecords("jsakamoto.info");
            records.Length.Is(0);

            // 原状復帰
            target.CreateRecord("jsakamoto.info", "www", "A", 0, "192.168.0.101");
        }

        public void VerifyInitialRecords(Dozens target)
        {
            var records = target.GetRecords("jsakamoto.info");
            VerifyInitialRecords(records);
        }

        public void VerifyInitialRecords(IEnumerable<DozensRecord> records)
        {
            records
                .Select(record => record.ToString())
                .Is(new[] {
                @"{Id = \d+, Name = www.jsakamoto.info, Type = A, Content = 192.168.0.101, Prio = 0, TTL = 7200}"
                }, RegexIsMatch);
        }

        [Fact]
        public void UpdateRecordsByIdTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            var recordId = target.GetRecords("jsakamoto.info").First(r => r.Name == "www.jsakamoto.info").Id;

            target
                .UpdateRecord(recordId, 1, "192.168.0.201", 7200)
                .Select(record => record.ToString())
                .Is(@"{Id = " + recordId + @", Name = www.jsakamoto.info, Type = A, Content = 192.168.0.201, Prio = 1, TTL = 7200}");

            var records = target.UpdateRecord(recordId, 0, "192.168.0.101", 7200);
            VerifyInitialRecords(records);
        }

        [Fact]
        public void UpdateRecordsByNameTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            target
                .UpdateRecord("jsakamoto.info", "www", 1, "192.168.0.201", 7200)
                .Select(record => record.ToString())
                .Is(new[] { @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.201, Prio = 1, TTL = 7200}" }, RegexIsMatch);

            var records = target.UpdateRecord("jsakamoto.info", "www", 0, "192.168.0.101", 7200);
            VerifyInitialRecords(records);
        }

        [Fact]
        public void UpdateRecordsByFQDNTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            target
                .UpdateRecord("jsakamoto.info", "www.jsakamoto.info", 1, "192.168.0.201", 7200)
                .Select(record => record.ToString())
                .Is(new[] { @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.201, Prio = 1, TTL = 7200}" }, RegexIsMatch);

            var records = target.UpdateRecord("jsakamoto.info", "www.jsakamoto.info", 0, "192.168.0.101", 7200);
            VerifyInitialRecords(records);
        }

        [Fact]
        public void UpdateRecordsByFQDNOnlyTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            target
                .UpdateRecord("www.jsakamoto.info", 1, "192.168.0.201", 7200)
                .Select(record => record.ToString())
                .Is(new[] { @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.201, Prio = 1, TTL = 7200}" }, RegexIsMatch);

            var records = target.UpdateRecord("www.jsakamoto.info", 0, "192.168.0.101", 7200);
            VerifyInitialRecords(records);
        }

        [Fact]
        public void UpdateRecordsPrioIsNullTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            target
                .UpdateRecord("www.jsakamoto.info", null, "192.168.0.201", 7200)
                .Select(record => record.ToString())
                .Is(new[] { @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.201, Prio = , TTL = 7200}" }, RegexIsMatch);

            var records = target.UpdateRecord("www.jsakamoto.info", 0, "192.168.0.101", 7200);
            VerifyInitialRecords(records);
        }

        [Fact]
        public void CreateAndDeleteRecordByIdTest()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            var records = target.CreateRecord("jsakamoto.info", "", "MX", 0, "smtp.jsakamoto.info", 7200);
            records
                .Select(r => r.ToString())
                .Is(new[]{
                    @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.101, Prio = 0, TTL = 7200}",
                    @"{Id = \d+, Name = jsakamoto\.info, Type = MX, Content = smtp\.jsakamoto\.info, Prio = 0, TTL = 7200}"
                }, RegexIsMatch);

            var record = records.First(r => r.Type == "MX");
            var finalRecords = target.DeleteRecord(record.Id);
            VerifyInitialRecords(finalRecords);
        }

        [Fact]
        public void CreateAndDeleteRecordByNameTest()
        {
            var target = this.CreateTarget();
            CreateCNAMETest(target);

            var records = target.DeleteRecord("jsakamoto.info", "pop3");
            VerifyInitialRecords(records);
        }

        [Fact]
        public void CreateAndDeleteRecordByFQDNTest()
        {
            var target = this.CreateTarget();
            CreateCNAMETest(target);

            var records = target.DeleteRecord("jsakamoto.info", "pop3.jsakamoto.info");
            VerifyInitialRecords(records);
        }

        [Fact]
        public void CreateAndDeleteRecordByFQDNOnlyTest()
        {
            var target = this.CreateTarget();
            CreateCNAMETest(target);

            var records = target.DeleteRecord("pop3.jsakamoto.info");
            VerifyInitialRecords(records);
        }

        [Fact]
        public void CreateAndDeleteRecordPrioIsNull()
        {
            var target = this.CreateTarget();
            VerifyInitialRecords(target);

            target
                .CreateRecord("jsakamoto.info", "pop3", "CNAME", null, "imap4.jsakamoto.info", 7200)
                .Select(r => r.ToString())
                .Is(new[]{
                    @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.101, Prio = 0, TTL = 7200}",
                    @"{Id = \d+, Name = pop3\.jsakamoto\.info, Type = CNAME, Content = imap4\.jsakamoto\.info, Prio = , TTL = 7200}"
                }, RegexIsMatch);

            var records = target.DeleteRecord("pop3.jsakamoto.info");
            VerifyInitialRecords(records);
        }

        private void CreateCNAMETest(Dozens target)
        {
            VerifyInitialRecords(target);
            target
                .CreateRecord("jsakamoto.info", "pop3", "CNAME", 10, "imap4.jsakamoto.info", 7200)
                .Select(r => r.ToString())
                .Is(new[]{
                    @"{Id = \d+, Name = www\.jsakamoto\.info, Type = A, Content = 192\.168\.0\.101, Prio = 0, TTL = 7200}",
                    @"{Id = \d+, Name = pop3\.jsakamoto\.info, Type = CNAME, Content = imap4\.jsakamoto\.info, Prio = 10, TTL = 7200}"
                }, RegexIsMatch);
        }
    }
}
