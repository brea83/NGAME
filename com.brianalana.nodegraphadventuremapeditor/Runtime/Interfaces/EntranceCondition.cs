using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME
{
    [Serializable]
    public abstract class EntranceCondition /*: ISerializationCallbackReceiver*/
    {
        public string Name;
        public string Description;

        //public Action OnSatisfied;
        //public Action<object> EvaluateWithObject;

        public abstract bool Evaluate();

        //public void OnBeforeSerialize()
        //{
        //    return;
        //}

        //public void OnAfterDeserialize()
        //{
        //    return;
        //}
    }

    

}
