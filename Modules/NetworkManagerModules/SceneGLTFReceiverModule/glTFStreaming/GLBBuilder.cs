using System;
using System.Collections.Generic;
using System.Text;

namespace tracer
{
    /// <summary>
    /// Provides the ability to build Binary glTF files based on JSON and binary data.
    /// </summary>//
    public class GLBBuilder
    {
        //private GLTFObject _newObject;
        private string _json;
        //private List<byte> _newBuffer;
        //private HashSet<ByteRange> _copiedRanges;
        private byte[] _data;
        public GLBBuilder(string json, byte[] data)
        {
            _data = data;
            _json = json;
            //_newObject = JsonConvert.DeserializeObject<GLTFObject>(glbInfo.Json);
            //_newBuffer = new List<byte>();
            //_copiedRanges = new HashSet<ByteRange>();

            //_newObject.nodes.Clear();
            //_newObject.meshes.Clear();
            //_newObject.scenes.ForEach(s => s.nodes.Clear());


            //foreach (var node in _nodeTracker.GetNodes())
            //{
            //    if (node.IsDownloaded() && node.RenderStatus == RenderStatus.Not)
            //    {
            //        // Only keep data for nodes to be rendered for efficient rendering
            //        //CopyNeededData(node);
            //        node.RenderStatus = RenderStatus.Fully;
            //    }
            //}
            // Remove all images/textures/materials
            //_newObject.images.Clear();
            //_newObject.textures.Clear();
            //_newObject.materials.Clear();
            //foreach (var mesh in _newObject.meshes)
            //{
            //    var primitives = mesh.primitives;
            //    foreach (var primitive in primitives)
            //    {
            //        primitive.material = null;
            //    }
            //}
        }


        //private void CopyNeededData(DetailedNode node)
        //{
        //    var allRanges = new List<ByteRange>();
        //    allRanges.AddRange(node.GeometryRanges);
        //    allRanges.AddRange(node.ImageRanges);
        //    allRanges.Sort((a, b) => a.StartByte.CompareTo(b.StartByte));
        //    foreach (var range in allRanges)
        //    {
        //        // Prevent adding the same ranges to the new buffer in case
        //        // multiple nodes use the same data (like images)
        //        if (_copiedRanges.Contains(range))
        //            continue;
        //        var offset = 20 + (int)_glbInfo.JSONSubheader.ChunkLength + 8;
        //        // Get range as it appears in their bufferview
        //        // bufferView.byteOffset = startByte - offset
        //        // bufferView.byteLength = endByte - offset - bufferView.byteOffset + 1
        //        var bvOffset = (int)range.StartByte - offset;
        //        var bvLength = range.EndByte - offset - bvOffset + 1;
        //        var tempBuffer = new byte[bvLength];
        //        var newIndex = _newBuffer.Count;
        //        Array.Copy(_data, bvOffset, tempBuffer, 0, bvLength);
        //        _newBuffer.AddRange(tempBuffer);
        //        _copiedRanges.Add(range);

        //        // Update old bufferviews to reference new and smaller buffer
        //        for (int i = 0; i < _originalObject.bufferViews.Count; i++)
        //        {
        //            var bv = _originalObject.bufferViews[i];
        //            if (bv.byteOffset == bvOffset && bv.byteLength == bvLength)
        //                _newObject.bufferViews[i].byteOffset = newIndex;
        //        }
        //    }
        //}

        public byte[] BuildGLBBytesAsync()
        {
            if (_data.Length == 0)
                return null;

            var jsonChunk = CreateJsonChunk();
            var binaryChunk = CreateBinaryChunk();
            var header = CreateGLBHeader(jsonChunk.Length, binaryChunk.Length);

            List<byte> glbBytes = new List<byte>();
            glbBytes.AddRange(header);
            glbBytes.AddRange(jsonChunk);
            glbBytes.AddRange(binaryChunk);

            return glbBytes.ToArray();
        }


        private byte[] CreateJsonChunk()
        {
            byte[] jsonDataChunk = Encoding.UTF8.GetBytes(_json);
            uint chunkLength = (uint)jsonDataChunk.Length;
            uint chunkType = 0x4E4F534A; // JSON

            byte[] jsonChunk = new byte[8 + jsonDataChunk.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(chunkLength), 0, jsonChunk, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(chunkType), 0, jsonChunk, 4, 4);
            Buffer.BlockCopy(jsonDataChunk, 0, jsonChunk, 8, jsonDataChunk.Length);

            return jsonChunk;
        }

        private byte[] CreateBinaryChunk()
        {
            uint chunkLength = (uint)_data.Length;
            uint chunkType = 0x004E4942; // BIN

            byte[] binaryChunk = new byte[8 + _data.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(chunkLength), 0, binaryChunk, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(chunkType), 0, binaryChunk, 4, 4);
            Buffer.BlockCopy(_data, 0, binaryChunk, 8, _data.Length);

            return binaryChunk;
        }




        private byte[] CreateGLBHeader(int jsonLength, int binaryLength)
        {
            List<byte> headerBytes = new List<byte>();
            // Magic
            uint magic = 0x46546C67;
            headerBytes.AddRange(BitConverter.GetBytes(magic));

            // Version
            uint version = 2;
            headerBytes.AddRange(BitConverter.GetBytes(version));

            // Length
            uint length = Convert.ToUInt32(12 + jsonLength + binaryLength);
            headerBytes.AddRange(BitConverter.GetBytes(length));
            return headerBytes.ToArray();
        }
    }
}
