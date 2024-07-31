using System.Collections.Generic;

namespace tracer
{
    public interface IResponse
    {
        ResponseType ResponseType { get; }
        UnityWebResponse UnityWebResponse { get; set; }
        List<ByteRange> GetDownloadedRanges();
    }
}
