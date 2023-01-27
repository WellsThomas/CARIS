namespace Communication.ActionPackage
{
    public class RemoveBlockAction : IAction
    {
        public int id;
        public int blockId;

        public RemoveBlockAction(int blockId, int id)
        {
            this.id = id;
            this.blockId = blockId;
        }

        public int GetHouseID()
        {
            return id;
        }
    }
}