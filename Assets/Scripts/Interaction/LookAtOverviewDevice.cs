using System;
using Player;

namespace UnityEngine.XR.ARFoundation.Samples.Interaction
{
    public class LookAtOverviewDevice
    {
        private readonly OverviewManager _overviewManager;
        private int _variableForDecreasingSpam;
        private bool _hasHitOverviewDevice;

        public LookAtOverviewDevice(OverviewManager overviewManager)
        {
            _overviewManager = overviewManager;
        }
        
        public void OnPhysicsRayCast(RaycastHit hit)
        {
            // Check if raycast hit the overview-device. Set activeness depending on this
            SetOverviewDeviceActiveness(hit.collider.gameObject.name == "OverviewPart");
        }

        public void SetOverviewDeviceActiveness(bool active)
        {
            var overviewDevice = _overviewManager.GetOverviewDevice();
            if (overviewDevice == null) return;
            _hasHitOverviewDevice = active;
            overviewDevice.SetOverviewActive(_hasHitOverviewDevice);
        }

        public bool OnClick()
        {
            if (!_hasHitOverviewDevice) return false;
            Debug.Log("Just clicked on overview device");
            

            return true;
        }
    }
}