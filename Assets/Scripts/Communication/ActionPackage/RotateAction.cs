namespace Communication.ActionPackage
{
    [System.Serializable]
    public class RotateAction : IAction
    {
        public SerializableQuaternion rotation;
        public int id;

        public RotateAction(SerializableQuaternion rotation, int id)
        {
            this.rotation = rotation;
            this.id = id;
        }

        public int GetHouseID()
        {
            return id;
        }
    }
}