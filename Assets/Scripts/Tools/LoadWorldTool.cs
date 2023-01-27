using Communication;
using Communication.ActionPackage;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARSubsystems;
using static System.Int32;
using Random = UnityEngine.Random;


namespace Tools
{
    public class LoadWorldTool : ITool
    {
        private HouseManager _manager;
        private GameObject _crosshair;
        private GameObject _crosshairObject;
        private Pose lastPose;
        private readonly ToolManager _toolManager;
        private HouseManager.WorldContent worldContent;
        private string _worldData;
        private GameObject helper;
        private bool _hasBeenPlaced = false;

        public LoadWorldTool(HouseManager manager, ToolManager toolManager, string worldData)
        {
            _toolManager = toolManager;
            _manager = manager;
            _worldData = worldData;
            
            // Spawn house using house-information
            worldContent = JsonUtility.FromJson<HouseManager.WorldContent>(worldData);
            Debug.Log("LOADED" + worldData);
            helper = manager.PlaceAllHouses(worldContent);

            _hasBeenPlaced = false;
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }

        public void OnRayCast(ARRaycastHit hit)
        {
            var hitPos = hit.pose.position;
            if (!_hasBeenPlaced)
            {
                helper.transform.position = hitPos;
                GeofenceManager.Get().FixGeofenceFromPlacement();
                return;
            }
            
            // Set Scale
            var pos = helper.transform.position;
            var dist = Vector3.Distance(pos, hitPos);
            var scale = dist/(worldContent.fence.scale.x / 2);
            helper.transform.localScale = new Vector3(scale, scale, scale);
            
            // Set Rotation
            var transform = helper.transform;
            transform.LookAt(hitPos);
            var rotation = transform.rotation;
            rotation.x = 0;
            rotation.z = 0;
            transform.rotation = rotation;
            
            // Update Geofence
            GeofenceManager.Get().FixGeofenceFromPlacement();
        }

        public void OnClick()
        {
            if (!_hasBeenPlaced)
            {
                _hasBeenPlaced = true;
                return;
            }
            
            _toolManager.ResetTool();
        }

        public void OnToolChange()
        {
            HouseManager.Get().DetachHelper(helper);
            
            foreach (var (key, house) in _manager.GetHouses())
            {
                house.Local.UpdateHouseInformation();
            }

            GeofenceManager.Get().FixGeofenceFromPlacement(true);
            
            var pose = new Pose(helper.transform.position,
                helper.transform.rotation);
            var anchor = _manager.CreateAnchor(pose);
            var anchorId = anchor.trackableId.ToString();
            var loadWorldAction = new LoadWorldAction(anchorId, helper.transform.localScale.x, _worldData);
            Stringifier.GetStringifier().StringifyAndForward<LoadWorldAction>(loadWorldAction, TypeOfPackage.LoadWorld, true);
        }

        public string GetName()
        {
            return "Load World Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.PlaceTool;
        }
    }
}