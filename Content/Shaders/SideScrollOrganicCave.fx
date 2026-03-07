#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This is very much like the implementation of the first organic depth shader,
// only thing is that the raymarching has a fixed setup, so we do not have forward movement illusion
// instead, we have an infinite side scrolling movement

// This texture is unused and is optimized out,
// leaving here just to keep the pattern of a shader
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

// Exposed properties
extern float Time;
extern float4 SurfaceColor;
extern float4 FogColor;
extern float4 BackgroundColor;
extern float CameraXMotionSpeed;
extern float2 ScreenSize;
extern float3 LightDirection;
extern float4 LightingStrength; // x=ambient y=diffuse z=specular w=rim
extern float2 LightingPower;    // x=specularPower, y=rimPower
extern float4 LightColor;       // rgb=color a=intensity
extern float2 WispScreenPos;    // screen-space pixels
extern float4 WispLightColor;   // rgb=color
extern float2 WispLightParams;  // x=radiusPx y=intensity
extern float3 PlayerAreaLight;            // x=screenX, y=screenY, z=radiusPx
extern float4 PlayerAreaLightColor;       // rgb=color a=intensity
extern float3 TargetAreaLight;            // x=screenX, y=screenY, z=radiusPx
extern float4 TargetAreaOpenLightColor;   // rgb=color a=intensity
extern float4 TargetAreaClosedLightColor; // rgb=color a=intensity
extern float TargetAreaOpenFactor;        // 0=closed, 1=open

static const float2 HASH21_SEED = float2(127.1, 311.7);
static const float HASH21_SCALE = 43758.5453123;
float hash21(float2 p)
{
    return frac(sin(dot(p, HASH21_SEED)) * HASH21_SCALE);
}

static const float NOISE_SMOOTH_A = 3.0;
static const float NOISE_SMOOTH_B = 2.0;
float noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (NOISE_SMOOTH_A - NOISE_SMOOTH_B * f);

    float a = hash21(i + float2(0.0, 0.0));
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

static const float FBM_BASE_AMPLITUDE = 0.5;
static const float FBM_FREQUENCY_MULTIPLIER = 2.02;
static const float FBM_AMPLITUDE_MULTIPLIER = 0.5;
float fbm(float2 p)
{
    float v = 0.0;
    float a = FBM_BASE_AMPLITUDE;
    v += a * noise2(p); p *= FBM_FREQUENCY_MULTIPLIER; a *= FBM_AMPLITUDE_MULTIPLIER;
    v += a * noise2(p);
    return v;
}

float tissueRelief(float2 p)
{
    float v = 0.0;
    float a = 0.6;
    float n0 = 1.0 - abs(noise2(p) * 2.0 - 1.0);
    p *= 2.1;
    v += n0 * n0 * a;
    a *= 0.55;
    float n1 = 1.0 - abs(noise2(p) * 2.0 - 1.0);
    v += n1 * n1 * a;
    return v;
}

float tissueHeight2D(float2 uv)
{
    float2 warp = float2(fbm(uv * 0.70), noise2(uv * 0.70 + float2(11.3, 7.11))) * 1.4;
    float2 tissueUv = uv + warp;

    float h = tissueRelief(tissueUv * 0.95) * 0.85;
    h += noise2(tissueUv * 2.0 + float2(13.7, -8.4)) * 0.30;

    float creasePhase = tissueUv.x * 3.7 + tissueUv.y * 2.9 + h * 5.5;
    float creases = pow(saturate(1.0 - abs(sin(creasePhase))), 9.0);
    h -= creases * 0.18;

    return h;
}

float tunnelLateralFlow(float z)
{
    float flow = 0.0;
    flow += sin(z * 0.11 + Time * 0.06) * 0.80;
    flow += sin(z * 0.037 - Time * 0.03) * 0.55;
    return flow;
}

float floorDisp(float2 xz)
{
    float flow = tunnelLateralFlow(xz.y);
    float2 floorUv = xz + float2(flow * 0.22, sin(flow * 0.5) * 0.10);

    float height = tissueRelief(floorUv * 0.42);
    height += noise2(floorUv * 1.4 + float2(7.1, 13.4)) * 0.22;
    return (height - 0.45) * 1.85;
}

