Shader "Unlit/AlphaSpin"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Spread ("Spread", Range(0,3)) = 1.0
		_TMult1("TimeMult1", Range(0,3)) = 2.0
		_TMult2("TimeMult1", Range(0,3)) = 1.0
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
			fixed _Spread;
			fixed _TMult1;
			fixed _TMult2;
			
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
				fixed2 uv = i.uv * 2.0 - 1.0; // move from 0-1 to -1 to 1
				fixed time = _Time[1] * _TMult1; // rotation speed
				fixed t = time % 6.28318530717958647; // wrap time to 0 -> 2pi
				fixed a = atan2(uv.y, uv.x);
				// create two opposite arcs for now
				fixed a2 = atan2(-uv.y, -uv.x);
				// map from -pi -> pi to pi -> 2*pi
				if (a < 0)
					a = a + 6.28318530717958647;
				if (a2 < 0)
					a2 = a2 + 6.28318530717958647;
				// spread of the arc section
				fixed s = (sin(time*_TMult2) + 1) * _Spread;
				
				// probably a more effcient way to calculate this...
				if (abs(t - a) < s || 
					abs(t - a + 6.28318530717958647) < s ||
					abs(t - a - 6.28318530717958647) < s ||
					abs(t - a2) < s ||
					abs(t - a2 + 6.28318530717958647) < s ||
					abs(t - a2 - 6.28318530717958647) < s)
				{
					col.a = col.a * 1.0;
				}
				else
				{
					col.a = 0.0;
				}	
				return col;
			}
			ENDCG
		}
	}
}
