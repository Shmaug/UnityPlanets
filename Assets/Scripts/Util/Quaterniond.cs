using UnityEngine;
using UnityEngine.Internal;

[System.Serializable]
public struct Quaterniond {
    /// <summary>
    ///   <para>X component of the Quaterniond. Don't modify this directly unless you know quaternions inside out.</para>
    /// </summary>
    public double x;

    /// <summary>
    ///   <para>Y component of the Quaterniond. Don't modify this directly unless you know quaternions inside out.</para>
    /// </summary>
    public double y;

    /// <summary>
    ///   <para>Z component of the Quaterniond. Don't modify this directly unless you know quaternions inside out.</para>
    /// </summary>
    public double z;

    /// <summary>
    ///   <para>W component of the Quaterniond. Don't modify this directly unless you know quaternions inside out.</para>
    /// </summary>
    public double w;

    private static readonly Quaterniond identityQuaternion = new Quaterniond(0f, 0f, 0f, 1f);

    public const double kEpsilon = 1E-06f;

    /*
    /// <summary>
    ///   <para>Returns the euler angle representation of the rotation.</para>
    /// </summary>
    public Vector3d eulerAngles {
        get {
            return Quaterniond.Internal_MakePositive(Quaterniond.Internal_ToEulerRad(this) * 57.29578f);
        }
        set {
            this = Quaterniond.Internal_FromEulerRad(value * 0.0174532924f);
        }
    }
    */

    public double this[int index] {
        get {
            double result;
            switch (index) {
                case 0:
                    result = this.x;
                    break;
                case 1:
                    result = this.y;
                    break;
                case 2:
                    result = this.z;
                    break;
                case 3:
                    result = this.w;
                    break;
                default:
                    throw new System.IndexOutOfRangeException("Invalid Quaterniond index!");
            }
            return result;
        }
        set {
            switch (index) {
                case 0:
                    this.x = value;
                    break;
                case 1:
                    this.y = value;
                    break;
                case 2:
                    this.z = value;
                    break;
                case 3:
                    this.w = value;
                    break;
                default:
                    throw new System.IndexOutOfRangeException("Invalid Quaterniond index!");
            }
        }
    }

    /// <summary>
    ///   <para>The identity rotation (Read Only).</para>
    /// </summary>
    public static Quaterniond identity {
        get {
            return Quaterniond.identityQuaternion;
        }
    }

    /// <summary>
    /// Conjugates and renormalizes the quaternion.
    /// </summary>
    public Quaterniond inverse {
        get {
            double lengthSq = 1.0 / sqrMagnitude;
            return new Quaterniond(-x * lengthSq, -y * lengthSq, -z * lengthSq, w * lengthSq);
        }
    }

    public double sqrMagnitude {
        get {
            return x * x + y * y + z * z + w * w;
        }
    }
    public double magnitude {
        get {
            return Mathd.Sqrt(x * x + y * y + z * z + w * w);
        }
    }
    
    /// <summary>
    ///   <para>Constructs new Quaterniond with given x,y,z,w components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    public Quaterniond(double x, double y, double z, double w) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    /// <summary>
    ///   <para>Set x, y, z and w components of an existing Quaterniond.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    /// <param name="newZ"></param>
    /// <param name="newW"></param>
    public void Set(double newX, double newY, double newZ, double newW) {
        this.x = newX;
        this.y = newY;
        this.z = newZ;
        this.w = newW;
    }

    public static Quaterniond Euler(double yaw, double pitch, double roll) {
        yaw *= Mathf.Deg2Rad;
        pitch *= Mathf.Deg2Rad;
        roll *= Mathf.Deg2Rad;

        double yawOver2 = yaw * 0.5;
        double cosYawOver2 = System.Math.Cos(yawOver2);
        double sinYawOver2 = System.Math.Sin(yawOver2);
        double pitchOver2 = pitch * 0.5;
        double cosPitchOver2 = System.Math.Cos(pitchOver2);
        double sinPitchOver2 = System.Math.Sin(pitchOver2);
        double rollOver2 = roll * 0.5;
        double cosRollOver2 = System.Math.Cos(rollOver2);
        double sinRollOver2 = System.Math.Sin(rollOver2);
        Quaterniond result;
        result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
        result.x = sinYawOver2 * cosPitchOver2 * cosRollOver2 + cosYawOver2 * sinPitchOver2 * sinRollOver2;
        result.y = cosYawOver2 * sinPitchOver2 * cosRollOver2 - sinYawOver2 * cosPitchOver2 * sinRollOver2;
        result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
        return result;
    }

