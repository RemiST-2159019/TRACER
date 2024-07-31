using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Assets.Scripts.BinaryFormat
{
    public class GLTFSubheader
    {
        public uint ChunkLength { get; private set; }
        public uint ChunkType { get; private set; }


        public GLTFSubheader(byte[] subheaderBytes)
        {
            ChunkLength = BitConverter.ToUInt32(subheaderBytes, 0);
            ChunkType = BitConverter.ToUInt32(subheaderBytes, sizeof(uint));
        }

        public override string ToString()
        {
            byte[] byteArray = BitConverter.GetBytes(ChunkType);

            string asciiString = Encoding.ASCII.GetString(byteArray);
            return $"Length: {ChunkLength}" +
                $"\nType: {asciiString}";
        }
    }
}
