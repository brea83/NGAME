using UnityEngine;

namespace NGAME
{
    [System.Serializable]
    public class EC_Test : EntranceCondition
    {
        public EC_Test()
        {
            Name = "EC_Test";
            Description = "This is just a test to make sure we don't add duplicate conditions in the editor. It always evaluates as true.";
        }
        public override bool Evaluate()
        {
            return true;
        }
    }
}
