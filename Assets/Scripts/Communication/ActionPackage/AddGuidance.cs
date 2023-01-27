namespace Communication.ActionPackage
{
    public class AddGuidance : IAction
    {
        public string AnchorID;
        public int GuidanceID;

        public AddGuidance(string anchorId, int guidanceID)
        {
            AnchorID = anchorId;
            GuidanceID = guidanceID;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}