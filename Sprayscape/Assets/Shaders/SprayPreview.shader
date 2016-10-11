Shader "SprayCam/Spray Preview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			#define PI  3.14159
			#define PI2 6.28319

			struct VertexInput
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct VertexToFragment  
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float alpha : TEXCOORD1;
			};

			VertexToFragment VertexProgram(VertexInput vertex)
			{
				VertexToFragment output;
				
				output.position = mul(UNITY_MATRIX_MVP, vertex.position);
				output.alpha = vertex.color.a;
				output.uv = vertex.uv;

				return output;
			}

			float4x4 _VP;

			sampler2D _MainTex;
			float4 _TexParams;

			float4 _Color;

			float3 _CamUp;
			float3 _CamRight;
			float3 _CamForward;

			fixed4 FragmentProgram(VertexToFragment fragment) : SV_Target
			{
				float2 centeredUV = (fragment.uv - 0.5) * 2;
				
				float3 planarCoords = normalize(_CamForward + (centeredUV.x * _CamRight) + (centeredUV.y * _CamUp));

				float x = 0.5 * (1 - atan2(planarCoords.z, planarCoords.x) / PI);
				float y = 0.5 * (1 - asin(planarCoords.y) / (PI * 0.5));
				
				// _TexParams.xy is the top left corner of the thumbnail the
				// texture atlas, while .zw is the resolution of the thumbnail.
				float u = _TexParams.x + _TexParams.z * x;
				float v = _TexParams.y + _TexParams.w * (1 - y);

				float4 color = tex2Dlod(_MainTex, float4(u, v, 0, 0));

				return float4(color.rgb, fragment.alpha);
			}
			ENDCG
		}
	}
}
