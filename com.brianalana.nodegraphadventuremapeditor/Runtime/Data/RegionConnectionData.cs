using System;
using System.Collections.Generic;
using UnityEngine;

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
        public string TypeName;
        public string Name;
        public RegionConnectionType ConnectionType;
        [SerializeReference]
        public List<EntranceCondition> EntranceConditions;
        public Vector3 Position;
    }
}
