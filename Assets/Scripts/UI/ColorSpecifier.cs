using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ColorSpecifier : MonoBehaviour
    {
        private bool _showColorMenu = false;
        [SerializeField] private BlockMaterials blockMaterials;
        [SerializeField] private ChangableColor changeableColor;

        private readonly List<GameObject> _colorMenu = new List<GameObject>();
        private void OnDisable()
        {
            SetColorMenu(false);
        }

        private void OnEnable()
        {
            // Ensure colors are reset
            var material = GetComponent<Image>();
            material.color = blockMaterials.GetMaterial(0).color;
            changeableColor.ChangeColor(0, blockMaterials);
        }

        private void Start()
        {
            UpdateColorMenuCircle(0);
            BuildColorMenu();
            var button = GetComponent<Button>();
            button.onClick.AddListener(ToggleColorMenu);
        }

        /**
         * Toggle whether to show build menu or not
         */
        public void SetColorMenu(bool active)
        {
            _showColorMenu = active;
            if (_colorMenu.Count == 0) return;
            foreach (var menu in _colorMenu)
            {
                menu.SetActive(active);
            }
        }

        /**
         * When called. It builds a menu with all materials listed
         */
        private void BuildColorMenu()
        {
            var amount = blockMaterials.GetMaterials().Count;

            for(int i = 0; i<amount; i++){
                // Instantiate and add
                var o = gameObject;
                var menuOption = Instantiate(o, o.transform.parent);
                Destroy(menuOption.GetComponent<ColorSpecifier>());
                _colorMenu.Add(menuOption);
                menuOption.SetActive(false);
                
                // Fix button
                var button = menuOption.GetComponent<Button>();
                var currentColorIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(delegate
                {
                    ChangeColor(currentColorIndex);
                });
                
                // Fix material
                var material = menuOption.GetComponent<Image>();
                material.color = blockMaterials.GetMaterial(i).color;
                
                // Fix position
                var transform = menuOption.GetComponent<RectTransform>();
                
                var row = Math.Floor((float) (i / 4));
                transform.anchoredPosition = new Vector2(
                    -280 - (i%4 * 200),
                    (float) (-400 - row * 200));

            }
        }

        private void ChangeColor(int colorIndex)
        {
            Debug.Log("CLICKED");
            changeableColor.ChangeColor(colorIndex, blockMaterials);
            SetColorMenu(false);
            UpdateColorMenuCircle(colorIndex);
        }
        
        /*
         * Set color of specific menu item
         */
        private void UpdateColorMenuCircle(int newColor)
        {
            var material = blockMaterials.GetMaterial(newColor);
            GetComponent<Image>().color = material.color;
        }
        
        public void ToggleColorMenu()
        {
            SetColorMenu(!_showColorMenu);
        }
    }
}