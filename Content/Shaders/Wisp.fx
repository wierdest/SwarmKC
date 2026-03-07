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

// Wisp controls
extern float Time;
extern float2 CellPosition;
extern float2 Velocity;
extern float Radius;
extern float Rotation;
extern float4 CoreColor;  // rgb=color, a=intensity
extern float4 RadianceColor; // rgb=color, a=intensity
extern float ParticleCount;
extern float ParticleSpinSpeed; // negative values = clockwise

static const int MAX_PARTICLES = 24;

float2 rotate2(float2 p, float a)
{
    float c = cos(a);
    float s = sin(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float hash21(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float fbm2(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    v += a * noise2(p); p *= 2.02; a *= 0.5;
    v += a * noise2(p); p *= 2.03; a *= 0.5;
    v += a * noise2(p);
    return v;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 source = tex2D(TargetTextureSampler, uv) * input.Color;
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;
    p = rotate2(p, -Rotation);

    float safeRadius = max(Radius, 1.0);
    float px = 1.0 / safeRadius;
    float speed = length(Velocity);
    float radiusPulse = 1.0 + 0.03 * sin(Time * 4.2 + dot(CellPosition, float2(0.03, 0.02)) + speed * 0.004);
    float2 pr = p / radiusPulse;
    float swirlA = fbm2(pr * 5.0 + float2(Time * 0.90, -Time * 0.65) + CellPosition * 0.01);
    float swirlB = fbm2(pr * 9.0 + float2(-Time * 1.35, Time * 0.70) + CellPosition * 0.02);
    float swirl = (swirlA - 0.5) * 0.10 + (swirlB - 0.5) * 0.05;
    float r = length(pr) + swirl;

    float core = 1.0 - smoothstep(0.20, 0.35 + px * 3.0, r);
    float glowNear = 1.0 - smoothstep(0.28, 0.64, r);
    float glowFar = 1.0 - smoothstep(0.52, 1.16, r);
    float flameEdge = saturate(1.0 - smoothstep(0.32, 0.78, r + swirl * 1.4));

    float shimmer = 0.92 + 0.08 * sin(Time * 9.0 + r * 18.0 + dot(CellPosition, float2(0.011, 0.017)));
    float3 coreRgb = saturate(CoreColor.rgb) * max(0.0, CoreColor.a);
    float3 radianceRgb = saturate(RadianceColor.rgb) * max(0.0, RadianceColor.a);

    float3 color = coreRgb * core * (1.18 + 0.28 * shimmer);
    color += radianceRgb * glowNear * 0.45 * shimmer;
    color += radianceRgb * glowFar * 0.20;
    color += radianceRgb * flameEdge * (0.10 + 0.18 * swirlA);

    // Particle spark layer around the orb.
    float particles = 0.0;
    [unroll]
    for (int i = 0; i < MAX_PARTICLES; i++)
    {
        float fi = (float)i;
        float active = 1.0 - step(ParticleCount, fi + 0.5);
        float seed = hash21(float2(fi, dot(CellPosition, float2(0.013, 0.017))));
        float ang = seed * 6.2831853 + Time * ParticleSpinSpeed * (0.8 + seed * 1.8);
        // Keep particles inside the quad so they never get clipped.
        float rad = 0.34 + seed * 0.46 + sin(Time * (1.27 + seed * 2.1) + fi) * 0.04;
        float2 c = float2(cos(ang), sin(ang)) * rad;
        float pradius = 0.010 + 0.025 * hash21(float2(fi, 77.1));
        float d = length(pr - c);
        particles += (1.0 - smoothstep(pradius, pradius + px * 2.2 + 0.010, d)) * active;
    }
    particles = saturate(particles);
    color += radianceRgb * particles * 0.60;

    float alpha = saturate(core * 0.95 + glowNear * 0.44 + glowFar * 0.22 + particles * 0.45);
    return float4(color * source.rgb, alpha * source.a);
}

technique Wisp
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
