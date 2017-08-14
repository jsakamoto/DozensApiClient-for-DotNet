using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DozensAPI
{
    internal class WebException : Exception
    {
        internal interface IHttpResponse
        {
            Stream GetResponseStream();
        }

        public IHttpResponse Response { get; set; }

    }
}
