using UnityEngine;

namespace Communication.ActionPackage
{
    [System.Serializable]
    public class ScaleAction : IAction
    {
        public SerializableVector3 scale;
        public int id;

        public ScaleAction(Vector3 scale, int id)
        {
            this.scale = scale;
            this.id = id;
        }

        public int GetHouseID()
        {
            return id;
        }
    }
    
}