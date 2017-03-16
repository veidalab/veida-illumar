using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tango;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Events;
using System.IO;

public class TangoCubemap : MonoBehaviour
{

    private int _screenWidth = 1280;
    private int _screenHeight = 720;

    public int _CubeSize = 32;

    public bool _45DegreeCube = false;

    public Vector3[,] _posX;
    public Vector3[,] _negX;
    public Vector3[,] _posY;
    public Vector3[,] _negY;
    public Vector3[,] _posZ;
    public Vector3[,] _negZ;

    public Vector3[,] _posX45;
    public Vector3[,] _negX45;
    public Vector3[,] _posY45;
    public Vector3[,] _negY45;
    public Vector3[,] _posZ45;
    public Vector3[,] _negZ45;

    public float[,] _posXMin;
    public float[,] _negXMin;
    public float[,] _posYMin;
    public float[,] _negYMin;
    public float[,] _posZMin;
    public float[,] _negZMin;

    private int _dirtyPixels = 0;
    private float _fillPercentage = 0f;
    private bool _paused = false;

    public Text _TextFillPercentage;
    public Button _ButtonClear;
    public Button _ButtonCapture;
    public Button _ButtonExport;
    public Toggle _TogglePause;
    public RawImage _RPosX;
    public RawImage _RNegX;
    public RawImage _RPosY;
    public RawImage _RNegY;
    public RawImage _RPosZ;
    public RawImage _RNegZ;

    public Texture2D _t2DPosX;
    public Texture2D _t2DNegX;
    public Texture2D _t2DPosY;
    public Texture2D _t2DNegY;
    public Texture2D _t2DPosZ;
    public Texture2D _t2DNegZ;


