using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class OceanRenderer : MonoBehaviour 
{
	#region Public Variables

	[Range(0f, 3f)]
	public float mult = 2f;
	public float unitWidth = 1f;
	public int resolution = 256;
	public float length = 256f;
	[Range(0f, 2f)]
	public float choppiness = 1.5f;
	[Range(0f, 2f)]
	public float amplitude = 1f;
	public Vector2 wind;

	public Shader initialShader;
	public Shader spectrumShader;
	public Shader spectrumHeightShader;
	public Shader fftShader;
	public Shader dispersionShader;
	public Shader normalShader;
	public Shader whiteShader;

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
	private Material heightMat;
	private Material fftMat;
	private Material dispersionMat;
	private Material normalMat;
	private Material normalSpecMat;
	private Material whiteMat;

	private RenderTexture initialTexture;
	private RenderTexture pingPhaseTexture;
	private RenderTexture pongPhaseTexture;
	private RenderTexture pingTransformTexture;
	private RenderTexture pongTransformTexture;
	private RenderTexture spectrumTexture;
	private RenderTexture heightTexture;
	private RenderTexture displacementTexture;
	private RenderTexture normalTexture;
	private RenderTexture whiteTexture;

	private Material oceanMat;

	#endregion

	#region MonoBehaviours

	private void Awake()
	{
		/// Normally this parameter should be pretty small ... 
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
			initialMat.SetFloat("_Amplitude", amplitude / 10000f);
			initialMat.SetFloat("_Length", length);
			initialMat.SetVector("_Wind", wind);
			oldLength = length;
			oldChoppiness = choppiness;
			oldAmplitude = amplitude;
			oldWind.x = wind.x;
			oldWind.y = wind.y;
			RenderInitial();
			Debug.Log("Param Changed! ");
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
		heightMat = new Material(spectrumHeightShader);
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
		heightTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		displacementTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		normalTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		whiteTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
		initialMat.SetFloat("_RandomSeed1", UnityEngine.Random.value * 10f);
		initialMat.SetFloat("_RandomSeed2", UnityEngine.Random.value * 10f);
		initialMat.SetFloat("_Amplitude", amplitude / 10000f);
		initialMat.SetFloat("_Length", length);
		initialMat.SetFloat("_Resolution", resolution);
		initialMat.SetVector("_Wind", wind);

		dispersionMat.SetFloat("_Length", length);
		dispersionMat.SetInt("_Resolution", resolution);

		spectrumMat.SetFloat("_Choppiness", choppiness);
		spectrumMat.SetFloat("_Length", length);
		spectrumMat.SetInt("_Resolution", resolution);

		heightMat.SetFloat("_Choppiness", choppiness);
		heightMat.SetFloat("_Length", length);
		heightMat.SetInt("_Resolution", resolution);

		normalMat.SetFloat("_Length", length);
		normalMat.SetFloat("_Resolution", resolution);

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
		heightMat.SetTexture("_Initial", initialTexture);
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
			RenderTexture blitTarget;
			fftMat.SetFloat("_SubTransformSize", Mathf.Pow(2, (i % (iterations / 2)) + 1));
			if (i == 0)
			{
				fftMat.SetTexture("_Input", spectrumTexture);
				blitTarget = pingTransformTexture;
			}
			else if (i == iterations - 1)
			{
				fftMat.SetTexture("_Input", (iterations % 2 == 0) ? pingTransformTexture : pongTransformTexture);
				blitTarget = displacementTexture;
			}
			else if (i % 2 == 1)
			{
				fftMat.SetTexture("_Input", pingTransformTexture);
				blitTarget = pongTransformTexture;
			}
			else
			{
				fftMat.SetTexture("_Input", pongTransformTexture);
				blitTarget = pingTransformTexture;
			}
			if (i == iterations / 2)
			{
				fftMat.DisableKeyword("_HORIZONTAL");
				fftMat.EnableKeyword("_VERTICAL");
			}
			Graphics.Blit(null, blitTarget, fftMat);
		}

		heightMat.SetTexture("_Phase", currentPhase? pingPhaseTexture : pongPhaseTexture);
		Graphics.Blit(null, spectrumTexture, heightMat);
		fftMat.EnableKeyword("_HORIZONTAL");
		fftMat.DisableKeyword("_VERTICAL");
		for (int i = 0; i < iterations; i++)
		{
			RenderTexture blitTarget;
			fftMat.SetFloat("_SubTransformSize", Mathf.Pow(2, (i % (iterations / 2)) + 1));
			if (i == 0)
			{
				fftMat.SetTexture("_Input", spectrumTexture);
				blitTarget = pingTransformTexture;
			}
			else if (i == iterations - 1)
			{
				fftMat.SetTexture("_Input", (iterations % 2 == 0) ? pingTransformTexture : pongTransformTexture);
				blitTarget = heightTexture;
			}
			else if (i % 2 == 1)
			{
				fftMat.SetTexture("_Input", pingTransformTexture);
				blitTarget = pongTransformTexture;
			}
			else
			{
				fftMat.SetTexture("_Input", pongTransformTexture);
				blitTarget = pingTransformTexture;
			}
			if (i == iterations / 2)
			{
				fftMat.DisableKeyword("_HORIZONTAL");
				fftMat.EnableKeyword("_VERTICAL");
			}
			Graphics.Blit(null, blitTarget, fftMat);
		}

		normalMat.SetTexture("_DisplacementMap", displacementTexture);
		normalMat.SetTexture("_HeightMap", heightTexture);
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
			oceanMat.SetTexture("_Height", heightTexture);
			saved = true;
		}
	}

	#endregion
}
