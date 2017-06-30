using UnityEngine;
using System.Collections;

public class FreezeRotation : MonoBehaviour {

    private Quaternion _rotation;

    public bool _FreezeX = false;
    public bool _FreezeY = false;
    public bool _FreezeZ = false;

    void Start()
    {
        _rotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = _rotation;
        if (_FreezeX)
            transform.eulerAngles = new Vector3(0, transform.rotation.y, transform.rotation.z);
        if (_FreezeY)
            transform.eulerAngles = new Vector3(transform.rotation.x, 0, transform.rotation.z);
        if (_FreezeZ)
            transform.eulerAngles = new Vector3(transform.rotation.x, transform.rotation.y, 0);
    }
}
