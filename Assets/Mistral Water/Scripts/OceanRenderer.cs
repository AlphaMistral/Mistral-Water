using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class OceanRenderer : MonoBehaviour 
{
	#region Public Variables

	public Texture2D dice;

	public float mult = 2f;
	public float unitWidth = 1f;
	public int resolution = 256;
	public float length = 256f;
	public float choppiness = 1.5f;
	public float amplitude = 1f;
	public Vector2 wind;

	public Shader initialShader;
	public Shader spectrumShader;
	public Shader fftShader;
	public Shader dispersionShader;
	public Shader normalShader;
	public Shader whiteShader;

	public Texture2D fff;

	#endregion

	#region Private Variables

	private bool saved = false;

	private float oldLength;
	private float oldChoppiness;
	private float oldAmplitude;
	private Vector2 oldWind;

	private bool currentPhase = false;

	private Mesh mesh;
	private Vector3[] vertices;
	private Vector2[] uvs;
	private int[] indices;
	private Vector3[] normals;

	private MeshFilter filter;

	private Material initialMat;
	private Material spectrumMat;
	private Material fftMat;
	private Material dispersionMat;
	private Material normalMat;
	private Material whiteMat;

	public RenderTexture initialTexture;
	public RenderTexture pingPhaseTexture;
	public RenderTexture pongPhaseTexture;
	public RenderTexture pingTransformTexture;
	public RenderTexture pongTransformTexture;
	public RenderTexture spectrumTexture;
	public RenderTexture displacementTexture;
	public RenderTexture normalTexture;
	public RenderTexture whiteTexture;

	private Material oceanMat;

	#endregion

	#region MonoBehaviours

	private void Awake()
	{
		filter = GetComponent<MeshFilter>();
		if (filter == null)
		{
			filter = gameObject.AddComponent<MeshFilter>();
		}
		mesh = new Mesh();
		filter.mesh = mesh;
		SetParams();
		GenerateMesh();
		RenderInitial();
	}

	private void Update()
	{
		GenerateTexture();
		dispersionMat.SetFloat("_Length", length);

		spectrumMat.SetFloat("_Choppiness", choppiness);
		spectrumMat.SetFloat("_Length", length);
		if (oldLength != length || oldWind != wind || oldAmplitude != amplitude)
		{
			initialMat.SetFloat("_Amplitude", amplitude);
			initialMat.SetFloat("_Length", length);
			initialMat.SetVector("_Wind", wind);
			oldLength = length;
			oldChoppiness = choppiness;
			oldAmplitude = amplitude;
			oldWind.x = wind.x;
			oldWind.y = wind.y;
			RenderInitial();
		}
	}

	#endregion

	#region Methods

	private void SetParams()
	{
		oldLength = length;
		oldChoppiness = choppiness;
		oldAmplitude = amplitude;
		oldWind = new Vector2();
		oldWind.x = wind.x;
		oldWind.y = wind.y;
		initialMat = new Material(initialShader);
		spectrumMat = new Material(spectrumShader);
		fftMat = new Material(fftShader);
		dispersionMat = new Material(dispersionShader);
		normalMat = new Material(normalShader);
		whiteMat = new Material(whiteShader);
		oceanMat = GetComponent<MeshRenderer>().material;
		vertices = new Vector3[resolution * resolution];
		indices = new int[(resolution - 1) * (resolution - 1) * 6];
		normals = new Vector3[resolution * resolution];
		uvs = new Vector2[resolution * resolution];
		resolution *= 8;
		initialTexture = new RenderTexture(resolution , resolution, 0, RenderTextureFormat.ARGBFloat);
		pingPhaseTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
		pongPhaseTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
		pingTransformTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		pongTransformTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		spectrumTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		displacementTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		normalTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RGB565);
		whiteTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RGB565);
		initialMat.SetFloat("_RandomSeed1", UnityEngine.Random.value * 10f);
		initialMat.SetFloat("_RandomSeed2", UnityEngine.Random.value * 10f);
		initialMat.SetFloat("_Amplitude", amplitude);
		initialMat.SetFloat("_Length", length);
		initialMat.SetFloat("_Resolution", resolution);
		initialMat.SetVector("_Wind", wind);

		dispersionMat.SetFloat("_Length", length);
		dispersionMat.SetInt("_Resolution", resolution);

		spectrumMat.SetFloat("_Choppiness", choppiness);
		spectrumMat.SetFloat("_Length", length);
		spectrumMat.SetInt("_Resolution", resolution);

		fftMat.SetFloat("_TransformSize", resolution);
		resolution /= 8;
	}

	private void GenerateMesh()
	{
		int indiceCount = 0;
		int halfResolution = resolution / 2;
		for (int i = 0; i < resolution; i++)
		{
			float horizontalPosition = (i - halfResolution) * unitWidth;
			for (int j = 0; j < resolution; j++)
			{
				int currentIdx = i * (resolution) + j;
				float verticalPosition = (j - halfResolution) * unitWidth;
				vertices[currentIdx] = new Vector3(horizontalPosition + (resolution % 2 == 0? unitWidth / 2f : 0f), 0f, verticalPosition + (resolution % 2 == 0? unitWidth / 2f : 0f));
				normals[currentIdx] = new Vector3(0f, 1f, 0f);
				uvs[currentIdx] = new Vector2(i * 1.0f / (resolution - 1), j * 1.0f / (resolution - 1));
				if (j == resolution - 1)
					continue;
				if (i != resolution - 1)
				{
					indices[indiceCount++] = currentIdx;
					indices[indiceCount++] = currentIdx + 1;
					indices[indiceCount++] = currentIdx + resolution;
				}
				if (i != 0)
				{
					indices[indiceCount++] = currentIdx;
					indices[indiceCount++] = currentIdx - resolution + 1;
					indices[indiceCount++] = currentIdx + 1;
				}
			}
		}
		mesh.vertices = vertices;
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.normals = normals;
		mesh.uv = uvs;
		filter.mesh = mesh;
	}

	private void RenderInitial()
	{
		Graphics.Blit(null, initialTexture, initialMat);
		spectrumMat.SetTexture("_Initial", initialTexture);
	}

	private void GenerateTexture()
	{
		float deltaTime = Time.deltaTime;

		currentPhase = !currentPhase;
		RenderTexture rt = currentPhase ? pingPhaseTexture : pongPhaseTexture;
		dispersionMat.SetTexture("_Phase", currentPhase? pongPhaseTexture : pingPhaseTexture);
		dispersionMat.SetFloat("_DeltaTime", deltaTime * mult);
		Graphics.Blit(null, rt, dispersionMat);

		spectrumMat.SetTexture("_Phase", currentPhase? pingPhaseTexture : pongPhaseTexture);
		Graphics.Blit(null, spectrumTexture, spectrumMat);

		fftMat.EnableKeyword("_HORIZONTAL");
		fftMat.DisableKeyword("_VERTICAL");
		int iterations = Mathf.CeilToInt((float)Math.Log(resolution * 8, 2)) * 2;
		for (int i = 0; i < iterations; i++)
		{
			fftMat.SetFloat("_SubTransformSize", Mathf.Pow(2, (i % (iterations / 2)) + 1));
			if (i == 0)
			{
				fftMat.SetTexture("_Input", spectrumTexture);
				Graphics.Blit(null, pingTransformTexture, fftMat);
			}
			else if (i == iterations - 1)
			{
				fftMat.SetTexture("_Input", (iterations % 2 == 0) ? pingTransformTexture : pongTransformTexture);
				Graphics.Blit(null, displacementTexture, fftMat);
			}
			else if (i % 2 == 1)
			{
				fftMat.SetTexture("_Input", pingTransformTexture);
				Graphics.Blit(null, pongTransformTexture, fftMat);
			}
			else
			{
				fftMat.SetTexture("_Input", pongTransformTexture);
				Graphics.Blit(null, pingTransformTexture, fftMat);
			}
			if (i == iterations / 2)
			{
				fftMat.DisableKeyword("_HORIZONTAL");
				fftMat.EnableKeyword("_VERTICAL");
			}
		}
		normalMat.SetTexture("_DisplacementMap", displacementTexture);
		Graphics.Blit(null, normalTexture, normalMat);
		whiteMat.SetTexture("_Displacement", displacementTexture);
		whiteMat.SetTexture("_Bump", normalTexture);
		whiteMat.SetFloat("_Resolution", resolution * 8);
		whiteMat.SetFloat("_Length", resolution);
		Graphics.Blit(null, whiteTexture, whiteMat);
		if (!saved)
		{
			oceanMat.SetTexture("_Anim", displacementTexture);
			oceanMat.SetTexture("_Bump", normalTexture);
			oceanMat.SetTexture("_White", whiteTexture);
			saved = true;
		}
	}

	#endregion
}
