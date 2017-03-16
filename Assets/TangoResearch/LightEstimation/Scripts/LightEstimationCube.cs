using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightEstimationCube : MonoBehaviour {


    public int _CubeSize = 32;
    public Cubemap _cubeMap;


    public Texture2D _t2DPosX;
    public Texture2D _t2DNegX;
    public Texture2D _t2DPosY;
    public Texture2D _t2DNegY;
    public Texture2D _t2DPosZ;
    public Texture2D _t2DNegZ;

    private void writeToTexture(Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ)
    {
        for (int i = 0; i < _CubeSize; i++)
        {
            for (int j = 0; j < _CubeSize; j++)
            {
                _t2DPosX.SetPixel(i, j, ImageProcessing.Vector3ToColor(posX[i, j]));
                _t2DNegX.SetPixel(i, j, ImageProcessing.Vector3ToColor(negX[i, j]));
                _t2DPosY.SetPixel(i, j, ImageProcessing.Vector3ToColor(posY[i, j]));
                _t2DNegY.SetPixel(i, j, ImageProcessing.Vector3ToColor(negY[i, j]));
                _t2DPosZ.SetPixel(i, j, ImageProcessing.Vector3ToColor(posZ[i, j]));
                _t2DNegZ.SetPixel(i, j, ImageProcessing.Vector3ToColor(negZ[i, j]));
            }
        }
        _t2DPosX.Apply();
        _t2DNegX.Apply();
        _t2DPosY.Apply();
        _t2DNegY.Apply();
        _t2DPosZ.Apply();
        _t2DNegZ.Apply();

    }

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

    private static List<List<FaceNode>> GetLocalLights(params FaceNode[] faces)
    {
        List<List<FaceNode>> clusters = new List<List<FaceNode>>();
        
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i]._Visited || faces[i]._Direction.magnitude <= float.Epsilon) continue;
            faces[i]._Visited = true;

            List<FaceNode> c = new List<FaceNode>();
            c.Add(faces[i]);
            foreach (var item in faces[i]._NeiborAngles)
            {
                if (item.Key._Visited) continue;
                c.Add(item.Key);
                item.Key._Visited = true;
            }

            clusters.Add(c);
        }

        return clusters;
    }

    private Vector3 estimateLightDir(Cubemap cubeMap, Vector3 origin)
    {
        Vector3[,] negX = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeX);
        Vector3[,] posX = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveX);
        Vector3[,] negZ = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeZ);
        Vector3[,] posZ = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveZ);
        Vector3[,] negY = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.NegativeY);
        Vector3[,] posY = ImageProcessing.CubemapFaceTo2DVector3Array(cubeMap, CubemapFace.PositiveY);

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            writeToTexture(posX, negX, posY, negY, posZ, negZ);
        }

        return EstimateLightDirSimple(posX, negX, posY, negY, posZ, negZ, origin);

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

    public static Vector3 EstimateLightDirSimple(Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ,
        Vector3 origin)
    {
        List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
            origin, new Vector2(posX.GetLength(0), posX.GetLength(1)));

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

        return estimatedDirs[0];
    }

    public static List<Vector3> EstimateLightDir(
        Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ,
        Vector3[,] posX45, Vector3[,] negX45,
        Vector3[,] posY45, Vector3[,] negY45,
        Vector3[,] posZ45, Vector3[,] negZ45,
        Vector3 origin,
        float clusterAngle)
    {
        List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
            origin, new Vector2(posX.GetLength(0), posX.GetLength(1)));

        List<Vector3> lightDirs45 = LightEstimation.CandidateLightDirections(negX45, posX45, negY45, posY45, negZ45, posZ45,
            origin, new Vector2(posX45.GetLength(0), posX45.GetLength(1)));

        for (int i = 0; i < lightDirs45.Count; i++)
        {
            lightDirs45[i] = Quaternion.Euler(0, 45, 0) * lightDirs45[i];
            lightDirs[i] = (lightDirs[i] + lightDirs45[i])/ 2f;
        }

        Debug.DrawRay(origin, lightDirs[0] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[1] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[2] * 5, Color.blue);
        Debug.DrawRay(origin, lightDirs[3] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[4] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[5] * 5, Color.blue);

        Debug.DrawRay(origin, lightDirs45[0] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs45[1] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs45[2] * 5, Color.blue);
        Debug.DrawRay(origin, lightDirs45[3] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs45[4] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs45[5] * 5, Color.blue);

        FaceNode[] faces = FaceNode.CreateFaceNodes(clusterAngle,
            lightDirs[0], lightDirs[1],
            lightDirs[2], lightDirs[3],
            lightDirs[4], lightDirs[5]);

        List<List<FaceNode>> clusters = GetLocalLights(faces);
        Debug.Log("Angle " + clusterAngle + " clusters: " + clusters.Count);

        List<Vector3> estimatedDirections = new List<Vector3>(clusters.Count);
        foreach (List<FaceNode> l in clusters)
        {
            Vector3 dir = Vector3.zero;
            foreach (FaceNode i in l)
            {
                dir += i._Direction;
            }
            dir /= l.Count;
            estimatedDirections.Add(dir);
        }

        return estimatedDirections;

        //VectorInt2[] edges =
        //{
        //    new VectorInt2(0,1), new VectorInt2(0,2), new VectorInt2(0,4), new VectorInt2(0,5),
        //    new VectorInt2(1,2), new VectorInt2(1,3), new VectorInt2(1,5),
        //    new VectorInt2(2,3), new VectorInt2(2,4),
        //    new VectorInt2(3,4), new VectorInt2(3,5),
        //    new VectorInt2(4,5)
        //};
        //VectorInt2 sEdge = LightEstimationCube.smallestAngle(ref lightDirs, edges);

        //float smallestAngle = Vector3.Angle(lightDirs[sEdge.X], lightDirs[sEdge.Y]);
        //Debug.Log("Shortest Angle: " + smallestAngle);

        //Vector3[] estimatedDirs = new Vector3[2];
        //if (smallestAngle > 45f)
        //{
        //    int first = 0;
        //    int second = 0;
        //    float max = float.MinValue;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        float cur = lightDirs[i].magnitude;
        //        if (cur > max)
        //        {
        //            max = cur;
        //            second = first;
        //            first = i;
        //        }
        //    }
        //    //estimatedDir = lightDirs[first].normalized;
        //    estimatedDirs[0] = lightDirs[first].normalized;
        //    estimatedDirs[1] = lightDirs[second].normalized;
        //}
        //else
        //{
        //    estimatedDirs[0] = ((lightDirs[sEdge.X] + lightDirs[sEdge.Y]) / 2f).normalized;
        //}


        //Debug.DrawRay(lightDirs[sEdge.X] * 5,
        //    (lightDirs[sEdge.Y] - lightDirs[sEdge.X]).normalized * 5 * Vector3.Distance(lightDirs[sEdge.Y], lightDirs[sEdge.X]),
        //    Color.black);

        //return estimatedDirs[0];
    }

    public static List<Vector3> EstimateLightDir(Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ,
        Vector3 origin,
        float clusterAngle)
    {
        List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
            origin, new Vector2(posX.GetLength(0), posX.GetLength(1)));

        Debug.DrawRay(origin, lightDirs[0] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[1] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[2] * 5, Color.blue);
        Debug.DrawRay(origin, lightDirs[3] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[4] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[5] * 5, Color.blue);

        FaceNode[] faces = FaceNode.CreateFaceNodes(clusterAngle, 
            lightDirs[0], lightDirs[1],
            lightDirs[2], lightDirs[3],
            lightDirs[4], lightDirs[5]);

        List<List<FaceNode>> clusters = GetLocalLights(faces);
        Debug.Log("Angle " + clusterAngle + " clusters: " + clusters.Count);

        List<Vector3> estimatedDirections = new List<Vector3>(clusters.Count);
        foreach (List<FaceNode> l in clusters)
        {
            Vector3 dir = Vector3.zero;
            foreach (FaceNode i in l)
            {
                dir += i._Direction;
            }
            dir /= l.Count;
            estimatedDirections.Add(dir);
        }
        return estimatedDirections;

        //int min = 0;
        //float minNS = float.MaxValue;
        //int count = 0;
        //for (int i = 0; i < faces.Length; i++)
        //{
        //    if (faces[i]._Direction.magnitude <= 0 || faces[i]._NeiborAngles.Count <= 0)
        //        continue;
        //    float cur = faces[i].GetNeighborSize();
        //    if (cur < minNS){
        //        minNS = cur;
        //        min = i;
        //    }
        //    count++;
        //}

        //if (count < 1)
        //{
        //    int first = 0;
        //    int second = 0;
        //    float max = float.MinValue;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        float cur = lightDirs[i].magnitude;
        //        if (cur > max)
        //        {
        //            max = cur;
        //            second = first;
        //            first = i;
        //        }
        //    }
            
        //    //estimatedDirs[0] = lightDirs[first].normalized;
        //    //estimatedDirs[1] = lightDirs[second].normalized;

        //    return lightDirs[first].normalized;
        //}

        ////Debug.Log(faces[min]._Direction + " " + faces[min]._NeiborAngles.Count + " " + faces[min].GetNeighborSize(45));

        //return faces[min]._Direction.normalized;

        //VectorInt2[] edges =
        //{
        //    new VectorInt2(0,1), new VectorInt2(0,2), new VectorInt2(0,4), new VectorInt2(0,5),
        //    new VectorInt2(1,2), new VectorInt2(1,3), new VectorInt2(1,5),
        //    new VectorInt2(2,3), new VectorInt2(2,4),
        //    new VectorInt2(3,4), new VectorInt2(3,5),
        //    new VectorInt2(4,5)
        //};
        //VectorInt2 sEdge = LightEstimationCube.smallestAngle(ref lightDirs, edges);

        //float smallestAngle = Vector3.Angle(lightDirs[sEdge.X], lightDirs[sEdge.Y]);
        //Debug.Log("Shortest Angle: " + smallestAngle);

        //Vector3[] estimatedDirs = new Vector3[2];
        //if (smallestAngle > 45f)
        //{
        //    int first = 0;
        //    int second = 0;
        //    float max = float.MinValue;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        float cur = lightDirs[i].magnitude;
        //        if (cur > max)
        //        {
        //            max = cur;
        //            second = first;
        //            first = i;
        //        }
        //    }
        //    //estimatedDir = lightDirs[first].normalized;
        //    estimatedDirs[0] = lightDirs[first].normalized;
        //    estimatedDirs[1] = lightDirs[second].normalized;
        //}
        //else
        //{
        //    estimatedDirs[0] = ((lightDirs[sEdge.X] + lightDirs[sEdge.Y]) / 2f).normalized;
        //}


        //Debug.DrawRay(lightDirs[sEdge.X] * 5,
        //    (lightDirs[sEdge.Y] - lightDirs[sEdge.X]).normalized * 5 * Vector3.Distance(lightDirs[sEdge.Y], lightDirs[sEdge.X]),
        //    Color.black);

        //return estimatedDirs[0];
    }

    public static List<LightInfo> EstimateLightDir_SampleNeighbor(
        Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ,
        float[,] posXMin, float[,] negXMin,
        float[,] posYMin, float[,] negYMin,
        float[,] posZMin, float[,] negZMin,
        Vector3 origin,
        float clusterAngle)
    {
        //List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
        //    origin, new Vector2(posX.GetLength(0), posX.GetLength(1)));

        List<Vector3> lightDirs = LightEstimation.CandidateLightDirections(negX, posX, negY, posY, negZ, posZ,
            negXMin, posXMin, negYMin, posYMin, negZMin, posZMin,
            origin, new Vector2(posX.GetLength(0), posX.GetLength(1)));

        List<LightInfo> lightDirsN = LightEstimation.SampleNeighbors(lightDirs, negX, posX, negY, posY, negZ, posZ, 5);


        Debug.DrawRay(origin, lightDirs[0] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[1] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[2] * 5, Color.blue);
        Debug.DrawRay(origin, lightDirs[3] * 5, Color.red);
        Debug.DrawRay(origin, lightDirs[4] * 5, Color.green);
        Debug.DrawRay(origin, lightDirs[5] * 5, Color.blue);

        //Debug.DrawRay(origin, lightDirsN[0] * 5, Color.Lerp(Color.red, Color.white, 0.75f));
        //Debug.DrawRay(origin, lightDirsN[1] * 5, Color.Lerp(Color.green, Color.white, 0.75f));
        //Debug.DrawRay(origin, lightDirsN[2] * 5, Color.Lerp(Color.blue, Color.white, 0.75f));
        //Debug.DrawRay(origin, lightDirsN[3] * 5, Color.Lerp(Color.red, Color.white, 0.75f));
        //Debug.DrawRay(origin, lightDirsN[4] * 5, Color.Lerp(Color.green, Color.white, 0.75f));
        //Debug.DrawRay(origin, lightDirsN[5] * 5, Color.Lerp(Color.blue, Color.white, 0.75f));
        for (int i = 0; i < lightDirsN.Count; i++)
        {
            Debug.DrawRay(origin, lightDirsN[i]._Direction * 5, Color.white);
        }

        lightDirsN.Sort((a, b) => a._Magnitude.CompareTo(b._Magnitude));

        return lightDirsN;

        //VectorInt2[] edges =
        //{
        //    new VectorInt2(0,1), new VectorInt2(0,2), new VectorInt2(0,4), new VectorInt2(0,5),
        //    new VectorInt2(1,2), new VectorInt2(1,3), new VectorInt2(1,5),
        //    new VectorInt2(2,3), new VectorInt2(2,4),
        //    new VectorInt2(3,4), new VectorInt2(3,5),
        //    new VectorInt2(4,5)
        //};
        //VectorInt2 sEdge = LightEstimationCube.smallestAngle(ref lightDirsN, edges);

        //float smallestAngle = Vector3.Angle(lightDirsN[sEdge.X], lightDirsN[sEdge.Y]);
        //Debug.Log("Shortest Angle: " + smallestAngle);

        //Vector3[] estimatedDirs = new Vector3[2];
        //if (smallestAngle > clusterAngle)
        //{
        //    int first = 0;
        //    int second = 0;
        //    float max = float.MinValue;
        //    for (int i = 0; i < 6; i++)
        //    {
        //        float cur = lightDirsN[i].magnitude;
        //        if (cur > max)
        //        {
        //            max = cur;
        //            second = first;
        //            first = i;
        //        }
        //    }
        //    estimatedDirs[0] = lightDirsN[first].normalized;
        //    estimatedDirs[1] = lightDirsN[second].normalized;
        //}
        //else
        //{
        //    estimatedDirs[0] = ((lightDirsN[sEdge.X] + lightDirsN[sEdge.Y]) / 2f).normalized;
        //}


        //Debug.DrawRay(lightDirsN[sEdge.X] * 5,
        //    (lightDirsN[sEdge.Y] - lightDirsN[sEdge.X]).normalized * 5 * Vector3.Distance(lightDirsN[sEdge.Y], lightDirsN[sEdge.X]),
        //    Color.black);

        //return estimatedDirs[0];


    }

    private void doLightEstimationCubemap()
    {
        GameObject go = new GameObject("LightEstimationCube");
        go.AddComponent<Camera>();
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.GetComponent<Camera>().cullingMask = 1 << LayerMask.NameToLayer("Misc");

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
        _cubeMap = new Cubemap(_CubeSize, TextureFormat.RGB24, false);

        _t2DPosX = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegX = new Texture2D(_CubeSize, _CubeSize);
        _t2DPosY = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegY = new Texture2D(_CubeSize, _CubeSize);
        _t2DPosZ = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegZ = new Texture2D(_CubeSize, _CubeSize);
    }

    private void Update()
    {
        doLightEstimationCubemap();
    }

}
