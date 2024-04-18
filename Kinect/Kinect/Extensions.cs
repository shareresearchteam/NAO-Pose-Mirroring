using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Media3D;
using System.Numerics;

namespace Kinect
{
    static class Extensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<int> FindAllIndexes<T>(this List<T> source, T value)
        {
            //return source.Select((item, index) => new { Item = item, Index = index })
            //                .Where(v => v.Item.Equals(value))
            //                .Select(v => v.Index)
            //                .ToList();
            return source.Select((item, index) => item.Equals(value) ? index : -1)
                            .Where(i => i >= 0)
                            .ToList();
        }

        public static Vector3 GetJointVector(this Joint source)
        {
            return new Vector3(source.Position.X, source.Position.Y, source.Position.Z);
        }

        public static Vector3 GetNormDiffVector(this Joint source, Joint otherJoint)
        {
            return Vector3.Normalize(source.GetJointVector() - otherJoint.GetJointVector());
        }

        /// <summary>
        /// Rotates the specified quaternion around the X axis.
        /// </summary>
        /// <param name="quaternion">The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Pitch(this Microsoft.Kinect.Vector4 quaternion)
        {
            double value1 = 2.0 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            double value2 = 1.0 - 2.0 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
            double roll = Math.Atan2(value1, value2);
            return roll * (180.0 / Math.PI);
        }
        /// <summary>
        /// Rotates the specified quaternion around the Y axis.
        /// </summary>
        /// <param name="quaternion">The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Yaw(this Microsoft.Kinect.Vector4 quaternion)
        {
            double value = 2.0 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            value = value > 1.0 ? 1.0 : value;
            value = value < -1.0 ? -1.0 : value;
            double pitch = Math.Asin(value);
            return pitch * (180.0 / Math.PI);
        }
        /// <summary>
        /// Rotates the specified quaternion around the Z axis.
        /// </summary>
        /// <param name="quaternion">The orientation quaternion.</param>
        /// <returns>The rotation in degrees.</returns>
        public static double Roll(this Microsoft.Kinect.Vector4 quaternion)
        {
            double value1 = 2.0 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            double value2 = 1.0 - 2.0 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            double yaw = Math.Atan2(value1, value2);
            return yaw * (180.0 / Math.PI);
        }
    }
}
