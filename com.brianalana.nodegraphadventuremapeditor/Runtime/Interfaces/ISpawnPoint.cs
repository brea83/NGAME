using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGAME
{
    public interface ISpawnPoint
    {
        public List<SO_SpawnTypeTag> AllowedSpawnableTypes { get; set; }
        public SpawnerData GetSpawnerData();
        public Vector3 GetPosition();
    }
}
