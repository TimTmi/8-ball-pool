// Fake-sphere ball shading for the top-down view. The flat Ball_XX sprite stays on the
// SpriteRenderer as the quad; this shader ignores its texture and instead treats every
// pixel of the quad as a point on a hemisphere, rotates that direction into the ball's
// own frame (BallRoll feeds _Rotation), and samples the equirectangular BallMap_XX.
Shader "EightBall/BallRoll"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _MapTex ("Surface Map", 2D) = "white" {}
        [HideInInspector] _Rotation ("Ball Rotation", Vector) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MapTex;
            float4 _Rotation;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            // Rotate v by the unit quaternion q = (x, y, z, w).
            float3 RotateByQuaternion(float4 q, float3 v)
            {
                float3 t = 2.0 * cross(q.xyz, v);
                return v + q.w * t + cross(q.xyz, t);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Quad space: 0 at the centre, 1 at the sprite edge. Outside the unit
                // circle there is no ball surface.
                float2 offset = IN.texcoord * 2.0 - 1.0;
                float r2 = dot(offset, offset);
                float edgeDistance = 1.0 - sqrt(r2);

                // The visible surface normal. The camera looks straight down, so x and y
                // come from the quad and z closes the unit length, pointing at the camera.
                float3 viewNormal = float3(offset.x, offset.y, sqrt(max(0.0, 1.0 - r2)));

                // Where that direction sits on the ball's own surface...
                float3 local = RotateByQuaternion(_Rotation, viewNormal);

                // ...as equirectangular map coordinates. The map is uniform in longitude
                // away from the number patches, so the atan2 seam at u = 0/1 is invisible.
                float2 mapUv;
                mapUv.x = atan2(local.x, local.z) * (0.5 / 3.14159265) + 0.5;
                mapUv.y = asin(clamp(local.y, -1.0, 1.0)) * (1.0 / 3.14159265) + 0.5;

                fixed4 map = tex2D(_MapTex, mapUv);

                // Cheap headlight shading so the disc reads as a sphere.
                map.rgb *= 0.65 + 0.35 * viewNormal.z;

                // Antialiased silhouette against whatever the ball sits on.
                float alpha = saturate(edgeDistance / max(fwidth(edgeDistance), 1e-5));

                // Sprite blending expects premultiplied output (Blend One OneMinusSrcAlpha).
                fixed4 result;
                result.rgb = map.rgb * IN.color.rgb * alpha;
                result.a = map.a * IN.color.a * alpha;
                return result;
            }
            ENDCG
        }
    }
}
