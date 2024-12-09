Shader "Custom/ObjectIDPass" {
    Properties 
    {
        _ObjectID ("Object ID", Float) = 4
    }
    SubShader 
    {
        Tags { "RenderType" = "Opaque" }
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _ObjectID;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_ObjectID, 0, 0, 1);
            }
            ENDCG
        }
    }
}
