using UnityEngine;
using System.Collections;

public static class Vector3Extensions
{
    public static bool IsNaN(this Vector3 vector) => float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);
    public static bool IsInfinity(this Vector3 vector) => float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z);
    public static bool IsInfinityOrNaN(this Vector3 vector) => vector.IsNaN() || vector.IsInfinity();

    // Performs a memberwise multiplication
    public static Vector3 Multiply(this Vector3 vector1, Vector3 vector2)
    {
        return Vector3.Scale(vector1, vector2);
    }
}