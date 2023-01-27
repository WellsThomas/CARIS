using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Interaction.VisualGuide;
using JetBrains.Annotations;
using Player;
using UI;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Interaction;
using UnityEngine.XR.ARFoundation.Samples.Player;
using UnityEngine.XR.ARSubsystems;
using static HouseManager;

namespace Tools
{
    [RequireComponent(typeof(ARAnchorManager))]
    [RequireComponent(typeof(ARRaycastManager))]
    public class ToolManager : MonoBehaviour
    {
        private ITool _tool;

        [SerializeField]
        private GameObject toolText;

        private Text toolTextField;
        private ARRaycastManager _RaycastManager;
        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        [SerializeField] private GameObject scripts;
        private OverviewManager overviewManager;
        private LookAtOverviewDevice lookAtOverviewDevice;

        private HouseManager _houseManager;
        private PlayerManager _playerManager;
        private BlockMaterials _blockMaterials;
        
        public BuildTool BuildTool;
        private static ToolManager _toolManager;
        
        public delegate void ToolChange<in T>(T x);
        public static event ToolChange<ITool> OnToolChange;

        public static ToolManager GetManager()
        {
            return _toolManager;
        }

        // Start is called before the first frame update
        void Start()
        {
            _toolManager = this;
            _houseManager = gameObject.GetComponent<HouseManager>();
            _playerManager = gameObject.GetComponent<PlayerManager>();
            _blockMaterials = scripts.GetComponent<BlockMaterials>();
            toolTextField = toolText.GetComponent<Text>();
            overviewManager = scripts.GetComponent<OverviewManager>();
            lookAtOverviewDevice = new LookAtOverviewDevice(overviewManager);
            BuildTool = new BuildTool(_houseManager, _blockMaterials, _playerManager, this);
            ChangeTool(BuildTool);
        }

        private ARParticipant _currentARParticipant = null;
        public void SetCurrentARParticipant([CanBeNull] ARParticipant newParticipant)
        {
            _currentARParticipant = newParticipant;
        }

        void Awake()
        {
            _RaycastManager = GetComponent<ARRaycastManager>();
        }

        // public void ChangeTool(ITool newTool)
        // {
        //     if (_tool == newTool) return;
        //     OnToolChange?.Invoke(_tool);
        //     _tool?.OnToolChange();
        //     _tool = newTool;
        //     toolTextField.text = _tool.GetName();
        // }
        
        public void ChangeTool(ITool newTool)
        {
            if (_tool == newTool) return;
            OnToolChange?.Invoke(_tool);
            _tool?.OnToolChange();

            // Log tool change
            string path = Application.persistentDataPath + "/tool_log.csv";
            using (StreamWriter writer = File.AppendText(path))
            {
                // string deviceName = UnityEngine.iOS.Device.name;
                string deviceName = SystemInfo.deviceName;

                string logLine = string.Format("{0},{1},{2}", DateTime.Now, deviceName, newTool.GetName());
                writer.WriteLine(logLine);
            }
            _tool = newTool;
            toolTextField.text = _tool.GetName();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateStalking();
            
            // If a tool is present. See if any parameters should be passed to it
            if (_tool == null) return;
            HandleInputUpdate();
            HandleRaycast();
            HandleClick();

        }

        private bool HandleClickOnParticipant()
        {
            if (_currentARParticipant == null) return false;
            
            stalkerHandler.StartStalking(_currentARParticipant);
            
            return true;
        }

        private StalkerHandler stalkerHandler;

        private void UpdateStalking()
        {
            stalkerHandler = StalkerHandler.Get();
            if (stalkerHandler == null || !stalkerHandler.IsStalking()) return;
            stalkerHandler.UpdateStalking(false);
        }

        /**
         * Trigger update on tool
         */
        private void HandleInputUpdate()
        {
            _tool.UpdateInput();
        }

        
        private bool HandleOverviewDeviceRaycast(Ray ray)
        {
            // If build tool. Perform special raycast
            int layerMask = 1 << 10;
            RaycastHit hits;
            
            if (Physics.Raycast(ray, out hits, Mathf.Infinity, layerMask))
            {
                lookAtOverviewDevice.OnPhysicsRayCast(hits);
                return true;
            }
            
            lookAtOverviewDevice.SetOverviewDeviceActiveness(false);
            return false;
        }

        private bool _isLookingAtGeofence;
        
        private bool HandleGeofenceRaycast(Ray ray)
        {
            // If build tool. Perform special raycast
            int layerMask = 1 << 12;
            RaycastHit hits;

            _isLookingAtGeofence = Physics.Raycast(ray, out hits, Mathf.Infinity, layerMask);
            GeofenceManager.Get().SetHighlight(_isLookingAtGeofence);
            return _isLookingAtGeofence;
        }

        /**
         * Handle raycast every update
         */
        void HandleRaycast()
        {
            if (Camera.main == null) return;
            
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            if(_tool.GetType() != ITool.ToolType.MoveTool && _tool.GetType() != ITool.ToolType.PlaceTool){
                if (_tool.GetType() == ITool.ToolType.SaveHouseTool)
                {
                    ((SaveHouseTool) _tool).PerformRaycast(ray);
                    return;
                }
                if (BuildTool.HandleBuildRaycast(ray)) return;
                //if (HandleOverviewDeviceRaycast(ray)) return;
                if (HandleGeofenceRaycast(ray)) return;
                
            }
            
            // AR FOUNDATION
            // Raycast against planes and feature points
            var trackableTypes = _tool.GetTrackableType();

            var rayVector3 = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));

