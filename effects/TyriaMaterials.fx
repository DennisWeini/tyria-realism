/*
 * Tyria Materials 0.3
 * Lightweight luminance-only material detail for Guild Wars 2 / ReShade 6.x.
 * Original code, MIT licensed as part of Tyria Realism.
 * Five colour samples. No temporal history, AI model or external texture.
 */

#include "ReShade.fxh"

uniform float FineDetail < ui_type = "slider"; ui_label = "Fine material detail"; ui_min = 0.0; ui_max = 1.25; ui_step = 0.01; > = 0.62;
uniform float BroadDetail < ui_type = "slider"; ui_label = "Local material contrast"; ui_min = 0.0; ui_max = 1.00; ui_step = 0.01; > = 0.26;
uniform float DetailLimit < ui_type = "slider"; ui_label = "Halo limit"; ui_min = 0.005; ui_max = 0.080; ui_step = 0.001; > = 0.030;
uniform float DarkProtection < ui_type = "slider"; ui_label = "Dark noise protection"; ui_min = 0.0; ui_max = 0.20; ui_step = 0.005; > = 0.055;
uniform float HighlightProtection < ui_type = "slider"; ui_label = "Highlight protection"; ui_min = 0.60; ui_max = 1.00; ui_step = 0.01; > = 0.88;
uniform float Colourfulness < ui_type = "slider"; ui_label = "Natural colour"; ui_min = -0.20; ui_max = 0.20; ui_step = 0.005; > = 0.015;
uniform float UiProtection < ui_type = "slider"; ui_label = "Screen-edge UI protection"; ui_min = 0.0; ui_max = 1.0; ui_step = 0.05; > = 0.75;
uniform float NaturalGrade < ui_type = "slider"; ui_label = "Natural colour and light"; ui_min = 0.0; ui_max = 1.0; ui_step = 0.01; > = 0.0;

float Luma(float3 c)
{
    return dot(c, float3(0.2126, 0.7152, 0.0722));
}

float ScreenContentMask(float2 uv)
{
    // Smoothly reduce work over GW2's top buttons, right tracker and bottom skill/chat UI.
    float top = smoothstep(0.045, 0.105, uv.y);
    float bottom = 1.0 - smoothstep(0.775, 0.895, uv.y);
    float right = 1.0 - smoothstep(0.835, 0.955, uv.x);
    return lerp(1.0, top * bottom * right, UiProtection);
}

float4 PS_TyriaMaterials(float4 position : SV_Position, float2 uv : TEXCOORD) : SV_Target
{
    float2 px = ReShade::PixelSize;
    float3 c = tex2D(ReShade::BackBuffer, uv).rgb;

    float l = Luma(c);
    float n = Luma(tex2D(ReShade::BackBuffer, uv + float2(0.0, -px.y)).rgb);
    float s = Luma(tex2D(ReShade::BackBuffer, uv + float2(0.0,  px.y)).rgb);
    float e = Luma(tex2D(ReShade::BackBuffer, uv + float2( px.x, 0.0)).rgb);
    float w = Luma(tex2D(ReShade::BackBuffer, uv + float2(-px.x, 0.0)).rgb);
    float localMean = 0.25 * (n + s + e + w);
    float localMin = min(l, min(min(n, s), min(e, w)));
    float localMax = max(l, max(max(n, s), max(e, w)));
    float localRange = localMax - localMin;

    float protectDark = smoothstep(DarkProtection * 0.35, DarkProtection, l);
    float protectBright = 1.0 - smoothstep(HighlightProtection, 1.0, l);
    float localActivity = smoothstep(0.008, 0.085, localRange);
    float hardEdgeProtection = 1.0 - smoothstep(0.28, 0.60, localRange);
    float amount = protectDark * protectBright * localActivity * hardEdgeProtection * ScreenContentMask(uv);

    float highPass = l - localMean;
    float detail = highPass * (FineDetail + BroadDetail * saturate(localRange * 7.0));
    detail = clamp(detail, -DetailLimit, DetailLimit) * amount;
    float targetLuma = clamp(l + detail, max(0.0, localMin - 0.012), min(1.0, localMax + 0.012));
    float3 detailed = c * ((targetLuma + 0.0001) / (l + 0.0001));

    float average = (detailed.r + detailed.g + detailed.b) / 3.0;
    detailed = average.xxx + (detailed - average.xxx) * (1.0 + Colourfulness * amount);
    // Analytic colour grade shares this pass and its existing five samples.
    // Neutral colours stay neutral; endpoint-preserving tone curve avoids raised blacks.
    float gradeMask = ScreenContentMask(uv) * NaturalGrade;
    float lum = Luma(detailed);
    float greenExcess = max(0.0, detailed.g - max(detailed.r, detailed.b));
    float greenWeight = smoothstep(0.015, 0.18, greenExcess);
    float3 graded = lerp(detailed, lum.xxx, 0.09 * greenWeight);
    float target = lum + 0.055 * lum * (1.0 - lum) * (2.0 * lum - 0.70);
    graded *= (target + 0.0001) / (lum + 0.0001);
    detailed = lerp(detailed, graded, gradeMask);
    return float4(saturate(detailed), 1.0);
}

technique TyriaMaterials
< ui_tooltip = "Low-cost material detail. Luminance-only to avoid neon colour shifts."; >
{
    pass
    {
        VertexShader = PostProcessVS;
        PixelShader = PS_TyriaMaterials;
    }
}

