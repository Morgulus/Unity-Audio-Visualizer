using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVisualiser : MonoBehaviour
{

    public const int SAMPLE_SIZE = 1024;
    public const float COLOR_LERP_MODIFIER = 0.04f;
    public const float DAMP_VELOCITY_MODIFIER = 3f;
    public const float CIRCLE_MATERIAL_METALLIC_FLOAT = 0.4f;
    public const float CIRCLE_MATERIAL_SMOOTHNESS_FLOAT = 0.3f;
    public const float LINE_MATERIAL_METALLIC_FLOAT = 1f;
    public const float LINE_MATERIAL_SMOOTHNESS_FLOAT = 0.1f;

    public float maxScale = 25f;
    public float visualAmplifier = 150f;
    public float amplifierZ = 0.2f;
    public float maxSmoothSpeed = 100f;
    public float maxDampSpeed = 0.1f;
    public float keepPercentage = 0.2f;
    public float radius = 15f;

    public int spawnAmount = 64;
    public int type;


    public Light light;

    public Texture normalMap;

    private AudioSource source;
    private float[] samplesOne;
    private float[] samplesTwo;
    private float[] spectrumOne;
    private float[] spectrumTwo;
    public GameObject[] cubes;
    public MeshRenderer[] cubeMeshRenderers;
    private Color[] lineColors;
    private Color[] circleColors;
    private Color currentColor;
    private Color startColor;
    private Color targetColor;
    public float[] scales;
    public float[] speeds;
    private float[] angles;
    private float rmsValue;
    private float maxDBValue = 40f;
    public float maxIntensity = 40f;
    public float maxLightSmoothSpeed = 1.6f;

    //public float speedLerpModifier;
    public float dbValue;
    private float pitchValue;

    private int averageSize;

    private Vector3 lightVelocity;
    private Vector3 velocityPos;
    public Vector3 center;

    public float tLine;
    public float tCircle;


    private void Start()
    {
        lineColors = new Color[] { new Color(150/255f, 0f, 0f),
                                   new Color(255/255f, 255/255f, 0f)
                                 };
        circleColors = new Color[] { new Color(107/255f, 0f, 218/255f),
                                     new Color(90/255f, 214/255f, 1f)
                                   };

        source = GetComponent<AudioSource>();
        samplesOne = new float[SAMPLE_SIZE];
        spectrumOne = new float[SAMPLE_SIZE];
        samplesTwo = new float[SAMPLE_SIZE];
        spectrumTwo = new float[SAMPLE_SIZE];
        speeds = new float[spawnAmount];

        type = 1;
        tLine = 0f;
        tCircle = 0f;

        SetStartTargetColors(type);
        GenerateCubes();
        SpreadSamples();
    }
    private void Update()
    {
        AnalyzeSound();
        SetStartTargetColors(type);
        UpdateVisual();
        UpdateLight();
    }
    private void GenerateCubes()
    {
        cubes = new GameObject[spawnAmount];
        cubeMeshRenderers = new MeshRenderer[spawnAmount];
        scales = new float[spawnAmount];
        angles = new float[spawnAmount];

        center = new Vector3(spawnAmount / 4f, 0f, 0f);

        for (int i = 0; i < spawnAmount; i++)
        {
            float angle = i * 1f / spawnAmount;
            angles[i] = angle * Mathf.PI * 2;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            cube.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            cubes[i] = cube;
            cubeMeshRenderers[i] = cube.GetComponent<MeshRenderer>();
            cubes[i].transform.position = Vector3.right * i/2;
            cubeMeshRenderers[i].material.SetTexture("_BumpMap", normalMap);
            cubeMeshRenderers[i].material.SetFloat("_Glossiness", LINE_MATERIAL_SMOOTHNESS_FLOAT);
            cubeMeshRenderers[i].material.SetFloat("_Metallic", LINE_MATERIAL_METALLIC_FLOAT);

        }

    }
    public void TransformToCircle()
    {
        maxScale = 10;
        //speedLerpModifier = 0.021f / 3.5f;

        for (int i = 0; i < spawnAmount; i++)
        {
            float x = Mathf.Cos(angles[i]) * radius;
            float y = Mathf.Sin(angles[i]) * radius;
            Vector3 pos = center + new Vector3(x, y, 0f);

            velocityPos = (pos - cubes[i].transform.position) * DAMP_VELOCITY_MODIFIER;

            cubeMeshRenderers[i].material.SetTexture("_BumpMap", null);
            cubeMeshRenderers[i].material.SetFloat("_Glossiness", CIRCLE_MATERIAL_SMOOTHNESS_FLOAT);
            cubeMeshRenderers[i].material.SetFloat("_Metallic", CIRCLE_MATERIAL_METALLIC_FLOAT);

            cubes[i].transform.rotation = Quaternion.LookRotation(Vector3.forward, pos - center);
            cubes[i].transform.position = Vector3.SmoothDamp(cubes[i].transform.position, pos, ref velocityPos, 1f, maxDampSpeed);
        }
        lightVelocity = (center - light.transform.position) * DAMP_VELOCITY_MODIFIER;
        light.transform.position = Vector3.SmoothDamp(light.transform.position, center, ref lightVelocity, 1f, maxDampSpeed);
    }

    public void TransformToLine()
    {
        maxScale = 35;
        //speedLerpModifier = 0.021f;
        
        for (int i = 0; i < spawnAmount; i++)
        {
            velocityPos = (Vector3.right * i/2 - cubes[i].transform.position) * DAMP_VELOCITY_MODIFIER;

            cubeMeshRenderers[i].material.SetTexture("_BumpMap", normalMap);
            cubeMeshRenderers[i].material.SetFloat("_Glossiness", LINE_MATERIAL_SMOOTHNESS_FLOAT);
            cubeMeshRenderers[i].material.SetFloat("_Metallic", LINE_MATERIAL_METALLIC_FLOAT);

            cubes[i].transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            cubes[i].transform.position = Vector3.SmoothDamp(cubes[i].transform.position, Vector3.right * i/2, ref velocityPos, 1f, maxDampSpeed);
        }
        Vector3 targetPos = new Vector3(-19f, 0f, 0f);
        lightVelocity = (targetPos - light.transform.position) * DAMP_VELOCITY_MODIFIER;
        light.transform.position = Vector3.SmoothDamp(light.transform.position, targetPos, ref lightVelocity, 1f, maxDampSpeed);
    }

    private void UpdateVisual()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        
        while (visualIndex < spawnAmount)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrumOne[spectrumIndex++];
                sum += spectrumTwo[spectrumIndex++];
                sum = sum / 2f;
                j++;
            }

            float scale = sum / averageSize * visualAmplifier;
            float speedLerpT = scales[visualIndex] / maxScale;
            speeds[visualIndex] = Mathf.Lerp(0f, maxSmoothSpeed, speedLerpT);
            scales[visualIndex] -= Time.deltaTime * speeds[visualIndex];

            if (scales[visualIndex] < scale)
                scales[visualIndex] = Mathf.Clamp(scale, 0f, maxScale);

            cubes[visualIndex].transform.localScale = new Vector3(0.5f, 1 + scales[visualIndex], 0.5f + scales[visualIndex] * amplifierZ);

            cubeMeshRenderers[visualIndex].material.color = Color.Lerp(startColor, targetColor, scales[visualIndex] / maxScale);
           
            visualIndex++;
        }
    }

    private void SetStartTargetColors(int type)
    {
        switch (type)
        {
            case 1:
            default:
                {
                    startColor = lineColors[0];
                    targetColor = lineColors[1];
                    break;
                }
            case 2:
                {
                    startColor = circleColors[0];
                    targetColor = circleColors[1]; 
                    break;
                }
        }
    }

    private void SpreadSamples()
    {
        averageSize = (int)(SAMPLE_SIZE  / spawnAmount * keepPercentage);
    }

    private void AnalyzeSound()
    {
        source.GetOutputData(samplesOne, 0);
        source.GetOutputData(samplesTwo, 1);

        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i++)
        {
            sum += (samplesOne[i] * samplesOne[i] + samplesTwo[i] * samplesTwo[i]) / 2f;
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        dbValue = 20 * Mathf.Log10(rmsValue / 0.1f);

        source.GetSpectrumData(spectrumOne, 0, FFTWindow.BlackmanHarris);
        source.GetSpectrumData(spectrumTwo, 1, FFTWindow.BlackmanHarris);
    }

    private void UpdateLight()
    {
        float lightLerpT = light.intensity / maxIntensity;
        float lightSmoothSpeed = Mathf.Lerp(0f, maxLightSmoothSpeed, lightLerpT);
        light.intensity -= lightSmoothSpeed;
        if (light.intensity < dbValue * 2)
        {
            light.intensity = Mathf.Clamp(dbValue * 2, 0f, maxDBValue);
        }
        light.color = Color.Lerp(startColor, targetColor, light.intensity / maxIntensity);
    }
}
