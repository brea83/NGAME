using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NGAME
{
    [Serializable]
    public class SceneSpawnData
    {
        public string SceneGUID;
        public List<SpawnerData> SpawnPoints = new();

        public SceneSpawnData()
        {

        }
        public SceneSpawnData(SceneData sceneData)
        {
            SceneGUID = sceneData.Guid;
            SpawnPoints = sceneData.SpawnPoints;
        }

        public Dictionary<string, int> CountSpawnersWithMatchingTypes()
        {
            Dictionary<string, int> result = new();

            foreach(SpawnerData spawner in SpawnPoints)
            {
                string validTypes = spawner.ValidTypesToString();
                if (result.ContainsKey(validTypes))
                {
                    result[validTypes] += 1;
                }
                else
                {
                    result.Add(validTypes, 1);
                }
            }

            return result;
        }

        public List<SO_SpawnTypeTag> GetUniqueSpawnTypeTags()
        {
            Dictionary < string, SO_SpawnTypeTag > result = new();

            foreach (SpawnerData spawner in SpawnPoints)
            {

                foreach(SO_SpawnTypeTag tag in spawner.ValidTypes)
                {
                    if (result.ContainsKey(tag.Tag))
                    {
                        continue;
                    }
                    else
                    {
                        result.Add(tag.Tag, tag);
                    }
                }
               
            }

            return result.Values.ToList();
        }
    }
}
