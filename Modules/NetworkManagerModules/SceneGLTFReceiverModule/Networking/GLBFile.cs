using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public class GLBFile
    {
        public GLTFHeader Header { get; set; }
        public GLTFSubheader JSONSubheader { get; set; }
        public string Json { get; set; }
        public byte[] Data { get; set; }
        public GLTFSubheader BinaryDataSubheader { get; set; }
    }
}
