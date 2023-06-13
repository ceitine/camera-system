HEADER
{
	Description = "Camera Overlay";
}

MODES
{
	Default();
    VrForward();
}

COMMON
{
	#include "postprocess/shared.hlsl"
}

struct VS_INPUT
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
};

struct PS_INPUT
{
	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
	#endif

	#if ( ( PROGRAM == VFX_PROGRAM_PS ) )
		float4 vPositionSs : SV_ScreenPosition;
	#endif
};

VS
{
    PS_INPUT MainVs( VS_INPUT i )
    {
        PS_INPUT o;

        o.vPositionPs = float4( i.vPositionOs.xyz, 1.0f );

        return o;
    }
}

PS
{
    SamplerState Sampler < Filter( POINT ); >;

    #include "postprocess/common.hlsl"
	CreateTexture2D( _ColorTexture ) < Attribute( "ColorTexture" ); SrgbRead( true ); Filter( POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    float3 scanline( float2 coord, float3 screen )
    {
        const float scale = 0.2;
        const float amt = 0.004;
        const float spd = 1.0;
        
        return screen.rgb + sin( (coord.y / scale - (g_flTime * spd * 6.28)) ) * amt;
    }

    float2 curve( float2 uv ) 
    {
        const float curvature = 7;

        float2 crtUV = uv.xy * 2 - 1;
        float2 offset= crtUV.yx / curvature;

        crtUV += crtUV * offset * offset;
        crtUV = crtUV * 0.5 + 0.5;
        
        return crtUV;
    }

	float4 MainPs( PixelInput i ): SV_Target
	{
		float2 uv = curve( CalculateViewportUv( i.vPositionSs.xy ) ); 
        float4 col = Tex2D( _ColorTexture, uv.xy );

        float2 screenSpace = uv.xy * g_vViewportSize.xy;
        col.rgb = scanline( screenSpace, col.rgb );

		return col;
	}
}