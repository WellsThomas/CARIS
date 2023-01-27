using UnityEngine;

namespace UI
{
    public class ChangableColor : MonoBehaviour
    {
        public virtual void ChangeColor(int i1, BlockMaterials blockMaterials)
        {
            Debug.Log("ChangeColor NOT OVERRIDDEN");
        }
    }
}