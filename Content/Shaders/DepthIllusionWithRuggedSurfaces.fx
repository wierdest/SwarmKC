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

// Exposed properties
extern float Time;
extern float4 SurfaceColor;
extern float4 FogColor;
extern float4 BackgroundColor;
extern float CameraTiltIntensity;
extern float CameraZMotionSpeed;
extern float2 ScreenSize;

// HASH21 = takes a 2D float returns a pseudo random 1D float.
static const float2 HASH21_SEED = float2(127.1, 311.7);
static const float HASH21_SCALE = 43758.5453123;
float hash21(float2 p)
{
    return frac(sin(dot(p, HASH21_SEED)) * HASH21_SCALE);
}

// 2D smooth noise: 
// splits position into cell coord (i) and local coord inside cell (f)
// smoothes f with cubic curve, so interpolation has soft transitions
// hash the corners to get pseudo random values using above method
// bilinearly interpolates those 4 values
static const float NOISE_SMOOTH_A = 3.0;
static const float NOISE_SMOOTH_B = 2.0;
static const float2 NOISE_CORNER_00 = float2(0.0, 0.0);
static const float2 NOISE_CORNER_10 = float2(1.0, 0.0);
static const float2 NOISE_CORNER_01 = float2(0.0, 1.0);
static const float2 NOISE_CORNER_11 = float2(1.0, 1.0);
float noise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (NOISE_SMOOTH_A - NOISE_SMOOTH_B * f);

    float a = hash21(i + NOISE_CORNER_00);
    float b = hash21(i + NOISE_CORNER_10);
    float c = hash21(i + NOISE_CORNER_01);
    float d = hash21(i + NOISE_CORNER_11);

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Minimal 2 octave fractal brownian motion (for reference https://thebookofshaders.com/13/)
// stacks two noise layers (octaves):
// -> each octave doubles-ish frequency (finer detail)
// -> each octave halves amplitude (a) (less contribution)
// results in a richer natural-looking variation (v)
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

// Tissue-like relief (v) using two ridged-noise octaves
// folds noise around the midpoint (0.5 to create crest/valley structures)
// squares it to sharpen the folds, then blends octaves with lower amplitudes
static const float TISSUE_RELIEF_BASE_AMPLITUDE = 0.6;
static const float TISSUE_RELIEF_FREQUENCY_MULTIPLIER = 2.1;
static const float TISSUE_RELIEF_AMPLITUDE_MULTIPLIER = 0.55;
static const float TISSUE_RELIEF_NOISE_SCALE = 2.0;
static const float TISSUE_RELIEF_NOISE_CENTER = 1.0;
float tissueRelief(float2 p)
{
    float v = 0.0;
    float a = TISSUE_RELIEF_BASE_AMPLITUDE;
    float n0 = TISSUE_RELIEF_NOISE_CENTER - abs(noise2(p) * TISSUE_RELIEF_NOISE_SCALE - TISSUE_RELIEF_NOISE_CENTER); 
    p *= TISSUE_RELIEF_FREQUENCY_MULTIPLIER; 
    v += n0 * n0 * a; 
    a *= TISSUE_RELIEF_AMPLITUDE_MULTIPLIER;
    float n1 = 1.0 - abs(noise2(p) * 2.0 - 1.0);
    v += n1 * n1 * a;

    return v;
}
// Builds 2D tissue-like height field
// warps UVs so it looks organic, adds main fold relief + finer detail layer
// carves thin crease lines to accentuate definition
static const float TISSUE_UV_WARP_SCALE = 0.70;
static const float2 TISSUE_UV_WARP_OFFSET = float2(11.3, 7.11);
static const float TISSUE_UV_WARP_STRENGTH = 1.4;

static const float TISSUE_MAIN_RELIEF_SCALE = 0.95;
static const float TISSUE_MAIN_RELIEF_MULTIPLIER = 0.85;

static const float TISSUE_DETAIL_SCALE = 2.0;
static const float2 TISSUE_DETAIL_OFFSET = float2(13.7, -8.4);
static const float TISSUE_DETAIL_MULTIPLIER = 0.30;

static const float2 TISSUE_CREASE_FREQUENCY = float2(3.7, 2.9);
static const float TISSUE_CREASE_HEIGHT_MULTIPLIER = 5.5;
static const float TISSUE_CREASE_SHARPNESS = 9.0;
static const float TISSUE_CREASE_MULTIPLIER = 0.18;
float tissueHeight2D(float2 uv)
{
    float2 warp = float2(
        fbm(uv * TISSUE_UV_WARP_SCALE), 
        noise2(uv * TISSUE_UV_WARP_SCALE + TISSUE_UV_WARP_OFFSET)
    ) * TISSUE_UV_WARP_STRENGTH;

    float2 tissueUv = uv + warp;

    float h = tissueRelief(tissueUv * TISSUE_MAIN_RELIEF_SCALE) * TISSUE_MAIN_RELIEF_MULTIPLIER;
    h += noise2(tissueUv * TISSUE_DETAIL_SCALE + TISSUE_DETAIL_OFFSET) * TISSUE_DETAIL_MULTIPLIER;
    
    float creasePhase = tissueUv.x * TISSUE_CREASE_FREQUENCY.x + tissueUv.y * TISSUE_CREASE_FREQUENCY.y + h * TISSUE_CREASE_HEIGHT_MULTIPLIER;
    float creases = pow(saturate(1.0 - abs(sin(creasePhase))), TISSUE_CREASE_SHARPNESS);
    h -= creases * TISSUE_CREASE_MULTIPLIER;

    return h;
}

// 1D smooth noise:
// - split xinto integer cell (i) and local offset (f)
// - smooth with cubic curve for sfot interpolation
// - hash two neighboring samples and lerp between them
static const float NOISE1_SMOOTH_A = 3.0;
static const float NOISE1_SMOOTH_B = 2.0;
static const float NOISE1_HASH_SEED_Y = 19.37;
static const float NOISE1_NEXT_SAMPLE_OFFSET = 1.0;
float noise1(float x)
{
    float i = floor(x);
    float f = frac(x);
    f = f * f * (NOISE1_SMOOTH_A - NOISE1_SMOOTH_B * f);
    float a = hash21(float2(i, NOISE1_HASH_SEED_Y));
    float b = hash21(float2(i + NOISE1_NEXT_SAMPLE_OFFSET, NOISE1_HASH_SEED_Y));
    return lerp(a, b, f);
}

// Lateral tunnel drift over depth (z axis)
// combines two sine waves with different spatial frequencies and time speeds,
// avoiding repetitive motion and keep the corridor organically offset
static const float LATERAL_OFFSET_FREQUENCY_1 = 0.11;
static const float LATERAL_OFFSET_TIME_SPEED_1 = 0.06;
static const float LATERAL_OFFSET_AMPLITUDE_1 = 0.80;

static const float LATERAL_OFFSET_FREQUENCY_2 = 0.037;
static const float LATERAL_OFFSET_TIME_SPEED_2 = 0.03;
static const float LATERAL_OFFSET_AMPLITUDE_2 = 0.55;
float tunnelLateralOffset(float z)
{
    float offset = 0.0;
    offset += sin(z * LATERAL_OFFSET_FREQUENCY_1 + Time * LATERAL_OFFSET_TIME_SPEED_1) * LATERAL_OFFSET_AMPLITUDE_1;
    offset += sin(z * LATERAL_OFFSET_FREQUENCY_2 - Time * LATERAL_OFFSET_TIME_SPEED_2) * LATERAL_OFFSET_AMPLITUDE_2;
    return offset;
}

// Floor displacement height:
// uses tunnel lateral flow to gently advect (liquid flow-like transfer) sampling coordinates
// combines coarse tissue relief and finer noise detail
// remaps result around a midpoint to control signed height
static const float FLOOR_FLOW_X_MULTIPLIER = 0.22;
static const float FLOOR_FLOW_Y_MULTIPLIER = 0.10;
static const float FLOOR_FLOW_Z_WAVE_FREQUENCY = 0.5;

static const float FLOOR_BASE_RELIEF_SCALE = 0.42;
static const float FLOOR_DETAIL_SCALE = 1.4;
static const float2 FLOOR_DETAIL_OFFSET = float2(7.1, 13.4);
static const float FLOOR_DETAIL_MULTIPLIER = 0.22;

static const float FLOOR_HEIGHT_MIDPOINT = 0.45;
static const float FLOOR_HEIGHT_AMPLITUDE = 1.85;

float floorDisp(float2 xz)
{
    float flow = tunnelLateralOffset(xz.y);
    float2 floorUv = xz + float2(flow * FLOOR_FLOW_X_MULTIPLIER, sin(flow * FLOOR_FLOW_Z_WAVE_FREQUENCY) * FLOOR_FLOW_Y_MULTIPLIER);

    float height = tissueRelief(floorUv * FLOOR_BASE_RELIEF_SCALE);
    height += noise2(floorUv * FLOOR_DETAIL_SCALE + FLOOR_DETAIL_OFFSET) * FLOOR_DETAIL_MULTIPLIER;
    return (height - FLOOR_HEIGHT_MIDPOINT) * FLOOR_HEIGHT_AMPLITUDE;
}

// Ceiling displacement height 
// (same as above, but for the ceiling)
// only one difference: there is an extra base offset on the tissueRelief
static const float CEILING_FLOW_X_MULTIPLIER = 0.16;
static const float CEILING_FLOW_Y_MULTIPLIER = 0.08;
static const float CEILING_FLOW_Z_WAVE_FREQUENCY = 0.45;

static const float CEILING_BASE_RELIEF_SCALE = 0.40;
static const float2 CEILING_RELIEF_OFFSET = float2(21.3, 5.4);

static const float CEILING_DETAIL_SCALE = 1.3;
static const float2 CEILING_DETAIL_OFFSET = float2(2.7, 9.6);
static const float CEILING_DETAIL_MULTIPLIER = 0.20;

static const float CEILING_HEIGHT_MIDPOINT = 0.45;
static const float CEILING_HEIGHT_AMPLITUDE = 1.60;
float ceilDisp(float2 xz)
{
    float flow = tunnelLateralOffset(xz.y);
    float2 ceilingUv = xz + float2(flow * CEILING_FLOW_X_MULTIPLIER, cos(flow * CEILING_FLOW_Z_WAVE_FREQUENCY) * CEILING_FLOW_Y_MULTIPLIER);

    float height = tissueRelief(ceilingUv * CEILING_BASE_RELIEF_SCALE + CEILING_RELIEF_OFFSET);
    height += noise2(ceilingUv * CEILING_DETAIL_SCALE + CEILING_DETAIL_OFFSET) * CEILING_DETAIL_MULTIPLIER;
    return (height - CEILING_HEIGHT_MIDPOINT) * CEILING_HEIGHT_AMPLITUDE;
}


float wallDisp(float2 zy, float seed)
{
    float flow = tunnelLateralOffset(zy.x);
    float2 q = zy + float2(flow * 0.18, sin(flow * 0.55) * 0.10);

    float n = tissueRelief(q * 0.44 + float2(seed, seed * 1.7));
    n += noise2(q * 1.3 + float2(seed * 1.7, seed * 0.8)) * 0.20;
    return (n - 0.45) * 2.05;
}

float leftWallX(float3 p)
{
    float cx = tunnelLateralOffset(p.z);
    return cx - 3.6 - wallDisp(float2(p.z, p.y), 3.1);
}

float rightWallX(float3 p)
{
    float cx = tunnelLateralOffset(p.z);
    return cx + 3.6 + wallDisp(float2(p.z, p.y), 7.9);
}

float floorYAt(float3 p)
{
    return floorDisp(float2(p.x, p.z));
}

float ceilYAt(float3 p)
{
    return 2.4 + ceilDisp(float2(p.x, p.z));
}

float corridorDist(float3 p)
{
    float dL = p.x - leftWallX(p);
    float dR = rightWallX(p) - p.x;
    float dF = p.y - floorYAt(p);
    float dC = ceilYAt(p) - p.y;
    return min(min(dL, dR), min(dF, dC));
}

float3 sceneNormal(float3 p)
{
    float e = 0.010;
    float c = corridorDist(p);
    float3 n = float3(
        corridorDist(p + float3(e, 0.0, 0.0)) - c,
        corridorDist(p + float3(0.0, e, 0.0)) - c,
        corridorDist(p + float3(0.0, 0.0, e)) - c);
    return normalize(n);
}

void buildCamera(float2 uv, out float3 ro, out float3 rd)
{
    float zTravel = Time * CameraZMotionSpeed;

    ro = float3(0.0, 1.20, -2.2 + zTravel);
    float3 ta = float3(0.0, 1.20, 8.5 + zTravel);

    float3 forward = normalize(ta - ro);
    float3 right = normalize(cross(float3(0.0, 1.0, 0.0), forward));
    float3 up = normalize(cross(forward, right));

    float nA = noise1(Time * 0.45) * 2.0 - 1.0;
    float nB = noise1(Time * 0.93 + 17.1) * 2.0 - 1.0;
    float nC = sin(Time * 0.70);
    float roll = (nA * 0.60 + nB * 0.25 + nC * 0.15) * saturate(CameraTiltIntensity) * 0.95;

    float cr = cos(roll);
    float sr = sin(roll);
    float3 rightRolled = right * cr + up * sr;
    float3 upRolled = -right * sr + up * cr;

    float fov = 1.00;
    rd = normalize(forward + uv.x * rightRolled * fov + uv.y * upRolled * fov * 0.82);
}

#define RM_STEPS 36
#define RM_MAX_DIST 65.0

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 res = max(ScreenSize, float2(1.0, 1.0));
    float2 uv = input.TextureCoordinates * 2.0 - 1.0;
    uv.x *= res.x / res.y;
    uv.y = -uv.y;

    float3 ro, rd;
    buildCamera(uv, ro, rd);

    float3 fogColor = saturate(FogColor.rgb);
    float fogStrength = max(0.0, FogColor.a);

    float3 surfColor = saturate(SurfaceColor.rgb);
    float surfGain = max(0.0, SurfaceColor.a);

    float3 bg = saturate(BackgroundColor.rgb);
    float3 color = bg;

    float t = 0.0;
    float hit = 0.0;
    float d = 0.0;

    for (int i = 0; i < RM_STEPS; i++)
    {
        float3 p = ro + rd * t;
        d = corridorDist(p);

        if (d < 0.014)
        {
            hit = 1.0;
            break;
        }

        t += clamp(d * 0.68, 0.010, 0.60);
        if (t > RM_MAX_DIST)
            break;
    }

    if (hit > 0.5)
    {
        float3 p = ro + rd * t;
        float3 n = sceneNormal(p);

        float3 viewDir = normalize(ro - p);
        if (dot(n, viewDir) < 0.0)
            n = -n;

        float3 triW = abs(n);
        triW /= (triW.x + triW.y + triW.z + 1e-5);

        float texScale = 2.6;

        float2 uvXY = p.xy * texScale;
        float2 uvXZ = p.xz * texScale;
        float hXY = tissueHeight2D(uvXY);
        float hXZ = tissueHeight2D(uvXZ);
        float h = hXY * triW.z + hXZ * (triW.x + triW.y);

        float3 lightDir = normalize(float3(-0.34, 0.76, -0.30));
        float3 refl = reflect(-lightDir, n);

        float diff = 0.46 + 0.54 * saturate(abs(dot(n, lightDir)));
        float spec = pow(saturate(dot(refl, viewDir)), 14.0);
        float rim = pow(1.0 - saturate(dot(n, viewDir)), 2.0);

        float strat = abs(frac(h * 8.0) - 0.5);
        float contour = 1.0 - smoothstep(0.06, 0.22, strat);

        float shade = 0.15 + diff * 0.75;
        shade += spec * 0.06 + rim * 0.08;
        shade += h * 0.42;
        shade -= contour * 0.20;

        float distFade = exp(-t * (0.042 + fogStrength * 0.08));
        shade *= distFade * surfGain;

        color = bg + surfColor * shade;

        float fogAmt = saturate(1.0 - exp(-t * (0.030 + fogStrength * 0.10)));
        color = lerp(color, fogColor * 0.52 + float3(0.016, 0.016, 0.018), fogAmt);
    }

    float vignette = saturate(1.0 - length(uv * float2(0.86, 1.04)));
    color *= lerp(0.55, 1.0, vignette);

    float4 source = tex2D(TargetTextureSampler, input.TextureCoordinates) * input.Color;
    float tintLum = dot(source.rgb, float3(0.299, 0.587, 0.114));
    color *= lerp(0.90, 1.05, tintLum);

    return float4(saturate(color), 1.0);
}

technique DepthIllusionWithRuggedSurfaces
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
