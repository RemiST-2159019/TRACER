using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.NodeProcessing
{
    /// <summary>
    /// Prunes unnecessary nodes from a GLTFRoot object.
    /// Keeps in mind the node hierarchy, meaning node parents will always stay included.
    /// </summary>
    public class NodePruner
    {
        private readonly GLTFRoot _root;
        private Dictionary<Node, bool> _nodeHasParentDict = new Dictionary<Node, bool>();

        public NodePruner(GLTFRoot originalRoot)
        {
            _root = originalRoot;
        }


        private void InitializeParentDict()
        {
            _nodeHasParentDict.Clear();
            foreach (var node in _root.Nodes)
            {
                var hasParent = _root.Nodes.Any(n =>
                {
                    if (n.Children == null) return false;
                    return n.Children.Any(c => c.Value == node);
                });
                _nodeHasParentDict[node] = hasParent;
            }
        }


        /// <summary>
        /// Creates a new GLTFRoot object that only includes the nodes to keep including their parents.
        /// </summary>
        /// <param name="nodesToKeep">The nodes to keep in the new GLTFRoot</param>
        /// <returns></returns>
        public GLTFRoot Prune(List<Node> nodesToKeep)
        {
            InitializeParentDict();
            var parentNodes = new HashSet<Node>();
            foreach (var nodeToKeep in nodesToKeep)
                NodeUtility.FindParentNodes(_root, nodeToKeep, parentNodes);

            foreach (var node in parentNodes)
            {
                if (!nodesToKeep.Contains(node))
                    nodesToKeep.Add(node);
            }

            var rootCopy = new GLTFRoot(_root);
            rootCopy.IsGLB = true;
            var nodesCopy = NodeUtility.DeepCopyNodes(nodesToKeep, rootCopy);
            var oldToNewIndexMap = GetOldToNewIdMap(_root, nodesToKeep);
            var newToOldIndexMap = oldToNewIndexMap.ToDictionary(x => x.Value, x => x.Key);


            rootCopy.Nodes = nodesCopy;



            // First remove all children that reference nodes to remove
            foreach (var node in rootCopy.Nodes)
            {
                if (node.Children == null) continue;
                node.Children.RemoveAll(c =>
                {
                    var index = c.Id;
                    if (nodesToKeep.Contains(_root.Nodes[index]))
                        return false;
                    return true;
                });
            }


            foreach (var node in rootCopy.Nodes)
            {
                if (node.Children == null) continue;
                foreach (var child in node.Children)
                {
                    child.Id = oldToNewIndexMap[child.Id];
                }
            }

            foreach (var scene in rootCopy.Scenes)
            {
                scene.Nodes.Clear();
                foreach (var index in oldToNewIndexMap.Values)
                {
                    var node = rootCopy.Nodes[index];
                    var oldClone = _root.Nodes[newToOldIndexMap[index]];
                    bool hasParent = _nodeHasParentDict[oldClone];
                    if (hasParent) continue; // don't add children to scene
                    scene.Nodes.Add(new NodeId()
                    {
                        Id = index,
                        Root = rootCopy
                    });
                }
            }


            return rootCopy;
        }


        private Dictionary<int, int> GetOldToNewIdMap(GLTFRoot originalRoot, List<Node> nodesToKeep)
        {
            var dict = new Dictionary<int, int>();
            for (int i = 0; i < originalRoot.Nodes.Count; i++)
            {
                var node = originalRoot.Nodes[i];
                for (int j = 0; j < nodesToKeep.Count; j++)
                {
                    var nodeToKeep = nodesToKeep[j];
                    if (node.Equals(nodeToKeep) && !dict.ContainsKey(i))
                        dict.Add(i, j);
                }
            }
            return dict;
        }
    }
}
