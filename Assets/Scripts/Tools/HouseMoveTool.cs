using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class HouseMoveTool : ITool
    {
        private HouseManager.HouseContent _houseToMove;
        private Vector3? firstHit;
        private ARRaycastHit lastRayHit;
        private bool currentlyReplaces;
        private readonly HouseManager _houseManager;

        public HouseMoveTool(HouseManager.HouseContent houseToMove, HouseManager houseManager)
        {
            _houseManager = houseManager;
            _houseToMove = houseToMove;
            firstHit = null;
        }
        
        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }

        private Vector3 offsetHit;
        
        public void OnRayCast(ARRaycastHit hit)
        {
            lastRayHit = hit;
            
            // If first hit. Save the hit and wait for next to see difference
            if (firstHit == null)
            {
                _houseToMove.Local.EnableReplacable(false);
                firstHit = hit.pose.position;
                offsetHit = (Vector3) (firstHit - _houseToMove.Local.gameObject.transform.position);
                return;
            }

            if (IsReplacingOtherHouse(hit.pose)) return;
            
            
            if (lastHouse != null)
            {
                _houseManager.StopVisualizeReplacement(_houseToMove.Local.GetID(), lastHouse.Value.Local.GetID(), true);
            }
            lastHouse = null;
            
            _houseToMove.Distributor.OffsetHouse(hit.pose.position - (Vector3) firstHit);
        }

        [CanBeNull] private HouseManager.HouseContent? lastHouse;
        private Quaternion originalRotation;
        private Vector3 originalScale;

        private bool IsReplacingOtherHouse(Pose poseOfHit)
        {
            var ray = new Ray(poseOfHit.position - offsetHit + new Vector3(0,2f,0), Vector3.down);
            //var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // If build tool. Perform special raycast
            int layerMask = 1 << 6;
            RaycastHit hit;
            currentlyReplaces = Physics.Raycast(ray, out hit, 2.5f, layerMask);
            if (!currentlyReplaces) return false;

            var house = hit.collider.gameObject.GetComponentInParent<House>();
            if (house == null)
            {
                currentlyReplaces = false;
                return false;
            }

            var houseContent =_houseManager.GetHouse(house.GetID());
            if (houseContent == null)
            {
                return false;
            }
            
            // For every other time hitting the prop. Do nothing
            if (Equals(houseContent, lastHouse)) return true;

            // Update lastHouse
            lastHouse = houseContent;

            _houseManager.StartVisualizeReplacement(_houseToMove.Local.GetID(), houseContent.Value.Local.GetID(), true);

            return true;
        }

        public void OnToolChange()
        {
            if (lastHouse != null)
            {
                _houseManager.EraseHouseGlobally(lastHouse.Value.Distributor.GetID());
            }
            
            // Get current pose
            var pose = new Pose(_houseToMove.Local.offsetObject.transform.position, _houseToMove.HouseObject.transform.rotation);
            
            // Ship this update
            _houseToMove.Distributor.MoveHouse(pose);
            
            // Enable replacable again
            _houseToMove.Local.EnableReplacable(true);
        }

        public string GetName()
        {
            return "World Move Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.MoveTool;
        }
    }
}
