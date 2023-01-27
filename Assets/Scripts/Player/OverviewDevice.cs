using UnityEngine;

namespace Player
{
    public class OverviewDevice : MonoBehaviour
    {
        // This class simply posts events that the Overview manager can listen to. This way, whenever an overview tablet is found,
        // the manager will be able to tell.
        public static event OverviewFound OnNewOverviewDevice;
        public static event OverviewRemoved OnOverviewDeviceRemoved;
        [SerializeField]
        private Material passiveMaterial;
        [SerializeField]
        private Material activeMaterial;

        public void OnEnable()
        {
            OnNewOverviewDevice?.Invoke(this);
        }

        public void OnDisable()
        {
            OnOverviewDeviceRemoved?.Invoke(this);
        }
    
        public delegate void OverviewFound(OverviewDevice o);
        public delegate void OverviewRemoved(OverviewDevice o);

        public void SetOverviewActive(bool active)
        {
            foreach (var meshRenderer in gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = active ? activeMaterial : passiveMaterial;
            }
        }
    }
}
