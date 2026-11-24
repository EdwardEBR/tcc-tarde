Shader "UI/LeftMenuBackgroundBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _NextTex ("Next Texture", 2D) = "white" {}
        _Blend ("Transition", Range(0, 1)) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 8)) = 4
        _Darken ("Darken", Range(0, 1)) = 0.42
        _FullScreen ("Full Screen Blur", Range(0, 1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            sampler2D _NextTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _BlurSize;
            float _Darken;
            float _Blend;
            float _FullScreen;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 BlurTexture(sampler2D textureToBlur, float2 uv, float2 stepSize)
            {
                fixed4 sum = tex2D(textureToBlur, uv + stepSize * float2(-1, -1));
                sum += tex2D(textureToBlur, uv + stepSize * float2( 0, -1)) * 2;
                sum += tex2D(textureToBlur, uv + stepSize * float2( 1, -1));
                sum += tex2D(textureToBlur, uv + stepSize * float2(-1,  0)) * 2;
                sum += tex2D(textureToBlur, uv) * 4;
                sum += tex2D(textureToBlur, uv + stepSize * float2( 1,  0)) * 2;
                sum += tex2D(textureToBlur, uv + stepSize * float2(-1,  1));
                sum += tex2D(textureToBlur, uv + stepSize * float2( 0,  1)) * 2;
                sum += tex2D(textureToBlur, uv + stepSize * float2( 1,  1));
                return sum / 16;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 stepSize = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 current = BlurTexture(_MainTex, input.uv, stepSize);
                fixed4 next = BlurTexture(_NextTex, input.uv, stepSize);
                fixed4 blurred = lerp(current, next, _Blend);

                float fadeEsquerda = 1.0 - smoothstep(0.30, 0.56, input.uv.x);
                float fade = lerp(fadeEsquerda, 1.0, _FullScreen);
                blurred.rgb *= 1.0 - (_Darken * fade);
                blurred *= input.color;
                blurred.a *= fade;

                #ifdef UNITY_UI_CLIP_RECT
                blurred.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(blurred.a - 0.001);
                #endif
                return blurred;
            }
            ENDCG
        }
    }
}