    public static Quaterniond operator *(Quaterniond lhs, Quaterniond rhs) {
        return new Quaterniond(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y, lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z, lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x, lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
    }

    public static Vector3d operator *(Quaterniond rotation, Vector3d point) {
        double num = rotation.x * 2f;
        double num2 = rotation.y * 2f;
        double num3 = rotation.z * 2f;
        double num4 = rotation.x * num;
        double num5 = rotation.y * num2;
        double num6 = rotation.z * num3;
        double num7 = rotation.x * num2;
        double num8 = rotation.x * num3;
        double num9 = rotation.y * num3;
        double num10 = rotation.w * num;
        double num11 = rotation.w * num2;
        double num12 = rotation.w * num3;
        Vector3d result;
        result.x = (1f - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
        result.y = (num7 + num12) * point.x + (1f - (num4 + num6)) * point.y + (num9 - num10) * point.z;
        result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1f - (num4 + num5)) * point.z;
        return result;
    }

    public static bool operator ==(Quaterniond lhs, Quaterniond rhs) {
        return Quaterniond.Dot(lhs, rhs) > 0.999999f;
    }

    public static bool operator !=(Quaterniond lhs, Quaterniond rhs) {
        return !(lhs == rhs);
    }

    public static explicit operator Quaternion(Quaterniond a) {
        return new Quaternion((float)a.x, (float)a.y, (float)a.z, (float)a.w);
    }
    public static explicit operator Quaterniond(Quaternion a) {
        return new Quaterniond(a.x, a.y, a.z, a.w);
    }

    /// <summary>
    ///   <para>The dot product between two rotations.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static double Dot(Quaterniond a, Quaterniond b) {
        return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    }
    
    /// <summary>
    ///   <para>Returns the angle in degrees between two rotations a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static double Angle(Quaterniond a, Quaterniond b) {
        double f = Quaterniond.Dot(a, b);
        return Mathd.Acos(Mathd.Min(Mathd.Abs(f), 1f)) * 2f * 57.29578f;
    }

    private static Vector3d Internal_MakePositive(Vector3d euler) {
        double num = -0.005729578;
        double num2 = 360f + num;
        if (euler.x < num) {
            euler.x += 360f;
        } else if (euler.x > num2) {
            euler.x -= 360f;
        }
        if (euler.y < num) {
            euler.y += 360f;
        } else if (euler.y > num2) {
            euler.y -= 360f;
        }
        if (euler.z < num) {
            euler.z += 360f;
        } else if (euler.z > num2) {
            euler.z -= 360f;
        }
        return euler;
    }

    public override int GetHashCode() {
        return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2 ^ this.w.GetHashCode() >> 1;
    }

    public override bool Equals(object other) {
        bool result;
        if (!(other is Quaterniond)) {
            result = false;
        } else {
            Quaterniond quaternion = (Quaterniond)other;
            result = (this.x.Equals(quaternion.x) && this.y.Equals(quaternion.y) && this.z.Equals(quaternion.z) && this.w.Equals(quaternion.w));
        }
        return result;
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string of the Quaterniond.</para>
    /// </summary>
    /// <param name="format"></param>
    public override string ToString() {
        return string.Format("({0:F1}, {1:F1}, {2:F1}, {3:F1})", new object[]
        {
                this.x,
                this.y,
                this.z,
                this.w
        });
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string of the Quaterniond.</para>
    /// </summary>
    /// <param name="format"></param>
    public string ToString(string format) {
        return string.Format("({0}, {1}, {2}, {3})", new object[]
        {
                this.x.ToString(format),
                this.y.ToString(format),
                this.z.ToString(format),
                this.w.ToString(format)
        });
    }
}