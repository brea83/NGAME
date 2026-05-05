using System.Collections.Generic;
using UnityEngine;

namespace NGAME
{
    public class RootNode : ScriptableObject//, IMapNode
    {
        //public properties, getters, setters
        public Vector2 Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string Guid { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }


        // private properties, getters, setters

        // public methods

        public void AddEdge(IMapNode otherNode, EdgeData edge)
        {
            throw new System.NotImplementedException();
        }

        public List<EdgeData> OutgoingEdges
        {
            get => new List<EdgeData>();
        }

        public void RemoveEdge(IMapNode otherNode, EdgeData edge)
        {
            throw new System.NotImplementedException();
        }

        // private methods
    }
}
