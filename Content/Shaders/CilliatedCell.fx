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
extern float2 CellPosition;
extern float CellRadius;
extern float Rotation;
extern float4 BaseColor;
extern float4 NeonLight;

float hash21(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
}

float noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash21(i + float2(0.0, 0.0));
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 source = tex2D(TargetTextureSampler, uv) * input.Color;

    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float safeRadius = max(CellRadius, 1.0);
    float px = 1.0 / safeRadius;

    float phase = Time * 0.95 + dot(CellPosition, float2(0.017, 0.013));

    float cr = cos(-Rotation);
    float sr = sin(-Rotation);
    float2 pr = float2(
        cr * p.x - sr * p.y,
        sr * p.x + cr * p.y);

    float breathe = 1.0 + 0.11 * sin(Time * 1.8 + dot(CellPosition, float2(0.031, 0.023)));
    float2 ps = pr / breathe;

    float r = length(ps);
    float angle = atan2(ps.y, ps.x);

    float wobbleA = sin(angle * 3.0 + phase * 0.8) * 0.028;
    float wobbleB = sin(angle * 7.0 - phase * 1.3) * 0.014;
    float directionalLobe = max(0.0, cos(angle - 0.7)) * 0.045;
    float boundary = 0.76 + wobbleA + wobbleB + directionalLobe;

    float edgeSoft = max(0.012, 1.5 * px);
    float bodyMask = 1.0 - smoothstep(boundary, boundary + edgeSoft, r);

    float ciliaStrandA = 0.5 + 0.5 * sin(angle * 36.0 + phase * 1.8);
    float ciliaStrandB = 0.5 + 0.5 * sin(angle * 67.0 - phase * 1.2 + 0.9);
    float ciliaPattern = saturate(ciliaStrandA * 0.62 + ciliaStrandB * 0.38);
    ciliaPattern = pow(ciliaPattern, 2.4);

    float ciliaLength = 0.048 + 0.220 * ciliaPattern;
    float ciliaOuter = boundary + ciliaLength;
    float ciliaMask = smoothstep(boundary - edgeSoft * 0.35, boundary + edgeSoft * 0.10, r) *
                      (1.0 - smoothstep(ciliaOuter - edgeSoft * 0.20, ciliaOuter + edgeSoft, r));
    float visibleMask = max(bodyMask, ciliaMask);

    if (visibleMask <= 0.0001)
        return float4(0.0, 0.0, 0.0, 0.0);

    float rr = saturate(r / max(boundary, 1e-3));
    float z = sqrt(saturate(1.0 - rr * rr));
    float3 n = normalize(float3(ps / max(boundary, 1e-3), z));

    float lightOrbit = Time * 1.25 + dot(CellPosition, float2(0.021, 0.017));
    float3 lightDir = normalize(float3(cos(lightOrbit) * 0.55, sin(lightOrbit) * 0.55, 0.75));
    float3 viewDir = float3(0.0, 0.0, 1.0);
    float3 halfDir = normalize(lightDir + viewDir);

    float diff = 0.26 + 0.74 * saturate(dot(n, lightDir));
    float spec = pow(saturate(dot(n, halfDir)), 24.0);
    float rim = pow(1.0 - saturate(dot(n, viewDir)), 2.1);

    float3 baseRgb = saturate(BaseColor.rgb);
    float3 neonRgb = saturate(NeonLight.rgb);
    float neonIntensity = max(0.0, NeonLight.a);
    float detail = noise2(ps * 6.0 + phase * 0.45) - 0.5;

    float3 color = baseRgb * (0.42 + 0.58 * diff);
    color += detail * 0.10;
    color += baseRgb * rim * 0.14;
    color += spec * 0.28;

    float ringOuter = boundary;
    float ringInner = boundary - (2.6 * px + 0.018);
    float ring = smoothstep(ringInner, ringInner + edgeSoft, r) *
                 (1.0 - smoothstep(ringOuter - edgeSoft, ringOuter, r));
    color += float3(0.20, 0.23, 0.24) * ring;
    color += neonRgb * ring * neonIntensity * 0.06;

    float ciliaLight = ciliaMask * (0.45 + 0.55 * ciliaPattern);
    float3 ciliaTint = lerp(baseRgb * 0.85, neonRgb, 0.35);
    color = lerp(color, ciliaTint, ciliaLight * 0.55);
    color += neonRgb * ciliaLight * neonIntensity * 0.11;

    float alpha = saturate(max(bodyMask, ciliaMask * 0.90) * BaseColor.a * source.a);
    float3 outRgb = saturate(color * source.rgb) * alpha;

    return float4(outRgb, alpha);
}

technique CilliatedCell
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
