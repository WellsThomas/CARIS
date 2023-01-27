using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Communication;
using Communication.ActionPackage;
using JetBrains.Annotations;
using Packages;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation.Samples.Communication;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class GeofenceManager : MonoBehaviour
    {
        private static GeofenceManager _geofenceManager;
        
        [SerializeField] private GameObject pole1;
        [SerializeField] private GameObject pole2;
        [SerializeField] private GameObject pole3;
        [SerializeField] private GameObject pole4;

        [SerializeField] private GameObject fence1;
        [SerializeField] private GameObject fence2;
        [SerializeField] private GameObject fence3;
        [SerializeField] private GameObject fence4;

        [SerializeField] private Material regularMaterial;
        [SerializeField] private Material highlightedMaterial;
        
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private GameObject scaleObject;

        private int _currentPoleIndex = 0;
        private bool _isConstructed = false;
        private bool _isConstructing = false;

        public Vector3? position { get; private set; } = null;
        private Quaternion _rotation;
        private Vector2 _scale;
        
        private List<House> _housesInsideGeofence = new List<House>();
        
        // Event which is triggered when new data is available. NetworkHandlers can subscribe to this to export information
        public static event GeofenceEvent<Geofence> OnGeofenceUpdate;
        
        // Required to define an event. See public static event in Stringifier class
        public delegate void GeofenceEvent<in TGeofence>([CanBeNull] Geofence? geofence);

        private List<MeshRenderer> meshes = new List<MeshRenderer>();

        private void Start()
        {
            _geofenceManager = this;
            foreach (var meshRenderer in pole1.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in pole2.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in pole3.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in pole4.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in fence1.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in fence2.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in fence3.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
            foreach (var meshRenderer in fence4.GetComponentsInChildren<MeshRenderer>())
                meshes.Add(meshRenderer);
        }

        public static GeofenceManager Get()
        {
            return _geofenceManager;
        }

        private bool isHighlighted = false;
        
        public void SetHighlight(bool active)
        {
            if (_isConstructing) active = false;
            
            if (active == isHighlighted) return;
            isHighlighted = active;
            
            SetMaterialOfFence(active ? highlightedMaterial : regularMaterial);
        }

        private void SetMaterialOfFence(Material newMaterial)
        {
            foreach (var meshRenderer in meshes)
            {
                meshRenderer.material = newMaterial;
            }
        }

        public void StartSetup()
        {
            SetActiveAll(false);
            _currentPoleIndex = 0;
            _isConstructing = true;
            _isConstructed = false;
            InvokeGeofenceRemovedEvent();
            pole1.SetActive(true);
        }

        public void InvokeGeofenceRemovedEvent()
        {
            OnGeofenceUpdate?.Invoke(null);
        }

        public void SetPositionOfCurrent(Vector3 position)
        {
            if (_isConstructing == false) return;

            if (_currentPoleIndex == 0)
                SetFirstPole(position);
            else if (_currentPoleIndex == 1)
                SetSecondPole(position);
            else if (_currentPoleIndex == 2)
                SetThirdPole(position);
            
        }
        
        // Sets first corner pole
        private void SetFirstPole(Vector3 position)
        {
            pole1.transform.position = position;
        }
        
        // Sets second corner pole along with rotation
        private void SetSecondPole(Vector3 position)
        {
            var position1 = pole1.transform.position;
            position.y = position1.y;
            pole2.transform.position = position;
            AdjustFenceFromPoints(fence1.transform, position1, position);
        }
        
        private void SetThirdPole(Vector3 position)
        {
            var point1 = pole1.transform.position;
            var point2 = pole2.transform.position;
            position.y = point1.y;
            var point3 = Utility.CalculateThirdPoint(point1, point2, position);
            
            pole3.transform.position = point3;
            var point4 = (point1 - point2) + point3;
            
            pole4.transform.position = point4;
            AdjustFenceFromPoints(fence2.transform, point2, point3);
            AdjustFenceFromPoints(fence3.transform, point3, point4);
            AdjustFenceFromPoints(fence4.transform, point4, point1);

            // Inform houses about this
            InvokeGeofenceChangeEvent(Utility.ConvertTo2D(point1),Utility.ConvertTo2D(point2),Utility.ConvertTo2D(point3),Utility.ConvertTo2D(point4));
        }

        public void SetScaleRotation(float scale, Vector3 rotate)
        {
            if (position == null) return;
            scaleObject.transform.position = (Vector3) position;
            scaleObject.transform.rotation = _rotation;
            
            // Set all houses parent to scale object
            foreach (var house in _housesInsideGeofence)
            {
                house.transform.SetParent(scaleObject.transform);
            }

            // Scale and rotate as necessary
            scaleObject.transform.localScale *= scale;
            scaleObject.transform.Rotate(rotate);
            
            // Rebuild geofence based on new scale
            BuildFromValues(position.Value, scaleObject.transform.rotation, _scale * scale);
            
            // Detach from scaleobject
            scaleObject.transform.DetachChildren();
            
            // Transfer scale from house object to house offset
            foreach (var house in _housesInsideGeofence)
            {
                var houseTransform = house.transform;
                var scaling = houseTransform.localScale.x;
                house.offsetObject.transform.localScale *= scaling;
                houseTransform.localScale = Vector3.one;
            }
        }

        public void DistributeScaleRotation()
        {
            if (position == null) return;
            var ScaleRotateHouseActionList = new List<ScaleRotateHouseAction>();

            var anchorPose = new Pose((Vector3)_geofenceManager.position, _geofenceManager._rotation);
            var anchor = HouseManager.Get().CreateAnchor(anchorPose);
            
            var index = 0;
            for (; index < _housesInsideGeofence.Count; index++)
            {
                var house = _housesInsideGeofence[index];
                var housePose = house.GetPose();
                var offsetPos = housePose.position - anchorPose.position;
                var newOffsetPosInversed = Quaternion.Inverse(anchorPose.rotation) * offsetPos;
                var newOffsetRotation = Quaternion.Inverse(anchorPose.rotation) * housePose.rotation;

                var scaleRotateHouse = new ScaleRotateHouseAction(
                    house.GetID(),
                    newOffsetPosInversed,
                    house.GetLocalScale(),
                    newOffsetRotation
                    );
                
                ScaleRotateHouseActionList.Add(scaleRotateHouse);
            }

            var anchorID = anchor.trackableId.ToString();
            
            var action = new ScaleRotateGeofenceAction(ScaleRotateHouseActionList, anchorID);
            Stringifier.GetStringifier().StringifyAndForward<ScaleRotateGeofenceAction>(action, TypeOfPackage.ScaleRotateGeofence, true);
            
            var buildAction = new GeofenceBuildAction(anchorID, _scale);
            Stringifier.GetStringifier().StringifyAndForward<GeofenceBuildAction>(buildAction, TypeOfPackage.CreateNewGeofence, true);
        }

        private readonly ArrayList _unprocessedRotateScaleActions = new ArrayList();

        public void AddUnprocessedRotateScaleActions(ScaleRotateGeofenceAction newAction)
        {
            _unprocessedRotateScaleActions.Add(newAction);
        }
        
        public void CheckUnprocessedRotateScaleAction(HouseManager manager)
        {
            // Check for each unprocessed move action
            var count = _unprocessedRotateScaleActions.Count;
            if (count == 0) return;
            var action = (ScaleRotateGeofenceAction) _unprocessedRotateScaleActions[count-1];
            
            // Whether any corresponding anchor has the right ID
            var anchor = manager.GetAnchor(action.anchorID);
            if (anchor == null) return;

            // If so. Create or move house
            var anchorTransform = anchor.transform;

            SetScaleFromOthers(action, anchorTransform);
            
            // Remove from unprocessed
            _unprocessedRotateScaleActions.Remove(action);
        }

        private void SetScaleFromOthers(ScaleRotateGeofenceAction actions, Transform anchorTransform)
        {
            var anchorPos = anchorTransform.position;
            var anchorRot = anchorTransform.rotation;
            var houseManager = HouseManager.Get();
            
            foreach (var action in actions.rotateScaleHouses)
            {
                var house = houseManager.GetHouse(action.houseID);
                if (house == null) return;
                
                var posOffsetMirrored = anchorRot * action.offsetPos;

                var moveTo = new Pose(anchorPos + posOffsetMirrored, anchorRot * action.offsetRotation);
                
                house.Value.Local.SetLocalScale(action.scale);
                house.Value.Local.MoveHouse(moveTo);
            }
        }

        public struct Geofence
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;
            public Vector2 D;

            public Geofence(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                A = a;
                B = b;
                C = c;
                D = c;
            }
        }
        
        [Serializable]
        public struct SerializableGeofence
        {
            public SerializableVector3 position;
            public SerializableQuaternion rotation;
            public SerializableVector2 scale;
            public SerializableGeofence(Vector3 position, Quaternion rotation, Vector2 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }
        }

        public void AddHouseToList(House house)
        {
            if (_housesInsideGeofence.Contains(house)) return;
            
            _housesInsideGeofence.Add(house);
        }
        
        public void RemoveHouseFromList(House house)
        {
            _housesInsideGeofence.Remove(house);
        }

        public Geofence? GetGeofence()
        {
            if (!_isConstructing && !_isConstructed || _isConstructing && !pole3.activeSelf) return null;
            var p1 = Utility.ConvertTo2D(pole1.transform.position);
            var p2 = Utility.ConvertTo2D(pole2.transform.position);
            var p3 = Utility.ConvertTo2D(pole3.transform.position);
            var p4 = Utility.ConvertTo2D(pole4.transform.position);
            return new Geofence(p1, p2, p3, p4);
        }

        public SerializableGeofence? GetSerializableGeofence()
        {
            if (!_isConstructing && !_isConstructed || _isConstructing && !pole3.activeSelf) return null;

            if (position != null)
                return new SerializableGeofence(position.Value
                    , _rotation
                    , _scale
                );
            return null;
        }

        public bool PlaceCurrentPole()
        {
            if (_currentPoleIndex == 0)
            {
                _currentPoleIndex++;
                pole2.SetActive(true);
                fence1.SetActive(true);
                return false;
            }
            if (_currentPoleIndex == 1)
            {
                _currentPoleIndex++;
                SetActiveAll(true);
                return false;
            }
            _currentPoleIndex++;

            SetupNewGeofence();
            return true;
        }

        private void SetupNewGeofence()
        {
            _isConstructing = false;
            _isConstructed = true;
            
            var p1 = pole1.transform.position;
            var p2 = pole2.transform.position;
            var p3 = pole3.transform.position;
            var p4 = pole4.transform.position;

            position = p1 + (p3 - p1) / 2;
            _rotation = Quaternion.LookRotation(p2 - p1);
            var x = Vector3.Distance(p1, p2);
            var y = Vector3.Distance(p2, p3);
            _scale = new Vector2(x, y);
            
            // Inform houses about this
            InvokeGeofenceChangeEvent(Utility.ConvertTo2D(p1),Utility.ConvertTo2D(p2),Utility.ConvertTo2D(p3),Utility.ConvertTo2D(p4));

            // Distribute this
            DistributeNewGeofence(position.Value, _rotation, _scale);
        }

        public void FixGeofenceFromPlacement(bool fixScale = false)
        {
            var p1 = pole1.transform.position;
            var p2 = pole2.transform.position;
            var p3 = pole3.transform.position;
            var p4 = pole4.transform.position;
            if (fixScale)
            {
                gameObject.transform.localScale = Vector3.one;
                AdjustFenceFromPoints(fence1.transform, p1, p2);
                AdjustFenceFromPoints(fence2.transform, p2, p3);
                AdjustFenceFromPoints(fence3.transform, p3, p4);
                AdjustFenceFromPoints(fence4.transform, p4, p1);
                pole1.transform.position = p1;
                pole2.transform.position = p2;
                pole3.transform.position = p3;
                pole4.transform.position = p4;
            }

            position = p1 + (p3 - p1) / 2;
            _rotation = Quaternion.LookRotation(p2 - p1);
            var x = Vector3.Distance(p1, p2);
            var y = Vector3.Distance(p2, p3);
            _scale = new Vector2(x, y);
            
            // Inform houses about this
            InvokeGeofenceChangeEvent(Utility.ConvertTo2D(p1),Utility.ConvertTo2D(p2),Utility.ConvertTo2D(p3),Utility.ConvertTo2D(p4));
        }

        private void DistributeNewGeofence(Vector3 pos, Quaternion rot, Vector2 scale)
        {
            var anchor = HouseManager.Get().CreateAnchor(new Pose(pos, rot));
            String id = anchor.trackableId.ToString();
            // Send anchor...
            var action = new GeofenceBuildAction(id, scale);
            Stringifier.GetStringifier().StringifyAndForward<GeofenceBuildAction>(action, TypeOfPackage.CreateNewGeofence, true);
        }

        private void InvokeGeofenceChangeEvent(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            OnGeofenceUpdate?.Invoke(new Geofence(a, b, c, d));
        }

        public bool IsConstructing()
        {
            return _isConstructing;
        }

        public void CancelConstruction()
        {
            _isConstructing = false;
            _isConstructed = false;
            SetActiveAll(false);
            if (position == null) return;
            
            BuildFromValues(position.Value, _rotation, _scale);
        }

        public void BuildFromValues(Vector3 position, Quaternion rotation, Vector2 scale)
        {
            SetActiveAll(true);
            _isConstructed = true;

            this.position = position;
            _scale = scale;
            _rotation = rotation;

            var p1 = position + _rotation * Vector3.back * (_scale.x / 2) + _rotation * Vector3.left * (_scale.y / 2);
            var p2 = _rotation * Vector3.forward * _scale.x + p1;
            var p4 = _rotation * Vector3.right * _scale.y + p1;
            var p3 = _rotation * Vector3.forward * _scale.x + p4;
            
            pole1.transform.position = p1;
            pole2.transform.position = p2;
            pole3.transform.position = p3;
            pole4.transform.position = p4;
            AdjustFenceFromPoints(fence1.transform, p1, p2);
            AdjustFenceFromPoints(fence2.transform, p2, p3);
            AdjustFenceFromPoints(fence3.transform, p3, p4);
            AdjustFenceFromPoints(fence4.transform, p4, p1);

            // Inform houses about this
            InvokeGeofenceChangeEvent(Utility.ConvertTo2D(p1),Utility.ConvertTo2D(p2),Utility.ConvertTo2D(p3),Utility.ConvertTo2D(p4));
        }

        private void SetActiveAll(bool active)
        {
            pole1.SetActive(active);
            pole2.SetActive(active);
            pole3.SetActive(active);
            pole4.SetActive(active);
            
            fence1.SetActive(active);
            fence2.SetActive(active);
            fence3.SetActive(active);
            fence4.SetActive(active);
        }

        private void AdjustFenceFromPoints(Transform fenceTransform, Vector3 a, Vector3 b)
        {
            var scale = Vector3.Distance(a, b);
            fenceTransform.localScale = new Vector3(0.003f, .1f, scale/10);
            var rotation = Quaternion.LookRotation(a - b);
            fenceTransform.rotation = rotation;
            fenceTransform.Rotate(new Vector3(0,0,90));
            fenceTransform.position = a + ((b - a) / 2) + new Vector3(0,.02f,0);
        }

        private List<GeofenceBuildAction> buildActions = new List<GeofenceBuildAction>();
        
        private void Update()
        {
            for (var i = 0; i < buildActions.Count; i++)
            {
                var buildAction = buildActions[i];
                foreach (var anchor in anchorManager.trackables)
                {
                    if (!anchor.trackableId.ToString().Equals(buildAction.AnchorID)) continue;
                    buildActions.RemoveAt(i);
                    var anchorTransform = anchor.transform;
                    BuildFromValues(anchorTransform.position, anchorTransform.rotation, buildAction.Scale);
                    return;
                }
            }
        }

        public void AddGeofenceToUnprocessed(GeofenceBuildAction action)
        {
            buildActions.Add(action);
        }

        public static bool IsPointInSquare(Geofence geofence, Vector3 point)
        {
            var m = Utility.ConvertTo2D(point);
            var AB = geofence.B - geofence.A;
            var AM = m - geofence.A;
            var BC = geofence.C - geofence.B;
            var BM = m - geofence.B;
            
            var dotABAM = Vector2.Dot(AB, AM);
            var dotABAB = Vector2.Dot(AB, AB);
            var dotBCBM = Vector2.Dot(BC, BM);
            var dotBCBC = Vector2.Dot(BC, BC);
            
            return 0 <= dotABAM && dotABAM <= dotABAB && 0 <= dotBCBM && dotBCBM <= dotBCBC;
        }
    }
}