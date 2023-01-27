using UnityEngine;

namespace Communication.ActionPackage
{
    public class OffsetAction : IAction
    {
        public int houseID;
        public SerializableVector3 Offset;

        public OffsetAction(int houseID, Vector3 offset)
        {
            Offset = offset;
            this.houseID = houseID;
        }

        public int GetHouseID()
        {
            return houseID;
        }
    }
}