Shader "Bonelyfans"
{
	Properties
	{
		_Color("Color", Color) = (0,0,0,1)
		[MaterialToggle] _UseMaterialColor("Use Material Color", int) = 0
	}
	SubShader
	{
		Tags{ "RenderType" = "Overlay"  "Queue" = "Overlay" }
		Pass
		{
			Cull Off
			ZWrite Off
			ZTest Always
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 _Color;
			int _UseMaterialColor;

			struct appdata
			{
				float4 col : COLOR;
				float4 pos : POSITION;
			};

			struct v2f {
				float4 col : COLOR;
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.col = v.col;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed3 _col = _Color.xyz * _UseMaterialColor;
				fixed3 _vert = i.col.xyz * (1 - _UseMaterialColor);
				return fixed4(_col + _vert, 1);
			}

			ENDCG
		}
	}
}