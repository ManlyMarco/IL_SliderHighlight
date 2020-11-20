Shader "Bonelyfans"
{
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

			struct appdata
			{
				float4 col : COLOR;
	            float4 pos : POSITION;
			};
			
	        struct v2f {
				float4 col : COLOR;
	            float4 pos : SV_POSITION;
	        };

	        v2f vert (appdata v) {
	            v2f o;
	            o.pos = UnityObjectToClipPos(v.pos);
	            o.col = v.col;
	            return o;
	        }

			fixed4 frag (v2f i) : SV_Target
	        {
	            return fixed4(i.col.xyz, 1);
	        }

			ENDCG
		}
	}
}