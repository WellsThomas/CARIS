using System;
using System.Collections.Generic;
using Communication;
using Communication.ActionPackage;
using Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;

namespace UI
{
    public class ClickEventHandler : ChangableColor
    {
        [SerializeField]
        private GameObject xrOrigin;
        [SerializeField]
        private GameObject scripts;
        private ToolManager _toolManager;
        private HouseManager _houseManager;
        private PlayerManager _playerManager;
        
        [SerializeField]
        private GameObject _exitPlaceTool;
        [SerializeField]
        private GameObject _placeTool;
        [SerializeField]
        private BlockMaterials colors;
        [SerializeField]
        private ColorSpecifier colorSpecifier;


        private readonly Color _neutralColor = new Color(255, 255, 255, 200);
        private readonly Color _selectedColor = new Color(0, 0, 255, 200);
        

        private BuildTool _buildToolActor;
        private void Start()
        {
            _playerManager = xrOrigin.GetComponent<PlayerManager>();
            _toolManager = xrOrigin.GetComponent<ToolManager>();
            _houseManager = scripts.GetComponent<HouseManager>();

            _houseManager.blockMaterials = colors;
            
            _buildToolActor = new BuildTool(_houseManager, colors, _playerManager, _toolManager);
        }
        
        private void OnEnable()
        {
            ToolManager.OnToolChange += CloseColorMenu;
        }
        
        private void OnDisable()
        {
            ToolManager.OnToolChange += CloseColorMenu;
        }

        private void CloseColorMenu(ITool tool)
        {
            colorSpecifier.SetColorMenu(false);
        }

        public void ChangeToHousePlaceTool()
        {
            if (_houseManager == null) return;
            _toolManager.ChangeTool(new HousePlaceTool(_houseManager, _toolManager, ViewPlaceTool));

            _placeTool.SetActive(false);
            _exitPlaceTool.SetActive(true);
        }
        
        public void ChangeToGeoFenceBuildTool()
        {
            _toolManager.ChangeTool(new GeofenceTool(_toolManager));
        }
        
        public void ExitHousePlaceTool()
        {
            if (_houseManager == null) return;
            _toolManager.ResetTool();
            ViewPlaceTool();
        }
        
        public void ForceStalk()
        {
            var myId = PlayerManager.GetManager().GetLocalPlayerID();
            var forceStalkAction = new ForceStalkAction(myId);
            Stringifier.GetStringifier().StringifyAndForward<ForceStalkAction>(forceStalkAction, TypeOfPackage.ForceStalkAction, true);
        }

        [SerializeField] private GameObject copyMenu;
        
        public void OpenCopyMenu()
        {
            copyMenu.SetActive(true);
        }

        private void ViewPlaceTool()
        {
            _placeTool.SetActive(true);
            _exitPlaceTool.SetActive(false);
        }

        public void UpdateARParticipantsIDs()
        {
            _playerManager.UpdateIDs();
            _playerManager.SendIDs();
        }
        
        /**
         * Update currentBlockColor and remove menu
         */
        public override void ChangeColor(int index, BlockMaterials blockMaterials)
        {
            _toolManager.BuildTool.currentColorId = index;
        }
    }
}
