using System;

namespace UnityEngine.XR.ARFoundation.Samples.Interaction
{
    public class Shaking : MonoBehaviour
    {
        private float movement { get; set; }

        private void Update()
        {
            movement = Vector3.Distance(Vector3.zero, Input.acceleration) / 45 + movement - movement / 45;
        }

        public float GetMovement()
        {
            return movement;
        }
    }
}