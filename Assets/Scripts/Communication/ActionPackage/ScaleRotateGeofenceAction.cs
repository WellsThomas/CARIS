using System.Collections.Generic;
using System.Numerics;
using UnityEngine.Serialization;

namespace Communication.ActionPackage
{
    [System.Serializable]
    public class ScaleRotateGeofenceAction
    {
        public List<ScaleRotateHouseAction> rotateScaleHouses;
        public string anchorID;

        public ScaleRotateGeofenceAction(List<ScaleRotateHouseAction> rotateScaleHouses, string anchorID)
        {
            this.rotateScaleHouses = rotateScaleHouses;
            this.anchorID = anchorID;
        }

        public int GetHouseID()
        {
            return 0;
        }
    }
}