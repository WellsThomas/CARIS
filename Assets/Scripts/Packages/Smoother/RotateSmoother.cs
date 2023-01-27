using System;
using UnityEngine;

namespace Packages.Smoother
{
    public class RotateSmoother
    {
        private Vector3 _currentRotation;
        private int _speed;

        
        /**
         * Sets smoothing constantly towards
         */
        public RotateSmoother(Quaternion initialRotation, int speed)
        {
            _speed = speed;
            _currentRotation = initialRotation.eulerAngles;
        }
        
        public Quaternion UpdateSmoothing(Quaternion latestUpdate)
        {
            var diff = GetDiff(latestUpdate.eulerAngles);
            _currentRotation += diff / _speed;
            _currentRotation = HandleOverflow(_currentRotation);
            
            // Return newest value
            return Quaternion.Euler(_currentRotation);
        }

        private Vector3 GetDiff(Vector3 latest)
        {
            var diff = latest - _currentRotation;

            switch (diff.x)
            {
                case > 180:
                    diff.x -= 360;
                    break;
                case < -180:
                    diff.x += 360;
                    break;
            }
            
            switch (diff.y)
            {
                case > 180:
                    diff.y -= 360;
                    break;
                case < -180:
                    diff.y += 360;
                    break;
            }
            
            switch (diff.z)
            {
                case > 180:
                    diff.z -= 360;
                    break;
                case < -180:
                    diff.z += 360;
                    break;
            }

            return diff;
        }
        
        private Vector3 HandleOverflow(Vector3 vector)
        {
            switch (vector.x)
            {
                case > 360:
                    vector.x -= 360;
                    break;
                case < 0:
                    vector.x += 360;
                    break;
            }
            
            switch (vector.y)
            {
                case > 360:
                    vector.y -= 360;
                    break;
                case < 0:
                    vector.y += 360;
                    break;
            }
            
            switch (vector.z)
            {
                case > 360:
                    vector.z -= 360;
                    break;
                case < 0:
                    vector.z += 360;
                    break;
            }

            return vector;
        }
    }
}