using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DozensAPI
{
    public class DozensRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int? Prio { get; set; }
        public string Content { get; set; }
        public int TTL { get; set; }

        public override string ToString()
        {
            return string.Format("{{Id = {0}, Name = {1}, Type = {2}, Content = {3}, Prio = {4}, TTL = {5}}}", Id, Name, Type, Content, Prio, TTL);
        }
    }
}
