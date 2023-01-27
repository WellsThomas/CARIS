using UnityEngine;

namespace Communication.ActionPackage
{
    public class GeofenceBuildAction
    {
        public string AnchorID;
        public Vector2 Scale;

        public GeofenceBuildAction(string anchorID, Vector2 scale)
        {
            AnchorID = anchorID;
            Scale = scale;
        }
    }
}