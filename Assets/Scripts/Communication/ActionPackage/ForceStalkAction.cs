namespace Communication.ActionPackage
{
    public class ForceStalkAction : IAction
    {
        public int StalkedID;

        public ForceStalkAction(int myId)
        {
            StalkedID = myId;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}