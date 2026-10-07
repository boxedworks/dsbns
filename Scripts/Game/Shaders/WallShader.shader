// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WallShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}

  SubShader {
    Tags{
      "RenderType"="Opaque"
    }

    ZWrite On
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
		  CGPROGRAM
      #pragma vertex vert             
      #pragma fragment frag

      struct vertInput {
          float4 pos : POSITION;
      };  

      struct vertOutput {
          float4 pos : SV_POSITION;
          float4 inPos : UV_COORD0;
      };

		  fixed4 _Color;

      vertOutput vert(vertInput input) {
          vertOutput o;
          o.pos = UnityObjectToClipPos(input.pos);
          return o;
      }

      half4 frag(vertOutput output) : SV_TARGET {
          half4 c = _Color;
          //c.r = c.r + sin(sin(output.pos.x / 50) + _SinTime + sin(output.pos.y)) - 0.5;
          //c.g = c.g + sin(output.pos.x / 10) - 0.5;
          return c;
      }
		  ENDCG
    }
  }

}