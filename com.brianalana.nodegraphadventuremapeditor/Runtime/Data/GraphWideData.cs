using System.Collections.Generic;
using UnityEngine;

namespace NGAME
{
    [System.Serializable]
    public class GraphWideData
    {
        public List<SceneData> ValidScenes;
        public List<string> ValidSceneGuids; // used to construct a dictionary in editor and runtime

        public List<SOWaveData> UsedWavesData;
        public List<string> WaveDataGuids; // used to construct a lookup dictionary in editor and runtime

        private Dictionary<string, SceneData> _scenes;
        private Dictionary<string, SOWaveData> _waves;

        private void InitializeDictionaries()
        {
            _scenes.Clear();
            _waves.Clear();

            for( int i = 0; i < ValidSceneGuids.Count; i++ )
            {
                string guid = ValidSceneGuids[i];
                SceneData data = ValidScenes[i];
                _scenes.Add( guid, data );
            }

            for (int i = 0; i < UsedWavesData.Count; i++)
            {
                string guid = WaveDataGuids[i];
                SOWaveData data = UsedWavesData[i];
                _waves.Add(guid, data);
            }
        }

        public SceneData GetSceneByGuid(string guid)
        {
            if(_scenes == null || _scenes.Count <= 0)
            {
                InitializeDictionaries();
            }

            SceneData result;
            _scenes.TryGetValue(guid, out result);
            return result;
        }

        public SOWaveData GetWaveByGuid(string guid)
        {
            if (_waves == null || _waves.Count <= 0)
            {
                InitializeDictionaries();
            }

            SOWaveData result;
            _waves.TryGetValue(guid, out result);
            return result;
        }
    }
}
