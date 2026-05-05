using System.Collections.Generic;
using UnityEngine;

namespace NGAME
{
    public interface IWaveData
    {
        public bool RespawnsOnBacktrack { get; set; }
        public List<ISpawnable> PossibleSpawns { get; }
        public List<GameObject> PrefabsNeeded { get; }
        public float SecondsBtwnSpawns { get; set; }
        public int NumToSpawn { get; set; }
        public bool UseSpawnByType {  get; set; }
        public Dictionary<SO_SpawnTypeTag, int> NumToSpawnByType { get;}
        public List<SO_SpawnTypeTag> GetUniqueSpawnTypes();
        public float MinSecondsBeforeNextWave {  get; set; }
        public int EnemiesRemainingTrigger { get; set; }

        //public void SerializeData();
        //TODO: make serialization hook in.

    }
}
