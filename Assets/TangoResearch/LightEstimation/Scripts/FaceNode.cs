using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FaceNode {

    public Vector3 _Direction;

    public bool _Visited = false;

    public Dictionary<FaceNode, float> _NeiborAngles;

    public FaceNode(Vector3 direction)
    {
        _Direction = direction;
        _NeiborAngles = new Dictionary<FaceNode, float>(4);
    }

    public void AddNeighbors(params FaceNode[] nodes)
    {
        if (this._Direction.magnitude <= float.Epsilon)
            return;

        for (int i = 0; i < nodes.Length; i++)
        {
            float angle = Vector3.Angle(_Direction, nodes[i]._Direction);
            _NeiborAngles.Add(nodes[i], angle);
            nodes[i]._NeiborAngles.Add(this, angle);
        }
    }

    public void AddNeighbors(float degreeThreshold, params FaceNode[] nodes)
    {
        if (this._Direction.magnitude <= float.Epsilon)
            return;

        for (int i = 0; i < nodes.Length; i++)
        {
            float angle = Vector3.Angle(_Direction, nodes[i]._Direction);
            if (angle < degreeThreshold)
            {
                _NeiborAngles.Add(nodes[i], angle);
                nodes[i]._NeiborAngles.Add(this, angle);
            }
        }
    }

    public float GetNeighborSize(float degreeThreshold = 360)
    {
        float sum = 0;
        int count = 0;
        foreach (var item in _NeiborAngles)
        {
            if (item.Value < degreeThreshold) {
                sum += item.Value;
                count++;
            }
        }

        //Debug.Log("sum x count: " + sum + "x" + count);

        return sum/count;
    }

    public static FaceNode[] CreateFaceNodes(float degreeThreshold, Vector3 negX, Vector3 posX, Vector3 negY, Vector3 posY, Vector3 negZ, Vector3 posZ)
    {
        FaceNode nX = new FaceNode(negX);
        FaceNode pX = new FaceNode(posX);
        FaceNode nY = new FaceNode(negY);
        FaceNode pY = new FaceNode(posY);
        FaceNode nZ = new FaceNode(negZ);
        FaceNode pZ = new FaceNode(posZ);

        nX.AddNeighbors(degreeThreshold, nY, pY, nZ, pZ);
        pX.AddNeighbors(degreeThreshold, nY, pY, nZ, pZ);
        nY.AddNeighbors(degreeThreshold, nZ, pZ);
        pY.AddNeighbors(degreeThreshold, nZ, pZ);

        return new FaceNode[] { nX, pX, nY, pY, nZ, pZ };
    }


}
