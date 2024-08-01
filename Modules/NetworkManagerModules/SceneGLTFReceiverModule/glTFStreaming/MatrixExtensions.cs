using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace tracer
{
    public static class MatrixExtensions
    {
        public static Vector3 ExtractTranslation(this Matrix4x4 trsMatrix)
        {
            return trsMatrix.GetColumn(3);
        }

        public static Quaternion ExtractRotation(this Matrix4x4 trsMatrix)
        {
            // Extract the upper-left 3x3 submatrix
            Matrix4x4 rotationMatrix = new Matrix4x4(
                trsMatrix.GetColumn(0),
                trsMatrix.GetColumn(1),
                trsMatrix.GetColumn(2),
                Vector4.zero
            );

            // Convert the rotation matrix to a quaternion
            return Quaternion.LookRotation(rotationMatrix.GetColumn(2), rotationMatrix.GetColumn(1));
        }

        public static Vector3 ExtractScaling(this Matrix4x4 trsMatrix)
        {
            // Extract the upper-left 3x3 submatrix
            Matrix4x4 rotationMatrix = new Matrix4x4(
                trsMatrix.GetColumn(0),
                trsMatrix.GetColumn(1),
                trsMatrix.GetColumn(2),
                Vector4.zero
            );

            // Scaling factors are the lengths of the columns of the rotation matrix
            return new Vector3(
                rotationMatrix.GetColumn(0).magnitude,
                rotationMatrix.GetColumn(1).magnitude,
                rotationMatrix.GetColumn(2).magnitude
            );
        }
    }
}
