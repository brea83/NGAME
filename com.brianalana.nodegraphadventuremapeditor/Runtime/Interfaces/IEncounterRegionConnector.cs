using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NGAME
{
    /// <summary>
    /// Interface the graph looks for on game objects in a scene, 
    /// the graph then uses their data to create ports on nodes associated with the scene
    /// and also to place representations of these connections within the preview image on a node
    /// </summary>
    public interface IEncounterRegionConnector
    {
        /// <summary>
        /// Returns RegionConnectionData. Could be used to return defaults or current data depending on implementation.
        /// </summary>
        /// <returns></returns>
        public RegionConnectionData GetRegionConnectionData();
        /// <summary>
        /// Intended for setting or overriding destination information.
        /// </summary>
        /// <param name="edge"> edge data where the source matches the calling IEncounterRegionConnector and the destination is another</param>
        public void SetDestination(EdgeData edge);
        /// <summary>
        /// This event getter is a work around 
        /// for the fact that interfaces cannot actually require the event directly
        /// </summary>
        public UnityEvent<EdgeData> ConnectorActivated { get; }
        /// <summary>
        /// Intended to allow the graph to overwrite some or all of the connection data 
        /// when a room is loaded via graph traversal. It also provides destination information
        /// reflecting what other part of the graph this connector is connected to.
        /// </summary>
        /// <param name="connectionData">connection data overrides from the graph.</param> 
        /// <param name="edge">data about the connection between this connector and another node in the graph.</param> 
        public void InitializeFromGraphData(RegionConnectionData connectionData, EdgeData edge);
    }
}
