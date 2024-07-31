using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public class UnityWebResponse
    {
        public long StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public byte[] Data { get; set; }
        /// <summary>
        /// Download time in milliseconds.
        /// </summary>
        public long DownloadTime { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }
    }
}
