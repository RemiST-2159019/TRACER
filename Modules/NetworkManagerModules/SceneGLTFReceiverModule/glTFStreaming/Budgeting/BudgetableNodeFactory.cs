using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace tracer
{
    public class BudgetableNodeFactory : IBudgetableFactory<Budgetable<ExtendedNode>, ExtendedNode>
    {
        private static Random _rnd = new Random();
        private StreamingState _state;
        public BudgetableNodeFactory()
        {
            _state = StreamingState.Instance;
        }

        private bool IsNoLevelOfDetailNode(Node node)
        {
            return node.Name.EndsWith("_noLOD");
        }

        public List<Budgetable<ExtendedNode>> CreateBudgetables(List<ExtendedNode> values)
        {
            var budgetables = new List<Budgetable<ExtendedNode>>();

            foreach (var node in values)
            {
                if (node.Progress.DownloadStatus == DownloadStatus.Completed
                    && node.Progress.RenderStatus == RenderStatus.Fully)
                    continue;

                var cost = node.Progress.GetUndownloadedByteLength();
                var priority = CalculatePriority(node);
                budgetables.Add(new Budgetable<ExtendedNode>(node)
                {
                    Cost = cost,
                    Priority = priority
                });
            }
            return budgetables;
        }

        public List<Budgetable<ExtendedNode>> CreateBudgetables()
        {
            return CreateBudgetables(_state.Nodes);
        }


        bool IsAnyVertexInFOV(Bounds bounds)
        {
            Vector3[] vertices = new Vector3[8];
            vertices[0] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z); // Front top right corner
            vertices[1] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z); // Front top left corner
            vertices[2] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z); // Front bottom right corner
            vertices[3] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z); // Front bottom left corner
            vertices[4] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z); // Back top right corner
            vertices[5] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z); // Back top left corner
            vertices[6] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z); // Back bottom right corner
            vertices[7] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z); // Back bottom left corner

            foreach (var vertex in vertices)
            {
                Vector3 screenPoint = Camera.main.WorldToViewportPoint(vertex);
                if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
                {
                    return true;
                }
            }

            return false;
        }

        private float CalculatePriority(ExtendedNode node)
        {
            float priority = 0f;

            if (_state.BudgetStrategy == BudgetStrategy.Random)
            {
                priority = (float)_rnd.NextDouble();
            }
            else if (_state.BudgetStrategy == BudgetStrategy.Distance)
            {
                var boundingBox = _state.NodeBoundingBoxes[node];
                var distance = Vector3.Distance(boundingBox.BoundingBox.center, Camera.main.transform.position);
                var maxDistance = _state.GetMaxDistance();
                //priority = /*maxDistance - distance * distance*/;
                priority = 1 / distance;

                if (!IsAnyVertexInFOV(boundingBox.BoundingBox))
                    priority -= 1_000_000; 
            }
            return priority;
        }
    }
}