float ceilDisp(float2 xz)
{
    float flow = tunnelLateralFlow(xz.y);
    float2 ceilingUv = xz + float2(flow * 0.16, cos(flow * 0.45) * 0.08);

    float height = tissueRelief(ceilingUv * 0.40 + float2(21.3, 5.4));
    height += noise2(ceilingUv * 1.3 + float2(2.7, 9.6)) * 0.20;
    return (height - 0.45) * 1.60;
}

float floorYAt(float3 p)
{
    return floorDisp(float2(p.x, p.z));
}

float ceilYAt(float3 p)
{
    return 2.4 + ceilDisp(float2(p.x, p.z));
}

// Floor + ceiling only (no side walls).
float corridorDist(float3 p)
{
    float dF = p.y - floorYAt(p);
    float dC = ceilYAt(p) - p.y;
    return min(dF, dC);
}

static const float NORMAL_EPSILON = 0.010;
float3 sceneNormal(float3 p)
{
    float c = corridorDist(p);
    float3 g = float3(
        corridorDist(p + float3(NORMAL_EPSILON, 0.0, 0.0)) - c,
        corridorDist(p + float3(0.0, NORMAL_EPSILON, 0.0)) - c,
        corridorDist(p + float3(0.0, 0.0, NORMAL_EPSILON)) - c);
    return normalize(g);
}

// Fixed ray setup
static const float3 FIXED_RAY_ORIGIN = float3(0.0, 1.20, -2.2);
static const float3 FIXED_FORWARD = normalize(float3(0.0, 0.0, 1.0));
static const float3 FIXED_RIGHT = normalize(float3(1.0, 0.0, 0.0));
static const float3 FIXED_UP = normalize(float3(0.0, 1.0, 0.0));

#define RM_STEPS 36
static const float RAY_MAX_DIST = 65.0;
static const float RAY_HIT_EPSILON = 0.014;
static const float RAY_STEP_MULTIPLIER = 0.68;

static const float RAY_STEP_MIN = 0.010;
static const float RAY_STEP_MAX = 0.60;

