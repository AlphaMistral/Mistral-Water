#ifndef LPW_WAVES_INCLUDED
#define LPW_WAVES_INCLUDED

float _Height_, _Length, _Stretch, _Speed_;
float4 _Direction_; //cos, sin, cos*steepness, sin*steepness

inline void Gerstner(inout float3 p, float phase){
    float x = p.x*_Direction_.x - p.z*_Direction_.y;
    float z = p.z*_Direction_.x + p.x*_Direction_.y;
    float2 xz = float2(x/_Stretch, z/_Length + phase);
    #if defined(_WAVES_HIGHQUALITY) || (SHADER_TARGET < 30) || defined(SHADER_API_GLES)
        float n = ValueNoise(xz);
    #else
        float n = TexNoise(xz);
    #endif
    p.y += _Height_*n;

    #if (SHADER_TARGET >= 30)
        p.xz -= n*_Direction_.wz;
    #endif
}

inline void WavesVert(inout float3 p0, inout float3 p1, inout float3 p2){
    float phase = LPW_TIME*_Speed_;
    Gerstner(p0, phase);
    Gerstner(p1, phase);
    Gerstner(p2, phase);
}

#endif // LPW_WAVES_INCLUDED