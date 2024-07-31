using Assets.Scripts.Enums;
using System.Collections.Generic;

namespace Assets.Scripts.Networking.Responses
{
    public class CompleteResponse : IResponse
    {
        private byte[] _data;
        public ResponseType ResponseType => ResponseType.Complete;

        public UnityWebResponse UnityWebResponse { get; set; }

        public CompleteResponse(byte[] data)
        {
            _data = data;
        }

        public byte[] GetData()
        {
            return _data;
        }

        public List<ByteRange> GetDownloadedRanges()
        {
            return new List<ByteRange> { new ByteRange(0, _data.Length) };
        }
    }
}
