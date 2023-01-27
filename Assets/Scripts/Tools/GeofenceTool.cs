using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class GeofenceTool : ITool
    {
        private GeofenceManager _geofenceManager;
        private ToolManager _toolManager;

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.GeofenceTool;
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }

        public GeofenceTool(ToolManager toolManager)
        {
            _geofenceManager = GeofenceManager.Get();
            _geofenceManager.StartSetup();
            _toolManager = toolManager;
        }
        
        public void OnRayCast(ARRaycastHit hit)
        {
            _geofenceManager.SetPositionOfCurrent(hit.pose.position);
        }
        
        public void OnClick()
        {
            if (!_geofenceManager.PlaceCurrentPole()) return;
            
            _toolManager.ResetTool();
        }

        public void OnToolChange()
        {
            if(_geofenceManager.IsConstructing())
                _geofenceManager.CancelConstruction();
        }

        public string GetName()
        {
            return "Geofence Setup";
        }
    }
}