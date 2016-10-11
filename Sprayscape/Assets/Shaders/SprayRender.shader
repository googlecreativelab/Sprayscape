Shader "SprayCam/Spray Render"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

			sampler2D _MainTex;
			
			VertexToFragment VertexProgram(VertexInput vertex)
			{
				VertexToFragment output;

				output.position = mul(UNITY_MATRIX_MVP, vertex.position);
				output.uv = float2(1 - vertex.uv.x, vertex.uv.y);

				return output;
			}
			
			fixed4 FragmentProgram(VertexToFragment fragment) : SV_Target
			{
				return tex2D(_MainTex, fragment.uv);
			}
			ENDCG
		}
	}
}
