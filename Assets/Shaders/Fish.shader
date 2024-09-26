Shader "Unlit/Fish"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FinColor ("Fin Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 5
        _EyeSize ("Eye Size", Float) = 2
        _FinWidth ("Fin Width", Float) = 5
        _FinHeight ("Fin Height", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            fixed4 _FinColor;

            float _OutlineWidth;
            float _EyeSize;
            float _FinWidth;
            float _FinHeight;

            float4 _SpinePoints[10];
            float4 _OutlinePoints[25];
            float4 _EyePoints[2];

            float4 _FinPoints[4];
            float _FinAngles[2];
            float _FinSizeMult[2];

            bool _DrawSpine;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Function to calculate the minimum distance from a point to a line segment
            float pointLineDistance(float2 p, float3 a, float3 b)
            {
                float2 ap = p - a;
                float2 ab = b - a;
                float ab2 = dot(ab, ab);
                float t = saturate(dot(ap, ab) / ab2);
                float2 closest = a + t * ab;
                return distance(p, closest);
            }

            float3 catmullRom(float3 p0, float3 p1, float3 p2, float3 p3, float t)
            {
                return 0.5 * (2.0 * p1 +
                              (-p0 + p2) * t +
                              (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t * t +
                              (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t * t * t);
            }

            bool pointInPolygon(float2 p, float4 outlinePoints[25], int numPoints)
            {
                bool inside = false;
                for (int i = 0, j = numPoints - 1; i < numPoints; j = i++) 
                {
                    float2 pi = outlinePoints[i].xy;
                    float2 pj = outlinePoints[j].xy;

                    // Check if the point is inside using the ray-casting method
                    if (((pi.y > p.y) != (pj.y > p.y)) &&
                        (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    {
                        inside = !inside;
                    }
                }
                return inside;
            }

            bool pointInEllipse(float2 p, float2 center, float2 axes, float angle)
            {
                // Translate point relative to the center of the ellipse
                float2 translated = p - center;

                // Rotate the point by the negative of the ellipse's angle
                float cosAngle = cos(-angle);
                float sinAngle = sin(-angle);
                float2 rotated = float2(
                    translated.x * cosAngle - translated.y * sinAngle,
                    translated.x * sinAngle + translated.y * cosAngle
                );

                // Check if the point is within the ellipse equation
                float ellipseEquation = (rotated.x * rotated.x) / (axes.x * axes.x) + 
                                        (rotated.y * rotated.y) / (axes.y * axes.y);

                return ellipseEquation <= 1.0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float minDist = 1.0;
                bool isSpinePoint = false;
                bool isEye = false;
                bool isFin = false;

                // Spine
                if (_DrawSpine)
                {
                    // Loop through each pair of points and calculate distance to the line segment
                    for (int j = 0; j < 10; j++)
                    {
                        float3 p0 = _SpinePoints[j].xyz;

                        if (j < 9)
                        {
                            float3 p1 = _SpinePoints[j + 1].xyz;
                            float dist = pointLineDistance(i.uv, p0, p1);
                            minDist = min(minDist, dist);
                        }
                    
                        if (distance(i.uv, p0.xy) < 0.002)
                        {
                            isSpinePoint = true;
                        }
                    }
                }

                // Outline
                for (int j = 0; j < 25; j++)
                {
                    float3 p0 = _OutlinePoints[max(j-1, 0)].xyz;
                    float3 p1 = _OutlinePoints[j].xyz;
                    float3 p2 = _OutlinePoints[min(j+1, 24)].xyz;
                    float3 p3 = _OutlinePoints[min(j+2, 24)].xyz;

                    // Interpolate along the curve
                    for (float t = 0.0; t <= 1.0; t += 0.1)
                    {
                        float3 curvePoint = catmullRom(p0, p1, p2, p3, t);
                        float dist = distance(i.uv, curvePoint.xy);
                        minDist = min(minDist, dist);
                    }
                }

                // Eyes
                for (int j = 0; j < 2; j++)
                {
                    float3 p = _EyePoints[j].xyz;

                    if (distance(i.uv, p.xy) < _EyeSize * 0.001)
                    {
                        isEye = true;
                    }
                }

                // Define parameters for multiple ellipses (fins)
                float2 axes = float2(_FinWidth * 0.01, _FinHeight * 0.01);
                float angles[4] = { 0.0, 0.0, 0.0, 0.0 };       // Different rotations
                
                angles[0] = radians(_FinAngles[0] - 30);
                angles[1] = radians(_FinAngles[0] + 30);
                angles[2] = radians(_FinAngles[1] - 30);
                angles[3] = radians(_FinAngles[1] + 30);

                // Loop through each fin and check if the current fragment is inside any of them
                for (int j = 0; j < 4; j++)
                {
                    if (pointInEllipse(i.uv, _FinPoints[j], axes, angles[j]))
                    {
                        isFin = true;
                    }
                }

                // Draw outline, spine, eyes
                if (minDist < (_OutlineWidth * 0.01) || isSpinePoint || isEye)
                {
                    return _OutlineColor;
                }
                // Draw color
                else if (pointInPolygon(i.uv, _OutlinePoints, 25))
                {
                    return _Color;
                }
                else if (isFin)
                {
                    return _FinColor;
                }

                // Otherwise, make the fragment transparent
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
