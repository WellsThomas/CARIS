using UnityEngine;

namespace Packages.Smoother
{
    public class VectorSmoother
    {
        private float _speed;
        private float _countSinceLast;
        private Vector3 currentVector;
        private Vector3 targetVector;
        private TypeOfSmoothing _typeOfSmoothing;

        private enum TypeOfSmoothing
        {
            DYNAMIC,
            CONSTANT
        }
        
        /**
         * Sets smoothing dynamically
         */
        public VectorSmoother(Vector3 initialVector)
        {
            _speed = 1;
            _countSinceLast = 1;
            currentVector = initialVector;
            _typeOfSmoothing = TypeOfSmoothing.DYNAMIC;
        }
        
        /**
         * Sets smoothing constantly towards
         */
        public VectorSmoother(Vector3 initialVector, int speed)
        {
            _speed = speed;
            _typeOfSmoothing = TypeOfSmoothing.CONSTANT;
            currentVector = initialVector;
        }

        public Vector3 UpdateSmoothing(Vector3 latestUpdate)
        {
            switch (_typeOfSmoothing)
            {
                case TypeOfSmoothing.DYNAMIC:
                    return UpdateSmoothingDynamic(latestUpdate);
                
                case TypeOfSmoothing.CONSTANT:
                    return UpdateSmoothingConstant(latestUpdate);
                    
            }
            
            return Vector3.forward;
        }


        private Vector3 UpdateSmoothingConstant(Vector3 latestUpdate)
        {
            Vector3 diff = latestUpdate - currentVector;
            currentVector += diff / _speed;

            // Return newest value
            return currentVector;
        }

        private Vector3 UpdateSmoothingDynamic(Vector3 latestUpdate)
        {
            // Stabilizes smoothing based on amount of updates with new values
            UpdateSmoothingSpeed(latestUpdate);
            
            // Calculate the actual smoothing
            targetVector = latestUpdate;
            Vector3 diff = currentVector - targetVector;
            currentVector += diff / _speed;

            // Return newest value
            return currentVector;
        }

        private void UpdateSmoothingSpeed(Vector3 latestUpdate)
        {
            if (latestUpdate.Equals(targetVector))
            {
                _countSinceLast++;
                return;
            }

            _speed = _countSinceLast;
            _countSinceLast = 1;
        }
    }
}