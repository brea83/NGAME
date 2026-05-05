using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace NGAME
{
    [System.Serializable]
    public class RoomNode : ScriptableObject, IMapNode
    {
        // public properties and geter/setters
        public string Guid { get => m_Guid; set => m_Guid = value; }
        public Vector2 Position { get => m_Position; set => m_Position = value; }

        [SerializeReference]
        public SceneData SceneData;
        public List<RegionConnectionData> OverridenConnectionData = new();

        [HideInInspector]
        public List<EdgeData> OutgoingEdges = new List<EdgeData>();
        [HideInInspector]
        public List<EdgeData> IncomingEdges = new List<EdgeData>();
        [SerializeField]
        public List<SOWaveData> Waves = new List<SOWaveData>();


        // private properties
        [SerializeField, HideInInspector]
        private string m_Guid;
        [SerializeField, HideInInspector]
        private Vector2 m_Position;
        [SerializeField]
        private bool _isStartNode = false;


        //private int _exitCount;
        //private int _entranceCount;

        public static RoomNode CreateNode(System.Type type, string guid)
        {
            RoomNode node = ScriptableObject.CreateInstance(type) as RoomNode;
            node.name = type.Name;
            node.Guid = guid;

            return node;
        }

        public List<EdgeData> GetOutgoingEdges()
        {
            return OutgoingEdges;
        }

        public List<EdgeData> GetIncomingEdges()
        {
            return IncomingEdges;
        }

        public List<SOWaveData> GetWaveData() { return Waves; }

        public void AddWave(SOWaveData wave)
        {
            Waves.Add(wave);
        }
        public void RemoveWave(int waveIndex)
        {
            Waves.RemoveAt(waveIndex);
        }

        public void PatchWaveData(SOWaveData wave, int index)
        {
            if(Waves.Count <= index)
            {
                return;
            }

            Waves[index] = wave;
        }

        public void AddEdge(IMapNode otherNode, EdgeData edge)
        {
            if (otherNode is RoomNode)
            {
                if (edge.DestinationNodeGuid == Guid) 
                {
                    IncomingEdges.Add(edge); 
                }
                else
                {
                    OutgoingEdges.Add(edge);
                }
            }
        }

        public void RemoveEdge(IMapNode otherNode, EdgeData edge)
        {
            if(otherNode is RoomNode)
            {
                if (edge.DestinationNodeGuid == Guid)
                {
                    RemoveIncomingRoomEdge(edge);
                }
                else
                {
                    RemoveOutgoingRoomEdge(edge);
                }
            }
        }

        private void RemoveOutgoingRoomEdge(EdgeData edge)
        {
            List<int> indexesToRemove = new List<int>();

            for (int i = 0; i < OutgoingEdges.Count; i++)
            {
                EdgeData edgeData = OutgoingEdges[i];

                if (edgeData.SourcePortName == edge.SourcePortName && edgeData.DestinationNodeGuid == edge.DestinationNodeGuid && edgeData.DestinationPortName == edge.DestinationPortName)
                {
                    indexesToRemove.Add(i);
                    //OutgoingEdges.Remove(edgeData);
                }
            }

            indexesToRemove.Sort();
            for (int i = indexesToRemove.Count - 1; i >= 0; i--)
            {
                OutgoingEdges.RemoveAt(indexesToRemove[i]);
            }
        }

        private void RemoveIncomingRoomEdge( EdgeData edge)
        {
            List<int> indexesToRemove = new List<int>();

            for (int i = 0; i < IncomingEdges.Count; i++)
            {
                EdgeData edgeData = IncomingEdges[i];

                if (edgeData.SourcePortName == edge.SourcePortName && edgeData.DestinationNodeGuid == edge.DestinationNodeGuid && edgeData.DestinationPortName == edge.DestinationPortName)
                {
                    indexesToRemove.Add(i);
                }
            }

            indexesToRemove.Sort();
            for (int i = indexesToRemove.Count - 1; i >= 0; i--)
            {
                IncomingEdges.RemoveAt(indexesToRemove[i]);
            }
        }

        public void SetAsStartRoom(bool isStartNode)
        {
            _isStartNode = isStartNode;
        }
        public void UpdateRoomData(SceneData room)
        {
            SceneData = room;
            OverridenConnectionData.Clear();
            if (room != null)
            {
                foreach(RegionConnectionData data in room.UniqueConnectionObjects)
                {
                    OverridenConnectionData.Add(data);
                }
            }
                
            UpdateEdges();
        }

        protected void UpdateEdges()
        {
            foreach(EdgeData edge in OutgoingEdges)
            {
                edge.ReplaceSceneAtNodeGuid(m_Guid, SceneData);
            }

            foreach(EdgeData edge in IncomingEdges)
            {
                edge.ReplaceSceneAtNodeGuid(m_Guid, SceneData);
            }
        }

        

        public string PrintNode()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Room Node guid: " + Guid + "\n");
            if (this.SceneData != null) 
            {
                sb.Append("Scene: " + this.SceneData.Name + "\n");
            }
            else
            {
                sb.Append("No Scene Data \n");
            }

            foreach (EdgeData edgeData in OutgoingEdges)
            {
                sb.Append("Exit: " + edgeData.SourcePortName + ", connects to ");
                sb.Append(edgeData.DestinationPortName + ", in Node: " + edgeData.DestinationNodeGuid);
                sb.Append("\n");
            } 

            return sb.ToString();
        }

        public List<EdgeData> GetAllEdgesAsOutgoing()
        {
            List<EdgeData> results = new();

            results.AddRange(OutgoingEdges);

            foreach(EdgeData edge in IncomingEdges)
            {
                results.Add(EdgeData.Invert(edge));
            }

            return results;
        }
    }
   

}