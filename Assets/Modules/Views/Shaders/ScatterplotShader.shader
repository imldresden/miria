﻿Shader "Custom/ScatterplotShader"
{
	Properties
	{
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
		_Size("Size", Range(0, 1)) = 0.5
		_Color("Main Color", Color) = (1,1,1,1)
		_Scale("Scale", Vector) = (1,1,1)
	}

		SubShader
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Pass
			{
				Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
				
				Zwrite Off
				ZTest On
				//Blend[_MySrcMode][_MyDstMode]
				Cull Off
				//Lighting Off
				Offset -1, -1 // This line is added to default Unlit/Transparent shader
				//LOD 200

				CGPROGRAM
					#pragma target 5.0
					#pragma multi_compile_instancing
					#pragma vertex VS_Main
					#pragma fragment FS_Main alpha
					#pragma geometry GS_Main
					//#pragma enable_d3d11_debug_symbols
					#include "UnityCG.cginc"


				// **************************************************************
				// Data structures												*
				// **************************************************************

				struct VS_INPUT {
					float4 position : POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					//float4 color: COLOR;
					//float3 normal: NORMAL;
					//float4 _MainTex : TEXCOORD0; // index, vertex size, filtered, prev size
				};

				struct GS_INPUT
				{
					float4	pos : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					//float2  tex0 : TEXCOORD0;
					//float4  color : COLOR;
				};

				struct FS_INPUT
				{
					float4	pos : POSITION;
					float2  tex0 : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
					//float4  color : COLOR;
				};

				// **************************************************************
				// Vars															*
				// **************************************************************

				float _Size;
				float4 _Color;
				sampler2D _MainTex;
				vector _Scale;
				float4x4 _WorldToLocalMatrix;


				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(VS_INPUT v)
				{
					GS_INPUT output;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_OUTPUT(GS_INPUT, output);					
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

					output.pos = mul(_WorldToLocalMatrix, v.position);
				
					return output;
				}



				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					UNITY_SETUP_INSTANCE_ID(p[0]);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(p[0]);
					float3 up = float3(0.0f, 1.0f, 0.0f);
					float3 right = float3(1.0f, 0.0f, 0.0f);

					float sizeX = (_Size / _Scale.x);
					float sizeY = (_Size / _Scale.y);
					//float sizeX = _Size;
					//float sizeY = _Size;

					float4 v[4];

					v[0] = float4(p[0].pos + sizeX * right - sizeY * up, 1.0f);
					v[1] = float4(p[0].pos + sizeX * right + sizeY * up, 1.0f);
					v[2] = float4(p[0].pos - sizeX * right - sizeY * up, 1.0f);
					v[3] = float4(p[0].pos - sizeX * right + sizeY * up, 1.0f);


					FS_INPUT pIn;

					//pIn.color = p[0].color;

					//UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);

					pIn.pos = UnityObjectToClipPos(v[0]);
					//pIn.pos = mul(UNITY_MATRIX_VP, v[0]);
					pIn.tex0 = float2(1.0f, 0.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[1]);
					//pIn.pos = mul(UNITY_MATRIX_VP, v[1]);
					pIn.tex0 = float2(1.0f, 1.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[2]);
					//pIn.pos = mul(UNITY_MATRIX_VP, v[2]);
					pIn.tex0 = float2(0.0f, 0.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[3]);
					//pIn.pos = mul(UNITY_MATRIX_VP, v[3]);
					pIn.tex0 = float2(0.0f, 1.0f);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(p[0], pIn);
					triStream.Append(pIn);

				}

				// Fragment Shader -----------------------------------------------
				fixed4 FS_Main(FS_INPUT input): SV_Target
				{
					fixed4 color = tex2D(_MainTex, input.tex0.xy)  * _Color;
					//color.a = 0.1;
					return color;
				}

				ENDCG
			} // end pass
		}
		FallBack "Diffuse"
}
