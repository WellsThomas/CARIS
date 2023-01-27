using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Communication;
using Communication.ActionPackage;
using FileSystem;
using Interaction.VisualGuide;
using JetBrains.Annotations;
using UI;
using UnityEngine;
using Unity.Collections;
using Unity.iOS.Multipeer;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;
using static System.String;
using Logger = UnityEngine.Logger;
using Random = UnityEngine.Random;

public class HouseManager : MonoBehaviour
{
    
    [FormerlySerializedAs("houseModel")] [SerializeField]
    private GameObject houseModel;

    [SerializeField] private GameObject anchorModel;
    [SerializeField] private GameObject crosshair;

    private GameObject _crosshairObject = null;
    public BlockMaterials blockMaterials { get; set; }

    private Stringifier stringifier;
    public bool shared { get; set; }
    private readonly Dictionary<int, HouseContent> _houses = new();
    private HouseContent _currentHouse;
    private PlayerManager _playerManager;
    public int currentHouseIndex { get; set; }
    ARAnchorManager _anchorManager;

    private static HouseManager _houseManager;

    void Start()
    {
        _anchorManager = gameObject.GetComponent<ARAnchorManager>();
        stringifier = new Stringifier();
        _playerManager = gameObject.GetComponent<PlayerManager>();
        _houseManager = this;
    }

    public static HouseManager Get()
    {
        return _houseManager;
    }

    public Stringifier GetStringifier()
    {
        return stringifier;
    }

    private void Update()
    {
        CheckForTimeBasedActions();
        CheckForNewAnchorBasedActions();
    }
    
    private int counter = 0;
    private long _timeOfLastAutoSave = ((DateTimeOffset) DateTime.Now).ToUnixTimeMilliseconds();
    private const long MillisecondsBetweenEachAutoSave = 60000;
    private const long MillisecondsBeforeAnchorDeletion = 15000;

    private void CheckForTimeBasedActions()
    {
        counter++;
        if (counter < 100) return;
        counter = 0;
        
        var currentUnixTime = ((DateTimeOffset) DateTime.Now).ToUnixTimeMilliseconds();
        CheckIfShouldAutoSave(currentUnixTime);
        CheckIfAnchorsShouldBeDeleted(currentUnixTime);
    }

    private void CheckIfShouldAutoSave(long currentTime)
    {
        if (currentTime <= MillisecondsBetweenEachAutoSave + _timeOfLastAutoSave) return;
        
        _timeOfLastAutoSave += MillisecondsBetweenEachAutoSave;
        FileMenu.PerformAutoSave();
    }

    /**
     * Struct to hold information about Content about House
     * Holds the object itself
     * The Distributor of actions
     * The local house
     */
    public struct HouseContent
    {
        public readonly GameObject HouseObject;
        public readonly HouseActionDistributor Distributor;
        public readonly House Local;

        public HouseContent(GameObject houseObject, HouseActionDistributor distributor, House local)
        {
            HouseObject = houseObject;
            Distributor = distributor;
            Local = local;
        }
    }

    /**
     * Set Crosshair for placing a house
     */
    public void SetCrosshair(Pose pose)
    {
        // If no crosshairObject exist just yet. Make one
        if (_crosshairObject == null)
        {
            _crosshairObject = Instantiate(crosshair, pose.position, Quaternion.identity);
            return;
        }

        _crosshairObject.transform.position = pose.position;
    }

    /**
     * Destroy crosshair object to remove it from view
     */
    public void ResetCrosshair()
    {
        if (_crosshairObject == null) return;
        Destroy(_crosshairObject);
    }

    public HouseContent CreateNewHouse(Pose pose)
    {
        // Generate ID
        int id = Random.Range(Int32.MinValue, Int32.MaxValue);

        // Move house locally
        var house = CreateOrMoveHouse(id, pose, true);

        // Create house and return to creator
        return house;
    }

    public HouseContent? GetCurrentHouse()
    {
        return GetHouse(currentHouseIndex);
        ;
    }

    public Dictionary<int, HouseContent> GetHouses()
    {
        return _houses;
    }

    public House FindClosestHouse(Vector3 position)
    {
        // For each house.
        // Assess the closest to participant
        var shortestDistance = float.MaxValue;
        House closestHouse = null;

        foreach (var (id, houseContent) in GetHouses())
        {
            var house = houseContent.Local;
            var housePosition = house.GetPose().position;
            var distance = Vector3.Distance(housePosition, position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestHouse = house;
            }
        }

        return closestHouse;
    }

