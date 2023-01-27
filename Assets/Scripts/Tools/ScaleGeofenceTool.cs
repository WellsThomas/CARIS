using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class ScaleGeofenceTool : ITool
    {
        public ScaleGeofenceTool()
        {
            _geofenceManager = GeofenceManager.Get();
        }

        private GeofenceManager _geofenceManager;

        public string GetName()
        {
            return "Orient Geofence Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.MoveTool;
        }

        private float initialDistance = 0;

        public TrackableType GetTrackableType()
        {
            return TrackableType.PlaneWithinBounds;
        }

        private Vector3 _startRotateVector = Vector3.zero;

        public void OnRayCast(ARRaycastHit hit)
        {
            // If first hit. Save the hit and wait for next to see difference
            if (initialDistance == 0)
            {
                if (_geofenceManager.position == null) return;
                
                initialDistance = GetDistanceIn2D(_geofenceManager.position.Value, hit.pose.position);
            }
            
            var newDistance = GetDistanceIn2D(_geofenceManager.position.Value, hit.pose.position);
            var applyScale = newDistance / initialDistance;
            if (Math.Abs(applyScale - 1) < .015f)
                applyScale = 1;
            else
                initialDistance = newDistance;
            
            // Get rotation
            var rotation = CalculateRotation(hit);
            
            _geofenceManager.SetScaleRotation(applyScale, rotation);
        }
        
        
        
        private Vector3 CalculateDirection(ARRaycastHit hit)
        {
            return _geofenceManager.position.Value - hit.pose.position;
        }

        private Vector3 CalculateRotation(ARRaycastHit hit)
        {
            if (_startRotateVector == Vector3.zero)
            {
                _startRotateVector = CalculateDirection(hit);
                return Vector3.zero;
            }
            
            // Get crossvector and angle
            var currVector = CalculateDirection(hit);
            float angleOffset = Vector3.Angle(_startRotateVector, currVector);
                
            Vector3 LR = Vector3.Cross(_startRotateVector, currVector);
                
            // z > 0 left rotation, z < 0 right rotation
            var rotation = new Vector3(0,angleOffset,0);
            rotation.y *= LR.y < 0 ? -1 : 1; // Invert if to the left
            _startRotateVector = currVector;


            return rotation;
        }
        
        
        

        private float GetDistanceIn2D(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z),new Vector2(b.x, b.z));
        }

        public void OnToolChange()
        {
            // Send changes to other participants
            _geofenceManager.DistributeScaleRotation();

            _startRotateVector = Vector3.zero;
        }
    }
}

