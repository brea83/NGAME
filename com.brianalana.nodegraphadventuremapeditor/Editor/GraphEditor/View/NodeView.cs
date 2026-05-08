using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public RoomGraphView m_RoomGraphView;
        public Action<NodeView> OnNodeSelected;
        public Action<NodeView> OnNodeValuesChanged;
        public Action<EdgeData> RequestPlayMode;
        public Action<SceneData> RequestEditScene;
        public RoomNode Node;
        public List<ConnectionPort> InputPorts = new();
        public List<ConnectionPort> OutputPorts = new();
        public List<ConnectionPort> OldConnectedPorts = new();
        //public List<ConnectionView> Connections = new();

        private static int m_InstanceCounter = 0;
        private int m_MyInstanceNumber;

        private Button EditSceneButton;
        private DropdownField m_RoomSelectDropdown;
        private int m_LastDropDownIndex = 0;
        public string CurrentSceneGuid { get; private set; }
        //private List<SceneConnectionsData> _roomDataObjects;
        private Color m_ValidPortColor = new();

        // container that input and output containers are in is called  topContainer on the parent class

        public SceneData CurrentSceneData { get => m_CurrentSceneData; }
        private SceneData m_CurrentSceneData;
        public SceneSpawnData CurrentSpawnData { get => m_CurrentSceneSpawnData; }
        private SceneSpawnData m_CurrentSceneSpawnData;
        private VisualElement m_EncountersContainer;
        private VisualElement m_SpawnerDataContainer;

        private VisualElement m_WavesContainer;
        private List<VisualElement> m_WaveItems = new List<VisualElement>();

        private RegionPreview m_RegionPreview;
        private EdgeData m_PlaymodeEntranceRequest = null;
        
        public NodeView(RoomGraphView graph, RoomNode node, List<SceneConnectionsData> roomDataObjects = null) 
        {
            m_MyInstanceNumber = ++m_InstanceCounter;
            this.m_RoomGraphView = graph;
            this.Node = node;
            CurrentSceneGuid = Node.SceneData != null ? Node.SceneData.Guid : "";
            this.title = node.name;
            this.viewDataKey = node.Guid;

            style.left = node.Position.x;
            style.top = node.Position.y;
            style.flexGrow = 1;
            
            //_roomDataObjects = roomDataObjects;

            m_RegionPreview = new RegionPreview(this);
            
            inputContainer.parent.Add(m_RegionPreview.Container);
            outputContainer.BringToFront();
            //m_RegionPreview.OnImageDrawn += OnPreviewDrawn;
            m_RegionPreview.OnImageDrawn += OnPreviewDrawn_LabelsOnly;
            //m_RegionPreview.OnImageDrawn += OnPreviewDrawn_CreatePorts;

            EditSceneButton = new Button()
            {
                name = "EditSceneButton",
                text = "Edit Scene",
            };
            EditSceneButton.clicked += OnEditSceneButtonClicked;
            titleButtonContainer.Add(EditSceneButton);
            

            if (roomDataObjects != null )
            {
                CreateRoomSelector( roomDataObjects );
            }

            if(Node.SceneData != null && !string.IsNullOrEmpty(Node.SceneData.Guid))
            {
                m_RoomGraphView.SceneLookup.TryGetValue(Node.SceneData.Guid, out m_CurrentSceneData);
            }

            Label entranceLabel = new();
            entranceLabel.text = "Entrances";
            inputContainer.Add( entranceLabel );

            Label exitLabel = new();
            exitLabel.text = "Exits";
            outputContainer.Add( exitLabel );


            m_EncountersContainer = new VisualElement();
            m_EncountersContainer.style.minHeight = 50;
            m_EncountersContainer.AddToClassList("nodeExtension");

            Label encountersLabel = new();
            encountersLabel.text = "Encounters";
            encountersLabel.AddToClassList("header2");
            m_EncountersContainer.Add(encountersLabel);

            Foldout spawnDataLabel = new();
            spawnDataLabel.text = "Count of Spawners by Allowed Type";
            spawnDataLabel.AddToClassList("header3");
            spawnDataLabel.value = true;
            spawnDataLabel.RegisterValueChangedCallback(bValue =>
            {
                RefreshExpandedState();
            });
            m_EncountersContainer.Add(spawnDataLabel);


            m_SpawnerDataContainer = new();
            spawnDataLabel.Add(m_SpawnerDataContainer);

            m_WavesContainer = new VisualElement();
            m_WavesContainer.AddToClassList("header3");
            VisualElement headerPanel = new();
            headerPanel.style.flexDirection = FlexDirection.Row;
            //headerPanel.AddToClassList("header3");

            Label wavesLabel = new();
            wavesLabel.text = "Waves";
            wavesLabel.AddToClassList("header3");

            Button addWaveButton = new();
            addWaveButton.text = "+";
            addWaveButton.clicked += AddWave;

            headerPanel.Add(wavesLabel);
            headerPanel.Add(addWaveButton);
            m_WavesContainer.Add(headerPanel);
            m_EncountersContainer.Add(m_WavesContainer);


            extensionContainer.Add(m_EncountersContainer);
            //extensionContainer.style.flexGrow = 1.0f;
            

            CreateInputPorts();
            CreateOutputPorts();

            UpdateCurrentSceneSpawnData();
            PopulateEncounterContainer();
            RefreshExpandedState();

            //RegisterCallback<AttachToPanelEvent>(evt =>
            //{
            //    Debug.Log($"I am NodeView {m_MyInstanceNumber} and I " +
            //        $"just got attached to panel '{evt.destinationPanel.visualTree.name}'");
            //});
            //RegisterCallback<DetachFromPanelEvent>(evt =>
            //{
            //    Debug.Log($"I am NodeView {m_MyInstanceNumber} and I " +
            //        $"just got detached from panel '{evt.originPanel.visualTree.name}'");
            //});
        }

        
        private void UpdateCurrentSceneSpawnData()
        {
            if(Node.SceneData == null)
            {
                m_CurrentSceneSpawnData = null;
                return;
            }
            m_CurrentSceneSpawnData = m_RoomGraphView.SpawnersByScene.FirstOrDefault((SceneSpawnData e) => e.SceneGUID == Node.SceneData.Guid);
        }

        private void OnEditSceneButtonClicked()
        {
            if (string.IsNullOrEmpty(CurrentSceneGuid))
            {
                Debug.Log("Node currently has no scene selected, so it cannot open a scene to edit");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(CurrentSceneGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Node currently has an invalid scene guid saved, so it cannot open a scene to edit");
                return;
            }

            if (RequestEditScene != null)
                RequestEditScene.Invoke(Node.SceneData);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if ( (Node.SceneData != null && !string.IsNullOrEmpty(Node.SceneData.Name))
                && evt.target is NodeView || evt.target is ConnectionPort)
            {
                evt.menu.AppendAction($"Open Scene ({Node.SceneData.Name}) to Edit", delegate
                {
                    OnEditSceneButtonClicked();
                }, (DropdownMenuAction a) => string.IsNullOrEmpty(CurrentSceneGuid) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            }

            if(evt.target is NodeView)
            {
                evt.menu.AppendAction($"Start PlayMode from {title}", delegate
                {
                    TryPlayFromNode();
                }, (DropdownMenuAction a) => string.IsNullOrEmpty(CurrentSceneGuid) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }
            base.BuildContextualMenu(evt);
        }
        internal void TryPlayFromNode()
        {
            EdgeData edgeData = new();
            edgeData.DestinationNodeGuid = Node.Guid;
            edgeData.DestinationSceneGuid = CurrentSceneGuid;
            edgeData.DestinationSceneName = Node.SceneData.Name;

            edgeData.DestinationPortName = InputPorts.Count > 0 ? InputPorts.First().portName : "";

            m_PlaymodeEntranceRequest = edgeData;
            if(RequestPlayMode != null) 
                RequestPlayMode.Invoke(m_PlaymodeEntranceRequest);
        }
        internal void TryPlayFromPort(ConnectionPort port)
        {
            if (port == null) return;
            //Debug.LogWarning($"Trying to play from port: {port.portName},  in Scene: {Node.SceneData.SceneName}, from Node: {Node.name}.");

            m_PlaymodeEntranceRequest = CreateEdgeDataFromPort(port, Direction.Input, true);
            if (RequestPlayMode != null)
                RequestPlayMode.Invoke(m_PlaymodeEntranceRequest);

            //Debug.Log("NodeView after RequestPlaymode called, but before leaving try play from port function");
        }
        

        private void CreateRoomSelector(List<NGAME.SceneConnectionsData> roomDataObjects)
        {
            bool bEditSceneButtonNeedsEnable = false;
            List<string> choices = new List<string>();
            choices.Add("None Selected");

            int defaultIndex = 0;
            for (int i = 0; i < roomDataObjects.Count(); i++ )
            {
                SceneConnectionsData room = roomDataObjects[i];

                choices.Add(room.SceneName);

                if(Node.SceneData != null && Node.SceneData.Name == room.SceneName)
                {
                    defaultIndex = i + 1; // plus 1 because we have the default none at index 0 of the list before this loop starts
                    bEditSceneButtonNeedsEnable = true;
                    CurrentSceneGuid = Node.SceneData.Guid;
                }
            }

            EditSceneButton.SetEnabled(bEditSceneButtonNeedsEnable);

            m_RoomSelectDropdown = new DropdownField(choices, defaultIndex);
            titleButtonContainer.Add(m_RoomSelectDropdown);
            titleButtonContainer.style.flexShrink = 0;
            titleButtonContainer.style.flexGrow = 0;

            m_RoomSelectDropdown.RegisterValueChangedCallback(OnSceneDropdownChanged);
            m_RoomSelectDropdown.SendToBack();
            EditSceneButton.SendToBack();
        }
        
        private void PopulateEncounterContainer()
        {
            m_SpawnerDataContainer.Clear();
            if(Node.SceneData == null)
            {
                return;
            }

            //Validate Connections?

            if(m_CurrentSceneSpawnData != null)
            {

                Dictionary<string, int> spawnerCountLookup = m_CurrentSceneSpawnData.CountSpawnersWithMatchingTypes();
                foreach(string spawnerType in spawnerCountLookup.Keys)
                {
                    int spawnerCount = spawnerCountLookup[spawnerType];
                    Label label = new Label();
                    label.AddToClassList("ListItem");
                    label.text = spawnerType + ": " + spawnerCount.ToString();
                    m_SpawnerDataContainer.Add(label);
                }
            }

            if(Node.Waves.Count != m_WaveItems.Count)
            {
                foreach(VisualElement row in m_WaveItems)
                {
                    row.RemoveFromHierarchy();
                }
                m_WaveItems.Clear();

                for(int i = 0; i < Node.Waves.Count; i++)
                {
                    CreateWaveItem(Node.Waves[i], i);
                }
            }
        }


        private void OnSceneDropdownChanged(ChangeEvent<string> change) 
        {
            if (change.newValue == change.previousValue) return;

            m_LastDropDownIndex = m_RoomSelectDropdown.index;
            string oldGuid = CurrentSceneGuid;
            SceneData selectedRoom = null;
            foreach (SceneConnectionsData room in m_RoomGraphView.ValidScenes )
            {
                if( room.SceneName == change.newValue )
                {
                    CurrentSceneGuid = room.SceneGuid;
                    m_RoomGraphView.SceneLookup.TryGetValue(room.SceneGuid, out selectedRoom);
                    m_CurrentSceneData = selectedRoom.DeepCopy();
                    EditSceneButton.SetEnabled(true);
                    break;
                }
            }

            Undo.RecordObject(Node, "NGAME - Scene Change on " + Node.name);
            Node.UpdateRoomData(selectedRoom);
            if (selectedRoom == null)
            {
                CurrentSceneGuid = "";
                EditSceneButton.SetEnabled(false);
                m_CurrentSceneData = null;
            }
            m_RoomGraphView.UpdateSceneDataRefrenceCounts(oldGuid, selectedRoom);
            
            UpdatePorts();
            MarkMissingSceneError("", false);

            UpdateCurrentSceneSpawnData();
            PopulateEncounterContainer();
            RefreshExpandedState();

            m_RegionPreview.UpdateBounds();
            EditorUtility.SetDirty(Node);
            if(OnNodeValuesChanged != null)
                OnNodeValuesChanged.Invoke(this);
        }

        private void UpdatePorts()
        {

            List<string> EntranceNames;
            List<string> ExitNames;
            //List<string> WaveNames = new();
            if (Node.SceneData == null)
            {
                EntranceNames = new();
                ExitNames = new();
            }
            else
            {
                EntranceNames = Node.SceneData.Entrances.ConvertAll(entrance => entrance.Name);
                ExitNames = Node.SceneData.Exits.ConvertAll(entrance => entrance.Name);
            }
            
            TryReconnectOldEdges(EntranceNames, Direction.Input);
            TryReconnectOldEdges(ExitNames, Direction.Output);

            RemoveExcessPorts(InputPorts, inputContainer, EntranceNames);
            AddMissingPorts(InputPorts, inputContainer, EntranceNames);

            RemoveExcessPorts(OutputPorts, outputContainer, ExitNames);
            AddMissingPorts(OutputPorts, outputContainer, ExitNames, false);

            foreach(Port port in InputPorts)
            {
                SetUsedPortsOtherDirectionEnabled(port, !port.connected);
            }
            foreach(Port port in OutputPorts)
            {
                SetUsedPortsOtherDirectionEnabled(port, !port.connected);
            }
        }

        private void TryReconnectOldEdges(List<string> newPortNames, Direction portDirection)
        {
            List<int> indexesToRemove = new();

            for (int i = 0; i < OldConnectedPorts.Count; i++)
            {
                ConnectionPort oldPort = OldConnectedPorts[i];
                if(oldPort.direction != portDirection)
                {
                    continue;
                }
                if (newPortNames.Contains(oldPort.portName))
                {
                    if(oldPort.direction == Direction.Input)
                    {
                        InputPorts.Add(oldPort);
                    }
                    else
                    {
                        OutputPorts.Add(oldPort);
                    }
                        //portCollection.Add(oldPort);
                        Edge oldEdge = null;
                    if (oldPort.connections.Count() >= 1 )
                    {
                        oldEdge = oldPort.connections.First();
                        MarkPortConnectionError(oldEdge.output as ConnectionPort, oldEdge, "", false);
                        MarkPortConnectionError(oldEdge.input as ConnectionPort, oldEdge, "", false);
                    }
                    MarkPortConnectionError(oldPort, oldEdge, "", false);
                    indexesToRemove.Add(i);
                }
            }

            indexesToRemove.Sort();

            for (int i = indexesToRemove.Count -1; i >= 0; i--)
            {
                OldConnectedPorts.RemoveAt(indexesToRemove[i]);
            }
        }

        private void AddMissingPorts(List<ConnectionPort> oldPorts, VisualElement portContainer, List<string> newPortNames, bool isInputPort = true)
        {
            List<string> missingPorts = GetMissingPortNames(oldPorts, newPortNames);

            foreach(string name in missingPorts)
            {
                if (isInputPort)
                {
                    CreatePort(oldPorts, portContainer, name, typeof(bool));
                }
                else
                {
                    CreatePort(oldPorts, portContainer, name, typeof (bool), Direction.Output, Port.Capacity.Single);
                }
            }
        }
        private void RemoveExcessPorts(List<ConnectionPort> oldPorts, VisualElement portContainer, List<string> newPortNames)
        {
            List<ConnectionPort> excessPorts = GetExcessPorts(oldPorts, newPortNames);
            
            foreach(ConnectionPort port in excessPorts )
            {

                bool bIsRetained = false;
                if (portContainer != m_WavesContainer)
                {
                    bIsRetained = TryRetainConnectedPorts(port, oldPorts);
                }

                if (!bIsRetained)
                {
                    oldPorts.Remove(port);
                    port.RemoveFromHierarchy();
                }
            }
        }

        private List<string> GetMissingPortNames(List<ConnectionPort> ports, List<string> portNames)
        {
            List<string> missingPortNames = new List<string>();
            List<string> existingPortNames = new List<string>();
            foreach (ConnectionPort port in ports)
            {
                existingPortNames.Add(port.portName);
            }

            foreach (string portName in portNames)
            {
                if (!existingPortNames.Contains(portName))
                {
                    missingPortNames.Add(portName);
                }
            }

            return missingPortNames;
        }

        private List<ConnectionPort> GetExcessPorts(List<ConnectionPort> ports, List<string> newPortNames)
        {
            List<ConnectionPort> excessPorts = new();

            foreach (ConnectionPort port in ports)
            {
                if (!newPortNames.Contains(port.portName))
                {
                    excessPorts.Add(port);
                }
            }

            return excessPorts;
        }
        private void CreateOutputPorts()
        {
            if (Node.SceneData == null || Node.SceneData.Guid == null) return;
            
            foreach(var exit in Node.SceneData.Exits)
            {
                ConnectionPort output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool)) as ConnectionPort;
                if(output != null)
                {
                    output.portName = exit.Name;
                    OutputPorts.Add(output);
                    outputContainer.Add(output);
                }
            }
        }

        private void CreateInputPorts()
        {
            if (Node.SceneData == null || Node.SceneData.Guid == null) return;
           
            foreach (var entrance in Node.SceneData.Entrances)
            {
                ConnectionPort newPort = CreatePort(InputPorts, /*contentContainer*/ inputContainer, entrance.Name, typeof(bool));
            }
        }

        //protected void OnPreviewDrawn_CreatePorts(VisualElement imageContainer)
        //{
        //    if (Node.SceneData == null)
        //    {
        //        return;
        //    }

        //    string guid = Node.SceneData.SceneGuid;
        //    if (!m_RoomGraphView.SceneLookup.ContainsKey(guid))
        //    {
        //        return;
        //    }
        //    float imageStyleWidth = imageContainer.style.width.value.value;
        //    float imageStyleHeight = imageContainer.style.height.value.value;
        //    Vector2 imageSize = new Vector2(imageStyleWidth, imageStyleHeight);

        //    SceneData data = m_RoomGraphView.SceneLookup[guid];
        //    Vector2 swizzledWorldMin = new Vector2(data.Bounds.MinPoint.x, data.Bounds.MaxPoint.y);
        //    Vector2 swizzledWorldMax = new Vector2(data.Bounds.MaxPoint.x, data.Bounds.MinPoint.y);

        //    List<RegionConnectionData> connectionData = data.UniqueConnectionObjects;
        //    Connections.ForEach((ConnectionView c) => { c.Container.RemoveFromHierarchy(); });
        //    Connections.Clear();

        //    foreach (var connection in connectionData)
        //    {
        //        Vector2 position = new Vector2(connection.Position.x, connection.Position.z);

        //        //this will need to be modified with style font sizes
        //        Vector2 offset = new Vector2(connection.Name.Length * -3.0f, -15.0f);
        //        Vector2 relativePosition = ScenePreviewRenderer.RemapVector2(position, swizzledWorldMin, swizzledWorldMax, Vector2.zero, imageSize);

        //        ConnectionView connectionView = new(connection, relativePosition, imageSize, this);
        //        imageContainer.Add(connectionView.Container);
        //        Connections.Add(connectionView);

        //        if (connectionView.Entrance != null)
        //        {
        //            InputPorts.Add(connectionView.Entrance);
        //        }

        //        if(connectionView.Exit != null)
        //        {
        //            OutputPorts.Add(connectionView.Exit);
        //        }
        //    }
        //}
        private void OnPreviewDrawn_LabelsOnly(VisualElement imageContainer)
        {
            if (Node.SceneData == null)
            {
                return;
            }

            string guid = Node.SceneData.Guid;
            if (!m_RoomGraphView.SceneLookup.ContainsKey(guid))
            {
                return;
            }
            float imageStyleWidth = imageContainer.style.width.value.value;
            float imageStyleHeight = imageContainer.style.height.value.value;
            Vector2 imageSize = new Vector2(imageStyleWidth, imageStyleHeight);

            SceneData data = m_RoomGraphView.SceneLookup[guid];
            Vector2 swizzledWorldMin = new Vector2(data.Bounds.MinPoint.x, data.Bounds.MaxPoint.y);
            Vector2 swizzledWorldMax = new Vector2(data.Bounds.MaxPoint.x, data.Bounds.MinPoint.y);

            List<RegionConnectionData> connectionData = data.UniqueConnectionObjects;

            foreach (var connection in connectionData)
            {
                Vector2 position = new Vector2(connection.Position.x, connection.Position.z);
                
                //this will need to be modified with style font sizes
                Vector2 offset = new Vector2(connection.Name.Length * -3.0f, -15.0f);
                Vector2 relativePosition = ScenePreviewRenderer.RemapVector2(position, swizzledWorldMin, swizzledWorldMax, Vector2.zero, imageSize);

                Label connectionLabel = new();
                connectionLabel.text = connection.Name;
                
                connectionLabel.style.position = Position.Absolute;
                connectionLabel.style.left = relativePosition.x + offset.x;
                connectionLabel.style.top = relativePosition.y + offset.y;
                connectionLabel.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);

                imageContainer.Add(connectionLabel);
            }
        }

        private void OnPreviewDrawn(VisualElement imageContainer)
        {
            if(Node.SceneData == null)
            {
                return;
            }

            string guid = Node.SceneData.Guid;
            if (!m_RoomGraphView.SceneLookup.ContainsKey(guid))
            {
                return;
            }
            float imageStyleWidth = imageContainer.style.width.value.value;
            float imageStyleHeight = imageContainer.style.height.value.value;
            Vector2 imageSize = new Vector2(imageStyleWidth, imageStyleHeight);

            SceneData data = m_RoomGraphView.SceneLookup[guid];
            Vector2 swizzledWorldMin = new Vector2(data.Bounds.MinPoint.x, data.Bounds.MaxPoint.y);
            Vector2 swizzledWorldMax = new Vector2(data.Bounds.MaxPoint.x, data.Bounds.MinPoint.y);
            
            List<RegionConnectionData> connectionData = data.UniqueConnectionObjects;

            foreach(var connection in connectionData)
            {
                string name = connection.Name;
                Port inputPort = null;
                Port outputPort = null;

                if(connection.ConnectionType == RegionConnectionType.EntranceOnly 
                    || connection.ConnectionType == RegionConnectionType.ExitAndEntrance)
                {
                    inputPort = GetPortByName(name, InputPorts);
                }

                if (connection.ConnectionType == RegionConnectionType.ExitOnly
                    || connection.ConnectionType == RegionConnectionType.ExitAndEntrance)
                {
                    outputPort = GetPortByName(name, OutputPorts);
                }


                if (inputPort == null && outputPort == null)
                {
                    continue;
                }

                Vector2 position = new Vector2(connection.Position.x, connection.Position.z);
                
                Vector2 relativePosition = ScenePreviewRenderer.RemapVector2(position, swizzledWorldMin, swizzledWorldMax, Vector2.zero, imageSize);

                SetPortPosition(inputPort, imageContainer, relativePosition);
                SetPortPosition(outputPort, imageContainer, relativePosition);

            }
        }

        private void SetPortPosition(Port port, VisualElement destinationContainer, Vector2 newPosition)
        {
            if (port == null)
            { 
                return; 
            }

            float offset = 10;
            if(port.direction == Direction.Input)
            {
                offset *= -1;
            }

             port.style.position = Position.Absolute;
            port.style.left = newPosition.x;
            port.style.top = newPosition.y + offset;

            VisualElement parentElement = port.parent;
            if(parentElement == destinationContainer)
            {
                return;
            }

            if (parentElement != null )
            {
                int indexInParent = parentElement.IndexOf(port);

                if (indexInParent >= 0 && indexInParent < parentElement.childCount)
                {
                    parentElement.RemoveAt(indexInParent);
                }
            }

            destinationContainer.Add(port);
            
        }

        private void CreateWaveItem(SOWaveData wave, int index) 
        { 
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.AddToClassList("ListItem");

            string waveName = "Wave " + m_WaveItems.Count.ToString();

            Foldout header = new Foldout();
            header.text = waveName;
            //header.contentContainer.style.flexDirection = FlexDirection.Row;

            ObjectField field = new ObjectField();
            field.objectType = typeof(SOWaveData);
            if(wave != null )
            {
                field.value = wave;
            }
            
            field.RegisterValueChangedCallback(
                evt => PatchWaveData(evt, index));

            field.dataSource = Node;
            PropertyPath path = new PropertyPath(nameof(Node.Waves) + "[" + index.ToString() + "]");
            DataBinding binding = new DataBinding
            {
                dataSourcePath = path
            };
            field.SetBinding("value", binding);

            Button removeMe = new Button();
            removeMe.style.flexGrow = 0;
            removeMe.text = "Remove Wave";
            removeMe.clicked += () =>
            {
                RemoveWave(row, index);
            };

            header.Add(removeMe);
            header.Add(field);

            row.Add(header);

            m_WavesContainer.Add(row);
            m_WaveItems.Add(row);
        }

        private void PatchWaveData(ChangeEvent<UnityEngine.Object> evt, int index)
        {
            OnValuesChanged();
        }
        private void AddWave()
        {
            SOWaveData wave = null;
            Node.AddWave(wave);
            CreateWaveItem(wave, Node.Waves.Count -1);
            OnValuesChanged();
        }

        private void RemoveWave(VisualElement waveItem, int waveIndex)
        {
            Node.RemoveWave(waveIndex);
            m_WaveItems.Remove(waveItem);
            waveItem.RemoveFromHierarchy();
            OnValuesChanged();
        }

        private ConnectionPort CreatePort(List<ConnectionPort> portList, VisualElement portContainer, string portName, System.Type passedDataType, 
            Direction flowDirection = Direction.Input, Port.Capacity portCapacity = Port.Capacity.Multi, 
            Orientation orientation = Orientation.Horizontal)
        {
            ConnectionPort newPort = InstantiatePort(orientation, flowDirection, portCapacity, passedDataType) as ConnectionPort;
            if (newPort != null)
            {
                //newPort.conn
                m_ValidPortColor = newPort.portColor;
                newPort.portName = portName;
                portList.Add(newPort);
                portContainer.Add(newPort);
            }
            return newPort;
        }

        public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type)
        {
            return ConnectionPort.Create<Edge>(orientation, direction, capacity, type);
        }

        protected bool TryRetainConnectedPorts(ConnectionPort port, List<ConnectionPort> portCollection)
        {
            if (port.connected)
            {
                string sourceTooltip = "Missing connection to port named " + port.portName;
                string destinationTooltip = "Connecetion named " + port.portName + ", doesn't exist in scene.";
                Port otherPort = null;
                Edge edge = null;
                List<Edge> edges = port.connections.ToList();
                foreach (Edge e in edges)
                {
                    edge = e;
                    otherPort = port.direction == Direction.Input ? e.output : e.input;
                    if(otherPort == null)
                    {
                        continue;
                    }
                }
                MarkPortConnectionError(otherPort as ConnectionPort, edge, sourceTooltip);
                MarkPortConnectionError(port, null, destinationTooltip);
                OldConnectedPorts.Add(port);
                portCollection.Remove(port);
                return true;
            }
            return false;
        }

        private void OnPortConnected(ConnectionPort port)
        {
            if(port != null)
            {
                MarkPortConnectionError(port, null, "", false);
                SetUsedPortsOtherDirectionEnabled(port, false);
            }
        }

        private static void SetUsedPortsOtherDirectionEnabled(Port port, bool value)
        {
            Port matchingPort = null;
            NodeView view = port.node as NodeView;
            if (port.direction == Direction.Input)
            {
                matchingPort = view.GetPortByName(port.portName, view.OutputPorts);
            }
            else
            {
                matchingPort = view.GetPortByName(port.portName, view.InputPorts);
            }

            if (matchingPort != null)
            {
                matchingPort.SetEnabled(value);
                matchingPort.tooltip = value ? matchingPort.tooltip : "";
            }
        }

        private void OnPortDisconnected(ConnectionPort port, Edge edge)
        {
            if(port != null)
            {
                MarkPortConnectionError(port, edge, "", false);
                SetUsedPortsOtherDirectionEnabled(port, true);
                
            }

            if (OldConnectedPorts.Contains(port))
            {
                OldConnectedPorts.Remove(port);
                port.RemoveFromHierarchy();
            }
        }

        public static void RemoveEdge(Edge edge)
        {
            NodeView sourceNode = edge.output.node as NodeView;
            if(sourceNode != null)
            {
                sourceNode.OnPortDisconnected(edge.output as ConnectionPort, edge);
            }

            NodeView destinationNode = edge.input.node as NodeView;
            if(destinationNode != null)
            {
                destinationNode.OnPortDisconnected(edge.input as ConnectionPort, edge);
            }

            // runtime node updates
             
            if(sourceNode != null && destinationNode != null)
            {
                EdgeData newEdgeData = CreateEdgeDataFromEdge(edge);

                Undo.IncrementCurrentGroup();
                Undo.RecordObjects(new UnityEngine.Object[]{ sourceNode.Node, destinationNode.Node}, "NGAME - disconnect nodes");
                sourceNode.Node.RemoveEdge(destinationNode.Node, newEdgeData);
                destinationNode.Node.RemoveEdge(sourceNode.Node, newEdgeData);

                if (sourceNode.OnNodeValuesChanged != null)
                    sourceNode.OnNodeValuesChanged.Invoke(sourceNode);
            } 
        }

        protected static EdgeData CreateEdgeDataFromEdge(Edge edge)
        {
            EdgeData result;
            NodeView sourceNode = edge.output.node as NodeView;
            NodeView destinationNode = edge.input.node as NodeView;
            SceneData sourceScene = sourceNode != null && sourceNode.Node != null ? sourceNode.Node.SceneData : null;
            SceneData destinationScene = destinationNode != null && destinationNode.Node != null ? destinationNode.Node.SceneData : null; 

            string outputName = edge.output != null ? edge.output.portName : "";
            string inputName = edge.input != null ? edge.input.portName : "";

            if (sourceScene != null && destinationScene != null)
            {
                 result = new EdgeData(sourceNode.Node.Guid, sourceScene.Guid, sourceScene.Name, outputName, 
                    destinationNode.Node.Guid, destinationScene.Guid, destinationScene.Name, inputName);
            }
            else if(sourceScene != null)
            {
                result = new EdgeData(sourceNode.Node.Guid, sourceScene.Guid, sourceScene.Name, outputName,
                    "", "", "", inputName);
            }
            else if (destinationScene != null)
            {
                result = new EdgeData("", "", "", outputName,
                    destinationNode.Node.Guid, destinationScene.Guid, destinationScene.Name, inputName);
            }
            else
            {
                result = new EdgeData();
            }

                return result;
        }

        protected EdgeData CreateEdgeDataFromPort(ConnectionPort port , Direction portDirectionOverride = Direction.Input, bool bUsePortDirectionOverride = false)
        {
            if (port.NGAMEData != null && bUsePortDirectionOverride == false)
                return port.NGAMEData;

            if(port.NGAMEData != null)
            {
                if(portDirectionOverride == port.direction)
                    return port.NGAMEData;

                return EdgeData.Invert(port.NGAMEData);
            }
            
            Direction usedDirection = bUsePortDirectionOverride ? portDirectionOverride : port.direction;

            if (port.connected)
            {
                Edge connection = port.connections.First();
                if (connection != null && port.direction == usedDirection)
                    return CreateEdgeDataFromEdge(connection);

                if(connection != null && port.direction != usedDirection)
                {
                    return EdgeData.Invert(CreateEdgeDataFromEdge(connection));
                }
            }

            EdgeData result = new();

            if(usedDirection == Direction.Input)
            {
                result.DestinationNodeGuid = Node.Guid;
                result.DestinationSceneGuid = CurrentSceneGuid;
                result.DestinationSceneName = Node.SceneData.Name;
                result.DestinationPortName = port.portName;
            }
            else
            {
                result.SourceNodeGuid = Node.Guid;
                result.SourceSceneGuid = CurrentSceneGuid;
                result.SourceSceneName = Node.SceneData.Name;
                result.SourcePortName = port.portName;
            }

                return result;
        }
        public static void AddEdge(Edge edge)
        {
            
            // editor view node updates
            NodeView sourceNode = edge.output.node as NodeView;
            ConnectionPort outputPort = edge.output as ConnectionPort;
            sourceNode.OnPortConnected(outputPort);

            NodeView destinationNode = edge.input.node as NodeView;
            ConnectionPort inputPort = edge.input as ConnectionPort;
            destinationNode.OnPortConnected(inputPort);

            // runtime node updates

            EdgeData newEdgeData = CreateEdgeDataFromEdge(edge);

            if (inputPort != null)
                inputPort.NGAMEData = newEdgeData;
            if (outputPort != null)
                outputPort.NGAMEData = newEdgeData;

            Undo.IncrementCurrentGroup();
            Undo.RecordObjects(new UnityEngine.Object[] { sourceNode.Node, destinationNode.Node }, "NGAME - connect nodes");
            sourceNode.Node.AddEdge(destinationNode.Node, newEdgeData);
            destinationNode.Node.AddEdge(sourceNode.Node, newEdgeData);

            if (sourceNode.OnNodeValuesChanged != null)
                sourceNode.OnNodeValuesChanged.Invoke(sourceNode);
        }

        private void MarkPortConnectionError(ConnectionPort port, Edge edge, string tooltip = "", bool bShowError = true)
        {
            if (port != null)
            {
                port.MarkInvalid(bShowError, tooltip);
            }

            if (bShowError)
            {
                if (edge != null)
                {
                    edge.AddToClassList("Error1");
                    edge.tooltip = tooltip;
                    edge.input.portColor = Color.red;
                }
            }
            else
            {
               
                if (edge != null)
                {
                    edge.RemoveFromClassList("Error1");
                    edge.tooltip = tooltip;
                    edge.input.portColor = m_ValidPortColor;
                }
            }
            
        }

        private void MarkMissingSceneError(string errorTooltip = "", bool bShowError = true)
        {
            if (bShowError)
            {
                AddToClassList("Error2");
                titleContainer.AddToClassList("Error1");
                titleContainer.tooltip = errorTooltip;
            }
            else
            {
                RemoveFromClassList("Error2");
                titleContainer.RemoveFromClassList("Error1");
                titleContainer.tooltip = "";
            }
        }

        public void ValidateNode(List<NGAME.SceneConnectionsData> mostRecentlyFetchedSceneData)
        {
            ValidateNodeScene(mostRecentlyFetchedSceneData);
            ValidateOutputEdges(mostRecentlyFetchedSceneData);
        }

        internal void ValidateNodeScene(List<NGAME.SceneConnectionsData> mostRecentlyFetchedSceneData)
        {

            if (Node.SceneData == null ||  string.IsNullOrEmpty(Node.SceneData.Guid))
            {
                return;
            }
            SceneConnectionsData matchingScene = mostRecentlyFetchedSceneData.FirstOrDefault((SceneConnectionsData e) => e.SceneGuid == Node.SceneData.Guid);
            if (matchingScene == null)
            {
                StringBuilder sb = new();
                sb.Append("Map Graph has a node not included in the valid scenes. ");
                sb.Append("If you wish to remove these nodes use menu option Remove Missing Rooms (NOT IMPLEMENTED).\n");
                sb.Append("Possible reasons for this include: \n");
                sb.Append("You may have unselected the scene in the NGAME settings window \n");
                sb.Append("Or the scene no longer includes NGAME compatible interfaces (Logs for filtering based on that to be added soon).\n");
                Debug.LogWarning(sb.ToString());

                MarkMissingSceneError("Scene named " + Node.SceneData.Name + ", not valid.");
                return;
            }


        }

        internal void ValidateOutputEdges(List<NGAME.SceneConnectionsData> mostRecentlyFetchedSceneData, bool bDeleteInvalidEdges = true)
        {
            List<int> indexOfInvalidEdges = new();
            SceneConnectionsData currentSceneData = null;
            List<RegionConnectionData> exits = null;
            if(Node != null && Node.SceneData != null && !string.IsNullOrEmpty(Node.SceneData.Guid))
            {
                currentSceneData = mostRecentlyFetchedSceneData.FirstOrDefault((SceneConnectionsData data) => data.SceneGuid == Node.SceneData.Guid);

                if(currentSceneData != null)
                    exits = currentSceneData.Exits;
            }
            

            for (int i = 0; i < Node.OutgoingEdges.Count; i++)
            {
                EdgeData serializedEdge = Node.OutgoingEdges[i];
                ConnectionPort sourcePort = GetPortByName(serializedEdge.SourcePortName, OutputPorts);

                
                // NOTE REMEMBER TO UNCOMMENT THE ABOVE AND COMMENT OUT THE CONNECTIONVIEW STUFF IF SWAPPING TO OTHER DISPLAY STYLE

                //ConnectionView connection = Connections.FirstOrDefault((ConnectionView c) => c.Title.text == serializedEdge.SourcePortName);
                //Port sourcePort = connection.Exit;

                NodeView destinationView = m_RoomGraphView.GetNodeByGuid(serializedEdge.DestinationNodeGuid) as NodeView;
                if (destinationView == null)
                {
                    sourcePort.AddToClassList("Error1");
                    string errorTooltip = "Connected to missing node guid: " + serializedEdge.DestinationNodeGuid;
                    //Debug.LogWarning("Node " + Node.Room.SceneName + ", has a connection to a missing node with guid: " + edge.DestinationNodeGuid + ". Removing edge from node.");
                    MarkPortConnectionError(sourcePort, null, errorTooltip);

                    indexOfInvalidEdges.Add(i);
                    continue;
                }

                ConnectionPort destinationPort = destinationView.GetPortByName(serializedEdge.DestinationPortName, destinationView.InputPorts);
                //ConnectionView destinationPortContainer = destinationView.Connections.FirstOrDefault((ConnectionView c) => c.Title.text == serializedEdge.DestinationPortName);
                //Port destinationPort = destinationPortContainer.Entrance;

                if (sourcePort != null)
                {
                    if (destinationPort != null)
                    {
                        SceneConnectionsData destinationData = null;
                        List<RegionConnectionData> entrances = null;
                        if (destinationView.Node != null && destinationView.Node.SceneData != null && !string.IsNullOrEmpty(destinationView.Node.SceneData.Guid))
                        {
                            destinationData = mostRecentlyFetchedSceneData.FirstOrDefault((SceneConnectionsData data) => data.SceneGuid == destinationView.Node.SceneData.Guid);
                            if(destinationData != null)
                                entrances = destinationData.Entrances;
                        }

                        Edge newEdge = sourcePort.ConnectTo(destinationPort);
                        m_RoomGraphView.AddElement(newEdge);

                        RegionConnectionData sourceExit = new();
                        RegionConnectionData destinationEntrance = new();
                        if (exits != null)
                            sourceExit = exits.FirstOrDefault((RegionConnectionData connection) => connection.Name == sourcePort.portName);

                        if(entrances != null)
                            destinationEntrance = entrances.FirstOrDefault((RegionConnectionData connection) => connection.Name == destinationPort.portName);

                        bool sourceUninitialized = string.IsNullOrEmpty(sourceExit.Name);
                        bool destinationUninitialized = string.IsNullOrEmpty(destinationEntrance.Name);
                        bool error = sourceUninitialized || destinationUninitialized;

                        if (error)
                        {
                            if(currentSceneData == null)
                            {
                                StringBuilder errorText = new();
                                errorText.Append(sourcePort.portName);
                                errorText.Append(", no longer valid because Node has no valid scene");
                                MarkPortConnectionError(sourcePort, newEdge, errorText.ToString());
                            }
                            else if (sourceUninitialized)
                            {
                                StringBuilder errorText = new();
                                errorText.Append(sourcePort.portName);
                                errorText.Append(", no longer valid Exit in scene: " + currentSceneData.SceneName);
                                errorText.Append(". Check if its entrance/exit type has been changed, or if the object has been removed.");
                                MarkPortConnectionError(sourcePort, newEdge, errorText.ToString());
                            }

                            if(destinationData == null)
                            {
                                StringBuilder errorText = new();
                                errorText.Append(destinationPort.portName);
                                errorText.Append(", no longer valid because Node has no valid scene");
                                MarkPortConnectionError(destinationPort, null, errorText.ToString());
                            }
                            if (destinationUninitialized)
                            {
                                StringBuilder errorText = new();
                                errorText.Append(destinationPort.portName);
                                errorText.Append(", no longer valid Entrance in scene: " + destinationData.SceneName);
                                errorText.Append(". Check if its entrance/exit type has been changed, or if the object has been removed.");
                                MarkPortConnectionError(destinationPort, null, errorText.ToString());
                            }

                        }

                            SetUsedPortsOtherDirectionEnabled(sourcePort, false);
                            SetUsedPortsOtherDirectionEnabled(destinationPort, false);
                    }
                    else
                    {
                        string errorTooltip = "Missing connection to port named " + serializedEdge.DestinationPortName;
                        string destinationTooltip = "Connecetion named " + serializedEdge.DestinationPortName + ", doesn't exist in scene.";
                        ConnectionPort newDestination = destinationView.AddErrorInputPort(serializedEdge.DestinationPortName);

                        Edge newEdge = sourcePort.ConnectTo(newDestination);
                        m_RoomGraphView.AddElement(newEdge);
                        MarkPortConnectionError(sourcePort, newEdge, errorTooltip);
                        MarkPortConnectionError(newDestination, null, destinationTooltip);
                        SetUsedPortsOtherDirectionEnabled(sourcePort, false);
                        SetUsedPortsOtherDirectionEnabled(newDestination, false);
                        //Debug.LogWarning("Node " + node.Room.SceneName + ", has a connection to a missing port named " + edge.DestinationPortName + ", this is probably because the node this port was connected to had its scene changed.");
                    }
                }
            }

            foreach (int index in indexOfInvalidEdges)
            {
                if (bDeleteInvalidEdges)
                {
                    Node.OutgoingEdges.RemoveAt(index);
                }
                EditorUtility.SetDirty(this.Node);
            }
        }

        protected ConnectionPort AddErrorInputPort( string portName)
        {
            return CreatePort(OldConnectedPorts, inputContainer, portName, typeof(bool)) ;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            Undo.RecordObject(Node, "NGAME - Move Node");
            Node.Position = new Vector2(newPos.xMin, newPos.yMin);
            if (OnNodeValuesChanged != null)
                OnNodeValuesChanged.Invoke(this);
        }

        public ConnectionPort GetPortByName(string name, List<ConnectionPort> portCollection)
        {
            return portCollection.FirstOrDefault((ConnectionPort e) => e.portName == name);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if(OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(null);
            }
        }

        public void OnValuesChanged()
        {
            EditorUtility.SetDirty(Node);
            
            if(OnNodeValuesChanged != null)
            {
                OnNodeValuesChanged.Invoke(this);
            }
            RefreshExpandedState();
        }
        
    }
}

//DropdownField formatListItemCallback seems to fire whenever the whole list is displayed
// DropdownField formatSelectedFieldCallback seems to fire whenever you select a field, but before the ValueChangedCallback is fired
