using System;
using System.Collections.Generic;
using Communication;
using Communication.ActionPackage;
using JetBrains.Annotations;
using Packages.Smoother;
using Tools;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;

namespace UI
{
    [RequireComponent(typeof(Transform))]
    public class StalkerHandler : MonoBehaviour
    {
        // Singleton
        private static StalkerHandler _handler;

        public static StalkerHandler Get()
        {
            return _handler;
        }
        
        private void Start()
        {
            _handler = this;
        }
        
        
        // Class
        [SerializeField]
        private GameObject stalkerIcon;
        
        [SerializeField]
        private GameObject StalkerCanvas;
        [SerializeField]
        private GameObject SwapButton;
        [SerializeField]
        private GameObject NormalCanvas;
        [SerializeField]
        private List<Image> participantFeedbackImages = new List<Image>();


        public Dictionary<int, GameObject> stalkers = new Dictionary<int, GameObject>();

        public void AddStalker(int stalkerId, int stalkedId)
        {
            if (!IsMyId(stalkedId)) return;
            
            var icon = Instantiate(stalkerIcon, NormalCanvas.transform);
            // set color
            var material = icon.GetComponent<Image>();
            material.color = Colors.s_Colors[stalkerId];
            
            // Add stalker to dict
            stalkers.TryAdd(stalkerId,icon);
            PositionStalkerIcons();
        }

        private static bool IsMyId(int id)
        {
            return PlayerManager.GetManager().GetLocalPlayerID() == id;
        }

        public void RemoveStalker(int stalkerId, int stalkedId)
        {
            if (!IsMyId(stalkedId)) return;
            
            Destroy(stalkers[stalkerId]);
            stalkers.Remove(stalkerId);
            PositionStalkerIcons();
        }

        private void PositionStalkerIcons()
        {
            var position = 0;
            foreach (var (id, o) in stalkers)
            {
                // Fix position
                var transform = o.GetComponent<RectTransform>();
                transform.position = new Vector2(100, 100 + position*150);
        
                position++;
            }
        }
        
        [CanBeNull] private ARParticipant _currentARParticipant = null;
        [CanBeNull] private Transform _currentTransform = null;
        private int _currentlyStalkedId = -1;
        private VectorSmoother _vectorSmoother;
        private RotateSmoother _rotateSmoother;


        public void StartStalking(int participantID)
        {
            var participant = PlayerManager.GetManager().GetParticipant(participantID);
            StartStalking(participant);
        }

        public void StartStalking(ARParticipant participant)
        {
            if (participant == null) return;
            Debug.Log("StartStalking");
            // Set camera
            CameraActive(true);
            
            // Set variables
            _currentARParticipant = participant;
            _currentlyStalkedId = PlayerManager.GetManager().GetID(_currentARParticipant);
            _currentTransform = _currentARParticipant.transform;
            
            // Disable ToolManager
            //ToolManager.GetManager().DisableTool(true);
            
            // Tell other peers
            InformOfStalking(_currentlyStalkedId, true);
            
            // Begin stalking
            UpdateStalking(true);
            
            // Update colors
            UpdateFeedbackColors(participant);
        }

        private void UpdateFeedbackColors(ARParticipant participant)
        {
            var color = Colors.s_Colors[PlayerManager.GetManager().GetID(participant)];
            var newColor = new Color(color.r, color.g, color.b, .75f);
            foreach (var img in participantFeedbackImages)
            {
                img.color = newColor;
            }
        }

        public void UpdateStalking(bool updateSmoother)
        {
            Debug.Log("Updated");
            if(_currentTransform == null)
            {
                Debug.Log("Stopped");
                StopStalking();
                Debug.Log("Finished");
                return;
            }

            if (updateSmoother)
            {
                _vectorSmoother = new VectorSmoother(_currentTransform.position, 20);
                _rotateSmoother = new RotateSmoother(_currentTransform.rotation, 10);
            }

            var secondaryCameraTransform = CameraManager.GetSecondary().transform;
            secondaryCameraTransform.position = _vectorSmoother.UpdateSmoothing(_currentTransform.position);
            secondaryCameraTransform.rotation = _rotateSmoother.UpdateSmoothing(_currentTransform.rotation);
            secondaryCameraTransform.Rotate(new Vector3(0,0,90));
        }

        public void StopStalking()
        {
            CameraActive(false);
            if (_currentARParticipant != null) 
                InformOfStalking(_currentlyStalkedId, false);
            _currentARParticipant = null;
            _currentTransform = null;
            //ToolManager.GetManager().DisableTool(false);
        }

        public bool IsStalking()
        {
            return _currentTransform != null;
        }

        public void SwapCamera()
        {
            if (!IsStalking()) return;
            
            CameraManager.Swap();
        }
        
        private void CameraActive(bool active)
        {
            CameraManager.ShowBoth(active);
            NormalCanvas.SetActive(!active);
            StalkerCanvas.SetActive(active);
            if (!active) return;

            updateSizeOfSwapButton();
            CameraManager.Swap();
        }

        private void updateSizeOfSwapButton()
        {
            var buttonRect = SwapButton.GetComponent<RectTransform>();
            var camRect = CameraManager.GetSecondary().pixelRect;
            var camSize = new Vector2(camRect.width, camRect.height);
            buttonRect.sizeDelta = camSize;
            buttonRect.position = camRect.position + (camSize / 2);
        }
        
        private void InformOfStalking(int stalked, bool active)
        {
            var myId = PlayerManager.GetManager().GetLocalPlayerID();
            var action = new StalkingAction(myId, stalked, active);
            var stringifier = Stringifier.GetStringifier();
            stringifier.StringifyAndForward<StalkingAction>(action, TypeOfPackage.StalkAction, true);
        }

        public void StalkVisualGuidance(Vector3 pos, Quaternion dir)
        {
            // Set camera
            CameraActive(true);
            
            // Set variables
            _currentTransform = new GameObject().transform;
            _currentARParticipant = null;
            _currentTransform.position = pos;
            _currentTransform.rotation = dir;
            
            // Initiate stalking
            UpdateStalking(true);
        }
    }
}