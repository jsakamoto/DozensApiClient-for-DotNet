using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DozensAPI
{
    public class DozensException : Exception
    {
        public int Code { get; set; }

        public DozensException(int code, string message, Exception innerException)
            : base(string.Format("{1}({0})", code, message), innerException)
        {
            this.Code = code;
        }
    }
}