    /**
     * Creates or moves house depending on whether phone has seen house before
     */
    private HouseContent CreateOrMoveHouse(int houseIndex, Pose pose, bool share)
    {
        HouseContent content;

        // If house is already present.
        if (_houses.ContainsKey(houseIndex))
        {
            content = _houses[houseIndex];
        }
        else
        {
            content = InstantiateHouse(pose, houseIndex);
        }

        // Depending on flag variable share the house-movement. Else only move the house locally
        if (share) content.Distributor.MoveHouse(pose);
        else content.Local.MoveHouse(pose);

        return content;
    }

    public HouseContent InstantiateHouse(Pose pose, int houseIndex)
    {
        HouseContent content;
        var house = Instantiate(houseModel, pose.position, pose.rotation);

        // Get Necessary components and add to houses dictionary
        var local = (House) house.GetComponent<House>();
        local.SetHouseManager(this); // Add houseManager
        local.SetMaterials(blockMaterials); // Add set of materials
        var distributor = new HouseActionDistributor(local, stringifier, this, _playerManager);
        content = new HouseContent(house, distributor, local);
        _houses.TryAdd(houseIndex, content);

        // Adjust ID and Color
        content.Distributor.SetID(houseIndex);

        _currentHouse = content;
        return content;
    }

    private void CheckIfAnchorsShouldBeDeleted(long currentUnixTime)
    { 
        foreach (var (anchorGameObject, timeOfDeletion) in _sentAnchors)
        {
            if (timeOfDeletion < currentUnixTime) Destroy(anchorGameObject);
        }
    }

    private readonly List<MoveAction> _unprocessedMoveActions = new List<MoveAction>();
    private readonly List<CopyAction> _unprocessedCopyActions = new List<CopyAction>();
    private readonly List<LoadWorldAction> _unprocessedWorldLoadActions = new List<LoadWorldAction>();
    private readonly Dictionary<GameObject, long> _sentAnchors = new Dictionary<GameObject, long>();

    /**
     * When receiving an action specifying a new position of the world. Add to unprocessedMoveActions.
     * When the respective anchor has been received - The world will be moved.
     */
    public void AddHouseMoveToUnprocessed(MoveAction moveAction)
    {
        _unprocessedMoveActions.Add(moveAction);
    }

    public void AddHouseCopyToUnprocessed(CopyAction copyAction)
    {
        _unprocessedCopyActions.Add(copyAction);
    }

    private bool iRequestedWorldLoad;
    
    public void AddWorldLoadToUnprocessed(LoadWorldAction loadWorldAction)
    {
        if (loadWorldAction.IsRequest)
        {
            if (!iRequestedWorldLoad) return;
            iRequestedWorldLoad = false;
        }
        
        _unprocessedWorldLoadActions.Add(loadWorldAction);
    }
    
    public void RequestWorldLoad()
    {
        iRequestedWorldLoad = true;
        var action = new RequestWorldLoad();
        GetStringifier().StringifyAndForward(action, TypeOfPackage.RequestLoadWorld, true);
    }


    /**
     * For every update, check whether unprocessed move actions have received
     * The corresponding anchor. Anchors and move-actions are send separately
     * and sometimes anchors take longer. This check ensures that any unprocessed
     * move objects are handled at some point in time.
     */
    private void CheckForNewAnchorBasedActions()
    {
        if (_anchorManager.trackables.count == 0) return;
        GeofenceManager.Get().CheckUnprocessedRotateScaleAction(this);
        UnprocessedMoveActions();
        UnprocessedCopyActions();
        CheckUnprocessedWorldLoadActions();
    }

    private void UnprocessedMoveActions()
    {
        // Check for each unprocessed move action
        var count = _unprocessedMoveActions.Count;
        if (count == 0) return;

        MoveAction moveAction = _unprocessedMoveActions[count - 1];
        
        // Whether any corresponding anchor has the right ID
        var anchor = GetAnchor(moveAction.anchorID);
        if (anchor == null) return;

        // If so. Create or move house
        var anchorTransform = anchor.transform;
        var pose = new Pose(anchorTransform.position, anchorTransform.rotation);
        CreateOrMoveHouse(moveAction.GetHouseID(), pose, false);

        // Remove from unprocessed
        _unprocessedMoveActions.Remove(moveAction);
    }

    private void UnprocessedCopyActions()
    {
        // Check for each unprocessed copy action
        var count = _unprocessedCopyActions.Count;
        if (count == 0) return;

        CopyAction copyAction = _unprocessedCopyActions[count - 1];

        // Whether any corresponding anchor has the right ID
        var anchor = GetAnchor(copyAction.AnchorID);
        if (anchor == null) return;

        // If so. copy house
        BuildSerializedHouse(copyAction.Data, copyAction.HouseID, true);

        // Remove from unprocessed
        _unprocessedCopyActions.Remove(copyAction);
    }

