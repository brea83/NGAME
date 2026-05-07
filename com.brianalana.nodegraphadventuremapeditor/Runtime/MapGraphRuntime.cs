using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NGAME
{
    /// <summary>
    /// This class handles traversing the graph data at runtime, and keep track of the current RoomNode. 
    /// It can accept requests to go to specific nodes, 
    /// or to follow specific connections between nodes via EdgeData.
    /// It is also set up to handle those requests coming from the graph in editor mode.
    /// </summary>
    public class MapGraphRuntime : MonoBehaviour
    {
        
        public UnityEvent<MapGraphRuntime> PlaymodeStartedFromGraph;
        private bool m_PlaymodeFromGraphInvoked = false;

        public RoomNode CurrentRoom {get => m_CurrentRoom;}
        public EdgeData LastTraversedEdge { get => m_MostRecentlyTraversedEdge;}
        public List<ISpawnPoint> CurrentSpawnPoints { get => m_CurrentSpawnPoints;}
        public List<IEncounterRegionConnector> CurrentConnectors { get => m_CurrentConnectors;}
        public List<EdgeData> CurrentExits { get => m_CurrentRoomExits;}

        [Header("Debug stuff")]
        public bool EnableNavigation = true;
        public bool PrintDebugLogs = false;

        [SerializeField]
        protected RoomGraph m_Graph;
        protected RoomNode m_CurrentRoom;
        protected EdgeData m_MostRecentlyTraversedEdge = null;
        protected List<EdgeData> m_CurrentRoomExits = new();
        protected List<IEncounterRegionConnector> m_CurrentConnectors = new();

        protected List<ISpawnPoint> m_CurrentSpawnPoints = new();

        protected Dictionary<string, int> m_RoomGuidVisitCounts = new();

        /// <summary>
        /// If EnableNavigation is true we initialize the current room 
        /// to be the root node of the graph, otherwise we set the current
        /// room to be null so that any navigation system using this graph 
        /// if the current room has already been set we do not reset it to 
        /// the roon node. This is also when optional debug print of the graph 
        /// happens if PrintDebugLogs is enabled. 
        /// </summary>
        private void Start()
        {
            StringBuilder sb = new StringBuilder();

            if (PrintDebugLogs)
            {
                m_Graph.PrintGraph();
            }

            if (!EnableNavigation)
            {
                m_CurrentRoom = null;
                return;
            }

            if(m_CurrentRoom == null)
                m_CurrentRoom = m_Graph.rootNode;
        }

        /// <summary>
        /// Allows a navigation system to go directly to the root node of the graph. 
        /// This could be used for restarting runs, or if you need to initialize the graph 
        /// before Start() is called.
        /// </summary>
        /// <returns> RoomNode: the root roomNode data, or null if there is no graph.</returns>
        public RoomNode TryEnterFirstRoom()
        {
            if(m_Graph == null)
            {
                return null;
            }
            m_CurrentRoom = m_Graph.rootNode;
            return m_Graph.rootNode;
        }
        /// <summary>
        /// Find RoomNode data by the node's guid. Typically used to check 
        /// information about the rooms connected to the current room.
        /// </summary>
        /// <param name="guid"> The guid of the room to search for</param>
        /// <returns>RoomNode</returns>
        public RoomNode GetRoomByGuid(string guid)
        {
            return m_Graph.GetNodeByGuid(guid) as RoomNode;
        }

        
        /// <summary>
        /// Completes the initialization of a room. It sets up data
        /// for the room's exits, and spawners, and increments the visit count.
        /// </summary>
        /// <param name="scene"> the scene which just loaded</param>
        /// <param name="mode"> LoadSceneMode is not actually usedin this function, 
        /// but required for event subscription</param>
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (m_CurrentRoom == null || m_CurrentRoom.SceneData == null 
                || scene.name != m_CurrentRoom.SceneData.name)
                return;
                
            m_CurrentRoomExits = m_CurrentRoom.GetAllEdgesAsOutgoing();
            InitConnectorsAndSpawners(scene);
            if (!m_RoomGuidVisitCounts.ContainsKey(m_CurrentRoom.Guid))
            {
                m_RoomGuidVisitCounts.Add(m_CurrentRoom.Guid, 1);
            }
            else
            {
                m_RoomGuidVisitCounts[m_CurrentRoom.Guid] ++;
            }

        }

        /// <summary>
        /// Clears the CurrentConntectors and CurrentSpawnPoints lists of the previous rooms components
        /// and loads them with the current room's components.
        /// </summary>
        /// <param name="loadedScene">The room's scene that is being initialized</param>
        protected void InitConnectorsAndSpawners(Scene loadedScene)
        {
            m_CurrentConnectors.Clear();
            m_CurrentSpawnPoints.Clear();
            if (!loadedScene.IsValid())
            {
                Debug.Log("MapGraph Runtime could not find current active scene ");
                return;
            }

            GameObject[] rootObjects = loadedScene.GetRootGameObjects();

            foreach (GameObject obj in rootObjects)
            {
                // get  connections
                IEncounterRegionConnector[] components = obj.GetComponentsInChildren<IEncounterRegionConnector>();

                if (components.Length > 0)
                {
                    m_CurrentConnectors.AddRange(components);
                }

                // get spawners

                ISpawnPoint[] spawnComponents = obj.GetComponentsInChildren<ISpawnPoint>();

                if (spawnComponents.Length > 0)
                {
                    m_CurrentSpawnPoints.AddRange(spawnComponents.ToList());
                }

            }
        }

        /// <summary>
        /// Intended to be called from a navigation system to begin the transition 
        /// to a new roomNode.
        /// </summary>
        /// <param name="edge"> data about the connection we wish to traverse, 
        /// technically only the destination portion of the data is necessary</param>
        /// <returns></returns>
        public bool TryEnterRoom(EdgeData edge)
        {
            if (PrintDebugLogs) Debug.Log("MapGraphRuntime is trying to enter a room");

            RoomNode nextRoom = m_Graph.GetNodeByGuid(edge.DestinationNodeGuid) as RoomNode;

            if (nextRoom != null)
            {
                m_CurrentRoom = nextRoom;
                m_MostRecentlyTraversedEdge = edge;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public bool TryEnterRoomFromGraph(EdgeData edge, RoomGraph graph)
        {
            if (graph == null || m_PlaymodeFromGraphInvoked)
                return false;

            RoomGraph oldGraph = m_Graph;
            m_Graph = graph;

            if (TryEnterRoom(edge) && PlaymodeStartedFromGraph != null)
            {
                m_PlaymodeFromGraphInvoked = true;
                PlaymodeStartedFromGraph.Invoke(this);
                return true;
            }
            m_Graph = oldGraph;
            return false;
        }
#endif

        /// <summary>
        /// Checks if a room has been visited before. Will return false if 
        /// TryEnterRoom has returned true but the scene it is associated 
        /// with has not completed loading, because the visit count is 
        /// incremented durring the OnSceneLoaded function.
        /// </summary>
        /// <param name="roomNodeGuid"> the guid for a RoomNode </param>
        /// <returns></returns>
        public bool IsRoomBacktracking(string roomNodeGuid) 
        {
            if (m_RoomGuidVisitCounts.ContainsKey(roomNodeGuid))
            {
                if(m_CurrentRoom.Guid == roomNodeGuid)
                {
                    return m_RoomGuidVisitCounts[roomNodeGuid] > 1;
                }
                return m_RoomGuidVisitCounts[roomNodeGuid] >= 1;
            }
            return false;
        }

    }
}
