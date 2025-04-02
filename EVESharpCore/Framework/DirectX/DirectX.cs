extern alias SC;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EVESharpCore.Cache;
using EVESharpCore.Framework.DirectX.Common;
using SC::SharedComponents.Py.D3DDetour;
using ServiceStack.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using BitmapInterpolationMode = SharpDX.Direct2D1.BitmapInterpolationMode;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device2 = SharpDX.DXGI.Device2;
using FillMode = SharpDX.Direct3D11.FillMode;
using InputElement = SharpDX.Direct3D11.InputElement;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace EVESharpCore.Framework.DirectX
{
    public class DirectX
    {
        public bool IsInitialized { get; set; }

        private readonly DirectEve _directEve;
        private SharpDX.Direct3D11.DeviceContext1 _d3dDeviceContext1;

        // pipeline objects
        private VertexShader _vertexShader;
        private InputLayout _vertexLayout;
        private PixelShader _pixelShader;
        private PixelShader _depthPixelShader;
        private SphereRenderer _sphereRenderer;
        private Buffer _perObjectBuffer;
        private Buffer _perFrameBuffer;
        private DepthStencilState _depthStencilState;
        private RasterizerState _rasterizerState;
        private StateBlock _sb;

        private int _width;
        private int _height;
        private static bool IS_DISABLED = true;


        public DirectX(DirectEve d)
        {
            _directEve = d;
            _sb = new StateBlock();
        }

        private void Log(string s)
        {
            _directEve.Log(s);
        }

        public void Initialize()
        {

            SwapChain swapChain = (SharpDX.DXGI.SwapChain)_directEve.SwapChainPtr;
            var device = swapChain.GetDevice<SharpDX.Direct3D11.Device>();
            var device1 = swapChain.GetDevice<SharpDX.Direct3D11.Device1>();
            
            _height = swapChain.Description.ModeDescription.Height;
            _width = swapChain.Description.ModeDescription.Width;
            Log($"DirectX init. Height {_height} Width {_width}");

            using (var vertexShaderBytecode = HLSLCompiler.CompileFromFile(@"Resources\EVESharpSource\EVESharp-master\EVESharpCore\Framework\DirectX\Shaders\VS.hlsl", "VSMain", "vs_5_0"))
            {
                _vertexShader = new VertexShader(device, vertexShaderBytecode);
                // Layout from VertexShader input signature
                _vertexLayout = new InputLayout(device,
                    vertexShaderBytecode.GetPart(ShaderBytecodePart.InputSignatureBlob),
                    new[]
                    {
                        // "SV_Position" = vertex coordinate in object space
                        new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0),
                        // "NORMAL" = the vertex normal
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        // "COLOR"
                        new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 24, 0),
                    });
            }

            // Compile and create the pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Resources\EVESharpSource\EVESharp-master\EVESharpCore\Framework\DirectX\Shaders\PS.hlsl", "PSMain", "ps_5_0"))
                _pixelShader = new PixelShader(device, bytecode);


            _sphereRenderer = new SphereRenderer();
            _sphereRenderer.CreateDeviceDependentResources(device1);

            // Create the constant buffer that will
            // store our worldViewProjection matrix
            _perObjectBuffer = new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<ConstantBuffers.PerObject>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Create the per frame constant buffer
            // lighting / camera position
            _perFrameBuffer = new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<ConstantBuffers.PerFrame>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var stencil = _d3dDeviceContext1.OutputMerger.DepthStencilState;
            Log($"{stencil.Description.IsDepthEnabled}");

            // Configure the depth buffer to discard pixels that are
            // further than the current pixel.
            _depthStencilState = new DepthStencilState(device,
                new DepthStencilStateDescription()
                {
                    IsDepthEnabled = false, // enable depth?
                    DepthComparison = Comparison.Less,
                    DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,
                    IsStencilEnabled = false,// enable stencil?
                    StencilReadMask = 0xff, // 0xff (no mask)
                    StencilWriteMask = 0xff,// 0xff (no mask)
                    // Configure FrontFace depth/stencil operations
                    FrontFace = new DepthStencilOperationDescription()
                    {
                        Comparison = Comparison.Always,
                        PassOperation = StencilOperation.Keep,
                        FailOperation = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Increment
                    },
                    // Configure BackFace depth/stencil operations
                    BackFace = new DepthStencilOperationDescription()
                    {
                        Comparison = Comparison.Always,
                        PassOperation = StencilOperation.Keep,
                        FailOperation = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Decrement
                    },
                });

            //IsInitialized = true;
            Log($"{ _d3dDeviceContext1.Rasterizer.State.Description.FillMode}");
            _rasterizerState = new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = false,
            });


            _d3dDeviceContext1.Rasterizer.State = _rasterizerState;

            // Tell the IA what the vertices will look like
            // in this case two 4-component 32bit floats
            // (32 bytes in total)
            _d3dDeviceContext1.InputAssembler.InputLayout = _vertexLayout;

            //var k = _d3dDeviceContext1.VertexShader.GetConstantBuffers(0, 100);

            // Set our constant buffer (to store worldViewProjection)
            _d3dDeviceContext1.VertexShader.SetConstantBuffer(0, _perObjectBuffer);
            _d3dDeviceContext1.VertexShader.SetConstantBuffer(1, _perFrameBuffer);

            // Set the vertex shader to run
            _d3dDeviceContext1.VertexShader.Set(_vertexShader);

            // Set our pixel constant buffers
            _d3dDeviceContext1.PixelShader.SetConstantBuffer(1, _perFrameBuffer);

            //// Set the pixel shader to run
            _d3dDeviceContext1.PixelShader.Set(_pixelShader);

            // Set our depth stencil state
            _d3dDeviceContext1.OutputMerger.DepthStencilState = _depthStencilState;

            IsInitialized = true;
        }

        public void Resize(object s, EventArgs e)
        {

            var args = (D3DResizeEventArgs)e;
            Log($"Resize called. Height [{args.Height}] Width [{args.Width}]");
            this._width = args.Width;
            this._height = args.Height;
            Dispose();
        }

        public void Dispose()
        {
            try
            {

                if (IS_DISABLED)
                    return;

                IsInitialized = false;
                _d3dDeviceContext1.Dispose();
                _vertexShader.Dispose();
                _vertexLayout.Dispose();
                _pixelShader.Dispose();
                _depthPixelShader.Dispose();
                _perObjectBuffer.Dispose();
                _perFrameBuffer.Dispose();
                _depthStencilState.Dispose();
                _rasterizerState.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void OnFrame()
        {

            if (IS_DISABLED)
                return;


            // Capture context state
            SwapChain swapChain = (SharpDX.DXGI.SwapChain)_directEve.SwapChainPtr;
            var device = swapChain.GetDevice<SharpDX.Direct3D11.Device>();
            _d3dDeviceContext1 = device.ImmediateContext.QueryInterface<SharpDX.Direct3D11.DeviceContext1>();
            _sb.Capture(_d3dDeviceContext1);

            if (!IsInitialized)
            {
                Initialize();
                return;
            }

            var viewMatrix = ESCache.Instance.DirectEve.SceneManager.ViewMatrix.ConvertToMaxtix();
            var perFrame = new ConstantBuffers.PerFrame();
            var camPosition = Matrix.Transpose(Matrix.Invert(viewMatrix)).Column4;
            var cameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

            // Initialize the world matrix
            var worldMatrix = Matrix.Transformation(Vector3.One, Quaternion.Zero, Vector3.One, Vector3.Zero, Quaternion.Zero, cameraPosition);
            
            //worldMatrix.TranslationVector = new Vector3((float)78.1602630615234, (float)-17.6212635040283, (float)-37.9685096740723);
            var perObject = new ConstantBuffers.PerObject();


            var projectionMatrix = ESCache.Instance.DirectEve.SceneManager.ProjectionMatrix.ConvertToMaxtix();

            perObject.view_proj = Matrix.Multiply(viewMatrix, projectionMatrix);
            perObject.center = camPosition;
            perObject.center.W = 1.0f;
            perObject.scale = Vector4.One;

            // Update the per frame constant buffer
            perFrame.CameraPosition = cameraPosition;
            _d3dDeviceContext1.UpdateSubresource(ref perFrame, _perFrameBuffer);

            _d3dDeviceContext1.UpdateSubresource(ref perObject, _perObjectBuffer);
            // SPHERE
            _sphereRenderer.DoRender(_d3dDeviceContext1);


            // Reset context state
            _sb.Apply(_d3dDeviceContext1);
        }
    }
}
