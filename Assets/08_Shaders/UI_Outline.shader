// Unity 내장 UI/Default 셰이더를 베이스로, 스프라이트 알파 경계에 네온 글로우(밝은 코어 + 컬러 헤일로 + 맥동)를 입히는 uGUI용 셰이더.
// Mask/RectMask2D(스텐실), UNITY_UI_CLIP_RECT, SRP 배칭 등 UI-Default가 지원하는 기능은 그대로 유지됨.
Shader "Custom/UI_Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Neon Outline)]
        _OutlineColor ("Outline Color (halo)", Color) = (0.2, 0.9, 1, 1)
        _CoreColor ("Core Color (hot edge)", Color) = (0.85, 1, 1, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 24)) = 0
        _CoreWidthRatio ("Core Width Ratio", Range(0.05, 1)) = 0.35
        _GlowIntensity ("Glow Intensity", Range(1, 4)) = 2
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25

        [Header(UI Mask)]
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _OutlineColor;
            fixed4 _CoreColor;
            float _OutlineWidth;
            float _CoreWidthRatio;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 8방향으로 알파를 샘플링해서 스프라이트 바깥쪽 경계에 걸쳐있는지 판별
            half SampleOutlineMask(float2 uv, float2 texel)
            {
                half m = 0;
                m = max(m, tex2D(_MainTex, uv + float2( texel.x,  0)).a);
                m = max(m, tex2D(_MainTex, uv + float2(-texel.x,  0)).a);
                m = max(m, tex2D(_MainTex, uv + float2( 0,  texel.y)).a);
                m = max(m, tex2D(_MainTex, uv + float2( 0, -texel.y)).a);
                m = max(m, tex2D(_MainTex, uv + float2( texel.x,  texel.y)).a);
                m = max(m, tex2D(_MainTex, uv + float2(-texel.x,  texel.y)).a);
                m = max(m, tex2D(_MainTex, uv + float2( texel.x, -texel.y)).a);
                m = max(m, tex2D(_MainTex, uv + float2(-texel.x, -texel.y)).a);
                return m;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 spriteColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                half innerMask = 0; // 경계 바로 바깥, 밝은 코어
                half outerMask = 0; // 코어보다 더 바깥으로 퍼지는 은은한 헤일로
                if (_OutlineWidth > 0.001)
                {
                    float2 texelUnit = _MainTex_TexelSize.xy;
                    innerMask = SampleOutlineMask(IN.texcoord, texelUnit * _OutlineWidth * _CoreWidthRatio);
                    outerMask = SampleOutlineMask(IN.texcoord, texelUnit * _OutlineWidth);
                }

                // 시간에 따라 밝기가 은은하게 맥동(네온사인 특유의 흔들림)
                half pulse = 1 - _PulseAmount + _PulseAmount * (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);

                // 코어에 가까울수록 밝은 코어 색, 멀어질수록 헤일로 색으로 섞고 발광 강도를 곱한다
                half3 glowColor = lerp(_OutlineColor.rgb, _CoreColor.rgb, innerMask) * _GlowIntensity * pulse;

                // 헤일로는 코어보다 약하게, 스프라이트가 이미 불투명한 픽셀에는 겹치지 않는다
                half glowMask = max(innerMask, outerMask * 0.6);
                half outlineAlpha = glowMask * (1 - spriteColor.a) * _OutlineColor.a;

                half4 color;
                color.rgb = lerp(glowColor, spriteColor.rgb, spriteColor.a);
                color.a = max(spriteColor.a, outlineAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
