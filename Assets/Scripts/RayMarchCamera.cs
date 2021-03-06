﻿using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayMarchCamera : MonoBehaviour
{
    public ComputeShader raymarching;

    private RenderTexture target;
    private Camera cam;
    private Light lightSource;
    private List<ComputeBuffer> buffersToDispose;

    /* For anti-aliasing */
    private uint _currentSample = 0;
    private Material _addMaterial;

    void Init()
    {
        cam = Camera.current;
        lightSource = FindObjectOfType<Light>();
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Init();
        buffersToDispose = new List<ComputeBuffer>();

        InitRenderTexture();
        CreateScene();
        SetParameters();

        raymarching.SetTexture(0, "Source", source);
        raymarching.SetTexture(0, "Destination", target);

        int threadGroupsX = Mathf.CeilToInt(cam.pixelWidth / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(cam.pixelHeight / 8.0f);
        raymarching.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        //if (_addMaterial == null)
        //    _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        //_addMaterial.SetFloat("_Sample", _currentSample);
        //_currentSample++;
        //Graphics.Blit(target, destination, _addMaterial);
        Graphics.Blit(target, destination);

        foreach (var buffer in buffersToDispose)
        {
            buffer.Dispose();
        }
    }

    void CreateScene()
    {
        List<Shape> allShapes = new List<Shape>(FindObjectsOfType<Shape>());
        allShapes.Sort((a, b) => a.operation.CompareTo(b.operation));

        List<Shape> orderedShapes = new List<Shape>();

        for (int i = 0; i < allShapes.Count; i++)
        {
            // Add top-level shapes (those without a parent)
            if (allShapes[i].transform.parent == null)
            {

                Transform parentShape = allShapes[i].transform;
                orderedShapes.Add(allShapes[i]);
                allShapes[i].numChildren = parentShape.childCount;
                // Add all children of the shape (nested children not supported currently)
                for (int j = 0; j < parentShape.childCount; j++)
                {
                    if (parentShape.GetChild(j).GetComponent<Shape>() != null)
                    {
                        orderedShapes.Add(parentShape.GetChild(j).GetComponent<Shape>());
                        orderedShapes[orderedShapes.Count - 1].numChildren = 0;
                    }
                }
            }

        }

        ShapeData[] shapeData = new ShapeData[orderedShapes.Count];
        for (int i = 0; i < orderedShapes.Count; i++)
        {
            var s = orderedShapes[i];
            Vector3 col = new Vector3(s.colour.r, s.colour.g, s.colour.b);
            shapeData[i] = new ShapeData()
            {
                position = s.Position,
                scale = s.Scale,
                colour = col,
                extra = s.radius,
                shapeType = (int)s.shapeType,
                operation = (int)s.operation,
                blendStrength = s.blendStrength * 3,
                numChildren = s.numChildren
            };
        }

        ComputeBuffer shapeBuffer = new ComputeBuffer(shapeData.Length, ShapeData.GetSize());
        shapeBuffer.SetData(shapeData);
        raymarching.SetBuffer(0, "shapes", shapeBuffer);
        raymarching.SetInt("numShapes", shapeData.Length);

        buffersToDispose.Add(shapeBuffer);
    }

    void SetParameters()
    {
        bool lightIsDirectional = lightSource.type == LightType.Directional;
        raymarching.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        raymarching.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        raymarching.SetVector("_Light", (lightIsDirectional) ? lightSource.transform.forward : lightSource.transform.position);
        raymarching.SetBool("positionLight", !lightIsDirectional);
        raymarching.SetVector("_PixelOffset", new Vector2(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f)));
    }

    void InitRenderTexture()
    {
        if (target == null || target.width != cam.pixelWidth || target.height != cam.pixelHeight)
        {
            if (target != null)
            {
                target.Release();
            }
            target = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

    struct ShapeData
    {
        public Vector3 position;
        public Vector3 scale;
        public Vector3 colour;
        public float extra;
        public int shapeType;
        public int operation;
        public float blendStrength;
        public int numChildren;

        public static int GetSize()
        {
            return sizeof(float) * 11 + sizeof(int) * 3;
        }
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }
}
