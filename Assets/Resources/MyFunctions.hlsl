bool IsRayTriangleIntersect(float3 rayFrom, float3 rayTo, float3 t0, float3 t1, float3 t2)
{
    const float EPSILON = 1e-6f;

    float3 dir = rayTo - rayFrom;

    float3 edge1 = t1 - t0;
    float3 edge2 = t2 - t0;

    float3 pvec = cross(dir, edge2);
    float det = dot(edge1, pvec);

    // レイが三角形と平行
    if (abs(det) < EPSILON)
        return false;

    float invDet = 1.0f / det;

    float3 tvec = rayFrom - t0;
    float u = dot(tvec, pvec) * invDet;
    if (u < 0.0f || u > 1.0f)
        return false;

    float3 qvec = cross(tvec, edge1);
    float v = dot(dir, qvec) * invDet;
    if (v < 0.0f || u + v > 1.0f)
        return false;

    float t = dot(edge2, qvec) * invDet;

    return (t >= 0.0f && t <= 1.0f);
}

bool IsInsideTriangle(float2 p, float2 t0, float2 t1, float2 t2)
{
    float2 v0 = t1 - t0;
    float2 v1 = t2 - t0;
    float2 v2 = p  - t0;

    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);

    float denom = d00 * d11 - d01 * d01;
    if (abs(denom) < 1e-8f)
        return false; // 退化三角形

    float invDenom = 1.0f / denom;
    float u = (d11 * d20 - d01 * d21) * invDenom;
    float v = (d00 * d21 - d01 * d20) * invDenom;

    return (u >= 0.0f) && (v >= 0.0f) && (u + v <= 1.0f);
}