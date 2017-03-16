using UnityEngine;
using System.Collections;
using Tango;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class CubeLightManager : MonoBehaviour, ITangoVideoOverlay, ITangoLifecycle
{
    private TangoApplication _tangoApplication;

    public float _clusterAngle = 45f;
    public int _ResDiv = 16;
    public bool _SampleNeighbor = false;

    public RenderTexture _InTexture;

    public Light _LightA;
    public Light _LightB;
    public Light _LightC;

    public Transform _LightDirGizmoA;
    public Transform _LightDirGizmoB;
    public Transform _LightDirGizmoC;

    public Transform _ModelSphere;
    public Transform _ModelLion;

    public TangoCubemap _TangoCube;

    public Slider _SliderClusterAngle;

    public Toggle _ToggleSphere;
    public Toggle _ToggleLion;

    void Start () {
        _tangoApplication = FindObjectOfType<TangoApplication>();
        if (_tangoApplication != null)
        {
            _tangoApplication.Register(this);
        }

        _SliderClusterAngle.onValueChanged.AddListener(onValueChangedClusterAngle);
        _ToggleSphere.onValueChanged.AddListener(delegate { onValueChangedModel("sphere"); });
        _ToggleLion.onValueChanged.AddListener(delegate { onValueChangedModel("lion"); });
    }

    private void onValueChangedClusterAngle(float val)
    {
        _clusterAngle = val;
    }

    private void onValueChangedModel(string s)
    {
        if (_ToggleSphere.isOn)
        {
            _ModelSphere.gameObject.SetActive(true);
            _ModelLion.gameObject.SetActive(false);
        }
        else
        {
            _ModelSphere.gameObject.SetActive(false);
            _ModelLion.gameObject.SetActive(true);
        }
    }

    void Update () {
	
	}

    void LateUpdate()
    {
        _LightDirGizmoA.position = Camera.main.transform.position + Camera.main.transform.forward;
        _LightDirGizmoB.position = Camera.main.transform.position + Camera.main.transform.forward;
        _LightDirGizmoC.position = Camera.main.transform.position + Camera.main.transform.forward;
    }

    private void doLightEstimation(TangoUnityImageData imageBuffer)
    {
        Vector3[,] pixels;
#if UNITY_ANDROID && !UNITY_EDITOR
        pixels = TangoHelpers.ImageBufferToArray(imageBuffer, (uint)_ResDiv, true);
#else
        pixels = ImageProcessing.RenderTextureToRGBArray(_InTexture);
#endif

        _TangoCube.UpdateCubeArrays(pixels);
        _TangoCube.WriteToTexture();

        if (_SampleNeighbor)
        {
            List<LightInfo> lightDir = LightEstimationCube.EstimateLightDir_SampleNeighbor(
                _TangoCube._posX, _TangoCube._negX,
                _TangoCube._posY, _TangoCube._negY,
                _TangoCube._posZ, _TangoCube._negZ,
                _TangoCube._posXMin, _TangoCube._negXMin,
                _TangoCube._posYMin, _TangoCube._negYMin,
                _TangoCube._posZMin, _TangoCube._negZMin,
                transform.position, _clusterAngle);

            _LightA.transform.position = lightDir[lightDir.Count - 1]._Direction.normalized * 100;
            _LightA.transform.LookAt(transform.position);
            _LightA.color = ImageProcessing.Vector3ToColor( lightDir[lightDir.Count - 1]._Color);

            if (lightDir.Count >= 2 && lightDir[lightDir.Count - 2]._Magnitude > float.Epsilon)
            {
                _LightB.transform.position = lightDir[lightDir.Count - 2]._Direction.normalized * 100;
                _LightB.transform.LookAt(transform.position);
                _LightB.gameObject.SetActive(true);
                _LightDirGizmoB.rotation = _LightB.transform.rotation;
                _LightB.color = ImageProcessing.Vector3ToColor(lightDir[lightDir.Count - 2]._Color);
                _LightDirGizmoB.gameObject.SetActive(true);
            }
            else
            {
                _LightB.gameObject.SetActive(false);
                _LightDirGizmoB.gameObject.SetActive(false);
            }

            if (lightDir.Count >= 3 && lightDir[lightDir.Count - 3]._Magnitude > float.Epsilon)
            {
                _LightC.transform.position = lightDir[lightDir.Count - 3]._Direction.normalized * 100;
                _LightC.transform.LookAt(transform.position);
                _LightC.gameObject.SetActive(true);
                _LightDirGizmoC.rotation = _LightC.transform.rotation;
                _LightC.color = ImageProcessing.Vector3ToColor(lightDir[lightDir.Count - 3]._Color);
                _LightDirGizmoC.gameObject.SetActive(true);
            }
            else
            {
                _LightC.gameObject.SetActive(false);
                _LightDirGizmoC.gameObject.SetActive(false);
            }

            _LightDirGizmoA.rotation = _LightA.transform.rotation;

            return;
        }

        //if (_TangoCube._45DegreeCube)
        //{
        //    //Vector3 lightDir45 = LightEstimationCube.EstimateLightDir(
        //    //    _TangoCube._posX45, _TangoCube._negX45,
        //    //    _TangoCube._posY45, _TangoCube._negY45,
        //    //    _TangoCube._posZ45, _TangoCube._negZ45,
        //    //    transform.position);

        //    //_LightA.position = (lightDir.normalized * 100 + Quaternion.Euler(0, 45, 0) * lightDir45.normalized * 100) / 2f;
        //    //_LightA.LookAt(transform.position);
        //    //_LightDirGizmo.rotation = _LightA.rotation;

        //    List<Vector3> lightDir45 = LightEstimationCube.EstimateLightDir(
        //        _TangoCube._posX, _TangoCube._negX,
        //        _TangoCube._posY, _TangoCube._negY,
        //        _TangoCube._posZ, _TangoCube._negZ,
        //        _TangoCube._posX45, _TangoCube._negX45,
        //        _TangoCube._posY45, _TangoCube._negY45,
        //        _TangoCube._posZ45, _TangoCube._negZ45,
        //        transform.position, _clusterAngle);

        //    lightDir45.Sort((a, b) => a.magnitude.CompareTo(b.magnitude));

        //    _LightA.position = lightDir45[lightDir45.Count - 1].normalized * 100;
        //    _LightA.LookAt(transform.position);
        //    _LightDirGizmoA.rotation = _LightA.rotation;

        //    //_LightA.position = lightDir45.normalized * 100;
        //    //_LightA.LookAt(transform.position);
        //    //_LightDirGizmo.rotation = _LightA.rotation;
        //}
        //else
        //{
        //    List<Vector3> lightDirs = LightEstimationCube.EstimateLightDir(
        //    _TangoCube._posX, _TangoCube._negX,
        //    _TangoCube._posY, _TangoCube._negY,
        //    _TangoCube._posZ, _TangoCube._negZ,
        //    transform.position, _clusterAngle);

        //    lightDirs.Sort((a, b) => a.magnitude.CompareTo(b.magnitude));

        //    _LightA.position = lightDirs[lightDirs.Count - 1].normalized * 100;
        //    _LightA.LookAt(transform.position);
        //    _LightDirGizmoA.rotation = _LightA.rotation;
        //}

        
    }

    #region Tango Events

    public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId, TangoUnityImageData imageBuffer)
    {
        doLightEstimation(imageBuffer);
    }

    public void OnTangoPermissions(bool permissionsGranted)
    {
    }

    public void OnTangoServiceConnected()
    {
        _tangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.DISABLED);
    }

    public void OnTangoServiceDisconnected()
    {
    }

    #endregion


}
