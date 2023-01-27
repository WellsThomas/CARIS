using System;
using System.Collections.Generic;
using System.Linq;
using Communication;
using Communication.ActionPackage;
using JetBrains.Annotations;
using UI;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using Random = UnityEngine.Random;

namespace Interaction.VisualGuide
{
    public class VisualGuidanceManager : MonoBehaviour
    {
        [SerializeField] private GameObject guidanceObject;

        private readonly Dictionary<int, GameObject> _guidanceObjects = new Dictionary<int, GameObject> ();
        private readonly Dictionary<int, GuidingObject.GuidingObjectData> _guidanceDataObjects = new Dictionary<int, GuidingObject.GuidingObjectData>();
        private readonly List<AddGuidance> _unprocessedAddGuidanceActions = new List<AddGuidance>();
        private ARAnchorManager _anchorManager;
        private static VisualGuidanceManager _guidanceManager;
        [CanBeNull] private GuidingObject _activeGuidingObject;
        private int i = 0;
        public void Update()
        {
            i++;
            if(i%15 == 0) CheckForAnchorBasedActions();
        }

        public bool UpdateActiveGuidingObject([CanBeNull] GameObject hitObject)
        {
            if(_activeGuidingObject != null) _activeGuidingObject.SetActive(false);
            _activeGuidingObject = null;
            if (hitObject == null || hitObject.layer != 13) return false;
            
            while (hitObject.name != "Visual Guide(Clone)" && hitObject.transform.parent != null)
                hitObject = hitObject.transform.parent.gameObject;

            if (hitObject.name != "Visual Guide(Clone)") return false;

            _activeGuidingObject = hitObject.GetComponent<GuidingObject>();
            if (_activeGuidingObject != null)
            {
                _activeGuidingObject.SetActive(true);
                return true;
            }

            return false;
        }

        public void AddGuidanceToUnprocessed(AddGuidance addGuidance)
        {
            _unprocessedAddGuidanceActions.Add(addGuidance);
        }

        void CheckForAnchorBasedActions()
        {
            _anchorManager = HouseManager.Get().GetAnchorManager();
            
            // Check for each unprocessed addGuidanceAction
            foreach (AddGuidance addGuidance in _unprocessedAddGuidanceActions)
            {
                // Whether any corresponding anchor has the right ID
                var anchor = GetAnchor(addGuidance.AnchorID);
                if (anchor == null) continue;

                // If so. Add Guidance
                var anchorTransform = anchor.transform;
                SpawnAndMoveGuidance(addGuidance.GuidanceID,anchorTransform.position,anchorTransform.rotation);

                // Remove from unprocessed
                _unprocessedAddGuidanceActions.Remove(addGuidance);
            }
        }
        
        private ARAnchor GetAnchor(string id)
        {
            foreach (var anchor in _anchorManager.trackables)
            {
                if (id != anchor.trackableId.ToString()) continue;
                return anchor;
            }

            return null;
        }

        public void Awake()
        {
            _guidanceManager = this;
        }

        public static VisualGuidanceManager Get()
        {
            return _guidanceManager;
        }

        public void SpawnGuidance()
        {
            var transform1 = Camera.current.transform;
            var camPos = transform1.position;
            var forward = transform1.forward;
            var position = camPos + forward * 0.16f;
            var direction = Quaternion.LookRotation(forward, Vector3.up);
            var newID = Random.Range(Int32.MinValue, Int32.MaxValue);
            
            
            SpawnAndMoveGuidance(newID, position, direction);
            
            // Distribute
            var anchor = HouseManager.Get().CreateAnchor(new Pose(position,direction));
            var action = new AddGuidance(anchor.trackableId.ToString(), newID);
            Stringifier.GetStringifier().StringifyAndForward<AddGuidance>(action, TypeOfPackage.AddGuidance, true);
        }

        public GameObject SpawnAndMoveGuidance(int id, Vector3 pos, Quaternion direction)
        {
            var guide = Instantiate(guidanceObject);
            guide.transform.position = pos;
            guide.transform.localScale = new Vector3(.15f, .15f, .15f);
            guide.transform.rotation = direction;
            guide.transform.Rotate(new Vector3(0, 90, 90));
            var guidingObject = guide.GetComponent<GuidingObject>();
            guidingObject.SetID(id);
            guidingObject.Direction = direction;
            guidingObject.Position = pos;

            _guidanceObjects.Add(id, guide);
            _guidanceDataObjects.Add(id, guidingObject.ProduceDataObject());

            return guide;
        }

        public void RemoveGuidance(int id)
        {
            if (!RemoveGuidanceLocally(id)) return;
            var action = new RemoveGuidance(id);
            Stringifier.GetStringifier().StringifyAndForward<RemoveGuidance>(action, TypeOfPackage.RemoveGuidance, true);
        }

        public bool RemoveGuidanceLocally(int id)
        {
            if (!_guidanceObjects.TryGetValue(id, out var guide)) return false;
            Destroy(guide);
            _guidanceObjects.Remove(id);
            _guidanceDataObjects.Remove(id);
            return true;
        }
        
        public bool RemoveActiveGuidance()
        {
            if (_activeGuidingObject == null) return false;
            RemoveGuidance(_activeGuidingObject.GetID());

            return true;
        }

        public bool OnClick()
        {
            if (_activeGuidingObject == null) return false;
            // _activeGuidingObject position
            var dir = _activeGuidingObject.Direction * Quaternion.Euler(Vector3.back * 90);
            var pos = _activeGuidingObject.Position + dir * Vector3.back * .16f;

            Debug.Log("Start stalking");
            StalkerHandler.Get().StalkVisualGuidance(pos, dir);
            
            return true;
        }

        public List<GuidingObject.GuidingObjectData> ProduceDataObjectList()
        {
            return _guidanceDataObjects.Values.ToList();
        }

        private readonly List<TemporaryGuideObject> _temporaryGuides = new List<TemporaryGuideObject>();

        public void BuildTemporaryGuideObjectsFromData(List<GuidingObject.GuidingObjectData> list, GameObject helper)
        {
            foreach (var guidingObjectData in list)
                _temporaryGuides.Add(
                    new TemporaryGuideObject(
                        guidingObjectData.ID,
                        guidingObjectData.Position,
                        guidingObjectData.Direction,
                        helper
                        )
                    );
        }

        public void EraseCurrentGuides()
        {
            foreach (var (key, guide) in _guidanceObjects)
            {
                Destroy(guide);
            }
            _guidanceObjects.Clear();
            _guidanceDataObjects.Clear();
        }

        public void ReplaceTemporaryGuides()
        {
            foreach (var temporaryGuideObject in _temporaryGuides)
                temporaryGuideObject.ReplaceWithGuideObject();
            _temporaryGuides.Clear();
        }
    }
}