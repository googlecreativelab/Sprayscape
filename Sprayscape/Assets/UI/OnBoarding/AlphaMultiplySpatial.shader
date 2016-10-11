	Shader "Unlit/AlphaMultiplySpatial"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FadeSize("Fade Size", Float) = 0.1 // the size of the fade in portion
		_Direction("Direction", Vector) = (1.0, 0.0, 0.0, 0.0)
		_T("t", Range(0,1)) = 1.0
		_AlphaMult("Alpha Multiply", Range(0,1)) = 1.0
		[Toggle(_INVERT_UV)] _InvertUV("Invert UV", Float) = 0.0
		[Toggle(_INVERT_T)] _InvertT("Invert T", Float) = 0.0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _INVERT_UV
			#pragma shader_feature _INVERT_T
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _FadeSize;
			fixed4 _Direction;
			fixed _T;
			fixed _AlphaMult;

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed2 uv = i.uv;
				fixed t = _T;
#ifdef _INVERT_T
				t = 1 - t;
#endif
#ifdef _INVERT_UV
				uv = 1 - uv;
#endif
				t = t * ((1 + _FadeSize) / 1);
				fixed start = t - _FadeSize;
				fixed2 puv = saturate((uv - start) / _FadeSize);  // fade amount 0-1
				fixed  p = dot(puv, _Direction.xy);
				col.a = col.a * p * _AlphaMult;
				return col;
			}
			ENDCG
		}
	}
}
