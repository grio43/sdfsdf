using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace EVESharpCore.Framework.DirectX
{

    public class SphereRenderer
    {
        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;

        int totalVertexCount = 0;

        public void CreateDeviceDependentResources(SharpDX.Direct3D11.Device1 device)
        {

            Vertex[] vertices;
            int[] indices;
            GeometricPrimitives.GenerateSphere(out vertices, out indices, Color.Gray);

            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indices);
            totalVertexCount = indices.Length;
        }

        public void DoRender(SharpDX.Direct3D11.DeviceContext1 context)
        {
            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            // Pass in the quad vertices (note: only 4 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 36 vertices that make up the two triangles in the quad
            // using the vertex indices
            context.DrawIndexed(totalVertexCount, 0, 0);
            // Note: we have called DrawIndexed so that the index buffer will be used
        }
    }

}