using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NGAME
{
    public enum RegionConnectionType
    {
        EntranceOnly = 0,
        ExitOnly = 1,
        ExitAndEntrance = 2,
    }

    [Serializable]
    public struct RegionConnectionData
    {
        public string TypeName;// = "IEncounterRegionConnector";
        public string Name;//= "Object Name";
        public RegionConnectionType ConnectionType;
        [SerializeReference]
        public List<EntranceCondition> EntranceConditions;
        public Vector3 Position;
    }
    [Serializable]
    public class SceneConnectionsData
    {
        public string SceneName = "";
        public string SceneGuid = "";
        [HideInInspector]
        public List<RegionConnectionData> Entrances = new();
        [HideInInspector]
        public List<RegionConnectionData> Exits = new();
        [HideInInspector]
        public Vector2 MinPoint = Vector3.zero;
        [HideInInspector]
        public Vector2 MaxPoint = Vector3.zero;
        [HideInInspector]
        public Vector2 WidthByHeight = Vector2.zero;

        public SceneConnectionsData() { }
        public SceneConnectionsData(SceneData sceneData)
        {
            SceneName = sceneData.Name;
            SceneGuid = sceneData.Guid;

            foreach (RegionConnectionData connection in sceneData.UniqueConnectionObjects)
            {
                switch (connection.ConnectionType)
                {
                    case RegionConnectionType.EntranceOnly:
                        Entrances.Add(connection);
                        break;
                    case RegionConnectionType.ExitOnly:
                        Exits.Add(connection);
                        break;
                    case RegionConnectionType.ExitAndEntrance:
                        Entrances.Add(connection);
                        Exits.Add(connection);
                        break;
                    default:
                        break;
                }
            }

            UpdateBounds();
        }

        public SceneConnectionsData ShallowCopy()
        {
            return (SceneConnectionsData)MemberwiseClone();
        }

        public SceneConnectionsData DeepCopy()
        {
            SceneConnectionsData other = (SceneConnectionsData)MemberwiseClone();
            other.Exits = new List<RegionConnectionData>(Exits);
            other.Entrances = new List<RegionConnectionData>(Entrances);
            other.MinPoint = MinPoint;
            other.MaxPoint = MaxPoint;
            other.WidthByHeight = WidthByHeight;

            return other;
        }

        public void UpdateBounds()
        {
            Vector3 min = CalculateMinBounds();
            Vector3 max = CalculateMaxBounds();
            MinPoint.x = min.x;
            MinPoint.y = min.z;

            MaxPoint.x = max.x;
            MaxPoint.y = max.z;

            WidthByHeight = new Vector2(max.x - min.x, max.z - min.z);
        }
        public Vector3 CalculateMinBounds()
        {
            Vector3 min = new Vector2(float.MaxValue, float.MaxValue);

            foreach(RegionConnectionData entrance in Entrances)
            {
                min = Vector3.Min(min, entrance.Position);
            }

            foreach(RegionConnectionData exit in Exits)
            {
                min = Vector3.Min(min, exit.Position);
            }

            return min;
        }

        public Vector3 CalculateMaxBounds()
        {
            Vector3 max = new Vector2(float.MinValue, float.MinValue);

            foreach (RegionConnectionData entrance in Entrances)
            {
                max = Vector3.Max(max, entrance.Position);
            }

            foreach (RegionConnectionData exit in Exits)
            {
                max = Vector3.Max(max, exit.Position);
            }

            return max;
        }
    }
    public interface IEncounterRegionConnector
    {
        public RegionConnectionData GetRegionConnectionData();
        public void SetDestination(EdgeData edge);

        public UnityEvent<EdgeData> ConnectorActivated { get; }

        public void InitializeFromGraphData(RegionConnectionData connectionData, EdgeData edge);
    }
}
