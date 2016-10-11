Shader "SprayCam/Onboarding Wireframe"
{
	Properties
	{
		_LineOpacity ("Line Opacity", Float) = 0.4
		_LineWeight  ("Line Weight", Float) = 0.01

		_FalloffStart ("Opacity Falloff Start", Float) = 1.0
		_FalloffEnd   ("Opacity Falloff End", Float) = 0.5

		_LatLines ("Latitude Lines", Int) = 16
		_LngLines ("Longitude Lines", Int) = 32
	}

	Category
	{

		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader
		{
			Pass
			{
				CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram
				#include "Assets/Shaders/SprayInclude.cginc"

				struct VertexInput
				{
					float4 position : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct VertexToFragment
				{
					float4 position : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				VertexToFragment VertexProgram(VertexInput vertex)
				{
					VertexToFragment output;

					output.position = mul(UNITY_MATRIX_MVP, vertex.position);
					output.uv = vertex.uv;

					return output;
				};

				float _LineOpacity;
				float _LineWeight;
				
				float _FalloffStart;
				float _FalloffEnd;

				int _LatLines;
				int _LngLines;

				fixed4 FragmentProgram(VertexToFragment fragment) : SV_Target
				{
					float2 uv = fragment.uv;

					// map the uv coordinates to a periodic function
					// TODO this seems to be creating some waviness where the uv coordinates compress towards the poles
					float uTiling = abs(cos(uv.x * PI * _LngLines));
					float vTiling = abs(cos(uv.y * PI * _LatLines));

					// step the cos function with some antialising
					float uSmoothed = smoothstep(1 - _LineWeight, 1, uTiling);
					float vSmoothed = smoothstep(1 - _LineWeight, 1, vTiling);

					// draw a line near either horizontal and veritcal maximums
					float a = max(uSmoothed, vSmoothed);

					// fade the lines towards the poles
					float centeredV = abs(uv.y - 0.5) * 2;
					a *= 1 - smoothstep(_FalloffEnd, _FalloffStart, centeredV);

					// fade the lines everywhere
					a *= _LineOpacity;

					return float4(1, 1, 1, a);
				}
				ENDCG
			}
		}
	}
}
