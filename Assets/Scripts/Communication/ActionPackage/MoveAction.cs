namespace Communication.ActionPackage
{
    public class MoveAction : IAction
    {
        public string anchorID;
        public int houseID;

        public MoveAction(string anchorID, int houseID)
        {
            this.anchorID = anchorID;
            this.houseID = houseID;
        }

        public int GetHouseID()
        {
            return houseID;
        }
    }
}