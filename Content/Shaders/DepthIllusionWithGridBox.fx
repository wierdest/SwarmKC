#if OPENGL
// MonoGame maps DirectX semantic names to OpenGL-compatible names when needed.
#define SV_POSITION POSITION
// Shader model for OpenGL path.
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
// Shader model for DirectX feature level 9_1 path.
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Input texture set from C# (we currently pass a 1x1 white pixel).
Texture2D TargetTexture;

// Sampler used to read TargetTexture.
sampler2D TargetTextureSampler = sampler_state
{
    Texture = <TargetTexture>;
};

// Default SpriteBatch vertex output that enters the pixel shader.
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;         // Screen-space position for this pixel.
    float4 Color : COLOR0;                 // Tint color passed by SpriteBatch.Draw.
    float2 TextureCoordinates : TEXCOORD0; // UV in [0,1].
};

// Time in seconds; used for forward movement and camera tilt variation.
extern float Time;
// User control for depth contrast.
extern float DepthStrength;   // 0..1
// User control for grid density.
extern float ParallaxScale;   // 2..8
// Fog color and fog strength.
extern float4 Fog;            // rgb=tint, a=strength
// Neon line color and extra intensity multiplier.
extern float4 LineColor;      // rgb=line color, a=extra intensity
// User control for random left/right camera roll.
extern float CameraTiltIntensity; // 0..1 suggested
// Screen size in pixels.
extern float2 ScreenSize;

// Builds line intensity from repeated UV space.
// p: tiled coordinates; width: core line size; feather: soft edge width.
float lineMask(float2 p, float width, float feather)
{
    // Center each cell at 0.5 and mirror around center.
    float2 g = abs(frac(p) - 0.5);
    // Distance to nearest horizontal/vertical grid axis.
    float d = min(g.x, g.y);
    // 1 at line center, smoothly fading to 0.
    return 1.0 - smoothstep(width, width + feather, d);
}

// 1D hash: deterministic pseudo-random from one float.
float hash11(float n)
{
    return frac(sin(n * 127.1) * 43758.5453123);
}

// 1D smooth value noise: interpolates between hashed integer samples.
float noise1(float x)
{
    // Integer cell coordinate.
    float i = floor(x);
    // Local coordinate inside the cell.
    float f = frac(x);
    // Smooth interpolation curve.
    f = f * f * (3.0 - 2.0 * f);
    // Interpolate two neighboring random values.
    return lerp(hash11(i), hash11(i + 1.0), f);
}

// Computes camera origin (ro) and ray direction (rd) for one pixel.
void buildCamera(float2 uv, out float3 ro, out float3 rd)
{
    // Constant forward movement along world Z.
    float zTravel = Time * 0.42;

    // Camera origin: centered in X, mid-height in Y, moving forward in Z.
    ro = float3(
        0.0,
        1.20,
        -2.2 + zTravel);

    // Camera target: same height, looking forward.
    float3 ta = float3(
        0.0,
        1.20,
        8.5 + zTravel);

    // Camera forward vector.
    float3 forward = normalize(ta - ro);
    // Camera right vector from world up and forward.
    float3 right = normalize(cross(float3(0.0, 1.0, 0.0), forward));
    // Camera up vector orthogonal to forward/right.
    float3 up = normalize(cross(forward, right));

    // Two low-frequency noise signals for smooth pseudo-random roll.
    float nA = noise1(Time * 0.22) * 2.0 - 1.0;
    float nB = noise1(Time * 0.47 + 19.37) * 2.0 - 1.0;
    // Final roll angle in radians, scaled by user intensity.
    float roll = (nA * 0.72 + nB * 0.28) * max(0.0, CameraTiltIntensity) * 0.20;

    // Roll rotation terms.
    float cr = cos(roll);
    float sr = sin(roll);
    // Rotate right/up around forward axis.
    float3 rightRolled = right * cr + up * sr;
    float3 upRolled = -right * sr + up * cr;

    // Horizontal field of view scalar.
    float fov = 1.00;
    // Build final ray direction from camera basis + screen uv.
    rd = normalize(forward + uv.x * rightRolled * fov + uv.y * upRolled * fov * 0.82);
}