    private void CheckUnprocessedWorldLoadActions()
    {
        // Check for each unprocessed world load action
        var count = _unprocessedWorldLoadActions.Count;
        if (count == 0) return;

        LoadWorldAction loadAction = _unprocessedWorldLoadActions[count - 1];
        // Whether any corresponding anchor has the right ID
        var anchor = GetAnchor(loadAction.AnchorID);
        if (anchor == null) return;

        // Get Scale and apply.
        var content = JsonUtility.FromJson<WorldContent>(loadAction.Data);

        // Place all houses
        var helper = PlaceAllHouses(content);

        // Rotate and finish placement
        var o = anchor.gameObject;
        helper.transform.position = o.transform.position;
        helper.transform.rotation = o.transform.rotation;
        helper.transform.localScale = new Vector3(loadAction.Scale, loadAction.Scale, loadAction.Scale);

        // Detach helper from children
        DetachHelper(helper);

        // Fix houses
        foreach (var (key, house) in GetHouses())
        {
            house.Local.UpdateHouseInformation();
        }

        // Fix Geofence
        GeofenceManager.Get().FixGeofenceFromPlacement(true);

        // Remove from unprocessed
        _unprocessedWorldLoadActions.Remove(loadAction);
    }
    
    public void DetachHelper(GameObject obj)
    {
        obj.transform.DetachChildren();
        VisualGuidanceManager.Get().ReplaceTemporaryGuides();
    }

    public ARAnchor GetAnchor(string id)
    {
        foreach (var anchor in _anchorManager.trackables)
        {
            if (id != anchor.trackableId.ToString()) continue;
            return anchor;
        }

        return null;
    }

    // Get House based on index
    public HouseContent? GetHouse(int index)
    {
        if (!_houses.ContainsKey(index)) return null;
        return _houses[index];
    }

    public void EraseHouseGlobally(int houseID)
    {
        EraseHouseLocally(houseID);

        // Inform other players of erase
        var action = new RemoveAction(houseID);
        stringifier.StringifyAndForward<RemoveAction>(action, TypeOfPackage.RemoveHouse, true);
    }

    public void EraseHouseLocally(int houseID)
    {
        var houseContent = GetHouse(houseID);
        if (houseContent == null) return;
        _houses.Remove(houseID);
        Destroy(houseContent.Value.HouseObject);
    }

    /**
     * Instantiate anchor and add ARAnchor to it.
     * The Anchor will be placed at the specified pose, and will sync
     * with other devices. Everybody will have their own respective pose
     * of the anchor, however by matching the IDs, one can utilize their
     * common position in physical space to build upon.
     * Returns the newly created anchor.
     */
    public ARAnchor CreateAnchor(Pose hit)
    {
        ARAnchor anchor = null;
        // Note: the anchor can be anywhere in the scene hierarchy
        var gameObject = Instantiate(anchorModel, hit.position, hit.rotation);

        // Make sure the new GameObject has an ARAnchor component
        anchor = gameObject.GetComponent<ARAnchor>();
        if (anchor == null)
        {
            anchor = gameObject.AddComponent<ARAnchor>();
        }

        var currentTime = DateTime.Now;
        var unixTime = ((DateTimeOffset) currentTime).ToUnixTimeMilliseconds() + MillisecondsBeforeAnchorDeletion;
        _sentAnchors.Add(gameObject, unixTime);

        return anchor;
    }

    public HouseContent? CopyHouseIfExists(int houseID)
    {
        var houseContent = GetHouse(houseID);
        if (houseContent == null) return null;

        // Create anchor
        var houseTransform = houseContent.Value.HouseObject.transform;
        var anchorId = CreateAnchor(new Pose(houseTransform.position, houseTransform.rotation)).trackableId.ToString();

        // Serialize house and apply anchor
        var serializedHouse = houseContent.Value.Local.SerializeWithAnchor(anchorId);
        var newHouseId = Random.Range(Int32.MinValue, Int32.MaxValue);
        var copiedHouseContent = BuildSerializedHouse(serializedHouse, newHouseId);

        // Distribute new house
        var copyAction = new CopyAction(anchorId, newHouseId, serializedHouse);
        stringifier.StringifyAndForward<CopyAction>(copyAction, TypeOfPackage.CopyHouse, true);

        return copiedHouseContent;
    }

    public HouseContent BuildSerializedHouse(string data, int houseId, bool usingAnchor = false)
    {
        return BuildHouseFromInformation(House.Parse(data), houseId, usingAnchor);
    }

