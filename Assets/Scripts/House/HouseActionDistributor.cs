using System;
using Communication;
using Communication.ActionPackage;
using JetBrains.Annotations;
using UI;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class HouseActionDistributor
    {
        [NotNull] private readonly House _house;
        private Stringifier _stringifier;
        private HouseManager _worldManager;
        private int ID;
        private readonly PlayerManager _playerManager;

        public HouseActionDistributor(House house, Stringifier stringifier, HouseManager worldManager, PlayerManager playerManager)
        {
            this._house = (House) house;
            this._stringifier = stringifier;
            this._worldManager = worldManager;
            this._playerManager = playerManager;
        }

        /**
         * Set new shared state of House. Distributes new setting to others
         */
        public bool SetShared(bool shouldBeShared)
        {
            var b = _house.SetShared(shouldBeShared);

            // Ship action
            var actionPackage = new SetSharedAction(shouldBeShared, ID);
            _stringifier.StringifyAndForward<SetSharedAction>(actionPackage, TypeOfPackage.SetHouseShared, true);

            // If new state is shared. Send position, scale & rotation
            if (!getShared()) return b;
            MoveHouse(_house.GetPose());
            Rotate(new Vector3(0, 0, 0));
            SetScale(GetScale());

            return b;
        }
    
        public void SetID(int id)
        {
            _house.SetID(id);
            this.ID = id;
        }
        
        public int GetID()
        {
            return ID;
        }

        public bool getShared()
        {
            return _house.GetShared();
        }

        /**
         * Rotate house and distribute new rotation to other phones
         */
        public void Rotate(Vector3 vector)
        {
            _house.Rotate(vector);
            SerializableQuaternion rotation = _house.offsetObject.transform.localRotation;
            
            // If shared. Send RotateAction to other phones
            if (!getShared()) return;
            var action = new RotateAction(rotation, _house.GetID());
            _stringifier.StringifyAndForward<RotateAction>(action, TypeOfPackage.SetHouseRotation, false);
        }

        public Vector3 GetScale()
        {
            return _house.GetLocalScale();
        }

        /**
         * Scale house and distribute new scale to other phones
         */
        public void SetScale(Vector3 localScale)
        {
            _house.SetLocalScale(localScale);
            
            // Ship action
            if (!getShared()) return;
            var action = new ScaleAction(localScale, _house.GetID());
            _stringifier.StringifyAndForward<ScaleAction>(action, TypeOfPackage.SetHouseScale, false);
        }

        /**
         * Move house and distribute new house to other phones
         */
        public void MoveHouse(Pose pose)
        {
            // Move House
            _house.MoveHouse(pose);

            if (!getShared()) return;

            // Create anchor if is shared
            var newAnchor = _worldManager.CreateAnchor(pose);
            
            // Send information to other clients
            var action = new MoveAction(newAnchor.trackableId.ToString(), ID);
            _stringifier.StringifyAndForward<MoveAction>(action, TypeOfPackage.CreateOrMove, true);
        }

        /**
         * Move house and distribute new house to other phones
         */
        public void OffsetHouse(Vector3 offset)
        {
            // Move House
            _house.OffsetHouse(offset);
            var localPosition = _house.offsetObject.transform.localPosition;

            if (!getShared()) return;

            // Send information to other clients
            var action = new OffsetAction(ID, localPosition);
            _stringifier.StringifyAndForward<OffsetAction>(action, TypeOfPackage.SetHouseOffset, false);
        }

        /**
         * Add new block to house and share update with other phones
         */
        public void NewBlock(Vector3 position, int colorId)
        {
            // Generate block ID and create block locally
            int id = Random.Range(Int32.MinValue, Int32.MaxValue);
            _house.NewBlock(position, id, colorId);
            
            // Send newly made block to others
            var action = new AddBlockAction(position, colorId, id, _house.GetID());
            _stringifier.StringifyAndForward<AddBlockAction>(action, TypeOfPackage.HouseAddBlock, true);
        }

        /**
         * Remove block from house and share update with other phones
         */
        public void RemoveBlock(int id)
        {
            // Remove locally
            _house.RemoveBlock(id);
            
            // Distribute to other phones
            var action = new RemoveBlockAction(id, _house.GetID());
            _stringifier.StringifyAndForward<RemoveBlockAction>(action, TypeOfPackage.HouseRemoveBlock, true);
        }
        
        /**
         * Set Crosshair position based on global positioning
         * If shouldShare is active, send crosshair
         */
        public void SetBuildCrosshair(Vector3 position, Quaternion rotation, bool shouldShare)
        {
            var playerID = _playerManager.GetLocalPlayerID();
            
            // Get crosshair and set its new position
            _house.GetCrosshair(playerID).SetBuildCrosshair(position, rotation);

            if (!shouldShare) return;

            // Get local position of crosshair
            var crosshairTransform = _house.GetCrosshair(playerID).transform;
            var localPosition = crosshairTransform.localPosition;
            
            // Distribute action to other phones
            var action = new AimMoveAction(GetID(), playerID, localPosition, crosshairTransform.localRotation);
            _stringifier.StringifyAndForward<AimMoveAction>(action, TypeOfPackage.HouseAimMoved, false);
        }
        
        /**
         * Set Crosshair based on local position
         * If shouldShare is active, send crosshair
         */
        public void SetBuildCrosshairBasedOnLocal(Vector3 localPosition, Quaternion localRotation, bool shouldShare)
        {
            var playerID = _playerManager.GetLocalPlayerID();
            // Get crosshair and set its new position
            
            _house.GetCrosshair(playerID).SetBuildCrosshairBasedOnLocal(localPosition, localRotation);

            if (!shouldShare) return;
            
            // Distribute action to other phones
            var action = new AimMoveAction(GetID(), playerID, localPosition, localRotation);
            _stringifier.StringifyAndForward<AimMoveAction>(action, TypeOfPackage.HouseAimMoved, false);
        }

        public void SetRotation(Quaternion rotation)
        {
            _house.offsetObject.transform.localRotation = rotation;
            
            // If shared. Send RotateAction to other phones
            if (!getShared()) return;
            var action = new RotateAction(rotation, _house.GetID());
            _stringifier.StringifyAndForward<RotateAction>(action, TypeOfPackage.SetHouseRotation, true);
        }

        public void SetPlotSize(int getDirectionIndex, int size)
        {
            _house.SetPlotSize(getDirectionIndex, size);
            
            var action = new PlotSizeAction(_house.GetID(), getDirectionIndex, size);
            _stringifier.StringifyAndForward<PlotSizeAction>(action, TypeOfPackage.SetPlotSizeAction, true);
        }
    }
}