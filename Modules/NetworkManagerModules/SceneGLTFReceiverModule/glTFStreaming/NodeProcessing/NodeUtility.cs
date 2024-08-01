using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public class NodeUtility
    {
        public static List<Node> DeepCopyNodes(List<Node> nodes, GLTFRoot newRoot)
        {
            var newList = new List<Node>();
            foreach (var node in nodes)
            {
                newList.Add(NodeUtility.DeepCopyNode(node, newRoot));
            }
            return newList;
        }

        public static Node DeepCopyNode(Node node, GLTFRoot newRoot)
        {
            var newNode = new Node(node, newRoot);
            newNode.Matrix = node.Matrix;
            return newNode;
        }

      
        public static void FindParentNodes(GLTFRoot root, Node possibleChild, HashSet<Node> parentNodes)
        {
            foreach (var node in root.Nodes)
            {
                if (ContainsChild(node, possibleChild) && !parentNodes.Contains(node))
                    parentNodes.Add(node);
            }
        }

        public static bool ContainsChild(Node node, Node nodeToSearch)
        {
            if (node.Children == null)
                return false;

            foreach (var child in node.Children)
            {
                if (child.Value.Equals(nodeToSearch))
                    return true;

                if (ContainsChild(child.Value, nodeToSearch))
                    return true;
            }

            return false;
        }
    }
}
