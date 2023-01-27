using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class HousePlaceTool : ITool
    {
        private HouseManager _manager;
        private GameObject _crosshair;
        private GameObject _crosshairObject;
        private Pose lastPose;
        private readonly ToolManager _toolManager;
        private Action _onToolChange;

        public HousePlaceTool(HouseManager manager, ToolManager toolManager, Action onToolChange)
        {
            _toolManager = toolManager;
            _manager = manager;
            _onToolChange = onToolChange;
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }
        
        public void OnRayCast(ARRaycastHit hit)
        {
            lastPose = hit.pose;
            _manager.SetCrosshair(hit.pose);
        }

        public void OnToolChange()
        {
            _onToolChange();
            _manager.ResetCrosshair();
        }        
     
        public void OnClick()
        {
            _toolManager.ResetTool();
            _manager.ResetCrosshair();
            _manager.CreateNewHouse(lastPose);
        }

        public string GetName()
        {
            return "World Placing Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.PlaceTool;
        }
    }
}
