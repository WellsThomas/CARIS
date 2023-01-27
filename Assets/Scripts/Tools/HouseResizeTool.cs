using System;
using Packages;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class HouseResizeTool : ITool
    {
        private HouseResizeArrow _arrow;
        private HouseManager.HouseContent _houseContent;
        
        public HouseResizeTool(HouseResizeArrow currentArrow, int latestHouseID)
        {
            _arrow = currentArrow;
            var content = HouseManager.Get().GetHouse(latestHouseID);
            if (content != null)
                _houseContent = (HouseManager.HouseContent)content;
            else
                ToolManager.GetManager().ResetTool();
        }

        private int _i = 0;
        public void UpdateInput()
        {
            if(_i == 0)
                _arrow.Highlight(true);
            _i++;
        }

        public Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found)
        {
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
 
            if (tmp == 0)
            {
                // No solution!
                found = false;
                return Vector2.zero;
            }
 
            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
 
            found = true;
 
            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }

        public void OnRayCast(ARRaycastHit hit)
        {
            var hitPos = Utility.ConvertTo2D(hit.pose.position);
            var housePosition = Utility.ConvertTo2D(_houseContent.HouseObject.transform.position);
            var dist = Vector2.Distance(housePosition, hitPos);
            var scale = _houseContent.Local.offsetObject.transform.localScale.x;

            // x = .5, dist = .3, 1 + 4 + 1 wide
            // x = 1, dist = .6, 1 + 4 + 1 wide
            // x = 2, dist = 1.2, 1 + 4 + 1 wide

            var size = (int)(((dist / scale) - .15f) * 10);
            if (_houseContent.Local.IsPlotChangeDisruptive(_arrow.GetDirectionIndex(), size)) return;
            _houseContent.Distributor.SetPlotSize(_arrow.GetDirectionIndex(), size);
        }

        public void OnToolChange()
        {
            _arrow.Highlight(false);
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }

        public string GetName()
        {
            return "House Resize Tool";
        }
        
        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.MoveTool;
        }
    }
}