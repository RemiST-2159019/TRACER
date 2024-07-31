using GLTF;
using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Assets.Scripts.NodeProcessing
{

    /// <summary>
    /// Calculates ranges and/or indices belonging to a specific node based on a GLTFRoot that contains that node.
    /// </summary>
    public class NodeCalculator
    {
        private GLTFRoot _object;
        private long _jsonLength;

        public NodeCalculator(GLTFRoot gltfObject, long jsonLength)
        {
            _object = gltfObject;
            _jsonLength = jsonLength;
        }

        #region Private calculation methods

        public List<ByteRange> CalculateGeometryRanges(Node node)
        {
            var ranges = new List<ByteRange>();
            if (node.Mesh == null) return ranges;
            var mesh = node.Mesh.Value;
            var accessors = ExtractAccessors(mesh);
            var bvIndices = GetAccessorBufferviews(accessors);
            return GetBufferviewRanges(bvIndices);
        }

        public List<TextureId> GetTextureIndices(Node node)
        {
            var indices = new List<TextureId>();
            if (node.Mesh == null) return new List<TextureId>();
            var mesh = _object.Meshes[node.Mesh.Id];

            foreach (var primitive in mesh.Primitives)
            {
                if (primitive.Material == null) continue;
                var material = _object.Materials[primitive.Material.Id];
                indices.AddRange(GetTextureIndices(material));
            }
            return indices.Distinct().ToList();

        }

        public List<int> GetSceneIndices(int nodeIndex)
        {
            var sceneIndices = new List<int>();
            for (int i = 0; i < _object.Scenes.Count; i++)
            {
                var scene = _object.Scenes[i];
                if (scene.Nodes.Any(n => n.Id == nodeIndex))
                    sceneIndices.Add(i);
            }
            return sceneIndices;
        }



        //public List<MaterialId> GetMaterialIndices(Node node)
        //{
        //    var indices = new List<int>();
        //    if (node.Mesh == null) return indices;

        //    foreach (var primitive in node.Mesh.Value.Primitives)
        //    {
        //        if (primitive.Material == null) continue;
        //        indices.Add(primitive.Material.Id);
        //    }
        //    return indices.Distinct().ToList();
        //}



        //public List<BufferViewId> GetBufferviewIndices(Node node)
        //{
        //    if (node.Mesh == null) return new List<int>();
        //    var accessors = ExtractAccessors(node.Mesh.Value);
        //    var bvIndices = GetAccessorBufferviews(accessors);
        //    var imageBufferviews = GetMaterialBufferviews(node.Mesh.Value);
        //    bvIndices.AddRange(imageBufferviews);
        //    return bvIndices.Distinct().ToList();
        //}

        public List<int> GetAccessorIndices(Node node)
        {

            if (node.Mesh == null) return new List<int>();
            var accessors = ExtractAccessors(node.Mesh.Value);
            var indices = new List<int>();
            for (int i = 0; i < _object.Accessors.Count; i++)
            {
                var accessor = _object.Accessors[i];
                if (accessors.Contains(accessor))
                    indices.Add(i);
            }
            return indices;
        }

        public List<ImageId> GetImageIndices(Node node)
        {
            var indices = new List<ImageId>();
            if (node.Mesh == null) return indices;

            foreach (var primitive in node.Mesh.Value.Primitives)
            {
                if (primitive.Material == null) continue;
                var textures = GetTextures(primitive.Material.Value);
                foreach (var texture in textures)
                {
                    if (texture.Source != null)
                        indices.Add(texture.Source);
                }
            }
            return indices.Distinct().ToList();
        }


        public List<ByteRange> CalculateImageRanges(Node node)
        {
            var ranges = new List<ByteRange>();
            if (node.Mesh == null) return ranges;
            var imageBufferviews = GetMaterialBufferviews(node.Mesh.Value);
            return GetBufferviewRanges(imageBufferviews);
        }


        private List<ByteRange> GetBufferviewRanges(List<BufferViewId> bufferviewIndices)
        {
            List<ByteRange> ranges = new List<ByteRange>();
            foreach (var index in bufferviewIndices)
            {
                var range = CalculateRange(index); ranges.Add(range);
            }
            return ranges.Distinct().ToList();
        }


        private ByteRange CalculateRange(BufferViewId bufferViewIndex)
        {
            var bufferView = bufferViewIndex.Value;

            // offset of header + json subheader + json data length + binary subheader length
            var headerJsonOffset = 12 + 8 + _jsonLength + 8;

            long startByte = headerJsonOffset + bufferView.ByteOffset;
            long endByte = headerJsonOffset + bufferView.ByteOffset + bufferView.ByteLength - 1;

            return new ByteRange(startByte, endByte);
        }




        private List<BufferViewId> GetAccessorBufferviews(List<Accessor> accessors)
        {
            var bvIndices = new List<BufferViewId>();
            foreach (var accessor in accessors)
            {
                if (accessor.BufferView == null) continue;
                bvIndices.Add(accessor.BufferView);
            }
            return bvIndices.Distinct().ToList();
        }



        private List<Accessor> ExtractAccessors(GLTFMesh mesh)
        {
            var accessors = new List<Accessor>();
            foreach (var primitive in mesh.Primitives)
            {
                // Add attributes accessors
                var indices = GetAccessorIndices(primitive);
                indices.ForEach(index => accessors.Add(index.Value));
                // Add indices accessor
                if (primitive.Indices != null)
                    accessors.Add(primitive.Indices.Value);
            }
            return accessors;
        }

        private List<BufferViewId> GetMaterialBufferviews(GLTFMesh mesh)
        {
            var indices = new List<BufferViewId>();
            foreach (var primitive in mesh.Primitives)
            {
                if (primitive.Material == null) continue;
                var material = _object.Materials[primitive.Material.Id];
                var textures = GetTextures(material);
                indices.AddRange(GetTexturesBufferviews(textures));
            }
            return indices.Distinct().ToList();
        }




        private List<TextureId> GetTextureIndices(GLTFMaterial material)
        {
            var indices = new List<TextureId>();
            if (material.PbrMetallicRoughness?.BaseColorTexture?.Index != null)
                indices.Add(material.PbrMetallicRoughness.BaseColorTexture.Index);
            if (material.PbrMetallicRoughness?.MetallicRoughnessTexture?.Index != null)
                indices.Add(material.PbrMetallicRoughness.MetallicRoughnessTexture.Index);
            if (material.NormalTexture?.Index != null)
                indices.Add(material.NormalTexture.Index);
            if (material.EmissiveTexture?.Index != null)
                indices.Add(material.EmissiveTexture.Index);
            if (material.OcclusionTexture?.Index != null)
                indices.Add(material.OcclusionTexture.Index);
            return indices;
        }

        private List<GLTFTexture> GetTextures(GLTFMaterial material)
        {
            var indices = GetTextureIndices(material);
            var textures = new List<GLTFTexture>();
            indices.ForEach(i => textures.Add(i.Value));
            return textures;
        }



        private List<BufferViewId> GetTexturesBufferviews(List<GLTFTexture> textures)
        {
            var indices = new List<BufferViewId>();
            foreach (var texture in textures)
            {
                if (texture.Source == null) continue;
                var image = _object.Images[(int)texture.Source.Id];
                if (image.BufferView == null) continue;
                indices.Add(image.BufferView);
            }
            return indices.Distinct().ToList();
        }



        private List<AccessorId> GetAccessorIndices(MeshPrimitive primitive)
        {
            List<AccessorId> indices = new List<AccessorId>();

            if (primitive.Attributes == null) return indices;
            indices.AddRange(primitive.Attributes.Values.ToList());
            return indices.Distinct().ToList();
        }

        #endregion
    }
}
