using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;

namespace Tools
{
    public class OrientTool : ITool
    {
        /// Flag set to true if the user currently makes an rotation gesture, otherwise false
        private bool rotating = false;
        /// The squared rotation width determining an rotation
        public const float TOUCH_ROTATION_WIDTH = 1; // Always
        /// The threshold in angles which must be exceeded so a touch rotation is recogniced as one
        public const float TOUCH_ROTATION_MINIMUM = .05f;
        /// Start vector of the current rotation
        Vector2 startVector = Vector2.zero;

        private float startDistance = 0;
        
    
        // House Manager to fetch house
        private readonly HouseManager _manager;

        public OrientTool(HouseManager manager)
        {
            _manager = manager;
        }

        private int b = 0;
        private HouseActionDistributor _houseDistributor;
        
        public void UpdateInput()
        {
            // Every 1/2 sec. Update the house variable
            if (b % 30 == 0)
            {
                _houseDistributor = _manager.GetCurrentHouse()?.Distributor;
            }
            b++;
            
            // If not house. Return
            if (_houseDistributor == null)
            {
                rotating = false;
                return;
            }
            
            if (Input.touchCount != 2)
            {
                rotating = false;
                return;
            }
            
            // Get points of touch
            var p1 = Input.touches[0].position;
            var p2 = Input.touches[1].position;
            
            // If this is initial point of rotation. Save variables and return
            if (!rotating) {
                startVector = p2 - p1;
                rotating = startVector.sqrMagnitude > TOUCH_ROTATION_WIDTH;
                startDistance = Vector2.Distance(p2, p1);
                return;
            }
            
            // Both check whether new points results in a scaling or rotation
            PerformScaling(p1, p2);
            PerformRotation(p1, p2);
        }

        private void PerformScaling(Vector2 p1, Vector2 p2)
        {
            // Calculate new distance and difference
            float newDistance = Vector2.Distance(p2, p1);
            float distanceDifference = newDistance - startDistance;
            
            // If difference is sufficient. Perform scaling
            if (Math.Abs(distanceDifference) > 3)
            {
                
                // New scale of object is
                float scale = 1 + distanceDifference / 400;
                
                // Disallow scaling below following
                if (scale < .3f) scale = .3f;
                
                // Get scale. Resize it and distribute new scale
                Vector3 newScale = _houseDistributor.GetScale();
                newScale.Scale(new Vector3(scale, scale, scale));
                _houseDistributor.SetScale(newScale);
                
                // Distribute scale
                startDistance = Vector2.Distance(p2, p1);
            }
        }

        private void PerformRotation(Vector2 p1, Vector2 p2)
        {
            // Get crossvector and angle
            Vector2 currVector = p2 - p1;
            float angleOffset = Vector2.Angle(startVector, currVector);
            
            // Change rotation of object
            if (angleOffset > TOUCH_ROTATION_MINIMUM) {
                Vector3 LR = Vector3.Cross(startVector, currVector);
                
                // z > 0 left rotation, z < 0 right rotation
                if (LR.z > 0)
                    _houseDistributor.Rotate(new Vector3(0,-angleOffset,0));
                else if(LR.z < 0)
                    _houseDistributor.Rotate(new Vector3(0,angleOffset,0));
                
                startVector = currVector;
            }
        }

        public string GetName()
        {
            return "Orientation Tool";
        }
        
        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.OrientTool;
        }
    }
}