using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace WpfSharpDXApp
{
    class Cube : IDisposable, ID3D11App
    {
        [StructLayout(LayoutKind.Sequential)]
        struct SimpleVertex
        {
            public Vector3 Position;
            public Vector4 Color;

            public SimpleVertex(Vector3 position, Vector4 color)
            {
                Position = position;
                Color = color;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ConstantBuffer
        {
            public Matrix World;
            public Matrix View;
            public Matrix Projection;
        }

        private Camera camera = new Camera();
        private IntPtr inst;
        private DriverType driverType;
        private FeatureLevel featureLevel;
        private Device device;
        private DeviceContext immediateContext;
        private SwapChain swapChain;
        private RenderTargetView renderTargetView;
        private InputLayout vertexLayout;
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Buffer constantBuffer;

        private Matrix world;
        private Matrix view;
        private Matrix projection;
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private int width;
        private int height;

        public Cube()
        {
        }

        public void InitDevice()
        {
            var createDeviceFlag = DeviceCreationFlags.BgraSupport;
            var driverTypes = new[] {DriverType.Hardware, DriverType.Warp, DriverType.Reference};
            var featureLevels = new[] {FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0};
            foreach (var dt in driverTypes)
            {
                try
                {
                    device = new Device(dt, createDeviceFlag, featureLevels);
                    this.driverType = dt;
                    this.featureLevel = device.FeatureLevel;
                    this.immediateContext = device.ImmediateContext;
                }
                catch (Exception e)
                {
                    continue;
                }
                break;
            }

            LoadShaders();

            // Create vertex buffer
            var vertices = new[]
            {
                new SimpleVertex(new Vector3(-1.0f, 1.0f, -1.0f), new Vector4(0.0f, 0.0f, 1.0f, 0.5f)),
                new SimpleVertex(new Vector3(1.0f, 1.0f, -1.0f), new Vector4(0.0f, 1.0f, 0.0f, 0.5f)),
                new SimpleVertex(new Vector3(1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 0.5f)),
                new SimpleVertex(new Vector3(-1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 0.5f)),
                new SimpleVertex(new Vector3(-1.0f, -1.0f, -1.0f), new Vector4(1.0f, 0.0f, 1.0f, 0.5f)),
                new SimpleVertex(new Vector3(1.0f, -1.0f, -1.0f), new Vector4(1.0f, 1.0f, 0.0f, 0.5f)),
                new SimpleVertex(new Vector3(1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 0.5f)),
                new SimpleVertex(new Vector3(-1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.5f)),
            };
            var vbd = new BufferDescription()
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<SimpleVertex>()*8,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            };
            vertexBuffer = Buffer.Create(device, vertices, vbd);

            // Set vertex buffer
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                vertexBuffer, Utilities.SizeOf<SimpleVertex>(), 0));

            // Create index buffer
            var indices = new ushort[]
            {
                3, 1, 0,
                2, 1, 3,

                0, 5, 4,
                1, 5, 0,

                3, 4, 7,
                0, 4, 3,

                1, 6, 5,
                2, 6, 1,

                2, 7, 6,
                3, 7, 2,

                6, 4, 5,
                7, 4, 6,
            };
            var ibd = new BufferDescription()
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ushort>() * 36,
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            };
            indexBuffer = Buffer.Create(device, indices, ibd);

            // Set index buffer
            immediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);

            // Set primitive topology
            immediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Create the constant buffer
            var cbd = new BufferDescription()
            {
                Usage =  ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ConstantBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            };
            constantBuffer = new Buffer(device, cbd);

            // Initialize the world matrix
            world = Matrix.Identity;

            var eye = new Vector3(0, 1, -5);
            var at = new Vector3(0, 1, 0);
            var up = new Vector3(0, 1, 0);
            view = Matrix.LookAtLH(eye, at, up);
        }

        private Stopwatch stopwatch;

        /// <summary>
        /// Renders a frame
        /// </summary>
        public void Render(IntPtr resource, bool isNewSurface)
        {
            // If we've gotten a new Surface, need to initialize the renderTarget.
            // One of the times that this happens is on a resize.
            if (isNewSurface)
            {
                immediateContext.OutputMerger.ResetTargets();
                InitRenderTarget(resource);
            }

            // Update our time
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }
            var t = (float)stopwatch.Elapsed.TotalSeconds;

            // Animate the cube
            world = Matrix.RotationX(t)*Matrix.RotationY(t);

            // Clear the back buffer
            immediateContext.ClearRenderTargetView(renderTargetView, new Color4(0,0,0,0));

            // Update the view matrix
            camera.Update();

            var viewProjection = Matrix.Multiply(camera.View, projection);

            var cb = new ConstantBuffer()
            {
                World =  Matrix.Transpose(world),
                View = Matrix.Transpose(view),
                Projection = Matrix.Transpose(viewProjection)
            };
            immediateContext.UpdateSubresource(ref cb, constantBuffer);

            // Renders a triangle
            immediateContext.VertexShader.SetShader(vertexShader, null, 0);
            immediateContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            immediateContext.PixelShader.SetShader(pixelShader, null, 0);
            immediateContext.DrawIndexed(36, 0, 0);

            immediateContext?.Flush();
        }

        public Camera GetCamera()
        {
            return camera;
        }

        private void InitRenderTarget(IntPtr resource)
        {
            IntPtr dxgiRsource;
            var dxgiResourceTypeGuid = Marshal.GenerateGuidForType(typeof(SharpDX.DXGI.Resource));
            Marshal.QueryInterface(resource, ref dxgiResourceTypeGuid, out dxgiRsource);
            var dxgiObject = new SharpDX.DXGI.Resource(dxgiRsource);
            IntPtr sharedHandle = dxgiObject.SharedHandle;
            var outputResource = device.OpenSharedResource<SharpDX.Direct3D11.Texture2D>(sharedHandle);
            dxgiObject.Dispose();
            var rtDesc = new RenderTargetViewDescription();
            rtDesc.Format = Format.B8G8R8A8_UNorm;
            rtDesc.Dimension = RenderTargetViewDimension.Texture2D;
            rtDesc.Texture2D.MipSlice = 0;

            renderTargetView = new RenderTargetView(device, outputResource, rtDesc);

            var outputResourceDesc = outputResource.Description;
            if (outputResourceDesc.Width != width || outputResourceDesc.Height != height)
            {
                width = outputResourceDesc.Width;
                height = outputResourceDesc.Height;
                SetUpViewport();
            }
            immediateContext.OutputMerger.SetRenderTargets(null, renderTargetView);
            if (outputResource != null)
            {
                outputResource.Dispose();
            }
        }

        private void SetUpViewport()
        {
            var vp = new Viewport(0, 0, width, height, 0f, 1f);
            immediateContext.Rasterizer.SetViewport(vp);
            projection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, width/(float) height, 0.01f, 100f);
        }

        void LoadShaders()
        {
            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("D3DVisualization.fx", "VS", "vs_4_0");
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("D3DVisualization.fx", "PS", "ps_4_0"))
            {
                pixelShader = new PixelShader(device, pixelShaderByteCode);
            }

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            vertexLayout = new InputLayout(device, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
            });

            immediateContext.InputAssembler.InputLayout = vertexLayout;
        }

        public void Dispose()
        {
            immediateContext?.ClearState();
            Utilities.Dispose(ref indexBuffer);
            Utilities.Dispose(ref pixelShader);
            Utilities.Dispose(ref vertexBuffer);
            Utilities.Dispose(ref vertexLayout);
            Utilities.Dispose(ref vertexShader);
            Utilities.Dispose(ref immediateContext);
            Utilities.Dispose(ref device);
        }
    }
}
