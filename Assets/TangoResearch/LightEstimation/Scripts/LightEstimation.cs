using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightEstimation : MonoBehaviour {

    public Vector3 _EstimatedLightPos = Vector3.zero;
    public float[,,] _LightErrorGrid;

    public void SetupLightErrorGrid(int size = 5)
    {
        _LightErrorGrid = new float[size, size, size];
    }

    public Vector3 GridBasedLightEstimation(ref List<Superpixel> superpixels, int textureWidth, int textureHeight)
    {
        VectorInt3 minError = new VectorInt3(0, 0, 0);
        Vector3 minLightPos = Vector3.zero;
        float[] Io = new float[superpixels.Count];

        for (int i = 0; i < superpixels.Count; i++)
        {
            Io[i] = superpixels[i].Intensity / 255f;
        }

        for (int x = 0; x < _LightErrorGrid.GetLength(0); x++)
        {
            for (int y = 0; y < _LightErrorGrid.GetLength(1); y++)
            {
                for (int z = 0; z < _LightErrorGrid.GetLength(2); z++)
                {

                    Vector3 lightPos = Camera.main.transform.TransformPoint(
                       x - _LightErrorGrid.GetLength(0) / 2f,
                       y - _LightErrorGrid.GetLength(1) / 2f,
                       z - _LightErrorGrid.GetLength(2) / 2f);

                    float error = IoIrL2Norm(ref superpixels, Io, lightPos, textureWidth, textureHeight);
                    _LightErrorGrid[x, y, z] = error;

                    if (error < _LightErrorGrid[minError.X, minError.Y, minError.Z])
                    {
                        minError = new VectorInt3(x, y, z);
                        minLightPos = lightPos;
                    }


                }
            }
        }

        Debug.Log("LightPos: " + minLightPos + " error: " + _LightErrorGrid[minError.X, minError.Y, minError.Z]);
        return minLightPos;
    }

    public Vector3 GridBasedLightEstimation(ref List<RegionPixel> pixels, int textureWidth, int textureHeight)
    {
        VectorInt3 minError = new VectorInt3(0, 0, 0);
        Vector3 minLightPos = Vector3.zero;
        float[] Io = new float[pixels.Count];

        for (int i = 0; i < pixels.Count; i++)
        {
            Io[i] = pixels[i].Intensity / 255f;
        }

        for (int x = 0; x < _LightErrorGrid.GetLength(0); x++)
        {
            for (int y = 0; y < _LightErrorGrid.GetLength(1); y++)
            {
                for (int z = 0; z < _LightErrorGrid.GetLength(2); z++)
                {

                    Vector3 lightPos = Camera.main.transform.TransformPoint(
                        x - _LightErrorGrid.GetLength(0) / 2f,
                        y - _LightErrorGrid.GetLength(1) / 2f,
                        z - _LightErrorGrid.GetLength(2) / 2f);

                    float error = IoIrL2Norm(ref pixels, Io, lightPos, textureWidth, textureHeight);
                    _LightErrorGrid[x, y, z] = error;

                    if (error < _LightErrorGrid[minError.X, minError.Y, minError.Z])
                    {
                        minError = new VectorInt3(x, y, z);
                        minLightPos = lightPos;
                    }


                }
            }
        }

        Debug.Log("LightPos: " + minLightPos + " error: " + _LightErrorGrid[minError.X, minError.Y, minError.Z]);
        return minLightPos;
    }

    public Vector3 GridBasedLightEstimation(ref List<RegionPixel> pixels, int textureWidth, int textureHeight, ref List<Vector3> candidateDir)
    {
        float minError = float.MaxValue;
        Vector3 minLightPos = Vector3.zero;
        float[] Io = new float[pixels.Count];

        for (int i = 0; i < pixels.Count; i++)
        {
            Io[i] = pixels[i].Intensity / 255f;
        }

        for (int i = 0; i < candidateDir.Count; i++)
        {
            Vector3 lightPos = candidateDir[i];

            float error = IoIrL2Norm(ref pixels, Io, lightPos, textureWidth, textureHeight);

            Debug.Log("LightPos: " + lightPos + " error: " + error);

            if (error < minError)
            {
                minError = error;
                minLightPos = lightPos;
            }
        }

        Debug.Log("LightPos: " + minLightPos + " error: " + minError);
        return minLightPos;
    }

    /// <summary>
    /// Estimates the light depth along a given direction.
    /// </summary>
    /// <param name="pixels">The pixels of the current frame.</param>
    /// <param name="textureWidth">Width of the texture.</param>
    /// <param name="textureHeight">Height of the texture.</param>
    /// <param name="origin">The origin (Camera position).</param>
    /// <param name="direction">The normalized light direction.</param>
    /// <param name="maxDepth">The maximum light depth.</param>
    /// <returns></returns>
    public Vector3 EstimateLightDepth(ref List<RegionPixel> pixels, int textureWidth, int textureHeight, Vector3 origin, Vector3 direction, float maxDepth = 10f)
    {
        float minError = float.MaxValue;
        Vector3 result = origin;
        float[] Io = new float[pixels.Count];

        for (int i = 0; i < pixels.Count; i++)
        {
            Io[i] = pixels[i].Intensity / 255f;
        }

        int t = 1;
        for (int i = 1; i <= maxDepth; i++)
        {
            Vector3 curDepth = direction * i;

            float error = IoIrL2Norm(ref pixels, Io, curDepth, textureWidth, textureHeight);

            if (error < minError)
            {
                minError = error;
                result = curDepth;
                t = i;
            }

        }

        return result;
    }

    public static float IoIrL2Norm(ref List<Superpixel> superpixels, float[] Io, Vector3 lightPos, int textureWidth, int textureHeight)
    {
        float[] Ir = new float[superpixels.Count];

        float dist = 0;
        for (int i = 0; i < superpixels.Count; i++)
        {
            if (superpixels[i].Normal.magnitude <= 0)
            {
                continue;
            }

            float ns = 0;
            float albedo = 0;
            float ir = 0;
            Vector3 lightDir = ImageProcessing.LightDirection(lightPos, superpixels[i].WorldPoint);
            ImageProcessing.ComputeAlbedo(Io[i], superpixels[i].Normal, lightDir, out ns, out albedo);
            if (ns > 0)
            {
                ImageProcessing.ComputeImageIntensity(albedo, superpixels[i].Normal, lightDir, out ir);
            }
            else
            {
                if (!superpixels[i].GetMedianSynthesizedIr(textureWidth, textureHeight, lightPos, out albedo, out ir))
                {
                    ir = Io[i];
                }
            }


            Ir[i] = ir;
            dist += Mathf.Pow(Io[i] - Ir[i], 2);
        }
        dist = Mathf.Pow(dist, 0.5f);

        //if (_debuggingLightPos)
        //{
        //    Debug.Log("IoIr: " + dist);
        //}

        return dist;
    }

    public static float IoIrL2Norm(ref List<RegionPixel> pixels, float[] Io, Vector3 lightPos, int textureWidth, int textureHeight)
    {
        float[] Ir = new float[pixels.Count];

        float dist = 0;
        for (int i = 0; i < pixels.Count; i++)
        {
            if (pixels[i].Normal.magnitude <= 0)
            {
                continue;
            }

            float ns = 0;
            float albedo = 0;
            float ir = 0;
            Vector3 lightDir = ImageProcessing.LightDirection(lightPos, pixels[i].WorldPoint);
            ImageProcessing.ComputeAlbedo(Io[i], pixels[i].Normal, lightDir, out ns, out albedo);

            if (ns > 0)
            {
                if (albedo > 2.5)
                {
                    continue;
                }
                ImageProcessing.ComputeImageIntensity(albedo, pixels[i].Normal, lightDir, out ir);
            }
            else
            {
                //ir = 0;
                ir = Io[i];
            }

            Ir[i] = ir;
            dist += Mathf.Pow(Io[i] - Ir[i], 2);
        }
        dist = Mathf.Pow(dist, 0.5f);

        //if (_debuggingLightPos)
        //{
        //    Debug.Log("IoIr: " + dist);
        //}

        return dist;
    }

    /// <summary>
    /// Given each face of a cubemap, returns six candidate light directions.
    /// </summary>
    /// <param name="negX">The negative x face.</param>
    /// <param name="posX">The position x face.</param>
    /// <param name="negY">The negative y face.</param>
    /// <param name="posY">The position y face.</param>
    /// <param name="negZ">The negative z face.</param>
    /// <param name="posZ">The position z face.</param>
    /// <param name="camPos">The camera position.</param>
    /// <param name="cubemapSize">Size (width and height) of the cubemap.</param>
    /// <returns>A list of six candidate light directions.</returns>
    public List<Vector3> CandidateLightDirections(Vector3[,] negX, Vector3[,] posX,
        Vector3[,] negY, Vector3[,] posY,
        Vector3[,] negZ, Vector3[,] posZ,
        Vector3 camPos,
        Vector2 cubemapSize)
    {
        List<Vector3> candidateDirections = new List<Vector3>(6);
        float[] faceIntensity = new float[6];

        Vector2 b = new Vector2();
        Vector3 dir = new Vector3();

        float faceAve = 0f;
        float cubeAve = 0f;

        // Positive directions.
        b = ImageProcessing.BrightestPoint(posX, out faceAve);
        posX[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.red);
        dir = new Vector3(
            0.5f,
            b.y / cubemapSize.y - 0.5f,
            (1f - b.x / cubemapSize.x) - 0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[0] = faceAve;

        b = ImageProcessing.BrightestPoint(posY, out faceAve);
        posY[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.green);
        dir = new Vector3(
            b.x / cubemapSize.x - 0.5f,
            0.5f,
            (1f - b.y / cubemapSize.y) - 0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[1] = faceAve;

        b = ImageProcessing.BrightestPoint(posZ, out faceAve);
        posZ[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.blue);
        dir = new Vector3(
            b.x / cubemapSize.x - 0.5f,
            b.y / cubemapSize.y - 0.5f,
            0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[2] = faceAve;

        // Negative directions.
        b = ImageProcessing.BrightestPoint(negX, out faceAve);
        negX[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.red);
        dir = new Vector3(
            -0.5f,
            b.y / cubemapSize.y - 0.5f,
            b.x / cubemapSize.x - 0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[3] = faceAve;

        b = ImageProcessing.BrightestPoint(negY, out faceAve);
        negY[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.green);
        dir = new Vector3(
            b.x / cubemapSize.x - 0.5f,
            -0.5f,
            b.y / cubemapSize.y - 0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[4] = faceAve;

        b = ImageProcessing.BrightestPoint(negZ, out faceAve);
        negZ[(int)b.x, (int)b.y] = ImageProcessing.ColorToVector3(Color.blue);
        dir = new Vector3(
            (1f - b.x / cubemapSize.x) - 0.5f,
            b.y / cubemapSize.y - 0.5f,
            -0.5f) - camPos;
        dir.Normalize();
        dir *= ImageProcessing.Grayscale(ImageProcessing.Vector3ToColor(posX[(int)b.x, (int)b.y])) / 1f;
        candidateDirections.Add(dir);
        cubeAve += faceAve;
        faceIntensity[5] = faceAve;

        cubeAve /= 6f;

        for (int i = 0; i < 6; i++)
        {
            if (faceIntensity[i] < cubeAve)
            {
                candidateDirections[i] = Vector3.zero;
            }
        }

        return candidateDirections;
    }


}
