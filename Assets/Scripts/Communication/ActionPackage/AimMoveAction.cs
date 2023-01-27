using UnityEngine;

namespace Communication.ActionPackage
{
    public class AimMoveAction : IAction
    {
        public int ID;
        public int playerID;
        public SerializableVector3 localPosition;
        public SerializableQuaternion localRotation;

        public AimMoveAction(int ID, int playerID, Vector3 localPosition, Quaternion localRotation)
        {
            this.ID = ID;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.playerID = playerID;
        }
        
        public int GetHouseID()
        {
            return ID;
        }
    }
}