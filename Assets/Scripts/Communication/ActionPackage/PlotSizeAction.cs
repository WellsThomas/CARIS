namespace Communication.ActionPackage
{
    public class PlotSizeAction: IAction
    {
        public int HouseID;
        public int Direction;
        public int Size;

        public PlotSizeAction(int houseID, int direction, int size)
        {
            this.HouseID = houseID;
            this.Direction = direction;
            this.Size = size;
        }

        public int GetHouseID()
        {
            return HouseID;
        }
    }
}