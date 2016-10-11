Shader "SprayCam/Spray Composite"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "" {}
	}

	Category
	{
		Tags { "Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

		Blend SrcAlpha OneMinusSrcAlpha

		AlphaTest Off
		Lighting Off
		Cull Back
		ZWrite On
		ZTest LEqual
		Fog { Mode Off }

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

				sampler2D _MainTex;

				float4x4 _VP;

				float2 _CaptureScale;
				float _CaptureAlpha;
				float _CaptureSize;

				fixed4 FragmentProgram(VertexToFragment fragment) : SV_Target
				{
					float u = fragment.uv.x;
					float v = fragment.uv.y - 0.5;

					// calculate world position spherically mapped based on UV coordinates
					float r = cos(PI * v);

					float4 worldPosition = float4(r * sin(TWO_PI * u), sin(PI * v), r * cos(TWO_PI * u), 1);
					float4 screenPosition = mul(_VP, worldPosition);

					float4 color = 0;

					if (screenPosition.w > 0)
					{
						screenPosition = screenPosition / screenPosition.w;

						if (screenPosition.x > -1 && screenPosition.x < 1 && screenPosition.y > -1 && screenPosition.y < 1)
						{
							// The camera image isn't necessarily the same aspect ratio as the screen. Since we are
							// using screen coordinates to sample the image, we need to scale the coords so that the
							// camera image is composited at it's original aspect ratio.
							float2 scaledCoords = screenPosition * _CaptureScale;

							// Map the coordinates from [-1, 1] to [0, 1].
							float2 textureCoords = (1 + scaledCoords) * 0.5;

							color = tex2Dlod(_MainTex, float4(textureCoords, 0, 0));

							float a = 1.0 - smoothstep(_CaptureSize * 0.5, _CaptureSize, length(screenPosition.xy));
							color.a = saturate(a * _CaptureAlpha);
						}
					}

					return color;
				}
				ENDCG
			}
		}
	}
}
