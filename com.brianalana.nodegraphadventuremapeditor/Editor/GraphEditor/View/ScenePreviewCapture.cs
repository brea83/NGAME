using UnityEditor;
using UnityEngine;

namespace NGAME.Editor
{

   
    [System.Serializable, CreateAssetMenu(fileName = "NpcSpawnTest", menuName = "TEST/NpcSpawnTest")]
    public class ScenePreviewCapture : ScriptableObject
    {
        
        public string scenePath;

        public int RenderTextureHeight = 1080;
    }
}
