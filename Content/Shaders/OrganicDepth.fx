#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Organic Depth shader:
// attempts to represent a organic tunnel in which surfaces are like soft tissue

// Unused in this shader, it is optimized out in compilation
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
extern float3 LightDirection;
extern float4 LightingStrength; // x=ambiente y=diffuse z=specular w=rim
extern float2 LightingPower; // x=specularPower, y= rimPower
extern float4 LightColor; //rgb=color a=intensity

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
float tunnelLateralFlow(float z)
{
    float flow = 0.0;
    flow += sin(z * LATERAL_OFFSET_FREQUENCY_1 + Time * LATERAL_OFFSET_TIME_SPEED_1) * LATERAL_OFFSET_AMPLITUDE_1;
    flow += sin(z * LATERAL_OFFSET_FREQUENCY_2 - Time * LATERAL_OFFSET_TIME_SPEED_2) * LATERAL_OFFSET_AMPLITUDE_2;
    return flow;
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
    float flow = tunnelLateralFlow(xz.y);
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
    float flow = tunnelLateralFlow(xz.y);
    float2 ceilingUv = xz + float2(flow * CEILING_FLOW_X_MULTIPLIER, cos(flow * CEILING_FLOW_Z_WAVE_FREQUENCY) * CEILING_FLOW_Y_MULTIPLIER);

    float height = tissueRelief(ceilingUv * CEILING_BASE_RELIEF_SCALE + CEILING_RELIEF_OFFSET);
    height += noise2(ceilingUv * CEILING_DETAIL_SCALE + CEILING_DETAIL_OFFSET) * CEILING_DETAIL_MULTIPLIER;
    return (height - CEILING_HEIGHT_MIDPOINT) * CEILING_HEIGHT_AMPLITUDE;
}

// Wall displacement height:
// same as above, except uses per-wall seed to decorrelate left/right wall patterns
static const float WALL_FLOW_Z_MULTIPLIER = 0.18;
static const float WALL_FLOW_Y_MULTIPLIER = 0.10;
static const float WALL_FLOW_Y_WAVE_FREQUENCY = 0.55;

static const float WALL_BASE_RELIEF_SCALE = 0.44;
static const float WALL_BASE_SEED_MULTIPLIER_A = 1.7;
static const float WALL_BASE_SEED_MULTIPLIER_B = 0.8;

static const float WALL_DETAIL_SCALE = 1.3;
static const float WALL_DETAIL_SEED_MULTIPLIER_A = 1.9;
static const float WALL_DETAIL_SEED_MULTIPLIER_B = 1.0;
static const float WALL_DETAIL_MULTIPLIER = 0.20;

static const float WALL_HEIGHT_MIDPOINT = 0.45;
static const float WALL_HEIGHT_AMPLITUDE = 2.05;
float wallDisp(float2 zy, float seed)
{
    float flow = tunnelLateralFlow(zy.x);
    float2 wallUv = zy + float2(flow * WALL_FLOW_Z_MULTIPLIER, sin(flow * WALL_FLOW_Y_WAVE_FREQUENCY) * WALL_FLOW_Y_MULTIPLIER);

    float height = tissueRelief(wallUv * WALL_BASE_RELIEF_SCALE + float2(seed * WALL_BASE_SEED_MULTIPLIER_A, seed * WALL_BASE_SEED_MULTIPLIER_B));

    height += noise2(wallUv * WALL_DETAIL_SCALE + float2(seed * WALL_DETAIL_SEED_MULTIPLIER_A, seed * WALL_DETAIL_SEED_MULTIPLIER_B)) * WALL_DETAIL_MULTIPLIER;

    return (height - WALL_HEIGHT_MIDPOINT) * WALL_HEIGHT_AMPLITUDE;
}

// Returns the X position of the tunnel wall at point p
// moves from center drift to the left by a base half-width
static const float TUNNEL_HALF_WIDTH = 3.6;
static const float LEFT_WALL_SEED = 3.1;
float leftWallX(float3 p)
{
    float centerX = tunnelLateralFlow(p.z);
    float wallOffset = wallDisp(float2(p.z, p.y), LEFT_WALL_SEED);
    return centerX - TUNNEL_HALF_WIDTH - wallOffset;
}
// same as above, but mirrored, with another seed
static const float RIGHT_WALL_SEED = 7.9;
float rightWallX(float3 p)
{
    float centerX = tunnelLateralFlow(p.z);
    float wallOffset = wallDisp(float2(p.z, p.y), RIGHT_WALL_SEED);
    return centerX + TUNNEL_HALF_WIDTH + wallOffset;
}

// Floor height (Y) for a given horizontal/depth position (X,Z)
// adds organic displacement
float floorYAt(float3 p)
{
    return floorDisp(float2(p.x, p.z));
}

