using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    public class ConnectionPort : UnityEditor.Experimental.GraphView.Port
    {
        public Action<ConnectionPort> OnConnectionPortConnect;
        public Action<ConnectionPort> OnConnectionPortDisconnect;

        public bool IsValidInScene { get; private set; }
        public EdgeData NGAMEData{ get; set;}

        private Color m_ValidPortColor;
        private Color m_InvalidPortColor;
        private static string m_CssClassWhenInvalid = "Error1";
        private static string m_RightClickHint = "Right Click for options";

        public NodeView nodeView => GetFirstAncestorOfType<NodeView>();
        //public ConnectionContainer GetConnectionContainer => GetFirstAncestorOfType<ConnectionContainer>();

        // TODO update this with better constraints once custom edge class is made?
        public static ConnectionPort Create<TEdge>(Orientation orientation, Direction direction, Capacity capacity, Type type, string name = "") where TEdge : Edge, new()
        {
            CustomEdgeConnectorListener listener = new(name);
            ConnectionPort port = new ConnectionPort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(listener)
            };

            ContextualMenuManipulator menuManipulator = new ContextualMenuManipulator(port.BuildContextualMenu);

            port.AddManipulator(menuManipulator);
            port.AddManipulator(port.m_EdgeConnector);
            if(string.IsNullOrEmpty(name))
                port.portName = direction == Direction.Input ? "In" : "Out";
            else
                port.portName = name;

            port.tooltip = m_RightClickHint;
            Label label = port.Q<Label>();

            if (label != null)
            {
                //label.AddManipulator(menuManipulator);
                label.focusable = true;
                label.pickingMode = PickingMode.Position;

            }

            return port;
        }

        protected ConnectionPort(Orientation portOrientation, Direction portDirection, 
            Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
            m_ValidPortColor = portColor;
            m_InvalidPortColor = Color.red;
            IsValidInScene = true;
        }

        protected class CustomEdgeConnectorListener : IEdgeConnectorListener
        {
            protected string m_PortName;
            protected GraphViewChange m_GraphViewChange;
            protected List<Edge> m_EdgesToCreate;
            protected List<GraphElement> m_EdgesToDelete;

            public CustomEdgeConnectorListener(string name = "defaultName")
            {
                m_PortName = name;
                m_EdgesToCreate = new();
                m_EdgesToDelete = new();
                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                        {
                            m_EdgesToDelete.Add(connection);
                        }
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection2 in edge.output.connections)
                    {
                        if (connection2 != edge)
                        {
                            m_EdgesToDelete.Add(connection2);
                        }
                    }
                }

                if (m_EdgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(m_EdgesToDelete);
                }

                List<Edge> edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
                }

                foreach (Edge item in edgesToCreate)
                {
                    Debug.Log("CustomEdgeConnectorListener: Edge created between " + edge.input.portName + ", and " + edge.output.portName);
                    graphView.AddElement(item);
                    edge.input.Connect(item);
                    edge.output.Connect(item);
                }
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                Debug.Log($"connectionPort: OnDropOutsidePort from port {m_PortName}. with edge input: {edge.input.portName}, and output {edge.output.portName}");
                
                NodeView.RemoveEdge(edge);
            }
        }

        public override void OnStartEdgeDragging()
        {
            base.OnStartEdgeDragging();
        }

        public override void OnStopEdgeDragging()
        {
            base.OnStopEdgeDragging();
        }

        public override void Connect(Edge edge)
        {
            base.Connect(edge);
            //Debug.Log("Port.Connect on" + this.portName + ": Edge created between " + edge.input.portName + ", and " + edge.output.portName);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
        }

        public void OrientCap(bool bInRow, bool bOnLeftOfNode)
        {
            if (bInRow)
            {
                SetLabelFront();
            }
            else if (bOnLeftOfNode)
            {
                if (direction == Direction.Input)
                    SetLabelFront();
                else
                    SetCapFront();
            }
            else
            {
                if (direction == Direction.Input)
                    SetCapFront();
                else
                    SetLabelFront();
            }
        }

        public void SetCapFront()
        {
            m_ConnectorBox.BringToFront();
        }

        public void SetLabelFront()
        {
            m_ConnectorText.BringToFront();
        }

        public void MarkInvalid(bool bIsInvalid = true, string tooltipText = "")
        {
            IsValidInScene = !bIsInvalid;
            if (bIsInvalid)
            {
                AddToClassList(m_CssClassWhenInvalid);
                tooltip = tooltipText == "" ? m_RightClickHint : tooltipText;
                portColor = m_InvalidPortColor;
            }
            else
            {
                RemoveFromClassList(m_CssClassWhenInvalid);
                tooltip = tooltipText == "" ? m_RightClickHint : tooltipText;
                portColor = m_ValidPortColor;
            }
        }


        //
        // Summary:
        //     Add menu items to the connection port's contextual menu.
        //
        // Parameters:
        //   evt:
        //     The event holding the menu to populate.
        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is ConnectionPort)
            {
                evt.menu.AppendAction($"Play From {portName}", delegate
                {
                    nodeView.TryPlayFromPort(this);
                }, (DropdownMenuAction a) => IsValidInScene ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
            }
        }
    }
}
