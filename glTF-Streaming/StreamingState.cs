using Assets.Scripts.Enums;
using Assets.Scripts.Networking;
using Assets.Scripts.NodeProcessing;
using GLTF.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public sealed class StreamingState
    {
        private static readonly Lazy<StreamingState> lazy =
               new Lazy<StreamingState>(() => new StreamingState());

        public static StreamingState Instance { get { return lazy.Value; } }

        private StreamingState() { }

        public GLBFile GLBFile { get; set; }
        public GLTFRoot GLTFRoot { get; set; }
        public string ResourceUri { get; set; }
        public float AverageDownloadSpeed { get; set; }
        public Transform RootTransform { get; set; }
        public NetworkSettings NetworkSettings { get; set; }
        public List<ExtendedNode> Nodes { get; set; }
        public Dictionary<ExtendedNode, NodeBoundingBox> NodeBoundingBoxes { get; set; }
        public BudgetStrategy BudgetStrategy { get; set; }
        public bool NoLevelOfDetail { get; set; }
        public long GetJsonLength() => GLBFile.JSONSubheader.ChunkLength;
        public float GetMaxDistance()
        {
            float maxDistance = 0f;
            foreach (var bbox in NodeBoundingBoxes.Values)
            {
                var distance = DistanceBetweenCubeAndCamera(bbox.BoundingBox.ClosestPoint(Camera.main.transform.position));
                if (distance > maxDistance)
                    maxDistance = distance;
            }
            return maxDistance;
        }

        float DistanceBetweenCubeAndCamera(Vector3 position)
        {
            return Vector3.Distance(Camera.main.transform.position, position);
        }
    }
}