// Ceiling height (Y) fora given horizontal/depth position (X,Z)
// from a base level, adds organic displacement
static const float CEILING_BASE_HEIGHT = 2.4;
float ceilYAt(float3 p)
{
    return CEILING_BASE_HEIGHT + ceilDisp(float2(p.x, p.z));
}

// Signed corridor distance (d) at point p
// positive inside, newar zero on the nearest boundary
// computes distance to each boundary, keeps the smallest one
float corridorDist(float3 p)
{
    float dL = p.x - leftWallX(p);
    float dR = rightWallX(p) - p.x;
    float dF = p.y - floorYAt(p);
    float dC = ceilYAt(p) - p.y;
    return min(min(dL, dR), min(dF, dC));
}

// Approximate surface normal from corridor distance field gradient
// samples field at small X/Y/Z offsets
// builds gradient vector
static const float NORMAL_EPSILON = 0.010;
static const float3 OFFSET_X = float3(NORMAL_EPSILON, 0.0, 0.0);
static const float3 OFFSET_Y = float3(0.0, NORMAL_EPSILON, 0.0);
static const float3 OFFSET_Z = float3(0.0, 0.0, NORMAL_EPSILON);
float3 sceneNormal(float3 p)
{
    float center = corridorDist(p);
    float3 gradient = float3(
        corridorDist(p + OFFSET_X) - center,
        corridorDist(p + OFFSET_Y) - center,
        corridorDist(p + OFFSET_Z) - center);
    return normalize(gradient);
}

// Builds camera origin (ro, ray origin) and view ray direction (rd)
// camera moves forward on Z (configurable speed)
// uses a fixed loot-at point target ahead (ta)
// applies subtle procedural roll (tilt) for organic motion
// maps screen UV to final ray using camera basis vectors
static const float CAMERA_BASE_Y = 1.20;
static const float CAMERA_BASE_Z = -2.2;
static const float CAMERA_TARGET_Z = 8.5;

static const float3 WORLD_UP = float3(0.0, 1.0, 0.0);

static const float ROLL_NOISE_FREQUENCY_A = 0.45;
static const float ROLL_NOISE_FREQUENCY_B = 0.93;
static const float ROLL_SINE_FREQUENCY_C = 0.70;

static const float ROLL_NOISE_OFFSET = 17.1;

static const float ROLL_MULTIPLIER_A = 0.60;
static const float ROLL_MULTIPLIER_B = 0.25;
static const float ROLL_MULTIPLIER_C = 0.15;
static const float ROLL_INTENSITY_SCALE = 0.95;

static const float NOISE_TO_SIGNED_SCALE = 2.0;
static const float NOISE_TO_SIGNED_BIAS = 1.0;

static const float CAMERA_FOV = 1.00;
static const float CAMERA_VERTICAL_FOV_SCALE = 0.82;
void buildCamera(float2 uv, out float3 ro, out float3 rd)
{
    float zTravel = Time * CameraZMotionSpeed;

    ro = float3(0.0, CAMERA_BASE_Y, CAMERA_BASE_Z + zTravel);
    float3 ta = float3(0.0, CAMERA_BASE_Y, CAMERA_TARGET_Z + zTravel);

    float3 forward = normalize(ta - ro);
    float3 right = normalize(cross(WORLD_UP, forward));
    float3 up = normalize(cross(forward, right));

    float nA = noise1(Time * ROLL_NOISE_FREQUENCY_A) * NOISE_TO_SIGNED_SCALE - NOISE_TO_SIGNED_BIAS;
    float nB = noise1(Time * ROLL_NOISE_FREQUENCY_B + ROLL_NOISE_OFFSET) * NOISE_TO_SIGNED_SCALE - NOISE_TO_SIGNED_BIAS;
    float nC = sin(Time * ROLL_SINE_FREQUENCY_C);
    float roll = (nA * ROLL_MULTIPLIER_A + nB * ROLL_MULTIPLIER_B + nC * ROLL_MULTIPLIER_C) * saturate(CameraTiltIntensity) * ROLL_INTENSITY_SCALE;

    float cr = cos(roll);
    float sr = sin(roll);
    float3 rightRolled = right * cr + up * sr;
    float3 upRolled = -right * sr + up * cr;

    rd = normalize(forward + uv.x * rightRolled * CAMERA_FOV + uv.y * upRolled * CAMERA_FOV * CAMERA_VERTICAL_FOV_SCALE);
}

// Main pixel shader pipeline:
// builds camera ray from UV
// raymarches corridor distance field
// if hit, compute normal + tissue detail + lighting + fog
// applies vignette and source tint modulation
// returns final color

// Vector and scalar helpers
static const float2 MIN_SCREEN_SIZE = float2(1.0, 1.0);
static const float UV_TO_NDC_SCALE = 2.0;
static const float UV_TO_NDC_BIAS = 1.0;
static const float TRI_BLEND_EPSILON = 1e-5; // tri planar blend (xy, xz, yz)
static const float HIT_THRESHOLD = 0.5;
static const float OUTPUT_ALPHA = 1.0;

