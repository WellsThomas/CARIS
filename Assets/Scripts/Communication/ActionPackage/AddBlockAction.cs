namespace Communication.ActionPackage
{
    public class AddBlockAction : IAction
    {
        public SerializableVector3 position;
        public int houseID;
        public int colorId;
        public int blockId;

        public AddBlockAction(SerializableVector3 position, int colorId, int blockId, int houseID)
        {
            this.position = position;
            this.houseID = houseID;
            this.blockId = blockId;
            this.colorId = colorId;
        }

        public int GetHouseID()
        {
            return houseID;
        }
    }
}