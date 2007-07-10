// OpenRA test shader
// Author: C. Forbes
//--------------------------------------------------------

shared texture DiffuseTexture;
shared float2 Scroll;

shared float2 r1, r2;		// matrix elements

sampler s_DiffuseTexture = sampler_state {
	Texture = <DiffuseTexture>;
	MinFilter = None;
	MagFilter = None;
	MipFilter = None;
  
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VertexIn {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
};

struct VertexOut {
	float4 Position: POSITION;
	float2 Tex0: TEXCOORD0;
};

struct FragmentIn {
	float2 Tex0: TEXCOORD0;
};

VertexOut Simple_vp(VertexIn v) {
	VertexOut o;
	
	float2 p = (v.Position.xy - Scroll.xy) * r1 + r2;
	o.Position = float4(p.x,p.y,0,1);
	o.Tex0 = v.Tex0;
	return o;
}

float4 Simple_fp(FragmentIn f) : COLOR0 {
	float4 color = tex2D(s_DiffuseTexture, f.Tex0);
	return color;
}

technique low_quality {
	pass p0 {
		AlphaBlendEnable = false;
		ZWriteEnable = true;
		ZEnable = false;
		CullMode = None;
		VertexShader = compile vs_2_0 Simple_vp();
		PixelShader = compile ps_2_0 Simple_fp();
	}
}
