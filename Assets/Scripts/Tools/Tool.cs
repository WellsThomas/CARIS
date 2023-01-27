using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public interface ITool
    {

        public TrackableType GetTrackableType()
        {
            return TrackableType.FeaturePoint |
                   TrackableType.Planes |
                   TrackableType.PlaneWithinPolygon |
                   TrackableType.Image;
        }
        void OnRayCast(ARRaycastHit hit)
        {
            
        }
        
        void OnPhysicsRayCast(RaycastHit hit)
        {
            
        }

        void UpdateInput()
        {
            
        }

        bool IsBuildTool()
        {
            return false;
        }

        void OnToolChange()
        {
            
        }


        string GetName();

        void OnClick()
        {
            
        }
        
        void OnLeftClick()
        {
            
        }
        
        void OnRightClick()
        {
            
        }

        public ToolType GetType()
        {
            return ToolType.DebugTool;
        }

        public enum ToolType
        {
            BuildTool,
            DebugTool,
            PlaceTool,
            MoveTool,
            RemoveTool,
            OrientTool,
            GeofenceTool,
            SaveHouseTool,
            GeofenceChangeTool
        }
    }
}