    void Start ()
    {
        _screenWidth = Camera.main.pixelWidth;
        _screenHeight = Camera.main.pixelHeight;

        _t2DPosX = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegX = new Texture2D(_CubeSize, _CubeSize);
        _t2DPosY = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegY = new Texture2D(_CubeSize, _CubeSize);
        _t2DPosZ = new Texture2D(_CubeSize, _CubeSize);
        _t2DNegZ = new Texture2D(_CubeSize, _CubeSize);

        _posX = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negX = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _posY = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negY = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _posZ = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negZ = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));

        _posXMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negXMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _posYMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negYMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _posZMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negZMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);

        _posX45 = new Vector3[_CubeSize, _CubeSize];
        _negX45 = new Vector3[_CubeSize, _CubeSize];
        _posY45 = new Vector3[_CubeSize, _CubeSize];
        _negY45 = new Vector3[_CubeSize, _CubeSize];
        _posZ45 = new Vector3[_CubeSize, _CubeSize];
        _negZ45 = new Vector3[_CubeSize, _CubeSize];

        _ButtonClear.onClick.AddListener(onClickClear);
        _ButtonCapture.onClick.AddListener(onClickCapture);
        _ButtonExport.onClick.AddListener(onClickButtonExport);
        _TogglePause.onValueChanged.AddListener(value => onValueChangedPause(value));

        //Debug.Log("45: " + _posX45.GetLength(0) + " " + _posX45.GetLength(1));

        //Vector3 expected = new Vector3(0, 0, 1);
        //Vector2 polar = CartesianToPolarCoordinates(expected);
        //float sinLat = Mathf.Sin(polar.y * Mathf.Deg2Rad);
        //Vector3 actual = new Vector3(
        //    sinLat * Mathf.Sin(polar.x * Mathf.Deg2Rad),
        //    Mathf.Cos(polar.y * Mathf.Deg2Rad),
        //    sinLat * Mathf.Cos(polar.x * Mathf.Deg2Rad));

        //Debug.Log("Lng/Lat: " + polar + " Expected vs Actual: " + expected + " " + actual);

        //expected = new Vector3(0, 1, 0);
        //polar = CartesianToPolarCoordinates(expected);
        //sinLat = Mathf.Sin(polar.y * Mathf.Deg2Rad);
        //actual = new Vector3(
        //    sinLat * Mathf.Sin(polar.x * Mathf.Deg2Rad),
        //    Mathf.Cos(polar.y * Mathf.Deg2Rad),
        //    sinLat * Mathf.Cos(polar.x * Mathf.Deg2Rad));

        //Debug.Log("Lng/Lat: " + polar + " Expected vs Actual: " + expected + " " + actual);

        //expected = new Vector3(1, 0, 0);
        //polar = CartesianToPolarCoordinates(expected);
        //sinLat = Mathf.Sin(polar.y * Mathf.Deg2Rad);
        //actual = new Vector3(
        //    sinLat * Mathf.Sin(polar.x * Mathf.Deg2Rad),
        //    Mathf.Cos(polar.y * Mathf.Deg2Rad),
        //    sinLat * Mathf.Cos(polar.x * Mathf.Deg2Rad));

        //Debug.Log("Lng/Lat: " + polar + " Expected vs Actual: " + expected + " " + actual);


    }

    private void onClickButtonExport()
    {
        exportFaces();
    }

    private void onClickCapture()
    {
        //updateCubemap();
        WriteToTexture();
    }

    private void onClickClear()
    {
        _posX = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negX = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _posY = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negY = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _posZ = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));
        _negZ = GetNew2DArray(_CubeSize, _CubeSize, new Vector3(-1, -1, -1));

        _posXMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negXMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _posYMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negYMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _posZMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);
        _negZMin = GetNew2DArray(_CubeSize, _CubeSize, float.MaxValue);

        _dirtyPixels = 0;
    }

    private void onValueChangedPause(bool val)
    {
        _paused = val;
    }

    public static T[,] GetNew2DArray<T>(int x, int y, T initialValue)
    {
        T[,] nums = new T[x, y];
        for (int i = 0; i < x * y; i++) nums[i % x, i / x] = initialValue;
        return nums;
    }

    private float mod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    private CubemapFace intToCubemapFace(int i)
    {
        switch (i)
        {
            case 0:
                return CubemapFace.PositiveX;
            case 1:
                return CubemapFace.NegativeX;
            case 2:
                return CubemapFace.PositiveY;
            case 3:
                return CubemapFace.NegativeY;
            case 4:
                return CubemapFace.PositiveZ;
            case 5:
                return CubemapFace.NegativeZ;
            default:
                return CubemapFace.Unknown;
        }
    }

    private static Vector2 CartesianToPolarCoordinates(Vector3 v)
    {
        float lat = Mathf.Rad2Deg * Mathf.Acos(v.y / v.magnitude); // North-South
        float lon = Mathf.Rad2Deg * Mathf.Atan2( v.x, v.z); // East-West

        return new Vector2(lon, lat);
    }

    private void decrementDirtyArray(int[,] arr) {
        for (int i = 0; i < arr.GetLength(0); i++)
        {
            for (int j = 0; j < arr.GetLength(1); j++)
            {
                if (arr[i, j] > 0) {
                    arr[i, j]--;
                }
            }
        }
    }

    private void exportFaces()
    {
        string dataPath = "/";
#if UNITY_ANDROID && !UNITY_EDITOR
        dataPath = "/../../../../DCIM/";
#endif

        // Encode texture into PNG
        byte[] bytesPosX = _t2DPosX.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "PosX.png", bytesPosX);

        byte[] bytesNegX = _t2DNegX.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "NegX.png", bytesNegX);

        byte[] bytesPosY = _t2DPosY.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "PosY.png", bytesPosY);

        byte[] bytesNegY = _t2DNegY.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "NegY.png", bytesNegY);

        byte[] bytesPosZ = _t2DPosZ.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "PosZ.png", bytesPosZ);

        byte[] bytesNegZ = _t2DNegZ.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + dataPath + "NegZ.png", bytesNegZ);


        Debug.Log("Exported: " + Application.persistentDataPath + dataPath + "PosX.png");
    }

    public void WriteToTexture()
    {
        for (int i = 0; i < _CubeSize; i++)
        {
            for (int j = 0; j < _CubeSize; j++)
            {
                _t2DPosX.SetPixel(i, j, ImageProcessing.Vector3ToColor(_posX[i, j]));
                _t2DNegX.SetPixel(i, j, ImageProcessing.Vector3ToColor(_negX[i, j]));
                _t2DPosY.SetPixel(i, j, ImageProcessing.Vector3ToColor(_posY[i, j]));
                _t2DNegY.SetPixel(i, j, ImageProcessing.Vector3ToColor(_negY[i, j]));
                _t2DPosZ.SetPixel(i, j, ImageProcessing.Vector3ToColor(_posZ[i, j]));
                _t2DNegZ.SetPixel(i, j, ImageProcessing.Vector3ToColor(_negZ[i, j]));
            }
        }
        _t2DPosX.Apply();
        _t2DNegX.Apply();
        _t2DPosY.Apply();
        _t2DNegY.Apply();
        _t2DPosZ.Apply();
        _t2DNegZ.Apply();

        _RPosX.texture = _t2DPosX;
        _RNegX.texture = _t2DNegX;
        _RPosY.texture = _t2DPosY;
        _RNegY.texture = _t2DNegY;
        _RPosZ.texture = _t2DPosZ;
        _RNegZ.texture = _t2DNegZ;
    }

    private void SetColor(Vector3 v, Vector3 color,
        Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ)
    {
        Vector3 d = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        int greatestIndex = 0;
        for (int i = 1; i < 3; i++)
            if (d[i] > d[greatestIndex])
                greatestIndex = i;

        v /= d[greatestIndex];

        float t = 1 - 0.65f;
        float luma = ImageProcessing.Grayscale(color);
        switch (greatestIndex)
        {
            case 0:
                int x = Mathf.Clamp(_CubeSize - 1 - (int)((v.z + 1) / 2f * _CubeSize), 0, _CubeSize - 1);
                int y = (int)((v.y + 1) / 2f * _CubeSize);
                if (v.x > 0)
                {
                    if (posX[x, y].x < 0)
                    {
                        _dirtyPixels++;
                    }
                    
                    if (luma < _posXMin[x, y]) {
                        _posXMin[x, y] = luma; 
                    }

                    posX[x, y] = color;
                }
                else
                {
                    if (negX[_CubeSize - 1 - x, y].x < 0)
                    {
                        _dirtyPixels++;
                        
                    }

                    if (luma < _negXMin[_CubeSize - 1 - x, y])
                    {
                        _negXMin[_CubeSize - 1 - x, y] = luma;
                    }

                    negX[_CubeSize - 1 - x, y] = color;
                }
                break;
            case 1:
                x = (int)((v.x + 1) / 2f * _CubeSize);
                y = _CubeSize - 1 - (int)((v.z + 1) / 2f * _CubeSize);
                if (v.y > 0)
                {
                    if (posY[x, y].x < 0)
                    {
                        _dirtyPixels++;
                        
                    }

                    if (luma < _posYMin[x, y])
                    {
                        _posYMin[x, y] = luma;
                    }

                    posY[x, y] = color;
                }
                else
                {
                    
                    if (negY[x, _CubeSize - 1 - y].x < 0)
                    {
                        _dirtyPixels++;
                        
                    }

                    if (luma < _negYMin[x, _CubeSize - 1 - y])
                    {
                        _negYMin[x, _CubeSize - 1 - y] = luma;
                    }

                    negY[x, _CubeSize - 1 - y] = color;
                }
                break;
            case 2:
                x = _CubeSize - 1 - (int)((v.x + 1) / 2f * _CubeSize);
                y = (int)((v.y + 1) / 2f * _CubeSize);
                if (v.z > 0)
                {
                    
                    if (posZ[_CubeSize - 1 - x, y].x < 0)
                    {
                        _dirtyPixels++;
                        
                    }

                    if (luma < _posZMin[_CubeSize - 1 - x, y])
                    {
                        _posZMin[_CubeSize - 1 - x, y] = luma;
                    }

                    posZ[_CubeSize - 1 - x, y] = color;
                }
                else
                {
                    if (negZ[x, y].x < 0)
                    {
                        _dirtyPixels++;
                        
                    }

                    if (luma < _negZMin[x, y])
                    {
                        _negZMin[x, y] = luma;
                    }

                    negZ[x, y] = color;
                }
                break;
        }

    }

    public static Vector3 PolarToCartesian(Vector2 polar)
    {
        float sinLat = Mathf.Sin(polar.y * Mathf.Deg2Rad);
        Vector3 cartesian = new Vector3(
            sinLat * Mathf.Sin(polar.x * Mathf.Deg2Rad),
            Mathf.Cos(polar.y * Mathf.Deg2Rad),
            sinLat * Mathf.Cos(polar.x * Mathf.Deg2Rad));

        return cartesian;
    }

    public static Vector3 GetColor(Vector3 v, int cubeSize,
        Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ)
    {
        Vector3 d = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        int greatestIndex = 0;
        for (int i = 1; i < 3; i++)
            if (d[i] > d[greatestIndex])
                greatestIndex = i;

        v /= d[greatestIndex];

        switch (greatestIndex)
        {
            case 0:
                int x = Mathf.Clamp(cubeSize - 1 - (int)((v.z + 1) / 2f * cubeSize), 0, cubeSize - 1);
                int y = (int)((v.y + 1) / 2f * (cubeSize-1));

                if (x >= posX.GetLength(0) || x < 0)
                {
                    Debug.Log("0:: x: " + x + " cubeSize: " + cubeSize);
                }
                else if (y >= posX.GetLength(1) || y < 0)
                {
                    Debug.Log("0:: y: " + y + " cubeSize: " + cubeSize);
                }


                if (v.x > 0)
                {
                    return posX[x, y];
                }
                else
                {
                    return negX[cubeSize - 1 - x, y];
                }
            case 1:
                x = (int)((v.x + 1) / 2f * cubeSize);
                y = cubeSize - 1 - (int)((v.z + 1) / 2f * (cubeSize - 1));

                if (x >= posX.GetLength(0) || x < 0)
                {
                    Debug.Log("1:: x: " + x + " cubeSize: " + cubeSize);
                }
                else if (y >= posX.GetLength(1) || y < 0)
                {
                    Debug.Log("1:: y: " + y + " cubeSize: " + cubeSize);
                }

                if (v.y > 0)
                {
                    return posY[x, y];
                }
                else
                {
                    return negY[x, cubeSize - 1 - y];
                }
            case 2:
                x = cubeSize - 1 - (int)((v.x + 1) / 2f * cubeSize);
                y = (int)((v.y + 1) / 2f * (cubeSize - 1));

                if (x >= posX.GetLength(0) || x < 0)
                {
                    Debug.Log("2:: x: " + x + " cubeSize: " + cubeSize);
                }
                else if (y >= posX.GetLength(1) || y < 0)
                {
                    Debug.Log("2:: y: " + y + " cubeSize: " + cubeSize);
                }

                if (v.z > 0)
                {
                    return posZ[cubeSize - 1 - x, y];
                }
                else
                {
                    return negZ[x, y];
                }
            default:
                return Vector3.zero;
        }

    }

    public static Vector3 SampleColorRange(Vector3 direction, int cubeSize, float angle,
        Vector3[,] posX, Vector3[,] negX,
        Vector3[,] posY, Vector3[,] negY,
        Vector3[,] posZ, Vector3[,] negZ)
    {
        Vector2 polar = CartesianToPolarCoordinates(direction);

        Vector3 left = PolarToCartesian(new Vector2(polar.x - angle, polar.y));
        Vector3 right = PolarToCartesian(new Vector2(polar.x + angle, polar.y));
        Vector3 down = PolarToCartesian(new Vector2(polar.x, polar.y - angle));
        Vector3 up = PolarToCartesian(new Vector2(polar.x, polar.y + angle));

        Vector3 c = GetColor(direction, cubeSize, posX, negX, posY, negY, posZ, negZ);
        c += GetColor(left, cubeSize, posX, negX, posY, negY, posZ, negZ);
        c += GetColor(right, cubeSize, posX, negX, posY, negY, posZ, negZ);
        c += GetColor(up, cubeSize, posX, negX, posY, negY, posZ, negZ);
        c += GetColor(down, cubeSize, posX, negX, posY, negY, posZ, negZ);
        c /= 5f;

        return c;
    }

    public void UpdateCubeArrays(Vector3[,] pixels)
    {
        if (_paused) return;
        for (int i = 1; i < _CubeSize - 1; i++)
        {
            for (int j = 1; j < _CubeSize - 1; j++)
            {
                Vector3 p = new Vector3(i / (float)_CubeSize * _screenWidth, j / (float)_CubeSize * _screenHeight, 0);

                Ray ray = Camera.main.ScreenPointToRay(p);
                //ray.direction = Quaternion.Euler(0, -45, 0) * ray.direction;
                int x = (int)(i / (float)(_CubeSize - 1) * (pixels.GetLength(0) - 1));
                int y = (pixels.GetLength(1) - 1) - (int)(j / (float)(_CubeSize - 1) * (pixels.GetLength(1) - 1));

                Vector3 c = pixels[x, y];
                //Debug.DrawRay(Camera.main.transform.position, ray.direction, ImageProcessing.Vector3ToColor(c / 255f));

                SetColor(ray.direction.normalized, c / 255f, _posX, _negX, _posY, _negY, _posZ, _negZ);
                if (_45DegreeCube) {
                    SetColor(Quaternion.Euler(0, -45, 0) * ray.direction.normalized, c / 255f, 
                        _posX45, _negX45, _posY45, _negY45, _posZ45, _negZ45);
                }
            }
        }

        //decrementDirtyArray(_posXDirty);
        //decrementDirtyArray(_negXDirty);
        //decrementDirtyArray(_posYDirty);
        //decrementDirtyArray(_negYDirty);
        //decrementDirtyArray(_posZDirty);
        //decrementDirtyArray(_negZDirty);

        _fillPercentage = _dirtyPixels / (float)(_CubeSize * _CubeSize * 6) * 100;
        _TextFillPercentage.text = "Fill: " + _fillPercentage + "%";
    }



}
