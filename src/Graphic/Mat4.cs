using System.Runtime.InteropServices;

namespace Engine.Graphic;

[StructLayout(LayoutKind.Sequential)]
public struct Mat4
{
    public float M00, M01, M02, M03;
    public float M10, M11, M12, M13;
    public float M20, M21, M22, M23;
    public float M30, M31, M32, M33;

    public static Mat4 Identity => new Mat4
    {
        M00 = 1f,
        M11 = 1f,
        M22 = 1f,
        M33 = 1f
    };

    public static Mat4 Translation(float x, float y, float z)
    {
        Mat4 result = Identity;
        result.M30 = x;
        result.M31 = y;
        result.M32 = z;
        return result;
    }

    public static Mat4 Scale(float x, float y, float z)
    {
        Mat4 result = Identity;
        result.M00 = x;
        result.M11 = y;
        result.M22 = z;
        return result;
    }

    public static Mat4 OrthographicOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float width = right - left;
        float height = top - bottom;
        float depth = far - near;

        Mat4 result = Identity;
        result.M00 = 2f / width;
        result.M11 = 2f / height;
        result.M22 = 1f / depth;
        result.M30 = -(right + left) / width;
        result.M31 = -(top + bottom) / height;
        result.M32 = -near / depth;
        return result;
    }

    public static Mat4 operator *(Mat4 a, Mat4 b)
    {
        return new Mat4
        {
            M00 = a.M00 * b.M00 + a.M10 * b.M01 + a.M20 * b.M02 + a.M30 * b.M03,
            M01 = a.M01 * b.M00 + a.M11 * b.M01 + a.M21 * b.M02 + a.M31 * b.M03,
            M02 = a.M02 * b.M00 + a.M12 * b.M01 + a.M22 * b.M02 + a.M32 * b.M03,
            M03 = a.M03 * b.M00 + a.M13 * b.M01 + a.M23 * b.M02 + a.M33 * b.M03,

            M10 = a.M00 * b.M10 + a.M10 * b.M11 + a.M20 * b.M12 + a.M30 * b.M13,
            M11 = a.M01 * b.M10 + a.M11 * b.M11 + a.M21 * b.M12 + a.M31 * b.M13,
            M12 = a.M02 * b.M10 + a.M12 * b.M11 + a.M22 * b.M12 + a.M32 * b.M13,
            M13 = a.M03 * b.M10 + a.M13 * b.M11 + a.M23 * b.M12 + a.M33 * b.M13,

            M20 = a.M00 * b.M20 + a.M10 * b.M21 + a.M20 * b.M22 + a.M30 * b.M23,
            M21 = a.M01 * b.M20 + a.M11 * b.M21 + a.M21 * b.M22 + a.M31 * b.M23,
            M22 = a.M02 * b.M20 + a.M12 * b.M21 + a.M22 * b.M22 + a.M32 * b.M23,
            M23 = a.M03 * b.M20 + a.M13 * b.M21 + a.M23 * b.M22 + a.M33 * b.M23,

            M30 = a.M00 * b.M30 + a.M10 * b.M31 + a.M20 * b.M32 + a.M30 * b.M33,
            M31 = a.M01 * b.M30 + a.M11 * b.M31 + a.M21 * b.M32 + a.M31 * b.M33,
            M32 = a.M02 * b.M30 + a.M12 * b.M31 + a.M22 * b.M32 + a.M32 * b.M33,
            M33 = a.M03 * b.M30 + a.M13 * b.M31 + a.M23 * b.M32 + a.M33 * b.M33
        };
    }
}
