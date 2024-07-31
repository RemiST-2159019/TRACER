using Assets.Scripts.Enums;
using System.Collections.Generic;

namespace Assets.Scripts.Networking.Responses
{
    public interface IResponse
    {
        ResponseType ResponseType { get; }
        UnityWebResponse UnityWebResponse { get; set; }
        List<ByteRange> GetDownloadedRanges();
    }
}
