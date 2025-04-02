using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using D3D2 = SharpDX.Direct3D;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace EVESharpCore.Framework.DirectX
{
    class StateBlock
    {
        private D3D.VertexShader _vs;
        private D3D.ClassInstance[] _vs_cis = new D3D.ClassInstance[16];
        private D3D.ShaderResourceView[] _vs_srvs = new D3D.ShaderResourceView[16];
        private D3D.Buffer[] _vs_cbuf = new D3D.Buffer[16];
        private D3D.SamplerState[] _vs_sss = new D3D.SamplerState[16];

        private D3D.PixelShader _ps;
        private D3D.ClassInstance[] _ps_cis = new D3D.ClassInstance[16];
        private D3D.ShaderResourceView[] _ps_srvs = new D3D.ShaderResourceView[16];
        private D3D.Buffer[] _ps_cbuf = new D3D.Buffer[16];
        private D3D.SamplerState[] _ps_sss = new D3D.SamplerState[16];

        private D3D.GeometryShader _gs;
        private D3D.ClassInstance[] _gs_cis = new D3D.ClassInstance[16];
        private D3D.ShaderResourceView[] _gs_srvs = new D3D.ShaderResourceView[16];
        private D3D.Buffer[] _gs_cbuf = new D3D.Buffer[16];
        private D3D.SamplerState[] _gs_sss = new D3D.SamplerState[16];

        private D3D.DepthStencilState _dss;
        private D3D.BlendState _bs;
        private D3D.RasterizerState _rs;
        private D3D.SamplerState _ss;

        private D3D2.PrimitiveTopology _prim_top;
        private int[] _vbs_offs = new int[16];
        private int[] _vbs_strides = new int[16];
        private D3D.Buffer[] _vbs = new D3D.Buffer[16];
        private int _ibs_offs;
        private DXGI.Format _ibs_fmt;
        private D3D.Buffer _ibs;
        private D3D.InputLayout _il;

        public void Capture(D3D.DeviceContext context)
        {
            _il = context.InputAssembler.InputLayout;
            _prim_top = context.InputAssembler.PrimitiveTopology;
            context.InputAssembler.GetVertexBuffers(0, 16, _vbs, _vbs_strides, _vbs_offs);
            context.InputAssembler.GetIndexBuffer(out _ibs, out _ibs_fmt, out _ibs_offs);

            _vs = context.VertexShader.Get(_vs_cis);
            context.VertexShader.GetShaderResources(0, 16).CopyTo(_vs_srvs, 0);
            _vs_sss = context.VertexShader.GetSamplers(0, 16);
            context.VertexShader.GetConstantBuffers(0, 16).CopyTo(_vs_cbuf, 0);

            _gs = context.GeometryShader.Get(_gs_cis);
            context.GeometryShader.GetShaderResources(0, 16).CopyTo(_gs_srvs, 0);
            context.GeometryShader.GetSamplers(0, 16).CopyTo(_gs_sss, 0);
            context.GeometryShader.GetConstantBuffers(0, 16).CopyTo(_gs_cbuf, 0);

            _ps = context.PixelShader.Get(_ps_cis);
            context.PixelShader.GetShaderResources(0, 16).CopyTo(_ps_srvs, 0);
            context.PixelShader.GetSamplers(0, 16).CopyTo(_ps_sss, 0);
            context.PixelShader.GetConstantBuffers(0, 16).CopyTo(_ps_cbuf, 0);

            _dss = context.OutputMerger.DepthStencilState;
            _bs = context.OutputMerger.BlendState;
            _rs = context.Rasterizer.State;
        }

        public void Apply(D3D.DeviceContext context)
        {
            context.InputAssembler.InputLayout = _il;
            context.InputAssembler.PrimitiveTopology = _prim_top;
            context.InputAssembler.SetVertexBuffers(0, _vbs, _vbs_strides, _vbs_offs);
            context.InputAssembler.SetIndexBuffer(_ibs, _ibs_fmt, _ibs_offs);


            context.VertexShader.Set(_vs);
            context.VertexShader.SetShaderResources(0, 16, _vs_srvs);
            context.VertexShader.SetSamplers(0, 16, _vs_sss);
            context.VertexShader.SetConstantBuffers(0, 16, _vs_cbuf);


            context.GeometryShader.Set(_gs);
            context.GeometryShader.SetShaderResources(0, 16, _gs_srvs);
            context.GeometryShader.SetSamplers(0, 16, _gs_sss);
            context.GeometryShader.SetConstantBuffers(0, 16, _gs_cbuf);

            context.PixelShader.Set(_ps);
            context.PixelShader.SetShaderResources(0, 16, _ps_srvs);
            context.PixelShader.SetSamplers(0, 16, _ps_sss);
            context.PixelShader.SetConstantBuffers(0, 16, _ps_cbuf);

            context.OutputMerger.DepthStencilState = _dss;
            context.OutputMerger.BlendState = _bs;
            context.Rasterizer.State = _rs;
        }
    }
}
