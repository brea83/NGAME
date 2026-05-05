using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NGAME
{
    [System.Serializable]
    public class SOWaveData : ScriptableObject, IWaveData
    {
        public bool RespawnsOnBacktrack { get => m_RespawnsOnBacktrack; set => m_RespawnsOnBacktrack = value; }
        public List<ISpawnable> PossibleSpawns { get => ExtractSpawnablesFromPrefabs(); }
        public float SecondsBtwnSpawns { get => m_SecBtwnSpawns; set => m_SecBtwnSpawns = value; }
        public int NumToSpawn { get => m_NumToSpawn; set => m_NumToSpawn = value; }
        public bool UseSpawnByType { get => m_UseSpawnByType; set => m_UseSpawnByType = value; }
        public Dictionary<SO_SpawnTypeTag, int> NumToSpawnByType { get => UpdateSpawnTypesList(); }
        public float MinSecondsBeforeNextWave { get => m_MinSecondsBeforeNextWave; set => m_MinSecondsBeforeNextWave = value; }
        public int EnemiesRemainingTrigger { get => m_EnemiesReamainingTrigger; set => m_EnemiesReamainingTrigger = value; }

        public List<GameObject> PrefabsNeeded => m_PossibleEnemies;

        [SerializeField]
        protected bool m_RespawnsOnBacktrack = false;
        [SerializeField, TypeConstraint(typeof(ISpawnable))]
        protected List<GameObject> m_PossibleEnemies = new List<GameObject>();
        //[SerializeField, HideInInspector]
        protected bool m_EnemyListDirty = true;
        [SerializeField]
        protected float m_SecBtwnSpawns = 0.5f;
        [SerializeField]
        protected int m_NumToSpawn = 1;
        [SerializeField, HideInInspector]
        protected bool m_UseSpawnByType = false;
        [SerializeField]
        protected float m_MinSecondsBeforeNextWave = 2.0f;
        [SerializeField]
        protected int m_EnemiesReamainingTrigger = 0;

        // store the dictionary of spawn count by type as two lists
        //[SerializeField, HideInInspector]
        protected List<SO_SpawnTypeTag> m_SpawnTypes = new List<SO_SpawnTypeTag>();
        //[SerializeField, HideInInspector]
        protected List<int> m_SpawnCountPerType = new List<int>();



        public Dictionary<SO_SpawnTypeTag, int> UpdateSpawnTypesList()
        {
            Dictionary<SO_SpawnTypeTag, int> result = new();
            if (m_SpawnTypes.Count > 0 && m_SpawnTypes.Count >= m_SpawnCountPerType.Count)
            {
                for (int i = 0; i < m_SpawnTypes.Count; i++)
                {
                    if (m_SpawnCountPerType.Count > i)
                    {
                        if (!result.ContainsKey(m_SpawnTypes[i]))
                        {
                            result.Add(m_SpawnTypes[i], m_SpawnCountPerType[i]);
                        }
                        else
                        {
                            result[m_SpawnTypes[i]] += m_SpawnCountPerType[i];
                        }
                    }
                    else
                    {
                        if (!result.ContainsKey(m_SpawnTypes[i]))
                        {
                            result.Add(m_SpawnTypes[i], 0);
                        }
                    }
                }
            }

            if ( m_PossibleEnemies.Count > 0)
            {
                for (int i = 0; i < m_PossibleEnemies.Count; i++)
                {
                    GameObject prefabObject = m_PossibleEnemies[i];

                    ISpawnable enemy = prefabObject.GetComponent<ISpawnable>();
                    if (enemy == null)
                    {
                        continue;
                    }
                    SO_SpawnTypeTag tag = enemy.SpawnTypeTag;

                    if (result.ContainsKey(tag))
                    {
                        continue;
                    }
                    else
                    {
                        result.Add(tag, 0);
                    }
                }
            }

            m_SpawnTypes = result.Keys.ToList();
            m_SpawnCountPerType = result.Values.ToList();

            m_EnemyListDirty = false;
            return result;
        }

        protected List<ISpawnable> ExtractSpawnablesFromPrefabs()
        {
            List<ISpawnable> result = new();
            foreach (GameObject prefabObject in m_PossibleEnemies)
            {
                ISpawnable enemy = prefabObject.GetComponent<ISpawnable>();
                if (enemy == null)
                {
                    continue;
                }
                result.Add(enemy);
            }

            return result;
        }

        public List<SO_SpawnTypeTag> GetUniqueSpawnTypes()
        {
            UpdateSpawnTypesList();
            return m_SpawnTypes;
        }
    }
}
