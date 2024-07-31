using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace tracer
{
    public class SinglePartResponse : IResponse
    {
        private byte[] _data;
        public ResponseType ResponseType => ResponseType.Single;
        public ContentRangeHeaderValue ContentRange { get; private set; }
        public ContentType ContentType { get; private set; }
        public UnityWebResponse UnityWebResponse { get; set; }

        public SinglePartResponse(string contentType, string contentRange, byte[] body)
        {
            _data = body;
            try
            {
                ContentType = contentType != null ? new ContentType(contentType) : null;
            }
            catch
            {
                throw new WebException($"Error parsing content type from '{contentType}'");
            }
            if (ContentRangeHeaderValue.TryParse(contentRange, out var cr))
                ContentRange = cr;
            else
                throw new WebException($"Error parsing content range from '{contentRange}'");
        }


        public byte[] GetData()
        {
            return _data;
        }

        public List<ByteRange> GetDownloadedRanges()
        {
            return new List<ByteRange>()
            {
                new ByteRange((long)ContentRange.From, (long)ContentRange.To)
            };
        }
    }
}
