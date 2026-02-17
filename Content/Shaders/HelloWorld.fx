#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Basic example pixel (fragment) shader for the game.
// Based on https://github.com/manbeardgames/monogame-hlsl-examples/blob/master/source/Example01ApplyShader/Content/BasicShader.fx

// It provides a basic rainbow / foil effect

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

float Time;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

// Our custom functions go here
// hue-shifted variations from a color, returns rgb
float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 baseColor = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

    float h = frac(Time * 0.12);
    float s = 1.0;
    float v = 1.0;

    float3 rainbow = hsv2rgb(float3(h, s, v));

    float mixAmount = 0.65;
    float3 outRgb = lerp(baseColor.rgb, rainbow, mixAmount);

    return float4(outRgb, baseColor.a);
}

technique RainbowColor
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
