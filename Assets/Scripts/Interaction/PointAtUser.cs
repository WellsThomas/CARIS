using System;
using System.Collections.Generic;
using Communication.ActionPackage;
using JetBrains.Annotations;
using Tools;
using UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;

namespace UnityEngine.XR.ARFoundation.Samples.Interaction
{
    public class PointAtUser : MonoBehaviour
    {
        [SerializeField] private GameObject phoneIndicatorObject;
        private ToolManager _toolManager;
        private PlayerManager _playerManager;

        // REQUIRES ARParticipantManager!
        [SerializeField] private GameObject XROrigin;
        private ARParticipantManager _arParticipantManager;
        private HouseManager _houseManager;

        private Dictionary<ARParticipant, GameObject> anchors = new Dictionary<ARParticipant, GameObject>();
        private Dictionary<GameObject, ARParticipant> participants = new Dictionary<GameObject, ARParticipant>();

        private void Start()
        {
            _arParticipantManager = XROrigin.GetComponent<ARParticipantManager>();
            _arParticipantManager.participantsChanged += ParticipantsChanged;
            _toolManager = XROrigin.GetComponent<ToolManager>();
            _houseManager = XROrigin.GetComponent<HouseManager>();
            _playerManager = XROrigin.GetComponent<PlayerManager>();
        }
        

        private void ParticipantsChanged(ARParticipantsChangedEventArgs args)
        {
            if(args.added.Count != 0) AddParticipants(args.added);
            if(args.removed.Count != 0) RemoveParticipants(args.removed);
        }

        private void RemoveParticipants(List<ARParticipant> deleted)
        {
            foreach (var arParticipant in deleted)
            {
                if (anchors.TryGetValue(arParticipant, out var gameObjectToDelete))
                {
                    Destroy(gameObjectToDelete);
                    participants.Remove(gameObjectToDelete);
                }
                anchors.Remove(arParticipant);
            }
        }

        private void AddParticipants(List<ARParticipant> added)
        {
            foreach (var arParticipant in added)
            {
                var newObject = Instantiate(phoneIndicatorObject);
                anchors.Add(arParticipant, newObject);
                participants.Add(newObject, arParticipant);
            }
        }

        private GameObject lastHitObject;
        private int ticksTillColorUpdate;

        private void UpdatePhoneIndicatorColor(GameObject indicator, int id)
        {
            var spriteRenderer = indicator.GetComponentInChildren<SpriteRenderer>();
            var newColor = Colors.s_Colors[id % Colors.s_Colors.Length];
            spriteRenderer.color = newColor;
        }

        private bool isLooking;
        
        private void Update()
        {
            // Update position and rotation of anchors
            foreach (var (arParticipant, phoneIndicator) in anchors)
            {
                var participantPos = arParticipant.transform.position;

                phoneIndicator.transform.position = participantPos;
                var diff = participantPos - Camera.main.transform.position;
                var newRotation = Quaternion.LookRotation(diff,Vector3.up);
                phoneIndicator.transform.rotation = newRotation;

                if (ticksTillColorUpdate % 60 == 30)
                {
                    UpdatePhoneIndicatorColor(phoneIndicator, _playerManager.GetID(arParticipant));
                }
            }
            ticksTillColorUpdate++;
            
            _toolManager.SetCurrentARParticipant(GetARParticipant(lastHitObject));

            // Throw raycast. If any hits, it scales that object and rescales the other one
            const int layerMask = 1 << 11;
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask))
            {
                var hitObject = hit.collider.gameObject.transform.parent?.gameObject;
                if (hitObject == null || hitObject == lastHitObject) return;
                if(lastHitObject) lastHitObject.transform.localScale = new Vector3(1, 1, 1);
                hitObject.transform.localScale = new Vector3(2, 2, 2);
                lastHitObject = hitObject;
            }
            else
            {
                if(lastHitObject) lastHitObject.transform.localScale = new Vector3(1, 1, 1);
                lastHitObject = null;
            }
            
        }

        private ARParticipant GetARParticipant(GameObject obj)
        {
            return obj == null ? null : participants.GetValueOrDefault(obj, null);
        }


    }
}