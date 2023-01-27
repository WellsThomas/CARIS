using UnityEngine;

namespace Player
{
    public class OverviewManager : MonoBehaviour
    {
        private OverviewDevice overviewDeviceDevice { get; set; }

        // Start is called before the first frame update
        public void OnEnable()
        {
            // Add event listener for when phone finds the overview device
            OverviewDevice.OnNewOverviewDevice += SetCurrentOverviewDevice;
            OverviewDevice.OnOverviewDeviceRemoved += RemoveCurrentOverviewDevice;
        }

        public void OnDisable()
        {
            // remove event listener for when phone finds the overview device
            OverviewDevice.OnNewOverviewDevice -= SetCurrentOverviewDevice;
            OverviewDevice.OnOverviewDeviceRemoved -= RemoveCurrentOverviewDevice;
        }

        private void SetCurrentOverviewDevice(OverviewDevice o)
        {
            overviewDeviceDevice = o;
        }

        private void RemoveCurrentOverviewDevice(OverviewDevice o)
        {
            overviewDeviceDevice = null;
        }

        public OverviewDevice GetOverviewDevice()
        {
            return overviewDeviceDevice;
        }
    }
}
