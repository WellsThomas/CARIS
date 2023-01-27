namespace Communication.ActionPackage
{
    public class CopyAction: IAction
    {
        public string AnchorID;
        public int HouseID;
        public string Data;

        public CopyAction(string anchorId, int houseId, string data)
        {
            Data = data;
            HouseID = houseId;
            AnchorID = anchorId;
        }

        public int GetHouseID()
        {
            return HouseID;
        }
    }
}