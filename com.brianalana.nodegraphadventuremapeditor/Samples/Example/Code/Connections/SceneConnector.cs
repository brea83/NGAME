using UnityEngine;
using NGAME;
using UnityEngine.Events;

namespace NGAME.Example
{
    /// <summary>
    /// Example of a Connector that the nodegraph adventure map editor can read from. 
    /// This connector is intended be a generic that could be either an entrance point
    /// or an exit which triggers the loading of the next room.
    /// </summary>
    public class SceneConnector : MonoBehaviour, IEncounterRegionConnector
    {
        /// <summary>
        /// Event called when the SceneConnector is activated by the player
        /// </summary>
        public UnityEvent<EdgeData> Activated;
        /// <summary>
        /// The ConnectorActivated event getter in IEncounterRegionConnector is a work around 
        /// for the fact that interfaces cannot actually require the event directly
        /// </summary>
        public UnityEvent<EdgeData> ConnectorActivated => Activated;

        [SerializeField]
        private RegionConnectionData m_data = new();

        private string m_DestinationScene;
        private string m_ConnectorInDestination;
        private void Awake()
        {
            m_data.TypeName = this.GetType().Name;
            m_data.Name = name;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public RegionConnectionData GetRegionConnectionData()
        {
            m_data.TypeName = this.GetType().Name;
            m_data.Name = name;
            m_data.Position = transform.position;
            return m_data;
        }

        public void InitializeFromGraphData(RegionConnectionData connectionData, EdgeData edge)
        {
            m_data.ConnectionType = connectionData.ConnectionType;
            m_data.EntranceConditions = connectionData.EntranceConditions;
            SetDestination(edge);
        }

        public void SetDestination(EdgeData edge)
        {
            m_DestinationScene = edge.DestinationSceneName;
            m_ConnectorInDestination = edge.DestinationPortName;
        }
    }

}
