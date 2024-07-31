using System;
using System.Text;

namespace Assets.Scripts.BinaryFormat
{
    public class GLTFHeader
    {
        public uint Magic { get; private set; }
        public uint Version { get; private set; }
        public uint Length { get; private set; }

        public GLTFHeader(byte[] headerBytes) {
            Magic = BitConverter.ToUInt32(headerBytes, 0);
            Version = BitConverter.ToUInt32(headerBytes, sizeof(uint));
            Length = BitConverter.ToUInt32(headerBytes, 2 * sizeof(uint));
            if (Magic != 0x46546C67)
                throw new ArgumentException("Magic must be equal to the ASCII string 'glTF'.");
        }

      

        public override string ToString()
        {
            byte[] byteArray = BitConverter.GetBytes(Magic);

            string asciiString = Encoding.ASCII.GetString(byteArray);
            return $"Magic: {asciiString}" +
                $"\nVersion: {Version}" +
                $"\nLength: {Length}";
        }
    }
}
