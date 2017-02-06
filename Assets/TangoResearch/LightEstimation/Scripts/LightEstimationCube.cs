using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightEstimationCube : MonoBehaviour {


    public Cubemap _cubeMap;

    private static VectorInt2 smallestAngle(ref List<Vector3> verts, VectorInt2[] edges)
    {
        VectorInt2 result = new VectorInt2();

        float minDist = float.MaxValue;
        for (int i = 0; i < edges.Length; i++)
        {
            if (verts[edges[i].X] == Vector3.zero || verts[edges[i].Y] == Vector3.zero)
                continue;

            //float cur = Vector3.Distance(verts[edges[i].X], verts[edges[i].Y]);
            float cur = Vector3.Angle(verts[edges[i].X], verts[edges[i].Y]);

            if (cur < minDist)
            {
                minDist = cur;
                result = new VectorInt2(edges[i]);
            }
            //Debug.Log("(" + i + ") " + cur + " result: " + result.ToString());
        }

        return result;
    }

    private static Vector3 estimateLightDir(Cubemap cubeMap, Vector3 origin)
    {
        Vector3[,] negX = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeX);
        Vector3[,] posX = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveX);
        Vector3[,] negZ = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeZ);
        Vector3[,] posZ = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveZ);
        Vector3[,] negY = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeY);
        Vector3[,] posY = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveY);

        List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
            origin, new Vector2(cubeMap.width, cubeMap.height));

        Debug.DrawRay(origin, lightDirs[0] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[1] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[2] * 5, Color.blue);
        Debug.DrawRay(origin, lightDirs[3] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[4] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[5] * 5, Color.blue);

        VectorInt2[] edges =
        {
            new VectorInt2(0,1), new VectorInt2(0,2), new VectorInt2(0,4), new VectorInt2(0,5),
            new VectorInt2(1,2), new VectorInt2(1,3), new VectorInt2(1,5),
            new VectorInt2(2,3), new VectorInt2(2,4),
            new VectorInt2(3,4), new VectorInt2(3,5),
            new VectorInt2(4,5)
        };
        VectorInt2 sEdge = LightEstimationCube.smallestAngle(ref lightDirs, edges);

        float smallestAngle = Vector3.Angle(lightDirs[sEdge.X], lightDirs[sEdge.Y]);
        Debug.Log("Shortest Angle: " + smallestAngle);

        Vector3[] estimatedDirs = new Vector3[2];
        if (smallestAngle > 45f)
        {
            int first = 0;
            int second = 0;
            float max = float.MinValue;
            for (int i = 0; i < 6; i++)
            {
                float cur = lightDirs[i].magnitude;
                if (cur > max)
                {
                    max = cur;
                    second = first;
                    first = i;
                }
            }
            //estimatedDir = lightDirs[first].normalized;
            estimatedDirs[0] = lightDirs[first].normalized;
            estimatedDirs[1] = lightDirs[second].normalized;
        }
        else
        {
            estimatedDirs[0] = ((lightDirs[sEdge.X] + lightDirs[sEdge.Y]) / 2f).normalized;
        }


        Debug.DrawRay(lightDirs[sEdge.X] * 5,
            (lightDirs[sEdge.Y] - lightDirs[sEdge.X]).normalized * 5 * Vector3.Distance(lightDirs[sEdge.Y], lightDirs[sEdge.X]),
            Color.black);

        //Debug.DrawRay(origin, lightDirs[0] * 5, Color.red);
        //Debug.DrawRay(origin, lightDirs[1] * 5, Color.green);
        //Debug.DrawRay(origin, lightDirs[2] * 5, Color.blue);
        //Debug.DrawRay(origin, lightDirs[3] * 5, Color.red);
        //Debug.DrawRay(origin, lightDirs[4] * 5, Color.green);
        //Debug.DrawRay(origin, lightDirs[5] * 5, Color.blue);

        return estimatedDirs[0];
    }

    private void doLightEstimationCubemap()
    {
        GameObject go = new GameObject("LightEstimationCube");
        go.AddComponent<Camera>();
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.GetComponent<Camera>().cullingMask = 1 << 10 | 1 << 11;

        if (!go.GetComponent<Camera>().RenderToCubemap(_cubeMap))
        {
            DestroyImmediate(go);
            return;
        }

        Vector3 estimatedDir = estimateLightDir(_cubeMap, transform.position);

        DestroyImmediate(go);
    }

    private void Start()
    {
        _cubeMap = new Cubemap(32, TextureFormat.RGB24, false);
    }

    private void Update()
    {
        doLightEstimationCubemap();
    }

}