// Main pixel shader.
float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Avoid zero-size viewport values.
    float2 res = max(ScreenSize, float2(1.0, 1.0));
    // Convert UV [0,1] to NDC-like [-1,1].
    float2 uv = input.TextureCoordinates * 2.0 - 1.0;
    // Aspect correction so geometry does not stretch.
    uv.x *= res.x / res.y;
    // MonoGame/SpriteBatch UV orientation fix (top-left origin).
    uv.y = -uv.y;

    // Build camera ray for this pixel.
    float3 ro, rd;
    buildCamera(uv, ro, rd);

    // Clamp fog and line colors to valid range.
    float3 fogColor = saturate(Fog.rgb);
    float3 lineColor = saturate(LineColor.rgb);
    // Keep intensity non-negative.
    float lineGain = max(0.0, LineColor.a);

    // Base background mix (dark + fog tint).
    float3 bg = lerp(float3(0.012, 0.006, 0.020), fogColor * 0.34, 0.45);
    // Accumulator for final color.
    float3 color = bg;

    // Box dimensions in world units.
    const float floorY = 0.0;
    const float ceilY = 2.4;
    const float wallX = 3.6;

    // Best (nearest) intersection tracking.
    float bestT = 1e20;
    float2 bestUV = float2(0.0, 0.0);
    float bestFace = -1.0; // 0=floor, 1=ceiling, 2=left wall, 3=right wall

    // Floor intersection (plane y = floorY).
    if (rd.y < -0.0001)
    {
        float tHit = (floorY - ro.y) / rd.y;
        if (tHit > 0.0)
        {
            float3 p = ro + rd * tHit;
            // Restrict floor to inside side walls.
            if (abs(p.x) <= wallX && tHit < bestT)
            {
                bestT = tHit;
                // Floor UV uses x/z.
                bestUV = p.xz;
                bestFace = 0.0;
            }
        }
    }

    // Ceiling intersection (plane y = ceilY).
    if (rd.y > 0.0001)
    {
        float tHit = (ceilY - ro.y) / rd.y;
        if (tHit > 0.0)
        {
            float3 p = ro + rd * tHit;
            // Restrict ceiling to inside side walls.
            if (abs(p.x) <= wallX && tHit < bestT)
            {
                bestT = tHit;
                // Ceiling UV uses x/z.
                bestUV = p.xz;
                bestFace = 1.0;
            }
        }
    }

    // Left wall intersection (plane x = -wallX).
    if (rd.x < -0.0001)
    {
        float tHit = (-wallX - ro.x) / rd.x;
        if (tHit > 0.0)
        {
            float3 p = ro + rd * tHit;
            // Restrict wall height to floor-ceiling range.
            if (p.y >= floorY && p.y <= ceilY && tHit < bestT)
            {
                bestT = tHit;
                // Wall UV uses z/y.
                bestUV = float2(p.z, p.y);
                bestFace = 2.0;
            }
        }
    }

    // Right wall intersection (plane x = +wallX).
    if (rd.x > 0.0001)
    {
        float tHit = (wallX - ro.x) / rd.x;
        if (tHit > 0.0)
        {
            float3 p = ro + rd * tHit;
            // Restrict wall height to floor-ceiling range.
            if (p.y >= floorY && p.y <= ceilY && tHit < bestT)
            {
                bestT = tHit;
                // Wall UV uses z/y.
                bestUV = float2(p.z, p.y);
                bestFace = 3.0;
            }
        }
    }

    // Shade the nearest box face if any face was hit.
    if (bestFace > -0.5)
    {
        // User-controlled density remapped to practical range.
        float density = saturate((ParallaxScale - 2.0) / 6.0);
        float gridScale = lerp(2.2, 7.0, density);
        // Tile coordinates for line generation.
        float2 gridUV = bestUV * gridScale;

        // On vertical walls, stretch Y to keep roughly square cells.
        if (bestFace > 1.5)
            gridUV.y *= 2.0;

        // Fine and coarse grid layers.
        float minor = lineMask(gridUV, 0.015, 0.010);
        float major = lineMask(gridUV / 5.0, 0.022, 0.012);

        // Extra local glow around line centers.
        float2 g = abs(frac(gridUV) - 0.5);
        float d = min(g.x, g.y);
        float glow = 1.0 - smoothstep(0.03, 0.28, d);

        // Depth response and distance attenuation.
        float depthBoost = lerp(0.70, 1.35, saturate(DepthStrength));
        float distFade = exp(-bestT * (0.052 + Fog.a * 0.08));

        // Face-specific brightness balancing.
        float faceMul = 1.0;
        if (bestFace > 1.5)
            faceMul = 0.90; // side walls
        if (bestFace > 0.5 && bestFace < 1.5)
            faceMul = 0.80; // ceiling

        // Final line intensity.
        float lineMix = minor * 0.72 + major * 1.24;
        float intensity = (lineMix * depthBoost + glow * 0.16) * distFade * lineGain * faceMul;

        // Add neon lines to background.
        color = bg + lineColor * intensity;

        // Fog blend by hit distance.
        float fogAmt = saturate(1.0 - exp(-bestT * (0.028 + Fog.a * 0.085)));
        color = lerp(color, fogColor * 0.42 + float3(0.012, 0.010, 0.020), fogAmt);
    }

    // Edge darkening to focus center.
    float vignette = saturate(1.0 - length(uv * float2(0.86, 1.04)));
    color *= lerp(0.55, 1.0, vignette);

    // Keep SpriteBatch tint in the pipeline and use its luminance as subtle gain.
    float4 source = tex2D(TargetTextureSampler, input.TextureCoordinates) * input.Color;
    float tintLum = dot(source.rgb, float3(0.299, 0.587, 0.114));
    color *= lerp(0.90, 1.05, tintLum);

    // Output clamped color with full alpha.
    return float4(saturate(color), 1.0);
}

// Technique name intentionally matches shader purpose.
technique DepthIllusionWithGridBox
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
