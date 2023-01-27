using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Interaction.VisualGuide;
using JetBrains.Annotations;
using UI;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Player;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class BuildTool : ITool
    {
        private HouseManager _manager;
        private PlayerManager _playerManager;
        private Vector3 newBlockPos;
        private bool newBlockPosBasedOnBlock;
        public int currentColorId { get; set; } = 0;
        private BlockMaterials _materials;
        public bool shareCrosshair { get; set; } = true;
        [CanBeNull] private GameObject lastHitObject;
        [CanBeNull] public int? LatestHouseID;
        private int variableForDecreasingSpam;
        [CanBeNull] private HouseResizeArrow currentArrow;
        
        public string GetName()
        {
            return "Build Tool";
        }


        public void OnToolChange()
        {
            ResetArrow();
        }

        /**
         * Create new block on left click
         */
        public void OnLeftClick()
        {
            if (LatestHouseID == null) return;
            if (currentArrow != null) return;
            
            // Find house based on latest aim and create new Block
            _manager.GetHouse((int)LatestHouseID)?.Distributor.NewBlock(newBlockPos, currentColorId);
            
            // Prevent double clicking, latestHouseID is reset to null
            LatestHouseID = null;
        }

        private void ChangeToArrowTool()
        {
            if (LatestHouseID != null) _toolManager.ChangeTool(new HouseResizeTool(currentArrow, (int)LatestHouseID));
        }

        /**
         * Retrieve latest id of house
         */
        public int? GetLatestHouseID()
        {
            return LatestHouseID;
        }

        /**
         * Remove new blocks on right click 
         */
        public void OnRightClick()
        {
            // If no house or object is selected. Return
            if (LatestHouseID == null) return;
            if (currentArrow != null) return;
            if (lastHitObject == null) return;

            // Get houseContent and blockID. If either is not present. Return
            var houseContent = _manager.GetHouse((int) LatestHouseID);
            var blockId = lastHitObject.GetComponent<BlockInformation>()?.blockID;
            if (houseContent == null || blockId == null) return;

            // Distribute that user is removing block
            houseContent.Value.Distributor.RemoveBlock(blockId.Value);
        }

        public BuildTool(HouseManager manager, BlockMaterials blockMaterials, PlayerManager playerManager, ToolManager toolManager)
        {
            _playerManager = playerManager;
            _manager = manager;
            _materials = blockMaterials;
            _toolManager = toolManager;
        }
        
        public bool IsBuildTool()
        {
            return true;
        }

        public bool HandleBuildRaycast(Ray ray)
        {
            // If build tool. Perform special raycast
            int layerMask = (1 << 7) | (1 << 13);
            RaycastHit hits;
            if (Physics.Raycast(ray, out hits, Mathf.Infinity, layerMask))
            {
                OnPhysicsRayCast(hits);
                return true;
            }
            
            _manager.currentHouseIndex = -1;
            ResetArrow();
            VisualGuidanceManager.Get().UpdateActiveGuidingObject(null);
            ResetGrabbable();
            return false;
        }

        private GameObject lastGrabbedObject;
        private readonly ToolManager _toolManager;

        // Whenever a physics raycast hits
        public void OnPhysicsRayCast(RaycastHit hit)
        {
            // Following decreases update-rate from 60 to 20hz. Should be sufficient for crosshair.
            // Should minimize spam when multiple phones spam cross-hair messages
            variableForDecreasingSpam++;
            if (variableForDecreasingSpam % 3 != 0) return;
            
            // Find collision object. 
            var hitObject = hit.collider.gameObject;

            // Assess if hit object is visual guidance
            if (VisualGuidanceManager.Get().UpdateActiveGuidingObject(hitObject))
            {
                _manager.currentHouseIndex = -1;
                ResetArrow();
                return;
            }

                // If it is not an object or doesnt have an ID
            int? hitObjectHouseID = hitObject.transform?.parent?.parent?.gameObject?.GetComponent<House>()?.GetID();
            if (hitObjectHouseID == null)
            {
                _manager.currentHouseIndex = -1;
                ResetArrow();
                return;
            }
            
            // Get houseContent. Return if it does not exist
            _manager.currentHouseIndex = (int) hitObjectHouseID;
            var houseContent = _manager.GetCurrentHouse();
            if (houseContent == null)
            {
                ResetArrow();
                return;
            }

            // Save latest house ID for when users choose to spawn
            LatestHouseID = hitObjectHouseID;

            if (SetGrabbable(hitObject, (HouseManager.HouseContent)houseContent))
            {
                ResetArrow();
                return;
            }
            ResetGrabbable();   
            
            // Depending on whether a plane or buildcube was hit. Place crosshair accordingly
            if (hitObject.name == "Plane") PlaceCrosshairOnPlane(houseContent.Value, hit);
            else if (hitObject.name == "BuildCube") PlaceCrosshairOnBlock(houseContent.Value, hit, hitObject);
            else if (hitObject.name.Substring(0,5) == "Arrow") HighlightArrow(houseContent.Value, hit, hitObject);
        }
        
        private HouseManager.HouseContent? latestGrabbedHouseContent;


        private void ResetArrow()
        {
            if (currentArrow != null)
                currentArrow.Highlight(false);
            currentArrow = null;
        }
        
        public HouseManager.HouseContent? GetGrabbable()
        {
            return lastGrabbedObject == null ? null : latestGrabbedHouseContent;
        }

        private void HighlightArrow(HouseManager.HouseContent houseContent, RaycastHit hit, GameObject hitObject)
        {
            currentArrow = hitObject.GetComponent<HouseResizeArrow>();
            if (currentArrow == null) return;
            
            currentArrow.Highlight(true);
        }

        private bool SetGrabbable(GameObject hitObject, HouseManager.HouseContent houseContent)
        {
            if (hitObject.name != "Grabbable") return false;
            
            lastGrabbedObject = hitObject;
            latestGrabbedHouseContent = houseContent;
            LatestHouseID = null;
            var renderer = lastGrabbedObject.GetComponent<MeshRenderer>();
            renderer.material = new Material(renderer.material)
            {
                color = new Color(0,0,255,0.30f)
            };
            
            return true;
        }

        private void ResetGrabbable()
        {
            if (lastGrabbedObject != null)
            {
                var renderer = lastGrabbedObject.GetComponent<MeshRenderer>();
                renderer.material = new Material(renderer.material)
                {
                    color = new Color(0,0,0,0.12f)
                };
            }
            lastGrabbedObject = null;
        }
        
        /**
         * Place crosshair on plane based on Raycast
         * After placing it distributes the crosshair
         */
        private void PlaceCrosshairOnPlane(HouseManager.HouseContent houseContent, RaycastHit hit)
        {
            // Set lastHitObject to null. This variable is used to remove blocks
            lastHitObject = null;
            
            // Get rotation of house-plot.
            var house = houseContent.Local;
            var planeRotation = house.offsetObject.transform.rotation;
            
            // Place crosshair in accordance to hit and planeRotation
            var crosshairObject = house.GetCrosshair(_playerManager.GetLocalPlayerID());
            crosshairObject.SetBuildCrosshair(hit.point, planeRotation);
            
            // Following code ensures that the placement of the crosshair snapped to the local grid
            var pos = crosshairObject.transform.localPosition;
            pos.x = (float) (Math.Round(pos.x * 10) / 10);
            pos.z = (float) (Math.Round(pos.z * 10) / 10);
            
            // Set position of potential new block if user chooses to place block
            newBlockPos = new Vector3(pos.x, pos.y + .05f, pos.z);
            
            // Slightly increase crosshair position to remove collision from ground
            pos.y += .01f;

            // Distribute newly positioned crosshair
            houseContent.Distributor.SetBuildCrosshairBasedOnLocal(pos, crosshairObject.gameObject.transform.localRotation, true);
        }
        
        /**
         * Place crosshair on block based on Raycast
         * After placing it distributes the crosshair
         */
        private void PlaceCrosshairOnBlock(HouseManager.HouseContent houseContent, RaycastHit hit, GameObject hitObject)
        {
            // Save last Hit Object in case user decides to delete
            lastHitObject = hitObject;
            
            // Get direction of crosshair from center of the block that was hit
            var house = houseContent.Local;
            var direction = Quaternion.LookRotation(hit.normal, hitObject.transform.right);
            
            // Based on scale, direction and hitObject position, find position of crosshair
            var localScale = house.offsetObject.transform.localScale;
            var scaling = localScale.x * .06f;
            var crosshairPosition = direction * Vector3.forward * scaling + hitObject.transform.position;

            // Set local crosshair position
            var crosshair = house.GetCrosshair(_playerManager.GetLocalPlayerID());
            crosshair.SetBuildCrosshair(crosshairPosition, direction);
            
            // Rotate crosshair accordingly
            var crosshairTransform = crosshair.transform;
            crosshairTransform.Rotate(new Vector3(90, 0, 0));
            
            // Distribute crosshair position
            var localPosition = crosshairTransform.localPosition;
            houseContent.Distributor.SetBuildCrosshairBasedOnLocal(localPosition, crosshairTransform.localRotation, true);
            
            // Set position of potential new block if user chooses to place block
            localPosition.x = (float) (Math.Round(localPosition.x * 10) / 10);
            localPosition.y = (float) (Math.Ceiling((localPosition.y) * 10) / 10) - .05f;
            localPosition.z = (float) (Math.Round(localPosition.z * 10) / 10);
            newBlockPos = localPosition;
        }

        public bool ArrowIsActive()
        {
            return currentArrow != null;
        }

        public void ActivateArrowDrag()
        {
            if (LatestHouseID == null) return;
            _toolManager.ChangeTool(new HouseResizeTool(currentArrow, (int)LatestHouseID));
        }
    }
}
