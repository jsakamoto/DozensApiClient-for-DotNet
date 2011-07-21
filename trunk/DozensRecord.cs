using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DozensAPI
{
    /// <summary>
    /// Dozens の DNS レコード情報です。
    /// </summary>
    public class DozensRecord
    {
        /// <summary>レコードを一意に識別する、Dozens 内部で用いるレコードのIDを取得または設定します。</summary>
        public int Id { get; set; }

        /// <summary>レコードのFQDN(ex."www.dozens.jp")を取得または設定します。</summary>
        public string Name { get; set; }

        /// <summary>レコードのタイプ(ex."A","CNAME","MX","TXT")を取得または設定します。</summary>
        public string Type { get; set; }

        /// <summary>レコードのプライオリティを取得または設定します(null指定可)。</summary>
        public int? Prio { get; set; }

        /// <summary>レコードの値(ex."192.168.0.1","mail.dozens.jp")を取得または設定します。</summary>
        public string Content { get; set; }

        /// <summary>レコードのTTLを取得または設定します。</summary>
        public int TTL { get; set; }

        /// <summary>現在のレコード情報の文字列形式を作成して返します。</summary>
        public override string ToString()
        {
            return string.Format("{{Id = {0}, Name = {1}, Type = {2}, Content = {3}, Prio = {4}, TTL = {5}}}", Id, Name, Type, Content, Prio, TTL);
        }
    }
}
