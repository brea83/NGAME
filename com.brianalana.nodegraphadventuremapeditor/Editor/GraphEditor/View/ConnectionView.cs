using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    public class ConnectionView 
    {
        public VisualElement Container { get; private set; }
        
        public ConnectionPort Exit { get; private set; }
        public ConnectionPort Entrance { get; private set; }

        public Label Title;

        public ConnectionView(RegionConnectionData data, Vector2 position, Vector2 containerSize, NodeView node)
        {
            Container = new VisualElement();
            Container.style.position = Position.Absolute;
            Vector2 offset = new Vector2(data.Name.Length * -3.0f, 10.0f);
            Container.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);

            Title = new Label();
            Title.text = data.Name;

            VisualElement row = new();


            float verticalThreshold = containerSize.y * 0.3f;
            float horizontalThreshold = containerSize.x * 0.5f;

            bool bTop = position.y <= verticalThreshold;
            bool bBottom = position.y >= containerSize.y - verticalThreshold;
            bool bRight = position.x >= horizontalThreshold;
            bool bLeft = position.x < horizontalThreshold;
            
            bool bInVerticalZone = bTop || bBottom;


            Type type = typeof(RegionConnectionData);
            Orientation orientation = bInVerticalZone ? Orientation.Vertical : Orientation.Horizontal;
            row.style.flexDirection = bInVerticalZone ? FlexDirection.Row : FlexDirection.Column;
            //Container.style.flexDirection = bInVerticalZone ? FlexDirection.Column : FlexDirection.Row;
            if (bTop)
                offset.y *= -4.0f;
            else if (!bBottom)
            {
                offset.y *= -2.0f;
                if (bLeft)
                    offset.x += offset.x - 5.0f;
                else
                    offset.x -= offset.x - 5.0f;
            }

            Container.style.left = position.x + offset.x;
            Container.style.top = position.y + offset.y;


            CreatePorts(data.ConnectionType, orientation, type);

            
            if (Entrance != null)
            {
                row.Add(Entrance);
                node.InputPorts.Add(Entrance);
                Entrance.OrientCap(bInVerticalZone, bLeft);
            }

            if (Exit != null)
            {
                row.Add(Exit);
                node.InputPorts.Add(Exit);

                Exit.OrientCap(bInVerticalZone, bLeft);
            }


            if (bTop) 
            { 
                Container.Add(row);
                Container.Add(Title);
                
            }
            else
            {
                Container.Add(Title);
                Container.Add(row);
            }
           

        }

        protected void CreatePorts(RegionConnectionType connectionType, Orientation orientation, Type type)
        {
            switch (connectionType)
            {
                case RegionConnectionType.EntranceOnly:
                    Entrance = ConnectionPort.Create<Edge>(orientation, Direction.Input, Port.Capacity.Multi, type);
                    Exit = null;
                    break;
                case RegionConnectionType.ExitOnly:
                    Entrance = null;
                    Exit = ConnectionPort.Create<Edge>(orientation, Direction.Output, Port.Capacity.Single, type);
                    break;
                case RegionConnectionType.ExitAndEntrance:
                    Entrance = ConnectionPort.Create<Edge>(orientation, Direction.Input, Port.Capacity.Multi, type);
                    Exit = ConnectionPort.Create<Edge>(orientation, Direction.Output, Port.Capacity.Single, type);
                    break;
                default:
                    break;
            }
        }


    }
}
