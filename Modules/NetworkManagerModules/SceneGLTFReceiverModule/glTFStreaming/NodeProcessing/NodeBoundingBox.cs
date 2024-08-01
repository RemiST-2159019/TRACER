using GLTF.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Extensions;

namespace tracer
{
    public class NodeBoundingBox
    {
        public Bounds BoundingBox { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Translation { get; private set; }
        public Vector3 Scale { get; private set; }
        public GLTFRoot Root { get; private set; }
        public NodeBoundingBox(GLTFRoot root, Node node)
        {
            Root = root;
            InitBoundingBox(node);
        }

      
    
        public Matrix4x4 GetParentTransformations(Node node)
        {
            var name = node.Name;
            Matrix4x4 cumulativeMatrix = Matrix4x4.identity;
            var parents = new HashSet<Node>();
            NodeUtility.FindParentNodes(Root, node, parents);

            foreach (var parent in parents)
            {
                // Extract TRS components from the parent node's transformation matrix
                Vector3 parentTranslation = GetTransformationMatrix(parent).ExtractTranslation();
                Quaternion parentRotation = GetTransformationMatrix(parent).ExtractRotation();
                Vector3 parentScaling = GetTransformationMatrix(parent).ExtractScaling();

                // Create TRS matrices for each component
                Matrix4x4 parentTranslationMatrix = Matrix4x4.Translate(parentTranslation);
                Matrix4x4 parentRotationMatrix = Matrix4x4.Rotate(parentRotation);
                Matrix4x4 parentScalingMatrix = Matrix4x4.Scale(parentScaling);

                // Combine the TRS matrices and apply to cumulativeMatrix
                cumulativeMatrix = parentTranslationMatrix * parentRotationMatrix * parentScalingMatrix * cumulativeMatrix;
            }

            return cumulativeMatrix;
        }

        /// <summary>
        /// Test function for debugging bboxes
        /// </summary>
        /// <param name="bound"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private GameObject CreateBoundingBoxGameObject(Bounds bound, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = bound.center;
            box.transform.localScale = new Vector3(Math.Abs(bound.size.x), Math.Abs(bound.size.y), Math.Abs(bound.size.z));
            Renderer renderer = box.GetComponent<Renderer>();
            renderer.material.color = color;
            return box;
        }

        private Matrix4x4 GetTransformationMatrix(Node node)
        {
            if (!node.UseTRS && node.Matrix != null && !node.Matrix.ToUnityMatrix4x4().isIdentity)
            {
                return node.Matrix.ToUnityMatrix4x4();
            }
            else
            {
                Translation = node?.Translation.ToUnityVector3Convert() ?? Vector3.zero;
                Rotation = node?.Rotation.ToUnityQuaternionConvert() ?? Quaternion.identity;
                Scale = node?.Scale.ToUnityVector3Convert() ?? Vector3.one;
                return Matrix4x4.TRS(Translation, Rotation, Scale);
            }
        }

        public Matrix4x4 ApplyParentTransformations(Matrix4x4 childMatrix, Matrix4x4 parentTRSMatrix)
        {
            Vector3 parentTranslation = parentTRSMatrix.ExtractTranslation();
            Quaternion parentRotation = parentTRSMatrix.ExtractRotation();
            Vector3 parentScaling = parentTRSMatrix.ExtractScaling();

            childMatrix = Matrix4x4.Scale(parentScaling) * childMatrix;

            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(parentRotation);
            childMatrix = rotationMatrix * childMatrix;

            childMatrix.m03 += parentTranslation.x;
            childMatrix.m13 += parentTranslation.y;
            childMatrix.m23 += parentTranslation.z;

            return childMatrix;
        }


        private void InitBoundingBox(Node node)
        {
            if (node.Mesh == null) return;
            var matrix = GetTransformationMatrix(node);
            var parentsMatrix = GetParentTransformations(node);
            matrix = ApplyParentTransformations(matrix, parentsMatrix);
            

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (var prim in node.Mesh.Value.Primitives)
            {
                if (!prim.Attributes.ContainsKey("POSITION")) continue;

                var accessor = prim.Attributes["POSITION"].Value;

                if (accessor != null)
                {
                    Vector3 accessorMin = new Vector3((float)accessor.Min[0], (float)accessor.Min[1], (float)accessor.Min[2]);
                    Vector3 accessorMax = new Vector3((float)accessor.Max[0], (float)accessor.Max[1], (float)accessor.Max[2]);

                    min = Vector3.Min(min, accessorMin);
                    max = Vector3.Max(max, accessorMax);
                }
            }

            min = matrix.MultiplyPoint(min);
            max = matrix.MultiplyPoint(max);

            // Calculate transformed center and size
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            BoundingBox = new Bounds(center, size);
            Color randomColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 0.3f);
            //CreateBoundingBoxGameObject(BoundingBox, randomColor);
        }
    }
}
