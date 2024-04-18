using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectCoordinateMapping;
using Microsoft.Kinect;
using System.Numerics;

namespace Kinect.Model
{
    public static class JointConversions
    {
        // Check NAO joint capabilties here: http://doc.aldebaran.com/2-8/family/nao_technical/joints_naov6.html#naov6-joints-left-arm-joints
        #region Angle Clamping Functions
        // All clamp functions expect radians, some need to know right or left
        // The shoulder pitch and elbow yaw functions are identical, BUT, in functional capability, the elbow yaws are inverted,
        // The right yaw rotates away from the body for the positive direction
        // The left yaw rotates towards the body for the positive direction
        public static double ClampShoulderPitch(double pitch)
        {
            // Range in Radians: -2.0857 to 2.0857
            // Range in Degrees: -119.5 to 119.5
            // We'll clamp a little short of that for general safety purposes, so
            // we clamp to 2.06 radians which is ~118 degrees
            if (pitch < -2.06) { return -2.06; }
            if (pitch > 2.06) { return 2.06; }
            return pitch;
        }

        public static double ClampShoulderRoll(double roll, bool isNAOLeft = true)
        {
            // Left arm
            // Range in Radians: -0.3142 to 1.3265
            // Range in Degrees: -18 to 76
            // Right arm
            // Range in Radians: -1.3265 to 0.3142
            // Range in Degrees: -76 to 18
            // We'll clamp a little short of that for general safety purposes, so we'll clamp to:
            //  1.31 radians which is ~75 degrees
            //  0.3 radians which is ~17 degrees
            if (isNAOLeft)
            {
                if (roll < -0.3) { return -0.3; }
                if (roll > 1.31) { return 1.31; }
                return roll;
            }
            if (roll < -1.31) { return -1.31; }
            if (roll > 0.3) { return 0.3; }
            return roll;
        }

        public static double ClampElbowRoll(double roll, bool isNAOLeft = true)
        {
            // Left arm
            // Range in Radians: -1.5446 to -0.0349
            // Range in Degrees: -88.5 to -2
            // Right arm
            // Range in Radians: 0.0349 to 1.5446
            // Range in Degrees: 2 to 88.5
            // We'll clamp a little short of that for general safety purposes, so we'll clamp to:
            //  1.53 radians which is ~87.5 degrees
            //  0.04 radians which is ~2.2 degrees
            if (isNAOLeft)
            {
                if (roll < -1.53) { return -1.53; }
                if (roll > -0.04) { return -0.04; }
                return roll;
            }
            if (roll < 0.04) { return 0.04; }
            if (roll > 1.53) { return 1.53; }
            return roll;
        }

        public static double ClampElbowYaw(double yaw)
        {
            // Range in Radians: -2.0857 to 2.0857
            // Range in Degrees: -119.5 to 119.5
            // We'll clamp a little short of that for general safety purposes, so
            // we clamp to 2.06 radians which is ~118 degrees
            if (yaw < -2.06) { return -2.06; }
            if (yaw > 2.06) { return 2.06; }
            return yaw;
        }
        #endregion

