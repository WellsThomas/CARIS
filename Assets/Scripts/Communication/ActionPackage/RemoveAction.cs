using UnityEngine;

namespace Communication.ActionPackage
{
    public class RemoveAction : IAction
    {
        public int houseID;

        public RemoveAction(int houseID)
        {
            this.houseID = houseID;
        }

        public int GetHouseID()
        {
            return houseID;
        }
    }
}