using System.Runtime.InteropServices;
using SharpDX;

namespace EVESharpCore.Framework.DirectX.Common
{
    public static class ConstantBuffers
    {
        /// <summary>
        /// Per Object constant buffer (matrices)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PerObject
        {
            // WorldViewProjection matrix
            public Matrix view_proj;

            public Vector4 center;
            public Vector4 scale;

            /// <summary>
            /// Transpose the matrices so that they are in row major order for HLSL
            /// </summary>
            internal void Transpose()
            {

            }
        }

        /// <summary>
        /// Per frame constant buffer (camera position)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerFrame
        {
            public SharpDX.Vector3 CameraPosition;
            float _padding0;
        }
    }
}