static const float2 MIN_SCREEN_SIZE = float2(1.0, 1.0);
static const float UV_TO_NDC_SCALE = 2.0;
static const float UV_TO_NDC_BIAS = 1.0;
static const float TRI_BLEND_EPSILON = 1e-5;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 resolution = max(ScreenSize, MIN_SCREEN_SIZE);
    float2 uv = input.TextureCoordinates * UV_TO_NDC_SCALE - UV_TO_NDC_BIAS;
    uv.x *= resolution.x / resolution.y;
    uv.y = -uv.y;

    float3 ro = FIXED_RAY_ORIGIN;
    float3 rd = normalize(FIXED_FORWARD + uv.x * FIXED_RIGHT * 1.00 + uv.y * FIXED_UP * 1.00 * 0.82);

    // This is the only movement: horizontal world scroll.
    float xScroll = Time * max(0.0, CameraXMotionSpeed);

    float3 fogColor = saturate(FogColor.rgb);
    float fogStrength = max(0.0, FogColor.a);

    float3 surfaceColor = saturate(SurfaceColor.rgb);
    float surfaceIntensity = max(0.0, SurfaceColor.a);

    float3 backgroundColor = saturate(BackgroundColor.rgb);
    float3 color = backgroundColor;

    float travel = 0.0;
    float hit = 0.0;
    float dist = 0.0;

    [loop]
    for (int i = 0; i < RM_STEPS; i++)
    {
        float3 p = ro + rd * travel;
        p.x += xScroll;

        dist = corridorDist(p);

        if (dist < RAY_HIT_EPSILON)
        {
            hit = 1.0;
            break;
        }

        travel += clamp(dist * RAY_STEP_MULTIPLIER, RAY_STEP_MIN, RAY_STEP_MAX);
        if (travel > RAY_MAX_DIST)
            break;
    }

    if (hit > 0.5)
    {
        float3 p = ro + rd * travel;
        p.x += xScroll;

        float3 normal = sceneNormal(p);

        float3 viewDir = normalize(ro - p);
        if (dot(normal, viewDir) < 0.0)
            normal = -normal;

        float3 triBlend = abs(normal);
        triBlend /= (triBlend.x + triBlend.y + triBlend.z + TRI_BLEND_EPSILON);

        float2 uvXY = p.xy * 2.6;
        float2 uvXZ = p.xz * 2.6;
        float hXY = tissueHeight2D(uvXY);
        float hXZ = tissueHeight2D(uvXZ);
        float height = hXY * triBlend.z + hXZ * (triBlend.x + triBlend.y);

        float3 lightDir = normalize(LightDirection);
        float3 lightColor = saturate(LightColor.rgb) * max(0.0, LightColor.a);

        float ambientStrength  = max(0.0, LightingStrength.x);
        float diffuseStrength  = max(0.0, LightingStrength.y);
        float specularStrength = max(0.0, LightingStrength.z);
        float rimStrength      = max(0.0, LightingStrength.w);

        float specPower = max(1.0, LightingPower.x);
        float rimPower  = max(1.0, LightingPower.y);

        float ndl = saturate(abs(dot(normal, lightDir)));
        float diffuse = ambientStrength + diffuseStrength * ndl;

        float3 reflected = reflect(-lightDir, normal);
        float specular = specularStrength * pow(saturate(dot(reflected, viewDir)), specPower);
        float rim = rimStrength * pow(1.0 - saturate(dot(normal, viewDir)), rimPower);

        float strata = abs(frac(height * 8.0) - 0.5);
        float contour = 1.0 - smoothstep(0.06, 0.22, strata);

        float shade = 0.15 + diffuse * 0.75;
        shade += specular * 0.06 + rim * 0.08;
        shade += height * 0.42;
        shade -= contour * 0.20;

        float distFade = exp(-travel * (0.042 + fogStrength * 0.08));
        shade *= distFade * surfaceIntensity;

        color = backgroundColor + (surfaceColor * lightColor) * shade;

        float fogAmount = saturate(1.0 - exp(-travel * (0.030 + fogStrength * 0.10)));
        color = lerp(color, fogColor * 0.52 + float3(0.016, 0.016, 0.018), fogAmount);
    }

    float vignette = saturate(1.0 - length(uv * float2(0.86, 1.04)));
    color *= lerp(0.55, 1.0, vignette);

    // Screen-space light halo that follows the player/wisp.
    float2 fragPx = input.TextureCoordinates * resolution;
    float2 toWisp = fragPx - WispScreenPos;
    float wispRadius = max(1.0, WispLightParams.x);
    float wispIntensity = max(0.0, WispLightParams.y);
    float falloff = exp(-dot(toWisp, toWisp) / (2.0 * wispRadius * wispRadius));
    float core = exp(-dot(toWisp, toWisp) / (2.0 * (wispRadius * 0.28) * (wispRadius * 0.28)));
    float wispMask = saturate(falloff * 0.9 + core * 0.65) * wispIntensity;
    color += saturate(WispLightColor.rgb) * wispMask;

    // Player area luminescence.
    float2 toPlayerArea = fragPx - PlayerAreaLight.xy;
    float paRadius = max(1.0, PlayerAreaLight.z);
    float paI = max(0.0, PlayerAreaLightColor.a);
    float paMask = exp(-dot(toPlayerArea, toPlayerArea) / (2.0 * paRadius * paRadius)) * paI;
    color += saturate(PlayerAreaLightColor.rgb) * paMask;

    // Target area luminescence (open/closed blend).
    float2 toTargetArea = fragPx - TargetAreaLight.xy;
    float taRadius = max(1.0, TargetAreaLight.z);
    float4 taColor = lerp(TargetAreaClosedLightColor, TargetAreaOpenLightColor, saturate(TargetAreaOpenFactor));
    float taI = max(0.0, taColor.a);
    float taMask = exp(-dot(toTargetArea, toTargetArea) / (2.0 * taRadius * taRadius)) * taI;
    color += saturate(taColor.rgb) * taMask;

    return float4(saturate(color), 1.0);
}

technique SideScrollOrganicCave
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
