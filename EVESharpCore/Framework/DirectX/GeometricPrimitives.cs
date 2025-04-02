using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics;
using Color = SharpDX.Color;

namespace EVESharpCore.Framework.DirectX
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;

        /// <summary>
        /// Create vertex with position (normal will be based on position and color will be white)
        /// </summary>
        /// <param name="position">Vertex position</param>
        public Vertex(Vector3 position)
            : this(position, Color.White)
        { }

        /// <summary>
        /// Create vertex with position and color (normal will be based on position)
        /// </summary>
        /// <param name="position">Vertex position</param>
        /// <param name="color">Vertex color</param>
        public Vertex(Vector3 position, Color color)
            : this(position, Vector3.Normalize(position), color)
        { }

        /// <summary>
        /// Create vertex with position from individual components (normal will be calculated and color will be white)
        /// </summary>
        /// <param name="pX">X</param>
        /// <param name="pY">Y</param>
        /// <param name="pZ">Z</param>
        public Vertex(float pX, float pY, float pZ)
            : this(new Vector3(pX, pY, pZ))
        { }

        /// <summary>
        /// Create vertex with position and color from individual components (normal will be calculated)
        /// </summary>
        /// <param name="pX">X</param>
        /// <param name="pY">Y</param>
        /// <param name="pZ">Z</param>
        /// <param name="color">color</param>
        public Vertex(float pX, float pY, float pZ, Color color)
            : this(new Vector3(pX, pY, pZ), color)
        { }

        /// <summary>
        /// Create vertex with position, normal and color from individual components
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <param name="pZ"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nZ"></param>
        /// <param name="color"></param>
        public Vertex(float pX, float pY, float pZ, float nX, float nY, float nZ, Color color)
            : this(new Vector3(pX, pY, pZ), new Vector3(nX, nY, nZ), color)
        { }

        /// <summary>
        /// Create vertex with position from individual components and normal and color
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <param name="pZ"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(float pX, float pY, float pZ, Vector3 normal, SharpDX.Color color)
            : this(new Vector3(pX, pY, pZ), normal, color)
        { }


        /// <summary>
        /// Create vertex with position, normal and color
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(Vector3 position, Vector3 normal, Color color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }
    public static class GeometricPrimitives
    {
        /// <summary>
        /// Creates a sphere primitive.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="diameter">The diameter.</param>
        /// <param name="tessellation">The tessellation.</param>
        /// <param name="toLeftHanded">if set to <c>true</c> vertices and indices will be transformed to left handed. Default is true.</param>
        /// <returns>A sphere primitive.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">tessellation;Must be >= 3</exception>
        public static void GenerateSphere(out Vertex[] vertices, out int[] indices, Color color, float radius = 0.5f,
            int tessellation = 16, bool clockWiseWinding = true)
        {
            if (tessellation < 3) throw new ArgumentOutOfRangeException("tessellation", "Must be >= 3");

            int verticalSegments = tessellation;
            int horizontalSegments = tessellation * 2;

            vertices = new Vertex[(verticalSegments + 1) * (horizontalSegments + 1)];
            indices = new int[(verticalSegments) * (horizontalSegments + 1) * 6];

            int vertexCount = 0;
            // Create rings of vertices at progressively higher latitudes.
            for (int i = 0; i <= verticalSegments; i++)
            {
                float v = 1.0f - (float)i / verticalSegments;

                var latitude = (float)((i * Math.PI / verticalSegments) - Math.PI / 2.0);
                var dy = (float)Math.Sin(latitude);
                var dxz = (float)Math.Cos(latitude);

                // Create a single ring of vertices at this latitude.
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    float u = (float)j / horizontalSegments;

                    var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                    var dx = (float)Math.Sin(longitude);
                    var dz = (float)Math.Cos(longitude);

                    dx *= dxz;
                    dz *= dxz;

                    var normal = new Vector3(dx, dy, dz);
                    var position = normal * radius;
                    // To generate a UV texture coordinate:
                    //var textureCoordinate = new Vector2(u, v);
                    // To generate a UVW texture cube coordinate
                    //var textureCoordinate = normal;

                    vertices[vertexCount++] = new Vertex(position, normal, color);
                }
            }

            // Fill the index buffer with triangles joining each pair of latitude rings.
            int stride = horizontalSegments + 1;

            int indexCount = 0;
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    int nextI = i + 1;
                    int nextJ = (j + 1) % stride;

                    indices[indexCount++] = (i * stride + j);
                    // Implement correct winding of vertices
                    if (clockWiseWinding)
                    {
                        indices[indexCount++] = (i * stride + nextJ);
                        indices[indexCount++] = (nextI * stride + j);
                    }
                    else
                    {
                        indices[indexCount++] = (nextI * stride + j);
                        indices[indexCount++] = (i * stride + nextJ);
                    }

                    indices[indexCount++] = (i * stride + nextJ);
                    // Implement correct winding of vertices
                    if (clockWiseWinding)
                    {
                        indices[indexCount++] = (nextI * stride + nextJ);
                        indices[indexCount++] = (nextI * stride + j);
                    }
                    else
                    {
                        indices[indexCount++] = (nextI * stride + j);
                        indices[indexCount++] = (nextI * stride + nextJ);
                    }
                }
            }
        }
    }
}