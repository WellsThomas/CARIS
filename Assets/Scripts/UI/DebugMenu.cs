using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UI
{
    public class DebugMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject debugPlanePrefab;
        [SerializeField]
        private GameObject debugAnchorPrefab;
        [SerializeField]
        private GameObject debugPointCloudPrefab;
        [SerializeField]
        private GameObject debugParticipantPrefab;
        [SerializeField]
        private GameObject xrOrigin;

        private ARPlaneManager _planeManager;

        private ARParticipantManager _participantManager;

        private ARPointCloudManager _pointCloudManager;

        private ARAnchorManager _anchorManager;

        public void Start()
        {
            _planeManager = xrOrigin.GetComponent<ARPlaneManager>();
            _participantManager = xrOrigin.GetComponent<ARParticipantManager>();
            _pointCloudManager = xrOrigin.GetComponent<ARPointCloudManager>();
            _anchorManager = xrOrigin.GetComponent<ARAnchorManager>();
        }

        /**
         * Hide or show all gameObjects used for debugging
         * Currently does not work ....
         */
        public void SetDebugMode(bool active)
        {
            _planeManager.planePrefab = active ? debugPlanePrefab : null;
            foreach (var plane in _planeManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }


            _participantManager.participantPrefab = active ? debugParticipantPrefab : null;
            foreach (var plane in _participantManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }

            
            _pointCloudManager.pointCloudPrefab = active ? debugPointCloudPrefab  : null;
            foreach (var plane in _pointCloudManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }

            
            _anchorManager.anchorPrefab = active ? debugAnchorPrefab : null;
            foreach (var plane in _anchorManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }

        }
    }
}