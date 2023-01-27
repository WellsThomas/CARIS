using System;

namespace Communication.ActionPackage
{
    
    public class ReplaceAction : IAction
    {
        public int ReplacerID;
        public int ReplacedID;
        public bool Active;

        public ReplaceAction(bool active, int replacerID, int replacedID)
        {
            Active = active;
            ReplacerID = replacerID;
            ReplacedID = replacedID;
        }

        public int GetHouseID()
        {
            return ReplacerID;
        }
    }
}