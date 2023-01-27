namespace Communication.ActionPackage
{
    public class StalkingAction : IAction
    {
        public int StalkerID;
        public int StalkedID;
        public bool IsStalking;

        public StalkingAction(int stalkerID,int stalkedID, bool isStalking)
        {
            StalkerID = stalkerID;
            StalkedID = stalkedID;
            IsStalking = isStalking;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}