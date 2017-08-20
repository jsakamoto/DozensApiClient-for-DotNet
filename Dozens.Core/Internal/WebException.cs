using System;
using System.IO;
using System.Text;

namespace DozensAPI
{
    internal class WebException : Exception
    {
        internal class WebExceptionResponse
        {
            internal Func<Stream> GetResponseStream { get; set; }
        }

        internal WebExceptionResponse Response { get; }

        internal WebException(string message, string content) : base(message)
        {
            var memStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            this.Response = new WebExceptionResponse { GetResponseStream = () => memStream };
        }
    }
}
