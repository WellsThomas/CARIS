using System;
using UnityEngine.Serialization;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class HouseResizeArrow : MonoBehaviour
    {
        private bool _isHighlighted;
        private int _direction = 0;
        
        private void Awake()
        {
            if (this.name[5].Equals('X')) _direction++;
            if (this.name[6].Equals('0')) _direction+=2;
        }

        public void Highlight(bool active)
        {
            if (_isHighlighted != active)
                ChangeToActiveColor(active);
            _isHighlighted = active;
        }

        private static readonly Color Highlighted = new Color(0, 0, 1, .4f);
        private static readonly Color Unhighlighted = new Color(.45f, .45f, .45f, .6f);

        private void ChangeToActiveColor(bool active)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().color = active ? Highlighted : Unhighlighted;
        }

        public int GetDirectionIndex()
        {
            return _direction;
        }

        public void SetPosition(int length)
        {
            var vectorLength = length * .1f + .18f;
            var pos = _direction switch
            {
                0 => new Vector3(0, 0, vectorLength),
                1 => new Vector3(vectorLength, 0, 0),
                2 => new Vector3(0, 0, -vectorLength),
                3 => new Vector3(-vectorLength, 0, 0),
                _ => default
            };

            gameObject.transform.localPosition = pos;
        }
    }
}