            // Perform the raycast
            if (!_RaycastManager.Raycast(rayVector3, s_Hits, trackableTypes)) return;
            // Raycast hits are sorted by distance, so the first one will be the closest hit.
            var hit = s_Hits[0];

            _tool.OnRayCast(hit);
        }

        private int _updatesSinceClickStart;
        private float _distanceSinceStart;
        private bool _currentClickIsLong;
        private bool _currentClickIsDouble;
        private bool _currentClickIsOnUI;
        
        /**
         * Handle all clicking
         */
        void HandleClick()
        {
            
            // If user is clicking on UI element or simply not clicking. Return
            if (Input.touchCount == 0)
            {
                if(_tool.GetType() != ITool.ToolType.PlaceTool && _tool.GetType() != ITool.ToolType.GeofenceTool && _tool.GetType() != ITool.ToolType.SaveHouseTool) 
                    ChangeTool(BuildTool);
                _currentClickIsLong = false;
                _currentClickIsDouble = false;
                _currentClickIsOnUI = false;
                _distanceSinceStart = 0;
                return;
            }
            
            if (_currentClickIsOnUI || IsClickOnUIElement())
            {
                _currentClickIsOnUI = true;
                return;
            }

            // If touch-phase is not begin return
            var touch = Input.GetTouch(0);
            _distanceSinceStart += Vector2.Distance(Vector2.zero, touch.deltaPosition);
            
            _updatesSinceClickStart++;
            if (touch.phase == TouchPhase.Began) _updatesSinceClickStart = 0;

            if (!_currentClickIsLong && Input.touchCount > 1) HandleMultiTouch();

            if (!_currentClickIsDouble && !_currentClickIsLong && _updatesSinceClickStart > 50 &&
                _tool.GetType() != ITool.ToolType.MoveTool) HandleLongTouch();
            
            if (_tool == BuildTool && touch.phase == TouchPhase.Began)
            {
                var houseContent = BuildTool.GetGrabbable();
                if (houseContent != null)
                {
                    ChangeTool(new HouseMoveTool((HouseContent) houseContent,_houseManager));
                    return;
                }

                if (BuildTool.ArrowIsActive())
                {
                    BuildTool.ActivateArrowDrag();
                    return;
                }
                
                if(_isLookingAtGeofence) ChangeTool(new ScaleGeofenceTool());
            }
            
            var shouldNotExecuteSingleTouch =
                touch.phase != TouchPhase.Ended || _currentClickIsDouble || _currentClickIsLong || _tool.GetType() == ITool.ToolType.MoveTool;
            if (shouldNotExecuteSingleTouch) return;
            HandleSingleClick(touch);
        }

        private void HandleLongTouch()
        {
            // If sufficient motion (Resembles erasing moving
            if (_distanceSinceStart > 4000)
            {
                // Erase houseContent player looks at
                if (_tool == BuildTool && _houseManager.currentHouseIndex != -1)
                {
                    _houseManager.EraseHouseGlobally(_houseManager.currentHouseIndex);
                    Handheld.Vibrate();
                }
                else if (VisualGuidanceManager.Get().RemoveActiveGuidance())
                {
                    Handheld.Vibrate();     
                }
            }
            else if (_tool == BuildTool)
            {
                var houseContent = AttemptHouseCopy();
                if(houseContent != null) {
                    Handheld.Vibrate();
                    
                    // Switch to move tool and move the world
                    _tool = new HouseMoveTool((HouseContent)houseContent,_houseManager);
                    return;
                }
            }
            // Do stuff on long click...
            _currentClickIsLong = true;
        }

        private HouseContent? AttemptHouseCopy()
        {
            var houseID = BuildTool.GetLatestHouseID();
            if (houseID == null) return null;
            
            return _houseManager.CopyHouseIfExists((int)houseID);
        }

        private void HandleMultiTouch()
        {
            _currentClickIsDouble = true;

            if (_tool.GetType() != ITool.ToolType.OrientTool)
            {
                ChangeTool(new OrientTool(_houseManager));
            }
            // Do multi touch stuff...
        }

        public void ResetTool()
        {
            ChangeTool(BuildTool);
        }

        private void HandleSingleClick(Touch touch)
        {
            // See if user clicked on participant. In that case react and return
            if (HandleClickOnParticipant()) return;
            if (lookAtOverviewDevice.OnClick()) return;
            if (VisualGuidanceManager.Get().OnClick()) return;

            // Send click event to tool
            _tool.OnClick();
            
            // Forward left or right click event to tool based on position of click
            if (touch.position.x < Camera.main.pixelWidth / 2)
                _tool.OnLeftClick();
            else
                _tool.OnRightClick();
        }

        /**
         * Calculates whether specific click is on a GameObject. It only registers UI GameObjects
         * This therefore detects whether a click hit a UI-element
         */
        public static bool IsClickOnUIElement()
        {
            return EventSystem.current.IsPointerOverGameObject(-1) || EventSystem.current.IsPointerOverGameObject(0);
        }
    }
}