// Raymarch controls (check https://en.wikipedia.org/wiki/Ray_marching)
#define RM_STEPS 36
static const float RAY_MAX_DIST = 65.0;
static const float RAY_HIT_EPSILON = 0.014;
static const float RAY_STEP_MULTIPLIER = 0.68;
static const float RAY_STEP_MIN = 0.010;
static const float RAY_STEP_MAX = 0.60;

// Surface sampling
static const float TISSUE_TEX_SCALE = 2.6;

// Stratification / contouring
static const float STRATA_FREQUENCY = 8.0;
static const float STRATA_CENTER = 0.5;
static const float CONTOUR_MIN = 0.06;
static const float CONTOUR_MAX = 0.22;

// Final shade composition
static const float SHADE_BASE = 0.15;
static const float SHADE_DIFFUSE_MULTIPLIER = 0.75;
static const float SHADE_SPECULAR_MULTIPLIER = 0.06;
static const float SHADE_RIM_MULTIPLIER = 0.08;
static const float SHADE_HEIGHT_MULTIPLIER = 0.42;
static const float SHADE_CONTOUR_MULTIPLIER = 0.20;

// Distance attenuation + fog
static const float DISTANCE_FADE_BASE = 0.042;
static const float DISTANCE_FADE_FOG_MULTIPLIER = 0.08;
static const float FOG_EXPONENTIAL_BASE = 0.030;
static const float FOG_EXPONENTIAL_MULTIPLIER = 0.10;
static const float FOG_COLOR_MULTIPLIER = 0.52;
static const float3 FOG_COLOR_OFFSET = float3(0.016, 0.016, 0.018);

// Vignette
static const float2 VIGNETTE_SCALE = float2(0.86, 1.04);
static const float VIGNETTE_MIN = 0.55;
static const float VIGNETTE_MAX = 1.0;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 resolution = max(ScreenSize, MIN_SCREEN_SIZE);
    float2 uv = input.TextureCoordinates * UV_TO_NDC_SCALE - UV_TO_NDC_BIAS;
    uv.x *= resolution.x / resolution.y;
    uv.y = -uv.y;

    float3 ro, rd;
    buildCamera(uv, ro, rd);

    float3 fogColor = saturate(FogColor.rgb);
    float fogStrength = max(0.0, FogColor.a);

    float3 surfaceColor = saturate(SurfaceColor.rgb);
    float surfaceIntensity = max(0.0, SurfaceColor.a);

    float3 backgroundColor = saturate(BackgroundColor.rgb);
    float3 color = backgroundColor;

    float travel = 0.0;
    float hit = 0.0;
    float dist = 0.0;

    for (int i = 0; i < RM_STEPS; i++)
    {
        float3 p = ro + rd * travel;
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

    if (hit > HIT_THRESHOLD)
    {
        float3 p = ro + rd * travel;
        float3 normal = sceneNormal(p);

        float3 viewDir = normalize(ro - p);
        if (dot(normal, viewDir) < 0.0)
            normal = -normal;

        float3 triBlend = abs(normal);
        triBlend /= (triBlend.x + triBlend.y + triBlend.z + TRI_BLEND_EPSILON);

        float2 uvXY = p.xy * TISSUE_TEX_SCALE;
        float2 uvXZ = p.xz * TISSUE_TEX_SCALE;
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

        float strata = abs(frac(height * STRATA_FREQUENCY) - STRATA_CENTER);
        float contour = 1.0 - smoothstep(CONTOUR_MIN, CONTOUR_MAX, strata);

        float shade = SHADE_BASE + diffuse * SHADE_DIFFUSE_MULTIPLIER;
        shade += specular * SHADE_SPECULAR_MULTIPLIER + rim * SHADE_RIM_MULTIPLIER;
        shade += height * SHADE_HEIGHT_MULTIPLIER;
        shade -= contour * SHADE_CONTOUR_MULTIPLIER;

        float distFade = exp(-travel * (DISTANCE_FADE_BASE + fogStrength * DISTANCE_FADE_FOG_MULTIPLIER));
        shade *= distFade * surfaceIntensity;

        color = backgroundColor + (surfaceColor* lightColor) * shade;

        float fogAmount = saturate(1.0 - exp(-travel * (FOG_EXPONENTIAL_BASE + fogStrength * FOG_EXPONENTIAL_MULTIPLIER)));
        color = lerp(color, fogColor * FOG_COLOR_MULTIPLIER + FOG_COLOR_OFFSET, fogAmount);
    }

    float vignette = saturate(1.0 - length(uv * VIGNETTE_SCALE));
    color *= lerp(VIGNETTE_MIN, VIGNETTE_MAX, vignette);
    
    return float4(saturate(color), OUTPUT_ALPHA);
}

technique OrganicDepth
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
