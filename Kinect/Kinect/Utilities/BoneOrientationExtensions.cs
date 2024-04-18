using System;
using Microsoft.Kinect;

namespace KinectCoordinateMapping
{
    /// <summary>
    /// Provides extension methods for transforming quaternions to rotations.
    /// </summary>
    /// 
    public static class BoneOrientationExtensions
    {

        public static Angle3 toEulerianAngle(this Vector4 data)
        {
            Angle3 ans = new Angle3();

            double q2sqr = data.Y * data.Y;
            double t0 = -2.0 * (q2sqr + data.Z * data.Z) + 1.0;
            double t1 = +2.0 * (data.X * data.Y + data.W * data.Z);
            double t2 = -2.0 * (data.X * data.Z - data.W * data.Y);
            double t3 = +2.0 * (data.Y * data.Z + data.W * data.X);
            double t4 = -2.0 * (data.X * data.X + q2sqr) + 1.0;

            t2 = t2 > 1.0 ? 1.0 : t2;
            t2 = t2 < -1.0 ? -1.0 : t2;

            ans.pitch = Math.Asin(t2);
            ans.roll = Math.Atan2(t3, t4);
            ans.yaw = Math.Atan2(t1, t0);

            return ans;
        }

        /// <summary>
        /// Rotates the specified quaternion around the X axis.
        /// </summary>
        /// <param name="quaternion">
        /// The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Pitch(this Vector4 quaternion)
        {
            double value1 = 2.0 *
            (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            double value2 = 1.0 - 2.0 *
            (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);

            double roll = Math.Atan2(value1, value2);

            return roll * (180.0 / Math.PI);
        }

        /// <summary>
        /// Rotates the specified quaternion around the Y axis.
        /// </summary>
        /// <param name="quaternion">
        /// The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Yaw(this Vector4 quaternion)
        {
            double value = 2.0 *
            (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            value = value > 1.0 ? 1.0 : value;
            value = value < -1.0 ? -1.0 : value;

            double pitch = Math.Asin(value);

            return pitch * (180.0 / Math.PI);
        }

        /// <summary>
        /// Rotates the specified quaternion around the Z axis.
        /// </summary>
        /// <param name="quaternion">
        /// The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Roll(this Vector4 quaternion)
        {
            double value1 = 2.0 *
            (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            double value2 = 1.0 - 2.0 *
            (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);

            double yaw = Math.Atan2(value1, value2);

            return yaw * (180.0 / Math.PI);
        }
    }
}

