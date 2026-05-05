using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NGAME
{
    [Serializable]
    public class SpawnerData
    {
        public string Name;
        public List<SO_SpawnTypeTag> ValidTypes = new();
        public Vector3 Position;

        public string ValidTypesToString()
        {
            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < ValidTypes.Count; i++)
            {
                SO_SpawnTypeTag tag = ValidTypes[i];
                sb.Append(tag.Tag);

                if(i < ValidTypes.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }
    }
}
