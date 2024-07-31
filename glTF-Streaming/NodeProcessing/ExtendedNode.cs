using Assets.Scripts.Enums;
using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.NodeProcessing
{
    public class ExtendedNode
    {
        private StreamingState _state;
        public Node OriginalNode { get; set; }
        public NodeProgress Progress { get; }
        public ExtendedNode(Node node, StreamingState state) 
        {
            _state = state;
            OriginalNode = node;
            Progress = new NodeProgress(state, node);
        }

        public List<ByteRange> GetNodeRanges()
        {
            return Progress.GetRanges();
        }

        public long GetNodeSizeInBytes()
        {
            long totalBytes = Progress.GetRanges().Sum(r => r.GetLength());
            return totalBytes;
        }
    }
}
