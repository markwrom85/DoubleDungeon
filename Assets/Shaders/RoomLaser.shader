Shader "DoubleDungeon/Room Laser"
{
    Properties
    {
        _RoomBounds ("Room bounds", Vector) = (-10000, -10000, 10000, 10000)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _RoomBounds;
            struct Attributes
            {
                float4 position : POSITION;
                fixed4 color : COLOR;
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 worldPosition : TEXCOORD0;
                fixed4 color : COLOR;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.worldPosition = mul(unity_ObjectToWorld, input.position).xy;
                output.color = input.color;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                clip(input.worldPosition - _RoomBounds.xy);
                clip(_RoomBounds.zw - input.worldPosition);
                return input.color;
            }
            ENDCG
        }
    }
}
