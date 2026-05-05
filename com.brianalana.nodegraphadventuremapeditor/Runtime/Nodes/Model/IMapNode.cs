using System.Collections.Generic;
using UnityEngine;

namespace NGAME
{
    public interface IMapNode
    {
        public void AddEdge(IMapNode otherNode, EdgeData edge);
        public void RemoveEdge(IMapNode otherNode, EdgeData edge);
        public List<EdgeData> GetOutgoingEdges();
        public List<EdgeData> GetIncomingEdges();

        public void AddWave(SOWaveData wave);
        public void RemoveWave(int index);
        public void PatchWaveData(SOWaveData wave, int index);
        public List<SOWaveData> GetWaveData();

        [HideInInspector] public Vector2 Position { get; set; }
        [HideInInspector] public string Guid { get; set; }

        public string PrintNode();
    }


}
