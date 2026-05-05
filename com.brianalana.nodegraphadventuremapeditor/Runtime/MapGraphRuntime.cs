using System.Collections.Generic;
using System.Linq;
using System.Text;



using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NGAME
{
    public class MapGraphRuntime : MonoBehaviour
    {
        //public UnityEvent RoomLoadStart;
        //public UnityEvent<IEncounterRegionConnector> RoomLoadComplete;
        public UnityEvent<MapGraphRuntime> PlaymodeStartedFromGraph;
        private bool m_PlaymodeFromGraphInvoked = false;
        //[Header("Room Load Effects")]
        //public CircleWipeControler CircleWipe;

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

        private void Start()
        {
            StringBuilder sb = new StringBuilder();

            if (PrintDebugLogs)
            {
                m_Graph.PrintGraph();
            }

            if(m_CurrentRoom == null)
                m_CurrentRoom = m_Graph.rootNode;

            if (!EnableNavigation)
            {
                return;
            }

            //SceneManager.sceneLoaded += OnSceneLoaded;
            //if( m_CurrentRoom.SceneData!= null && m_CurrentRoom.SceneData.SceneName != SceneManager.GetActiveScene().name)
            //{
            //    StartCoroutine(LoadAfterSeconds(2, m_CurrentRoom.SceneData.SceneName));
            //}
            //else
            //{
            //    //m_CurrentRoomExits = m_CurrentRoom.GetAllEdgesAsOutgoing();
            //    //InitConnectorsAndSpawners(SceneManager.GetActiveScene());
            //    OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            //}
        }

        public RoomNode TryEnterFirstRoom()
        {
            if(m_Graph == null)
            {
                return null;
            }
            m_CurrentRoom = m_Graph.rootNode;
            return m_Graph.rootNode;
        }

        public RoomNode GetRoomByGuid(string guid)
        {
            return m_Graph.GetNodeByGuid(guid) as RoomNode;
        }

        //private IEnumerator LoadAfterSeconds(float seconds, string sceneName)
        //{
        //    if (RoomLoadStart != null) RoomLoadStart.Invoke();
        //    yield return new WaitForSeconds(seconds);

        //    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        //}

        //protected void LoadScene(string sceneName)
        //{
        //    //if (CircleWipe != null)
        //    //{
        //    //    StartCoroutine(LoadAfterSeconds(CircleWipe.FadeSeconds));
        //    //}
        //    //else
        //    //{
        //    //    if (RoomLoadStart != null) RoomLoadStart.Invoke();
        //    //    RuntimeManager.PlayOneShot("event:/sfx/RoomTransition");
        //    //    SceneManager.LoadScene(_nextRoom.SceneName, LoadSceneMode.Single);
        //    //}

        //    if (RoomLoadStart != null) RoomLoadStart.Invoke();
        //    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        //}

        //this completes initialization of current room data 
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
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

        //intended to be called BEFORE a room is entered a second time (currently visit is incremented on scene load)
        public bool IsRoomBacktracking(string roomNodeGuid) 
        {
            if (m_RoomGuidVisitCounts.ContainsKey(roomNodeGuid))
            {
                return m_RoomGuidVisitCounts[roomNodeGuid] >= 1;
            }
            return false;
        }

        //intended to be called AFTER a room is entered and the visit count has been incremented
        public bool IsCurrentRoomBacktracking()
        {
            if (m_RoomGuidVisitCounts.ContainsKey(m_CurrentRoom.Guid))
            {
                return m_RoomGuidVisitCounts[m_CurrentRoom.Guid] > 1;
            }
            return false;
        }
    }
}
