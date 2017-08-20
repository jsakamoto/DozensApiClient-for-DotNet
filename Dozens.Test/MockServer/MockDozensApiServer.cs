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
using Microsoft.Extensions.DependencyInjection;
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
        public List<DozensZone> Zones { get; }

        /// <summary>ゾーンIDで区別される、ゾーンごとのレコードのリスト</summary>
        public Dictionary<int, List<DozensRecord>> Records { get; }

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
                            .ConfigureServices(svc =>
                            {
                                svc.AddMvc();
                                svc.AddSingleton(_ => this);
                            })
                            .Configure(app => app.UseMvc())
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

        public void Dispose()
        {
            this.Host?.Dispose();
        }
    }
}
