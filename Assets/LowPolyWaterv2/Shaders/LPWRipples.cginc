#ifndef LPW_RIPPLES_INCLUDED
#define LPW_RIPPLES_INCLUDED

float _RSpeed_, _RHeight_;

inline float Ripple(float2 p, float phase){
    

    #if (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
        p *= 10;
        float2 uv = float2(p.x, phase+p.y);
        uv /= _Scale_;
        float2 pn = floor(uv);
        float n = pn.x*57.0 + pn.y;
        float2 f = frac(uv);
        float vn = lerp(lerp( Hash(n), Hash(n+1.0),f.y), lerp( Hash(n+57.0), Hash(n+58.0),f.y),f.x) -0.5;
        return vn*_RHeight_;
    #else
        float2 uv = float2(p.x, phase+p.y);
        return (SAMPLE_NOISE_TEX(uv)-0.5)*_RHeight_;
    #endif
}

inline void RipplesVert(inout float3 p0, inout float3 p1, inout float3 p2){
    float phase = LPW_TIME*_RSpeed_;
    p0.y += Ripple(p0.xz, phase);
    p1.y += Ripple(p1.xz, phase);
    p2.y += Ripple(p2.xz, phase);
}

inline void RipplesVertCustom(half3 normal, inout float3 p0, inout float3 p1, inout float3 p2){
    float phase = LPW_TIME*_RSpeed_;
    p0 = p0 + normal*Ripple(float2(p0.x+57.3*p0.y, p0.z), phase);
    p1 = p1 + normal*Ripple(float2(p1.x+57.3*p1.y, p1.z), phase);
    p2 = p2 + normal*Ripple(float2(p2.x+57.3*p2.y, p2.z), phase);
}

#endif // LPW_RIPPLES_INCLUDED