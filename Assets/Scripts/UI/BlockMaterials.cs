using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /**
     * Block Material Object used to store materials used in game
     */
    public class BlockMaterials : MonoBehaviour
    {
        [SerializeField]
        private List<Material> materials;

        public List<Material> GetMaterials()
        {
            return materials;
        }

        public Material GetMaterial(int index)
        {
            return (Material) materials[index];
        }
    }
}