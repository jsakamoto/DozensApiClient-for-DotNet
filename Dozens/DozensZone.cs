using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DozensAPI
{
    /// <summary>
    /// Dozens の DNS ゾーン情報です。
    /// </summary>
    public class DozensZone
    {
        /// <summary>ゾーンを一意に識別する、Dozens 内部で用いるゾーンのIDを取得または設定します。</summary>
        public int Id { get; set; }

        /// <summary>ゾーンの名前(ex."dozens.jp")を取得または設定します。</summary>
        public string Name { get; set; }

        /// <summary>現在のゾーン情報の文字列形式を作成して返します。</summary>
        public override string ToString()
        {
            return string.Format("{{Id = {0}, Name = \"{1}\"}}", this.Id, this.Name);
        }
    }
}
