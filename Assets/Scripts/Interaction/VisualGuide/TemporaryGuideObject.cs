using UnityEngine;

namespace Interaction.VisualGuide
{
    public class TemporaryGuideObject
    {
        private readonly int _id;
        private GameObject _obj;
        
        public TemporaryGuideObject(int ID, Vector3 pos, Quaternion dir, GameObject helper)
        {
            this._id = ID;
            _obj = new GameObject
            {
                transform =
                {
                    position = pos,
                    rotation = dir,
                    parent = helper.transform
                }
            };
        }

        public void ReplaceWithGuideObject()
        {
            var manager = VisualGuidanceManager.Get();
            manager.SpawnAndMoveGuidance(_id,_obj.transform.position,_obj.transform.rotation);
            Object.Destroy(_obj);
        }
    }
}