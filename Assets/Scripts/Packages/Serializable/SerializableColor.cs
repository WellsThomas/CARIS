using UnityEngine;
namespace Packages.Serializable {
    
    [System.Serializable]
    public class SerializableColor{
        public float[] colorStore = new float[4]{1F,1F,1F,1F};

        public SerializableColor(Color value)
        {
            colorStore[0] = value.r;
            colorStore[1] = value.g;
            colorStore[2] = value.b;
            colorStore[3] = value.a;
        }
     
        //makes this class usable as Color, Color normalColor = mySerializableColor;
        public static implicit operator Color( SerializableColor instance )
        {
            return new Color(instance.colorStore[0], instance.colorStore[1], instance.colorStore[2], instance.colorStore[3]);
        }
     
        //makes this class assignable by Color, SerializableColor myColor = Color.white;
        public static implicit operator SerializableColor( Color color ){
            return new SerializableColor(color);
        }
    }
}