    private HouseContent BuildHouseFromInformation(House.HouseInformation houseInformation, int houseId, bool usingAnchor = false)
    {
        Pose pose;
        if (usingAnchor)
        {
            var anchorTransform = GetAnchor(houseInformation.attachmentAnchor).transform;
            pose = new Pose(anchorTransform.position, anchorTransform.rotation);
        }
        else
        {
            pose = new Pose(houseInformation.globalPosition, houseInformation.globalRotation);
        }
        var houseContent = CreateOrMoveHouse(houseId, pose, false);

        houseContent.Local.BuildFromInformation(houseInformation);

        return houseContent;
    }

    public void StartVisualizeReplacement(int replacer, int toBeReplaced, bool share)
    {
        var replacerHouse = GetHouse(replacer)?.Local;
        var replacedHouse = GetHouse(toBeReplaced)?.Local;
        if (replacerHouse == null || replacedHouse == null) return;

        replacedHouse.SetReplacedMode(true);
        replacerHouse.ReplaceHouse(replacedHouse.offsetObject);

        if (!share) return;

        var replaceAction = new ReplaceAction(true, replacer, toBeReplaced);
        stringifier.StringifyAndForward<ReplaceAction>(replaceAction, TypeOfPackage.ReplaceAction, true);
    }

    public void StopVisualizeReplacement(int replacer, int toBeReplaced, bool share)
    {
        var replacerHouse = GetHouse(replacer)?.Local;
        var replacedHouse = GetHouse(toBeReplaced)?.Local;
        if (replacerHouse == null || replacedHouse == null) return;

        replacedHouse.SetReplacedMode(false);
        replacerHouse.StopReplacingHouse();

        if (!share) return;

        var replaceAction = new ReplaceAction(false, replacer, toBeReplaced);
        stringifier.StringifyAndForward<ReplaceAction>(replaceAction, TypeOfPackage.ReplaceAction, true);
    }

    [Serializable]
    public struct WorldContent
    {
        public List<House.HouseInformation> houses;
        public GeofenceManager.SerializableGeofence fence;
        public List<GuidingObject.GuidingObjectData> guidingObjects;
    }
    
    [CanBeNull]
    public string SerializeAllHouses()
    {
        var geofence = GeofenceManager.Get().GetSerializableGeofence();
        if (geofence == null) return null;
        
        var content = new WorldContent
        {
            fence = geofence.Value,
            houses = new List<House.HouseInformation>(_houses.Count),
            guidingObjects = VisualGuidanceManager.Get().ProduceDataObjectList()
        };
        
        foreach (var (key, house) in _houses)
        {
            house.Local.UpdateHouseInformation();
            content.houses.Add(house.Local.houseInformation);
        }
        
        return JsonUtility.ToJson(content);
    }

    public GameObject PlaceAllHouses(WorldContent content)
    {
        // Remove existing world ...
        var ids = new List<int>();
        foreach (var (id, house) in _houses)
            ids.Add(id);
        
        foreach (var id in ids)
            EraseHouseLocally(id);

        // Instantiate all data in a helper object
        var helperObject = new GameObject("helper")
        {
            transform =
            {
                position = content.fence.position,
                rotation = content.fence.rotation
            }
        };
        
        var geofenceManager = GeofenceManager.Get();
        geofenceManager.BuildFromValues(content.fence.position, content.fence.rotation, content.fence.scale);
        geofenceManager.transform.parent = helperObject.transform;
        
        // Delete and replace with new
        var visualGuidanceManager = VisualGuidanceManager.Get();
        visualGuidanceManager.EraseCurrentGuides();
        visualGuidanceManager.BuildTemporaryGuideObjectsFromData(content.guidingObjects, helperObject);

        foreach (var houseInformation in content.houses)
        {
            var houseContent = BuildHouseFromInformation(houseInformation, houseInformation.id);
            houseContent.HouseObject.transform.parent = helperObject.transform;
        }

        return helperObject;
    }

    public void AnswerLoadWorldRequest()
    {
        if (_playerManager.GetLocalPlayerID() != 0) return;
        
        var fence = GeofenceManager.Get().GetSerializableGeofence();
        if (fence == null) return;
        
        var anchor = CreateAnchor(new Pose(fence.Value.position, fence.Value.rotation));
        var worldData = HouseManager.Get().SerializeAllHouses();
        var anchorId = anchor.trackableId.ToString();
        var loadWorldAction = new LoadWorldAction(anchorId, 1, worldData, true);
        Stringifier.GetStringifier().StringifyAndForward<LoadWorldAction>(loadWorldAction, TypeOfPackage.LoadWorld, true);
    }

    public ARAnchorManager GetAnchorManager()
    {
        return _anchorManager;
    }
}
