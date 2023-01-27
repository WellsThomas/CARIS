using Communication;
using Communication.ActionPackage;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARSubsystems;
using static System.Int32;
using Random = UnityEngine.Random;

namespace Tools
{
    public class LoadHouseTool : ITool
    {
        private HouseManager _manager;
        private GameObject _crosshair;
        private GameObject _crosshairObject;
        private Pose lastPose;
        private readonly ToolManager _toolManager;
        private HouseManager.HouseContent houseContent;
        private bool hasBeenPlaced;

        public LoadHouseTool(HouseManager manager, ToolManager toolManager, string houseData)
        {
            _toolManager = toolManager;
            _manager = manager;
            
            // Spawn house using house-information
            var id = Random.Range(MinValue, MaxValue);
            houseContent = manager.BuildSerializedHouse(houseData, id);
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }
        
        public void OnRayCast(ARRaycastHit hit)
        {
            houseContent.Local.MoveHouse(hit.pose);
        }

        public void OnToolChange()
        {
            // If not placed. Remove house...
            if (!hasBeenPlaced)
                _manager.EraseHouseLocally(houseContent.Distributor.GetID());
        }        
     
        public void OnClick()
        {
            hasBeenPlaced = true;
            _toolManager.ResetTool();

            var pose = new Pose(houseContent.HouseObject.transform.position,
                houseContent.HouseObject.transform.rotation);
            var anchor = _manager.CreateAnchor(pose);
            var anchorId = anchor.trackableId.ToString();
            var data = houseContent.Local.SerializeWithAnchor(anchorId);
            var copyAction = new CopyAction(anchorId, houseContent.Distributor.GetID(), data);
            Stringifier.GetStringifier().StringifyAndForward<CopyAction>(copyAction, TypeOfPackage.CopyHouse, true);
        }

        public string GetName()
        {
            return "Load House Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.PlaceTool;
        }
    }
}
