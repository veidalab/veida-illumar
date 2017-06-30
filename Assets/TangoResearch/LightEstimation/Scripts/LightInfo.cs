using UnityEngine;
using System.Collections;

public class LightInfo {

    public float _Magnitude;
    public Vector3 _Color;
    public Vector3 _Direction;

    public LightInfo(Vector3 direction, Vector3 color) {
        _Direction = direction;
        _Color = color;
        _Magnitude = _Direction.magnitude;
    }

    public LightInfo(Vector3 direction, Vector3 color, float magnitude)
    {
        _Direction = direction;
        _Color = color;
        _Magnitude = magnitude;
    }

}
