using System.Collections.Generic;
using System.Linq;
//using UnityEditor;
using UnityEngine;
//using UnityEditor.Experimental.GraphView;
//using System.Text;

namespace NGAME
{
    [CreateAssetMenu(fileName = "NGAME_Graph", menuName = "NGAME/New Graph")]
    public class RoomGraph : ScriptableObject
    {
        public RoomNode rootNode;
        public List<RoomNode> nodes = new List<RoomNode>();
        

        public void AddNode(RoomNode node)
        {
            if (nodes.Contains(node))
            {
                return;
            }

            if (nodes.Count <= 0)
            {
                SetStartNode(node);
            }

            nodes.Add(node);
            
        }
        
        public void DeleteNode(RoomNode node)
        {
            nodes.Remove(node);
        }

        public void SetStartNode(RoomNode node)
        {
            if(rootNode != null)
            {
                rootNode.SetAsStartRoom(false);
            }
            rootNode = node;
            node.SetAsStartRoom(true);
        }

        public IMapNode GetNodeByGuid(string guid)
        {
            return nodes.FirstOrDefault((IMapNode e) => e.Guid == guid);
        }

        public void PrintGraph()
        {
            Debug.Log("===========================");
            Debug.Log("Printing the Connected Nodes of Graph: " + this.name);
            PrintNodesRecursive(rootNode);
            Debug.Log("===========================");
        }

        private void PrintNodesRecursive(IMapNode node)
        {
            Debug.Log(node.PrintNode());
            if (node.GetOutgoingEdges().Count > 0)
            {
                Dictionary<string, IMapNode> children = GetChildren(node);
                foreach (var child in children) 
                {
                    PrintNodesRecursive(child.Value);
                }
            }
        }

        private Dictionary<string, IMapNode> GetChildren(IMapNode node)
        {
            Dictionary<string, IMapNode> results = new Dictionary<string, IMapNode>();

            if(node == null)
            {
                return results;
            }
            List<EdgeData> edges = node.GetOutgoingEdges();
            foreach (EdgeData edge in edges)
            {
                results.Add(edge.DestinationNodeGuid, GetNodeByGuid(edge.DestinationNodeGuid));
            }

            return results;
        }

        
        
    }
}