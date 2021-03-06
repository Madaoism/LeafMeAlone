﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using Assimp;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.X3DAudio;

namespace Shared
{
    public static class Utility
    {

        private static Random rnd;

        public static Vector3 ToVector3(this Vector3D vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector4 ScalarMultiply(this Vector4 me, Vector4 other)
        {
            Vector4 newVec = new Vector4();
            newVec.X = me.X * other.X;
            newVec.Y = me.Y * other.Y;
            newVec.Z = me.Z * other.Z;
            newVec.W = me.W * other.W;
            return newVec;
        }

        public static SlimDX.Quaternion ToQuaternion(this Assimp.Quaternion q)
        {
            SlimDX.Quaternion ret = new SlimDX.Quaternion();
            ret.X = q.X;
            ret.Y = q.Y;
            ret.Z = q.Z;
            ret.W = q.W;
            return ret;
        }

        public static Assimp.Quaternion ToQuaternion(this SlimDX.Quaternion q)
        {
            Assimp.Quaternion ret= new Assimp.Quaternion();
            ret.W = q.W;
            ret.X = q.X;
            ret.Y = q.Y;
            ret.Z = q.Z;
            return ret;
        }

        public static Matrix ToMatrix(this Matrix4x4 mat)
        {
            Matrix ret = new Matrix();

            ret.M11 = mat.A1;
            ret.M12 = mat.A2;
            ret.M13 = mat.A3;
            ret.M14 = mat.A4;

            ret.M21 = mat.B1;
            ret.M22 = mat.B2;
            ret.M23 = mat.B3;
            ret.M24 = mat.B4;

            ret.M31 = mat.C1;
            ret.M32 = mat.C2;
            ret.M33 = mat.C3;
            ret.M34 = mat.C4;
            
            ret.M41 = mat.D1;
            ret.M42 = mat.D2;
            ret.M43 = mat.D3;
            ret.M44 = mat.D4;

            ret = Matrix.Transpose(ret);
            return ret;
        }

        public static float ToRadians(this float degrees)
        {
            return degrees * ((float)Math.PI / 180.0f);
        }

        public static Vector2 IntersectionPoint(this Vector2 a, Vector2 b)
        {
            Vector2 c = b - a;
            float t = Vector2.Dot(b,c) / Vector2.Dot(a,b);
            return a + (a * t);
        }


        public static void Copy(this Vector4 dest, Vector4 src)
        {
            dest.X = src.X;
            dest.Y = src.Y;
            dest.Z = src.Z;
            dest.W = src.W;
        }

        public static void Copy(this Vector3 dest, Vector3 src)
        {
            dest.X = src.X;
            dest.Y = src.Y;
            dest.Z = src.Z;
        }

        public static void Copy(this Vector4 dest, Vector3 src)
        {
            dest.X = src.X;
            dest.Y = src.Y;
            dest.Z = src.Z;
            dest.W = 1;
        }

        public static void Copy(this Vector3 dest, Vector4 src)
        {
            dest.X = src.X;
            dest.Y = src.Y;
            dest.Z = src.Z;
        }

        public static Matrix Clone(this Matrix mat)
        {
            Matrix ret = new Matrix(  );
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {

                    ret[i, j] = mat[i, j];
                }
            }

            return ret;
        }

        public static float NextFloat(this Random r)
        {
            return (float) r.NextDouble();
        }

        public static float Range(this Random r, float max)
        {
            return r.NextFloat() * max;
        }


        public static Vector4 Mult(this Matrix m, Vector4 multBy)
        {
            return Vector4.Transform(multBy, m);
        }

        /// <summary>
        /// Create new texture.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static ShaderResourceView CreateTexture(string fileName, Device d)
        {
            return File.Exists(fileName) ? ShaderResourceView.FromFile(d, fileName) : null;
        }

        /// <summary>
        /// Get a random float in the specified range (inclusive).
        /// </summary>
        /// <param name="low">Low value</param>
        /// <param name="high">High value.</param>
        /// <returns>Random value between low and high.</returns>
        public static float RandomInRange(float low, float high)
        {

            // Get a random number.
            if (rnd == null)
            {
                rnd = new Random();
            }

            // Get the next float.
            float random = rnd.NextFloat();

            // Bound random float between low and high.
            random = (random * (high - low)) + low;

            // Return random number.
            return random;


        }

        /// <summary>
        /// Get the angle between two vectors (Using atan2).
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>Angle in degrees</returns>
        public static float AngleBetweenVectors(Vector3 a, Vector3 b)
        {

            // It's a 2D game!
            a.Y = 0.0f;
            b.Y = 0.0f;

            // Get the angle in radians.
            float angle = (float)( Math.Atan2(b.Z, b.X) - Math.Atan2(a.Z, a.X));

            // Convert to degrees.
            angle *= (180.0f / (float)Math.PI);

            // Return the angle.
            return angle;


        }
    }
}