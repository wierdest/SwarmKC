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
extern float SymbolRadius;      // world/pixel radius of projectile
extern float Rotation;          // radians
extern float4 SymbolColor;      // rgba (a = intensity)
extern float SymbolType;        // 0 = heart, 1 = star

void drawHeart(float2 symbolP, float px, out float fill, out float outline)
{
    float2 q = symbolP;
    q.y = q.y * 1.5 + 0.22;
    q.x *= 1.5;

    float h = q.x * q.x + q.y * q.y - 1.0;
    float heartField = h * h * h - q.x * q.x * q.y * q.y * q.y; // <= 0 is inside

    fill = 1.0 - smoothstep(-0.10, 0.08, heartField);
    outline = 1.0 - smoothstep(0.0, 0.06 + px * 0.5, abs(heartField));
}

void drawStar(float2 symbolP, float px, out float fill, out float outline)
{
    float angle = atan2(symbolP.y, symbolP.x);
    float radius = length(symbolP);

    float starWave = 0.5 + 0.5 * cos(angle * 5.0);
    float starRadius = lerp(0.40, 0.98, starWave);
    float starField = radius - starRadius;

    fill = 1.0 - smoothstep(-0.03, 0.04, starField);
    outline = 1.0 - smoothstep(0.0, 0.06 + px * 0.6, abs(starField));
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // gets the normalized texture coordinates for the current pixel
    float2 uv = input.TextureCoordinates;
    // samples the texture at the coordinates and multiplies by the vertex color
    float4 source = tex2D(TargetTextureSampler, uv) * input.Color;

    // local symbol coords in -1...1
    float2 p = uv * 2.0 - 1.0;
    // invert for monogame
    p.y = -p.y;
    float safeRadius = max(SymbolRadius, 1.0);
    float px = 1.0 / safeRadius;

    // rotation
    float cr = cos(-Rotation);
    float sr = sin(-Rotation);
    float2 pr = float2(
        cr * p.x - sr * p.y,
        sr * p.x + cr * p.y);
    
    float2 symbolP = pr / 0.90;

    float fill = 0.0;
    float outline = 0.0;

    if (SymbolType > 0.5)
        drawStar(symbolP, px, fill, outline);
    else
        drawHeart(symbolP, px, fill, outline);

    float3 symbolRgb = saturate(SymbolColor.rgb);
    float symbolIntensity = max(0.0, SymbolColor.a);

    // effects
    float3 tint = lerp(symbolRgb * 0.70, symbolRgb, 0.65);
    float3 glow = lerp(symbolRgb, float3(1.0, 1.0, 1.0), 0.20);
    float pulse = 0.5 + 0.5 * sin(Time * 3.1);
    float mix = 0.72 + 0.28 * saturate(symbolIntensity);
    float glowStrength = 0.35 + 0.65 * saturate(symbolIntensity);

    float3 color = float3(0.0, 0.0, 0.0);
    color = lerp(color, tint, fill * mix);
    color += glow * fill * (0.14 + 0.18 * pulse) * glowStrength;
    color += glow * outline * 0.22 * glowStrength;

    float alpha = saturate(max(fill, outline) * source.a);
    float3 outRgb = saturate(color * source.rgb) * alpha;

    return float4(outRgb, alpha);
}

technique Symbol
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
