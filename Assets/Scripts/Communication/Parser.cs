using System.Xml;
using Communication;
using Communication.ActionPackage;
using Interaction.Drawable;
using Interaction.VisualGuide;
using UI;
using Unity.Collections;
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARFoundation.Samples.Player;

namespace UnityEngine.XR.ARFoundation.Samples.Communication
{
    public class Parser
    {
        private HouseManager _houseManager;
        private PlayerManager _playerManager;
        private VisualGuidanceManager _guidanceManager;

        public Parser(HouseManager houseManager, PlayerManager playerManager)
        {
            this._houseManager = houseManager;
            this._playerManager = playerManager;
            this._guidanceManager = VisualGuidanceManager.Get();
        }

        /**
         * Parse NSData to Actionpackage.
         * The internal action is then forwarded correctly to the system
         */
        public void Parse(TypeOfPackage typeOfPackage, byte[] data)
        {
            HouseManager.HouseContent? house;
            
            // Depending on type of package. Forward correctly to system
            switch (typeOfPackage)
            {
                case TypeOfPackage.CreateOrMove:
                    var moveHouseAction = ToAction<MoveAction>(data);
                    _houseManager.AddHouseMoveToUnprocessed(moveHouseAction);
                    break;
                
                // Built new geofence
                case TypeOfPackage.CreateNewGeofence:
                    var action = ToAction<GeofenceBuildAction>(data);
                    GeofenceManager.Get().AddGeofenceToUnprocessed(action);
                    break;

                // Set House offset to reduce anchors sent
                case TypeOfPackage.SetHouseOffset:
                    var offsetAction = ToAction<OffsetAction>(data);
                    house = _houseManager.GetHouse(offsetAction.GetHouseID());
                    house?.Local.OffsetHouseBasedOnLocal(offsetAction.Offset);
                    break;
                
                // Set House offset to reduce anchors sent
                case TypeOfPackage.RemoveHouse:
                    var removeHouseAction = ToAction<RemoveAction>(data);
                    _houseManager.EraseHouseLocally(removeHouseAction.GetHouseID());
                    break;
                
                // Change stage of sharedness
                case TypeOfPackage.SetHouseShared:
                    var setSharedAction = ToAction<SetSharedAction>(data);
                    house = _houseManager.GetHouse(setSharedAction.GetHouseID());
                    house?.Local.SetShared(setSharedAction.shared);
                    break;

                // Change rotation of house
                case TypeOfPackage.SetHouseRotation:
                    var rotateAction = ToAction<RotateAction>(data);
                    house = _houseManager.GetHouse(rotateAction.GetHouseID());
                    if (house != null && house.Value.Local != null) house.Value.Local.SetLocalRotation(rotateAction.rotation);
                    break;
                
                // Change scale of house
                case TypeOfPackage.SetHouseScale:
                    var scaleAction = ToAction<ScaleAction>(data);
                    house = _houseManager.GetHouse(scaleAction.GetHouseID());
                    if (house != null && house.Value.Local != null) house.Value.Local.SetLocalScale(scaleAction.scale);
                    break;

                // Add a block to the house
                case TypeOfPackage.HouseAddBlock:
                    var blockAction = ToAction<AddBlockAction>(data);
                    house = _houseManager.GetHouse(blockAction.GetHouseID());
                    if (house != null && house.Value.Local != null) house.Value.Local.NewBlock(blockAction.position, blockAction.blockId, blockAction.colorId);
                    break;
                
                // Remove a block from house
                case TypeOfPackage.HouseRemoveBlock:
                    var removeAction = ToAction<RemoveBlockAction>(data);
                    house = _houseManager.GetHouse(removeAction.GetHouseID());
                    if (house != null && house.Value.Local != null) house.Value.Local.RemoveBlock(removeAction.blockId);
                    break;
                
                // Remove the aim of player
                case TypeOfPackage.HouseAimMoved:
                    var aimMoved = ToAction<AimMoveAction>(data);
                    house = _houseManager.GetHouse(aimMoved.GetHouseID());
                    if (house != null && house.Value.Local != null) house.Value.Local.GetCrosshair(aimMoved.playerID).SetBuildCrosshairBasedOnLocal(aimMoved.localPosition, aimMoved.localRotation);
                    break;
                
                // New PlayerID assignments has been distributed
                case TypeOfPackage.PlayerIDs:
                    if (_playerManager != null) _playerManager.HandleIncomingIDs(data);
                    break;
                
                // CopyHouse
                case TypeOfPackage.CopyHouse:
                    var copyHouseAction = ToAction<CopyAction>(data);
                    _houseManager.AddHouseCopyToUnprocessed(copyHouseAction);
                    break;
                
                // CopyHouse
                case TypeOfPackage.ReplaceAction:
                    var replaceAction = ToAction<ReplaceAction>(data);
                    if (replaceAction.Active)
                        _houseManager.StartVisualizeReplacement(replaceAction.ReplacerID, replaceAction.ReplacedID, false);
                    else
                        _houseManager.StopVisualizeReplacement(replaceAction.ReplacerID, replaceAction.ReplacedID, false);
                    break;
                
                // Adjust whether stalk action happens
                case TypeOfPackage.StalkAction:
                    var stalkAction = ToAction<StalkingAction>(data);
                    if (stalkAction.IsStalking)
                        StalkerHandler.Get().AddStalker(stalkAction.StalkerID, stalkAction.StalkedID);
                    else
                        StalkerHandler.Get().RemoveStalker(stalkAction.StalkerID, stalkAction.StalkedID);
                    break;
                
                case TypeOfPackage.ForceStalkAction:
                    var forceStalkAction = ToAction<ForceStalkAction>(data);
                    StalkerHandler.Get().StartStalking(forceStalkAction.StalkedID);
                    break;
                
                // Load new world
                case TypeOfPackage.LoadWorld:
                    var loadWorldAction = ToAction<LoadWorldAction>(data);
                    _houseManager.AddWorldLoadToUnprocessed(loadWorldAction);
                    break;
                
                // Request world data from other participants
                case TypeOfPackage.RequestLoadWorld:
                    _houseManager.AnswerLoadWorldRequest();
                    break;
                
                // Add Guidance
                case TypeOfPackage.AddGuidance:
                    Debug.Log("Add Guidance received");
                    var addGuidanceAction = ToAction<AddGuidance>(data);
                    _guidanceManager.AddGuidanceToUnprocessed(addGuidanceAction);
                    break;
                
                // Remove Guidance
                case TypeOfPackage.RemoveGuidance:
                    var removeGuidanceAction = ToAction<RemoveGuidance>(data);
                    _guidanceManager.RemoveGuidanceLocally(removeGuidanceAction.GuidanceID);
                    break;
                
                // Scale & Rotate Geofence
                case TypeOfPackage.ScaleRotateGeofence:
                    var scaleRotateActions = ToAction<ScaleRotateGeofenceAction>(data);
                    GeofenceManager.Get().AddUnprocessedRotateScaleActions(scaleRotateActions);
                    break;
                
                case TypeOfPackage.SetPlotSizeAction:
                    var setPlotSize = ToAction<PlotSizeAction>(data);
                    house = _houseManager.GetHouse(setPlotSize.GetHouseID());
                    house?.Local.SetPlotSize(setPlotSize.Direction, setPlotSize.Size);
                    break;

                case TypeOfPackage.DistributeFreezeFrame:
                    var distributeFreezeFrame = ToAction<DistributeFreezeFrameAction>(data);
                    FreezeFrame.Get().DisplayForeignFrame(distributeFreezeFrame);
                    break;
                
                case TypeOfPackage.AddLineSegment:
                    if (!Drawable.IsDrawableAvailable()) return;
                    var newLineSegment = ToAction<LineSegment>(data);
                    Drawable.Get().QueueLineSegment(newLineSegment);
                    break;
                
                case TypeOfPackage.RemoveDrawing:
                    if (!Drawable.IsDrawableAvailable()) return;
                    Drawable.Get().ResetDrawing();
                    break;
                
                case TypeOfPackage.RemoveLine:
                    if (!Drawable.IsDrawableAvailable()) return;
                    var removeLineAction = ToAction<RemoveLine>(data);
                    Drawable.Get().RemoveLine(removeLineAction.ID);
                    break;
            }
        }
        
        /**
         * Convert datastring to Action Object
         */
        private T ToAction<T>(byte[] data)
        {
            var str = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(str);
        }
    }
}