namespace Communication.ActionPackage
{
    
    [System.Serializable]
    public class SetSharedAction : IAction
    {
        public bool shared;
        public int id;

        public SetSharedAction(bool shared, int id)
        {
            this.shared = shared;
            this.id = id;
        }

        public int GetHouseID()
        {
            return id;
        }
    }
}