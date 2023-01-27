using UnityEngine;

namespace Communication.ActionPackage
{
    public class LoadWorldAction: IAction
    {
        public string AnchorID;
        public float Scale;
        public string Data;
        public bool IsRequest;

        public LoadWorldAction(string anchorId, float scale, string data, bool isRequest = false)
        {
            Data = data;
            Scale = scale;
            AnchorID = anchorId;
            IsRequest = isRequest;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}