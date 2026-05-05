using UnityEngine;

namespace NGAME
{
    [CreateAssetMenu(fileName = "SO_SpawnTypeTag", menuName = "NGAME/Scriptable Objects/SO_SpawnTypeTag")]
    public class SO_SpawnTypeTag : ScriptableObject
    {
        public string Tag = "Default Enemy Type";
    }
}
