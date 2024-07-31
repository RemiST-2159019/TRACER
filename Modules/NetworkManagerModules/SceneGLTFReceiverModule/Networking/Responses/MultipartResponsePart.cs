using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace tracer
{
    public class MultipartResponsePart
    {
        public ContentRangeHeaderValue ContentRange { get; set; }
        public ContentType ContentType { get; set; }
        public List<byte> Data { get; set; } = new List<byte>();
    }
}
