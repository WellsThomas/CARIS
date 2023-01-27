using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using JetBrains.Annotations;
using Packages;
using UI;
using Unity.Collections;
using Unity.iOS.Multipeer;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class House : MonoBehaviour
{
    private void OnDestroy()
    {
        GeofenceManager.Get().RemoveHouseFromList(this);
        GeofenceManager.OnGeofenceUpdate -= CalculateIsInsideGeofence;
    }

    [FormerlySerializedAs("_offsetObject")] [SerializeField] public GameObject offsetObject;
    [FormerlySerializedAs("_plane")] [SerializeField] public GameObject plane;
    [FormerlySerializedAs("_crosshair")] [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject grabbable;
    [SerializeField] private GameObject newBlockObject;

    private Dictionary<int, Crosshair> crosshairs = new Dictionary<int, Crosshair>();
    private bool shared = true;
    private int ID;
    private bool _shouldBeVisible;
    private bool _isReplacing;
    private HouseManager _houseManager;
    private Pose lastAnchorPose;
    private bool _isInsideGeofence;
    private float _maxDistance = 2.5f;
    private int[] _plotSize = { 4,4,4,4 };
    [SerializeField] private HouseResizeArrow[] arrows;

    // Dictionary with all blocks contained on House plot
    private readonly Dictionary<int, GameObject> _blocks = new Dictionary<int, GameObject>();
    public HouseInformation houseInformation { get; } = new HouseInformation();
    
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    private void Start()
    {
        _isInsideGeofence = false;
        GeofenceManager.OnGeofenceUpdate += CalculateIsInsideGeofence;
        _shouldBeVisible = true;
        _isReplacing = false;
    }

    public GameObject GetGrabbable()
    {
        return grabbable;
    }
    
    private void CalculateIsInsideGeofence(GeofenceManager.Geofence? geofence)
    {
        var isInsideGeofenceUpdate = geofence != null && GeofenceManager.IsPointInSquare(geofence.Value, offsetObject.transform.position);

        if (isInsideGeofenceUpdate != _isInsideGeofence)
        {
            if(isInsideGeofenceUpdate)
                GeofenceManager.Get().AddHouseToList(this);
            else
                GeofenceManager.Get().RemoveHouseFromList(this);
        }

        _isInsideGeofence = isInsideGeofenceUpdate;
    }

    private void CalculateGhostMode()
    {
        if (_isInsideGeofence)
        {
            SetVisibilityTo(true);
            return;
        }
        
        var housePos = Utility.ConvertTo2D(offsetObject.transform.position);
        var mainCamPos = Utility.ConvertTo2D(Camera.main.transform.position);
        var secondCam = CameraManager.GetSecondary();
        var secondCamActivated = secondCam.isActiveAndEnabled;
        var secondCamPos = Utility.ConvertTo2D(secondCam.transform.position);
        var dist1 = Vector2.Distance(mainCamPos, housePos);
        var dist2 = secondCamActivated ? Vector3.Distance(secondCamPos, housePos) : 100;
        if (dist2 < dist1)
            dist1 = dist2; // Get longest distance

        SetVisibilityTo(dist1 < _maxDistance);
    }

    private int _counter = 0;
    private void Update()
    {
        _counter++;
        if (_counter % 5 != 0) return;
        
        CalculateGhostMode();
    }
    
    public void SetHouseManager(HouseManager houseManager)
    {
        _houseManager = houseManager;
    }

    /**
     * Get crosshair of specific player
     */
    public Crosshair GetCrosshair(int playerID)
    {
        // Return if crosshair already exists
        if (crosshairs.ContainsKey(playerID))
        {
            return crosshairs[playerID];
        }
        
        // Create new crosshair otherwise
        return CreateCrosshair(playerID);
    }

    /**
     * Create new crosshair for playerID
     */
    public Crosshair CreateCrosshair(int playerID)
    {
        var newCrosshair = Instantiate(
            crosshair,
            offsetObject.transform);
        
        newCrosshair.SetActive(true);
        newCrosshair.GetComponentInChildren<SpriteRenderer>().color = Colors.s_Colors[playerID%8];
        var newCrosshairComponent = newCrosshair.GetComponent<Crosshair>();
        crosshairs.Add(playerID, newCrosshairComponent);
        return newCrosshairComponent;
    }

    private BlockMaterials _materials;
    
    public void SetMaterials(BlockMaterials materials)
    {
        _materials = materials;
    }

    public void SetLocalRotation(Quaternion localRotation)
    {
        offsetObject.transform.localRotation = localRotation;
    }

    public bool SetShared(bool shouldBeShared)
    {
        this.shared = shouldBeShared;
        return shared;
    }

    public bool GetShared()
    {
        return this.shared;
    }
    
    public void SetID(int id)
    {
        this.ID = id;
    }

    public int GetID()
    {
        return ID;
    }

    public Vector3 GetLocalScale()
    {
        return offsetObject.transform.localScale;
    }

    public void SetLocalScale(Vector3 scale)
    {
        offsetObject.transform.localScale = scale;
    }

    public Pose GetPose()
    {
        var o = gameObject;
        return new Pose(o.transform.position, o.transform.rotation);
    }
    
    /**
     * Move house in accordance to global position and rotation
     */
    public void MoveHouse(Pose pose)
    {
        lastAnchorPose = pose;
        var o = gameObject;
        o.transform.position = pose.position;
        o.transform.rotation = pose.rotation;
        offsetObject.transform.localPosition = Vector3.zero;
        CalculateIsInsideGeofence(GeofenceManager.Get().GetGeofence());
    }
    
    /**
     * Offset House based on last anchorPoint
     */
    public void OffsetHouse(Vector3 offset)
    {
        offsetObject.transform.position = lastAnchorPose.position + offset;
        CalculateIsInsideGeofence(GeofenceManager.Get().GetGeofence());
    }
    
    /**
     * Offset House based on local position
     */
    public void OffsetHouseBasedOnLocal(Vector3 offset)
    {
        offsetObject.transform.localPosition = offset;
        CalculateIsInsideGeofence(GeofenceManager.Get().GetGeofence());
    }

    /**
     * Build a block on local position
     * Add block to blocks dictionary
     */
    public void NewBlock(Vector3 localPosition, int id, int colorID)
    {
        var newBlock = Instantiate(newBlockObject, new Vector3(0,0,0), offsetObject.transform.rotation,offsetObject.transform);
        var blockInformation = newBlock.GetComponent<BlockInformation>();
        
        // Set new block information
        newBlock.transform.localScale = new Vector3(.1f, .1f, .1f);
        newBlock.transform.localPosition = localPosition;
        newBlock.GetComponent<Renderer>().material = _materials.GetMaterial(colorID);
        newBlock.transform.name = "BuildCube";

        // Set block information
        blockInformation.blockID = id;

        // Add to dictionary of blocks
        _blocks.Add(id, newBlock);
        
        // Add block to blockInformation
        houseInformation.AddBlock(new Block(localPosition, id, colorID));
    }

    /**
     * Attempt at removing block on ID
     */
    public void RemoveBlock(int id)
    {
        GameObject obj;
        if (!_blocks.TryGetValue(id, out obj)) return;
        
        Destroy(obj);
        _blocks.Remove(id);
        houseInformation.RemoveBlock(id);
    }

    public void Rotate(Vector3 vector)
    {
        offsetObject.transform.Rotate(vector);
    }

    private Color _normalGround = new Color(1, 0.86f, 0.6745f, 0.5f);
    private Color _hiddenGround = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    private void SetVisibilityTo(bool active)
    {
        foreach (var (key, value) in crosshairs)
        {
            value.ShouldHide(!active);
        }
        
        if (active == _shouldBeVisible) return;
        if (_isReplacing) return;

        _shouldBeVisible = active;
        var material = plane.GetComponent<MeshRenderer>().material;
        material.color = active ? _normalGround : _hiddenGround;
        
        foreach (var (x, obj) in _blocks)
        {
            obj.SetActive(_shouldBeVisible);
        }
    }

    public void SetReplacedMode(bool active)
    {
        if(active)
            SetVisibilityTo(false);
        _isReplacing = active;
    }

    [SerializeField] private GameObject _replacable;
    

    public void EnableReplacable(bool active)
    {
        _replacable.SetActive(active);
    }

    private Vector3? lastOffsetPosition;
    private Vector3? lastOffsetScale;
    private Quaternion? lastOffsetRotation;

    public void ReplaceHouse(GameObject replacedHouse)
    {
        var replacedTransform = replacedHouse.transform;
        var replacerTransform = offsetObject.transform;
        
        // Save
        lastOffsetRotation = replacerTransform.localRotation;
        lastOffsetScale = replacerTransform.localScale;
        lastOffsetPosition = replacerTransform.localPosition;
        
        // Replace
        replacerTransform.rotation = replacedTransform.rotation;
        replacerTransform.localScale = replacedTransform.localScale;
        replacerTransform.position = replacedTransform.position;
    }

    public void StopReplacingHouse()
    {
        // If not existing. Don't do anything
        if (lastOffsetPosition == null || lastOffsetRotation == null || lastOffsetScale == null) return;
        
        // Obtain same transform
        var replacerTransform = offsetObject.transform;
        replacerTransform.localRotation = (Quaternion) lastOffsetRotation;
        replacerTransform.localScale = (Vector3) lastOffsetScale;
        replacerTransform.localPosition = (Vector3) lastOffsetPosition;
        
        // Erase last
        lastOffsetRotation = null;
        lastOffsetScale = null;
        lastOffsetPosition = null;
    }

    [Serializable]
    public struct Block
    {
        public SerializableVector3 Position;
        public int ID;
        public int ColorID;

        public Block(Vector3 position, int id, int colorID)
        {
            Position = position;
            ID = id;
            ColorID = colorID;
        }
    }

    [Serializable]
    public class HouseInformation
    {
        public List<Block> blocks = new List<Block>();
        public SerializableVector3 globalPosition;
        public SerializableQuaternion globalRotation;
        public float localScale;
        public SerializableQuaternion localRotation;
        public string attachmentAnchor;
        public int[] plotSize = { 4,4,4,4 };
        public int id;

        public void AddBlock(Block block)
        {
            blocks.Add(block);
        }
        
        public void RemoveBlock(int id)
        {
            var blockIndex = blocks.FindIndex(block => block.ID == id);
            if (blockIndex == -1) return;
            blocks.RemoveAt(blockIndex);
        }
    }


    public void UpdateHouseInformation()
    {
        var houseTransform = gameObject.transform;
        houseInformation.globalPosition = houseTransform.position;
        houseInformation.globalRotation = houseTransform.rotation;

        var offsetTransform = offsetObject.transform;
        var scale = gameObject.transform.localScale.x;
        if (Math.Abs(scale - 1) > .0002f)
        {
            offsetTransform.localScale = Vector3.Scale(new Vector3(scale, scale, scale), offsetTransform.localScale);
            gameObject.transform.localScale = Vector3.one;
        }
        
        // Plot size
        /*houseInformation.plotSize[0] = _plotSize[0];
        houseInformation.plotSize[1] = _plotSize[1];
        houseInformation.plotSize[2] = _plotSize[2];
        houseInformation.plotSize[3] = _plotSize[3];*/
        
        houseInformation.localScale = offsetTransform.localScale.x;
        houseInformation.localRotation = offsetTransform.localRotation;
        houseInformation.id = ID;
        
        lastAnchorPose = new Pose(offsetTransform.position, offsetTransform.rotation);
    }
    
    public string Serialize()
    {
        UpdateHouseInformation();
        return JsonUtility.ToJson(houseInformation);
    }

    public string SerializeWithAnchor(string anchorID)
    {
        houseInformation.attachmentAnchor = anchorID;
        return Serialize();
    }

    public static HouseInformation Parse(string data)
    {
        return JsonUtility.FromJson<HouseInformation>(data);
    }

    /**
     * Rebuild based on information
     */
    public void BuildFromInformation(HouseInformation newInformation)
    {
        // Setup plot size
        SetPlotSize(0, newInformation.plotSize[0]);
        SetPlotSize(1, newInformation.plotSize[1]);
        SetPlotSize(2, newInformation.plotSize[2]);
        SetPlotSize(3, newInformation.plotSize[3]);
        
        // Setup offsetObject
        var offsetTransform = offsetObject.transform;
        offsetTransform.localScale =
            new Vector3(newInformation.localScale, newInformation.localScale, newInformation.localScale);
        offsetTransform.localRotation = newInformation.localRotation;
        lastAnchorPose = new Pose(offsetTransform.position, offsetTransform.rotation);

        foreach (var block in newInformation.blocks)
        {
            NewBlock(block.Position,block.ID, block.ColorID);
        }
    }

    public void SetPlotSize(int directionIndex, int newSize)
    {
        if (directionIndex is < 0 or > 4 || newSize is < 0 or > 16) return;
        _plotSize[directionIndex] = newSize;
        
        // Adjust Size
        var Xsize = ((float)_plotSize[1] + _plotSize[3]) / 100;
        var Zsize = ((float)_plotSize[0] + _plotSize[2]) / 100;
        plane.transform.localScale = new Vector3(Xsize + .01f, 1, Zsize + .01f);
        grabbable.transform.localScale = new Vector3(Xsize + .02f, 1, Zsize + .02f);

        // Adjust Center
        var Zoffset = _plotSize[0] - _plotSize[2];
        var Xoffset = _plotSize[1] - _plotSize[3];
        plane.transform.localPosition = new Vector3(Xoffset * .05f, 0, Zoffset * .05f);
        grabbable.transform.localPosition = new Vector3(Xoffset * .05f, -.01f, Zoffset * .05f);
        
        // Adjust Arrow
        arrows[directionIndex].SetPosition(newSize);

        // Save size in HouseInformation
        houseInformation.plotSize[directionIndex] = newSize;
    }

    public bool IsPlotChangeDisruptive(int directionIndex, int newSize)
    {
        var length = newSize * .1f + .02f;
        for (var index = 0; index < houseInformation.blocks.Count; index++)
        {
            var block = houseInformation.blocks[index];
            var pos = block.Position;
            if (pos.y is > .06f and > .04f) continue;
            
            /*0 => new Vector3(0, 0, vectorLength),
            1 => new Vector3(vectorLength, 0, 0),
            2 => new Vector3(0, 0, -vectorLength),
            3 => new Vector3(-vectorLength, 0, 0),*/
            
            var illegal = directionIndex switch
            {
                0 => pos.z > length,
                1 => pos.x > length,
                2 => pos.z < -length,
                3 => pos.x < -length,
                _ => default
            };

            if (illegal) return true;

        }

        return false;
    }
}
