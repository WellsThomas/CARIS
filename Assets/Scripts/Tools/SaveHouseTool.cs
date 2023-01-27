using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Tools
{
    public class SaveHouseTool : ITool
    {
        private HouseManager _manager;
        private readonly Action<House> _onSelect;
        [CanBeNull] private House _currentHouse = null;

        public SaveHouseTool(HouseManager manager, ToolManager toolManager, Action<House> onSelect)
        {
            _manager = manager;
            _onSelect = onSelect;
        }

        private void SetCurrentHouse(House house)
        {
            SetHighlightOfCurrentHouse(false);

            _currentHouse = house;
            if (house == null) return;
            SetHighlightOfCurrentHouse(true);
        }
        
        

        private void SetHighlightOfCurrentHouse(bool active)
        {
            if (_currentHouse == null) return;

            var renderer = _currentHouse.GetGrabbable().GetComponent<MeshRenderer>();
            renderer.material = new Material(renderer.material)
            {
                color = active ? new Color(0,255,0,0.45f) : new Color(0,0,0,0.12f)
            };
        }

        private bool _hasBeenClicked = false;
        
        public void OnClick()
        {
            if (_currentHouse == null) return;
            if (_hasBeenClicked) return;
            _hasBeenClicked = true;
            _onSelect(_currentHouse);
            ToolManager.GetManager().ResetTool();
        }

        private int _i = 0;
        
        public void PerformRaycast(Ray ray)
        {
            _i++;
            if (_i % 5 != 0) return;
            var layerMask = 1 << 7;
            RaycastHit hits;
            if (!Physics.Raycast(ray, out hits, Mathf.Infinity, layerMask))
            {
                SetCurrentHouse(null);
                return;
            }
            var hitObject = hits.transform.gameObject;

            SetCurrentHouse(hitObject.GetComponentInParent<House>());
        }

        public string GetName()
        {
            return "Save House Tool";
        }

        public new ITool.ToolType GetType()
        {
            return ITool.ToolType.SaveHouseTool;
        }
    }
}