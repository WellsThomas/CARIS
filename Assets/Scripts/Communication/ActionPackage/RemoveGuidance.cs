namespace Communication.ActionPackage
{
    public class RemoveGuidance : IAction
    {
        public int GuidanceID;

        public RemoveGuidance(int guidanceID)
        {
            GuidanceID = guidanceID;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}