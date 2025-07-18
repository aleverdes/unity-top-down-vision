#ifndef GLOBAL_TEXTURE_PROVIDER_INCLUDED
#define GLOBAL_TEXTURE_PROVIDER_INCLUDED

TEXTURE2D(_VisionGlobalMask);
SAMPLER(sampler_VisionGlobalMask);

void getVisionMaskGlobalTex_float(in float2 uv, out float4 color)
{
    color = SAMPLE_TEXTURE2D(_VisionGlobalMask, sampler_VisionGlobalMask, uv).rgba;
}

#endif