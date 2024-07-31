using GLTF.Schema;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Extensions;

namespace Assets.Scripts.NodeProcessing
{
    public class NodeCopier
    {
        private Node _node;
        private GLTFRoot _root;
        private float _distance;

        public NodeCopier(Node node, GLTFRoot root)
        {
            _node = node;
            _root = root;
            var bbox = new NodeBoundingBox(root, node);
            _distance = Vector3.Distance(bbox.BoundingBox.ClosestPoint(Camera.main.transform.position), Camera.main.transform.position);
        }

        private List<NodeId> CopyChildren(Node node, float maxDistance)
        {
            List<NodeId> copies = new List<NodeId>();
            foreach(var child in node.Children)
            {
                NodeCopier copier = new NodeCopier(child.Value, _root);
                copies.Add(copier.CopyWithoutLOD(maxDistance));
            }
            return copies;
        }

        public NodeId CopyWithoutLOD(float maxDistance)
        {
            var copy = new Node(_node, _root);
            if (_node.Children != null && _node.Children.Count > 0)
            {
                var children = CopyChildren(_node, maxDistance);
                copy.Children = new List<NodeId>(children.Count);
                copy.Children.AddRange(children);
            }
            copy.Matrix = _node.Matrix;
            copy.Name += "_noLOD";
            _root.Nodes.Add(copy);
            var copyId = _root.Nodes.Count - 1;
            var nodeIdCopy = new NodeId()
            {
                Id = copyId,
                Root = _root
            };
            _root.Scenes[0].Nodes.Add(nodeIdCopy);
            if (copy.Mesh == null)
                return nodeIdCopy;

            var meshCopy = new GLTFMesh(copy.Mesh.Value, _root);
            meshCopy.Name += "_noLOD";
            _root.Meshes.Add(meshCopy);
            copy.Mesh = new MeshId()
            {
                Id = _root.Meshes.Count - 1,
                Root = _root
            };

            foreach (var primitive in meshCopy.Primitives)
            {
                var materialCopy = CopyMaterial(primitive.Material.Value, maxDistance);
                _root.Materials.Add(materialCopy);
                primitive.Material = new MaterialId()
                {
                    Id = _root.Materials.Count - 1,
                    Root = _root
                };
            }
            return nodeIdCopy;
        }

        public GLTFMaterial CopyMaterial(GLTFMaterial material, float maxDistance)
        {
            var copy = new GLTFMaterial(material, _root);
            copy.PbrMetallicRoughness = new PbrMetallicRoughness()
            {
                //BaseColorFactor = GetColor(_distance, maxDistance),
                BaseColorFactor = Color.gray.ToNumericsColorRaw(),
                RoughnessFactor = 0.5,
                MetallicFactor = 0
            };
            copy.EmissiveTexture = null;
            copy.OcclusionTexture = null;
            copy.NormalTexture = null;
            copy.PbrMetallicRoughness.MetallicRoughnessTexture = null;
            copy.PbrMetallicRoughness.BaseColorTexture = null;
            return copy;
        }

        public GLTF.Math.Color GetColor(float distance, float maxDistance)
        {
            // Ensure distance is within bounds
            distance = Mathf.Clamp(distance, 0f, maxDistance);

            // Normalize distance between 0 and 1
            float t = distance / maxDistance;

            // Interpolate between green and red based on distance
            Color color = Color.Lerp(Color.green, Color.red, t);

            return color.ToNumericsColorRaw();
        }


    }
}
