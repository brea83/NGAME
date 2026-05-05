using UnityEngine;
/*
 * based on discussions about constraining objectfield properties by component types
 * https://discussions.unity.com/t/how-do-i-expose-a-list-of-interfaces-in-the-inspector/204992/2
 */
namespace NGAME
{
    public class TypeConstraintAttribute : PropertyAttribute
    {
        private System.Type type;

        public TypeConstraintAttribute(System.Type type)
        {
            this.type = type;
        }

        public System.Type Type
        {
            get { return type; }
        }
    }
}
