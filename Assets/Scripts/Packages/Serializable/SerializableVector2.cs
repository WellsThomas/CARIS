using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableVector2
{
    /// <summary>
    /// x component
    /// </summary>
    public float x;
     
    /// <summary>
    /// y component
    /// </summary>
    public float y;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    public SerializableVector2(float rX, float rY)
    {
        x = rX;
        y = rY;
    }
     
    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}]", x, y);
    }
     
    /// <summary>
    /// Automatic conversion from SerializableVector3 to Vector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Vector2(SerializableVector2 rValue)
    {
        return new Vector2(rValue.x, rValue.y);
    }
     
    /// <summary>
    /// Automatic conversion from Vector3 to SerializableVector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableVector2(Vector2 rValue)
    {
        return new SerializableVector2(rValue.x, rValue.y);
    }
}
