#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D TargetTexture;

sampler2D TargetTextureSampler = sampler_state
{
    Texture = <TargetTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

extern float Time;
extern float ParticleRadius;
extern float Rotation;
extern float4 ParticleColor; // rgb=color, a=intensity

float2 rotate2(float2 p, float a)
{
    float c = cos(a);
    float s = sin(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 source = tex2D(TargetTextureSampler, uv) * input.Color;

    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;
    p = rotate2(p, -Rotation);

    float safeRadius = max(ParticleRadius, 1.0);
    float px = 1.0 / safeRadius;

    float pulse = 1.0 + 0.08 * sin(Time * 8.5);
    float2 pr = p / pulse;
    float r = length(pr);

    float core = 1.0 - smoothstep(0.18, 0.30 + px * 2.0, r);
    float glowNear = 1.0 - smoothstep(0.28, 0.55, r);
    float glowFar = 1.0 - smoothstep(0.50, 0.95, r);

    float spark = 0.5 + 0.5 * sin(Time * 14.0 + atan2(pr.y, pr.x) * 5.0);
    float halo = glowNear * (0.9 + 0.1 * spark) + glowFar * 0.55;

    float3 baseRgb = saturate(ParticleColor.rgb) * max(0.0, ParticleColor.a);
    float3 color = baseRgb * core * 1.2;
    color += baseRgb * halo * 0.75;

    float alpha = saturate(core * 0.95 + halo * 0.55);
    return float4(saturate(color) * source.rgb, alpha * source.a);
}

technique LightParticle
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
