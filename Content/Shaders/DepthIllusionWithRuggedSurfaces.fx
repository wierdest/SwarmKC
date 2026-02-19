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
extern float4 SurfaceColor;
extern float4 BackgroundColor;
extern float CameraTiltIntensity;
extern float CameraZMotionSpeed;
extern float2 ScreenSize;

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

float fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    v += a * noise2(p); p *= 2.02; a *= 0.5;
    v += a * noise2(p);
    return v;
}

float ridged(float2 p)
{
    float v = 0.0;
    float a = 0.6;

    float n0 = 1.0 - abs(noise2(p) * 2.0 - 1.0); p *= 2.1; v += n0 * n0 * a; a *= 0.55;
    float n1 = 1.0 - abs(noise2(p) * 2.0 - 1.0);          v += n1 * n1 * a;

    return v;
}

float rockHeight2D(float2 uv)
{
    float2 warp = float2(fbm(uv * 0.70), noise2(uv * 0.70 + float2(11.3, 7.1))) * 1.4;
    float2 q = uv + warp;

    float h = ridged(q * 0.95) * 0.85;
    h += noise2(q * 2.0 + float2(13.7, -8.4)) * 0.30;

    float cracks = pow(saturate(1.0 - abs(sin(q.x * 3.7 + q.y * 2.9 + h * 5.5))), 9.0);
    h -= cracks * 0.18;

    return h;
}

float noise1(float x)
{
    float i = floor(x);
    float f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(float2(i, 19.37));
    float b = hash21(float2(i + 1.0, 19.37));
    return lerp(a, b, f);
}

float centerOffsetX(float z)
{
    float c = 0.0;
    c += sin(z * 0.11 + Time * 0.06) * 0.80;
    c += sin(z * 0.037 - Time * 0.03) * 0.55;
    return c;
}

float floorDisp(float2 xz)
{
    float flow = centerOffsetX(xz.y);
    float2 q = xz + float2(flow * 0.22, sin(flow * 0.5) * 0.10);

    float n = ridged(q * 0.42);
    n += noise2(q * 1.4 + float2(7.1, 13.4)) * 0.22;
    return (n - 0.45) * 1.85;
}

float ceilDisp(float2 xz)
{
    float flow = centerOffsetX(xz.y);
    float2 q = xz + float2(flow * 0.16, cos(flow * 0.45) * 0.08);

    float n = ridged(q * 0.40 + float2(21.3, 5.4));
    n += noise2(q * 1.3 + float2(2.7, 9.6)) * 0.20;
    return (n - 0.45) * 1.60;
}

float wallDisp(float2 zy, float seed)
{
    float flow = centerOffsetX(zy.x);
    float2 q = zy + float2(flow * 0.18, sin(flow * 0.55) * 0.10);

    float n = ridged(q * 0.44 + float2(seed, seed * 1.7));
    n += noise2(q * 1.3 + float2(seed * 1.7, seed * 0.8)) * 0.20;
    return (n - 0.45) * 2.05;
}

float leftWallX(float3 p)
{
    float cx = centerOffsetX(p.z);
    return cx - 3.6 - wallDisp(float2(p.z, p.y), 3.1);
}

float rightWallX(float3 p)
{
    float cx = centerOffsetX(p.z);
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

    float3 fogColor = float3(0.16, 0.07, 0.19);
    float fogStrength = 0.35;

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
        float hXY = rockHeight2D(uvXY);
        float hXZ = rockHeight2D(uvXZ);
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
