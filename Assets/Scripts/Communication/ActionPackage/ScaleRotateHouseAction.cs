using UnityEngine;

namespace Communication.ActionPackage
{
    [System.Serializable]
    public class ScaleRotateHouseAction
    {
        public SerializableQuaternion offsetRotation;
        public SerializableVector3 offsetPos;
        public SerializableVector3 scale;
        public int houseID;
                
        public ScaleRotateHouseAction(int houseID, Vector3 offsetPos, Vector3 scale, Quaternion offsetRotation)
        {
            this.houseID = houseID;
            this.offsetPos = offsetPos;
            this.scale = scale;
            this.offsetRotation = offsetRotation;
        }
        
        public int GetHouseID()
        {
            return houseID;
        }
    }
}
