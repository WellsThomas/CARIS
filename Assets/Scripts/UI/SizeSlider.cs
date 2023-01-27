using System;
using Interaction.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SizeSlider : MonoBehaviour
    {
        private Slider _slider;
        [SerializeField] private Drawable _drawable;
        private void Start()
        {
            _slider = gameObject.GetComponent<Slider>();
            _slider.onValueChanged.AddListener (delegate {OnValueChange ();});
        }

        private void OnValueChange()
        {
            _drawable.SetCurrentSize((int)_slider.value);
        }
    }
}