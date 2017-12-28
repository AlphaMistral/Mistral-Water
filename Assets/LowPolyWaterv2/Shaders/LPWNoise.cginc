#ifndef LPW_NOISE_INCLUDED
#define LPW_NOISE_INCLUDED


float _Scale_;

inline float Hash( float n ){
    #ifdef LPW_DISPLACE
        return frac(sin(n))*10.0);
    #else
        return frac(sin(n)*43758.5453);
    #endif
}


inline float ValueNoise( float2 x ){
    x /= _Scale_;

    float2 p = floor(x);
    float n = p.x*57.0 + p.y;
    
    float2 f = frac(x);
    f = f*f*(3.0-2.0*f);//f = smoothstep(0.0, 1.0, f); 

    return lerp(lerp( Hash(n), Hash(n+1.0),f.y), lerp( Hash(n+57.0), Hash(n+58.0),f.y),f.x) -0.5;
}


// needs to support tex2Dlod
#if (SHADER_TARGET >= 30) && !defined(SHADER_API_GLES)
    sampler2D _NoiseTex;
    float _TexSize_;

    #define SAMPLE_NOISE_TEX(uv) tex2Dlod(_NoiseTex, float4(uv/_TexSize_, 0,0)).a

    inline float TexNoise(float2 uv){
        float n = SAMPLE_NOISE_TEX(uv);
        return n*n*(3.0-2.0*n)-0.5;
    }
#endif

#endif // LPW_NOISE_INCLUDED