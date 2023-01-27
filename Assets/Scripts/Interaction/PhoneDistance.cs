using System.Collections.Generic;

namespace UnityEngine.XR.ARFoundation.Samples.Interaction
{
    /**
     * Component for detecting and reacting on phones coming close.
     * To detect this. Whenever a participant joins or leaves in the ARParticipants
     * a local list of pariticpants is updated.
     *
     * The class checks every update whether the distance of the local phone and each 
     */
    public class PhoneDistance : MonoBehaviour
    {
        
        // REQUIRES ARParticipantManager!
        [SerializeField] private GameObject XROrigin;
        private ARParticipantManager _arParticipantManager;
        private Shaking _shaking;
        
        [SerializeField]
        private GameObject closeProxemicsButton;

        private void Start()
        {
            _arParticipantManager = XROrigin.GetComponent<ARParticipantManager>();
            _shaking = gameObject.GetComponent<Shaking>();
            
            closeProxemicsButton.SetActive(false);
        }
        
        private void Update()
        {
            AssessAllDistances();
        }
    
        /**
         * Assess distance to each participant and react if under threshold
         */
        private void AssessAllDistances()
        {
            foreach (var arParticipant in _arParticipantManager.trackables)
            {
                AssessDistance(arParticipant);
            }
            
            if (0 < shakeCooldown) shakeCooldown--;
        }

        // Shakecooldown to prevent spam of operations triggered by shake
        private int shakeCooldown = 0;

        /**
         * Assess distance to a participant and react if under threshold
         */
        private void AssessDistance(ARParticipant arParticipant)
        {
            var participantPos = arParticipant.transform.position;
            var dist = Vector3.Distance(Camera.main.transform.position,participantPos);

            // If within 40cm and no cooldown
            if (dist < .40f && _shaking != null && shakeCooldown == 0)
            {
                
                // If sufficient shake
                if (_shaking.GetMovement() > 1.35f)
                {
                    // Do stuff
                    shakeCooldown = 90;
                    Debug.Log("Close shaking");
                    Handheld.Vibrate();
                    // run action ...
                }
            }

            // If very close. enable/disable closeProxemicsButton
            closeProxemicsButton.SetActive(dist < .16f);
        }
    }
}