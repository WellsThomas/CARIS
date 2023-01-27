using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.VisualGuide
{
    public class GuidingObject : MonoBehaviour
    {
        [SerializeField] private List<GameObject> arrow = new List<GameObject>();
        [SerializeField] private List<GameObject> guides = new List<GameObject>();
        private int _id;
        public Vector3 Position { get; set; }
        public Quaternion Direction { get; set; }
        private bool _isActive;
        private bool _isInvisible = false;

        public void SetID(int newID)
        {
            _id = newID;
        }

        public int GetID()
        {
            return _id;
        }
        
        private int i = 0;

        // Update is called once per frame
        void Update()
        {
            i++;
            if (i % 3 == 0) UpdateArrow();
        }
        
        void UpdateArrow()
        {
            var o = gameObject;
            var dist = Vector3.Distance(Camera.current.transform.position, o.transform.position);
            var scale = dist switch
            {
                > .5f => 1,
                > .3f => 5 * (dist - .3f),
                _ => 0f
            };
            var newColor = _isActive ? new Color(0, 1, 0, scale * .5f) : new Color(1, 0, 0, scale * .5f);

            if (scale == 0f)
            {
                if (!_isInvisible)
                {
                    SetArrowVisibility(false);
                    _isInvisible = true;
                }
                return;
            }
            
            if (_isInvisible)
            {
                SetArrowVisibility(true);
                _isInvisible = false;
            }
            
            foreach (var arrowPart in arrow)
            {
                var meshRenderer = arrowPart.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;
                meshRenderer.material.color = newColor;
            }
        }

        void SetArrowVisibility(bool active)
        {
            foreach (var arrowPart in arrow)
            {
                arrowPart.transform.position = arrowPart.transform.position + new Vector3(active ? 10000 : -10000, 0, 0);
            }
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            var newColor = _isActive ? new Color(0, 1, 0, .5f) : new Color(1, 0, 0, .5f);

            foreach (var guidePart in guides)
            {
                var meshRenderer = guidePart.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;
                meshRenderer.material.color = newColor;
            }
        }

        public GuidingObjectData ProduceDataObject()
        {
            return new GuidingObjectData
                (
                    _id,
                    Position,
                    Direction
                );
        }

        [Serializable]
        public class GuidingObjectData
        {
            public int ID;
            public SerializableVector3 Position;
            public SerializableQuaternion Direction;
            
            public GuidingObjectData(int id, SerializableVector3 position, SerializableQuaternion direction)
            {
                ID = id;
                Position = position;
                Direction = direction;
            }
        }
    }
}
