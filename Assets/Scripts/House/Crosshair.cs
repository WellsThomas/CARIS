using System;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class Crosshair : MonoBehaviour
    {
        private int _shouldRemoveCrosshair = 0;
        private bool _shouldHide = false;
        private void Start()
        {
            // Ensure proper scaling on scrollhair
            gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        public void ShouldHide(bool active)
        {
            _shouldHide = active;
        }
        
        private void Update()
        {
            // If no updates within 30 updates. Remove it. (User is no longer looking at world)
            if(_shouldRemoveCrosshair == 30 || _shouldHide) gameObject.SetActive(false);
            else if (_shouldRemoveCrosshair < 30) _shouldRemoveCrosshair++;
        }
        
        // Place build crosshair in world according to global position and rotation
        public void SetBuildCrosshair(Vector3 pos, Quaternion rot)
        {
            if (_shouldHide) return;
            GameObject o;
            (o = gameObject).SetActive(true);
            o.transform.position = pos;
            o.transform.rotation = rot;
            _shouldRemoveCrosshair = 0;
        }
    
        // Place crosshair in house plot
        public void SetBuildCrosshairBasedOnLocal(Vector3 localPosition, Quaternion localRotation)
        {
            if (_shouldHide) return;
            GameObject o;
            (o = gameObject).SetActive(true);
            o.transform.localPosition = localPosition;
            o.transform.localRotation = localRotation;
            _shouldRemoveCrosshair = 0;
        }

    }
}