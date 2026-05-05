using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NGAME
{
    [System.Serializable]
    public class SceneData : ScriptableObject
    {
        public string Name { get => name; set => name = value; }
        public string Guid;
        public string FilePath;
        public string Description;
        public SceneBounds Bounds;

        public List<RegionConnectionData> UniqueConnectionObjects;

        public List<SpawnerData> SpawnPoints;

        public List<RegionConnectionData> Entrances { get => GetEntrances(); }
        public List<RegionConnectionData> Exits { get => GetExits(); }
        public List<RegionConnectionData> GetEntrances()
        {
            return UniqueConnectionObjects.TakeWhile( 
                data => data.ConnectionType == RegionConnectionType.EntranceOnly
                || data.ConnectionType == RegionConnectionType.ExitAndEntrance
            ).ToList();
        }

        public List<RegionConnectionData> GetExits()
        {
            return UniqueConnectionObjects.TakeWhile(
                data => data.ConnectionType == RegionConnectionType.ExitOnly
                || data.ConnectionType == RegionConnectionType.ExitAndEntrance
            ).ToList();
        }

        public SceneData ShallowCopy()
        {
            return (SceneData)MemberwiseClone();
        }

        public SceneData DeepCopy()
        {
            SceneData other = (SceneData)MemberwiseClone();
            
            other.UniqueConnectionObjects = new List<RegionConnectionData>(UniqueConnectionObjects);
            other.SpawnPoints = new List<SpawnerData>(SpawnPoints);
            return other;
        }

    }
}
