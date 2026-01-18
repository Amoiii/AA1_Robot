using System;

// Reemplazo propio para Vector3
[System.Serializable]
public struct MyVec3
{
    public float x, y, z;

    public MyVec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

    // Conversión a Unity para poder aplicarlo al Transform al final
    public UnityEngine.Vector3 ToUnity() => new UnityEngine.Vector3(x, y, z);
    public static MyVec3 FromUnity(UnityEngine.Vector3 v) => new MyVec3(v.x, v.y, v.z);

   // Operaciones vectoriales básicas [cite: 919]
    public static MyVec3 operator +(MyVec3 a, MyVec3 b) => new MyVec3(a.x + b.x, a.y + b.y, a.z + b.z);
    public static MyVec3 operator -(MyVec3 a, MyVec3 b) => new MyVec3(a.x - b.x, a.y - b.y, a.z - b.z);
    public static MyVec3 operator *(MyVec3 a, float d) => new MyVec3(a.x * d, a.y * d, a.z * d);
    public static MyVec3 operator /(MyVec3 a, float d) => new MyVec3(a.x / d, a.y / d, a.z / d);

  // Magnitud y Normalización [cite: 891-892]
    public float Magnitude() => MyMath.Sqrt(x * x + y * y + z * z);

    public MyVec3 Normalized()
    {
        float m = Magnitude();
        if (m > 1e-5f) return this / m;
        return new MyVec3(0, 0, 0);
    }

    public static float Distance(MyVec3 a, MyVec3 b) => (a - b).Magnitude();

   // Producto Escalar (Dot Product) [cite: 921]
    public static float Dot(MyVec3 a, MyVec3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
}

// Reemplazo propio para Mathf y Quaternion (usando Euler)
public static class MyMath
{
    public const float PI = 3.14159265f;
    public const float Deg2Rad = PI / 180f;
    public const float Rad2Deg = 180f / PI;

    public static float Sin(float angleRad) => (float)Math.Sin(angleRad);
    public static float Cos(float angleRad) => (float)Math.Cos(angleRad);
    public static float Sqrt(float val) => (float)Math.Sqrt(val);
    public static float Abs(float val) => val < 0 ? -val : val;

    // Interpolación Lineal
    public static float Lerp(float a, float b, float t)
    {
        if (t < 0) t = 0; if (t > 1) t = 1;
        return a + (b - a) * t;
    }

    // Interpolación de ángulos (para evitar el salto de 360 a 0)
    public static float LerpAngle(float a, float b, float t)
    {
        float diff = b - a;
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return a + diff * t;
    }

    public static float Clamp(float val, float min, float max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    // Función Atan2 para calcular ángulos de la base
    public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
}