        // Currently the only used function is GetArmAngles
        // shoulder, elbow, wrist are x,y,z positions, isNAOLeft is a boolean that's basically, is the arm joints we provided
        // supposed to be on the Nao's left arm (for mirroring, person's right)
        public static List<JointAngle> GetArmAngles(Joint shoulder, Joint elbow, Joint wrist, bool isNAOLeft = true)
        {
            string side = isNAOLeft ? "L" : "R";
            // Get normalized 3d vector for the upper arm link by subtracting shoulder point from elbow point and normalizing the result
            Vector3 p_arm = elbow.GetNormDiffVector(shoulder);
            // Get normalized 3d vector for the lower arm link by subtracting elbow point from wrist point and normalizing the result
            Vector3 p_hand = wrist.GetNormDiffVector(elbow);

            #region Shoulder Pitch and Roll
            // Shoulder Pitch
            double shoulderPitch = 0.0;
            var diffZ = shoulder.Position.Z - elbow.Position.Z;
            var deltaZ = Math.Abs(diffZ);
            // if the shoulder is below the elbow
            if (shoulder.Position.Y < elbow.Position.Y)
            {
                // Y is opposite, Z is adjacent, and absolute em
                shoulderPitch = Math.Atan(Math.Abs(shoulder.Position.Y - elbow.Position.Y) / deltaZ);
                shoulderPitch = -shoulderPitch;
            }
            // if the shoulder is above the elbow
            else
            {
                // Z is opposite, Y is adjacent
                shoulderPitch = Math.Atan(diffZ / (shoulder.Position.Y - elbow.Position.Y));
                shoulderPitch = (Math.PI/2) - shoulderPitch;
            }
            shoulderPitch = ClampShoulderPitch(shoulderPitch);
            //Console.WriteLine(side + " Shoulder pitch clamped: " + (shoulderPitch * 180 / Math.PI).ToString());

            // Shoulder Roll
            // if our deltaZ is really small... make it slightly bigger?
            if (deltaZ < 0.1) { deltaZ = 0.2f; }
            // X is opposite, deltaZ is adjacent
            double shoulderRoll = Math.Atan((shoulder.Position.X - elbow.Position.X) / deltaZ);
            shoulderRoll = -shoulderRoll;
            shoulderRoll = ClampShoulderRoll(shoulderRoll, isNAOLeft);

            #endregion

            #region Elbow Roll and Yaw
            // Elbow Roll
            
            // get elbow roll and yaw using the dot product angle formula, becuase this gets the angle between the two vectors
            // This doesn't care about any axis outside of the two vectors, which is perfect for the elbow roll
            double elbowRoll = Math.Acos(Vector3.Dot(p_arm, p_hand) / (p_arm.Length() * p_hand.Length()));
            if (isNAOLeft) { elbowRoll = -elbowRoll; }
            elbowRoll = ClampElbowRoll(elbowRoll, isNAOLeft);

            // Elbow yaw

            // Get the shoulder rotation to tell us the original rotation of the shoulder joint itself,
            // which we need to determine the yaw, as it relies on the original shoulder rotation
            // in origin land
            // so, shoulder pitch from nao space is in kinect roll space (around x)
            // shoulder roll from nao space is in kinect pitch space (around y)
            Quaternion shoulderRotation = Quaternion.CreateFromYawPitchRoll(0, (float)shoulderRoll, (float)shoulderPitch);
            var x_new = Vector3.Transform(Vector3.UnitX, shoulderRotation);
             
            // Cross product the p_arm and p_hand vector to get the vector perpendicular to the two links
            // This gives us a plane for getting the yaw
            Vector3 z_new = Vector3.Cross(p_arm, p_hand);
            // Then use the dot product angle formula to get the angle between p_hand and the modified z_new
            // which should maybe be our yaw angle
            double elbowYaw = Math.Acos(Vector3.Dot(x_new, z_new) / (x_new.Length() * z_new.Length()));
            elbowYaw = elbowYaw - Math.PI / 2;
            if (!isNAOLeft) { elbowYaw = -elbowYaw; }
            //Console.WriteLine(side + " Elbow yaw unclamped: " + (elbowYaw * 180 / Math.PI).ToString());
            elbowYaw = ClampElbowYaw(elbowYaw);
            //Console.WriteLine(side + " Elbow yaw clamped: " + (elbowYaw * 180 / Math.PI).ToString());
            
            #endregion

            // Make and return our list
            List<JointAngle> jointAngles = new List<JointAngle>();
            jointAngles.Add(new JointAngle() { AngleName = side + "ShoulderPitch", Value = shoulderPitch });
            jointAngles.Add(new JointAngle() { AngleName = side + "ShoulderRoll", Value = shoulderRoll });
            jointAngles.Add(new JointAngle() { AngleName = side + "ElbowRoll", Value = elbowRoll });
            jointAngles.Add(new JointAngle() { AngleName = side + "ElbowYaw", Value = elbowYaw });

            return jointAngles.Where(jangle => jangle.Value.ToString() != "NaN").ToList();
        }
    }
}
