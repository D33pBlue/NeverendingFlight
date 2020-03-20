using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class NoiseGen : MonoBehaviour
{
	const int chunkSize = 241;
	public enum DrawMode{NoiseMap,ColorMap,MeshMap};
	public DrawMode drawMode;
    public int elevation=10;
    [Range(0,6)]
    public int PreviewLOD;
    public AnimationCurve elevationCurve;
    public float size;
    public int octaves=4;
    [Range(0.000001f,0.99999999f)]
    public float persistance=0.5f;
    public float lacunarity=2;
    public int seed;
    public Vector2 offset;
    public bool updating = true;
    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mdtiQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> mstiQueue = new Queue<MapThreadInfo<MeshData>>();

    public float[,] generateNoise(Vector2 center){
    	System.Random prng = new System.Random(seed);
    	Vector2[] octaveOffsets = new Vector2[octaves];
    	float maxPossibleH = 0;
    	float amplitude = 1;
    	float frequency = 1;
    	for(int i=0;i<octaves;i++){
    		float offsetX = prng.Next(-100000,100000)+offset.x+center.x;
    		float offsetY = prng.Next(-100000,100000)-offset.y-center.y;
    		octaveOffsets[i] = new Vector2(offsetX,offsetY);
    		maxPossibleH += amplitude;
    		amplitude *= persistance;
    	}
    	if(size<=0){// not to divide by zero later
    		size = 0.000001f;
    	}
    	float[,] gen = new float[chunkSize,chunkSize];
    	float maxH = float.MinValue;
    	float minH = float.MaxValue;
    	for(int y=0;y<chunkSize;y++){
    		for(int x=0;x<chunkSize;x++){
    			amplitude = 1;
    			frequency = 1;
    			float noiseHeight = 0;
    			for(int i=0;i<octaves;i++){
    				float sampleX = (x-0.5f*chunkSize+octaveOffsets[i].x)/size*frequency;
    				float sampleY = (y-0.5f*chunkSize+octaveOffsets[i].y)/size*frequency;
    				float perlinValue = Mathf.PerlinNoise(sampleX,sampleY)*2-1;
    				noiseHeight += perlinValue*amplitude;
    				amplitude *= persistance;
    				frequency *= lacunarity;
    			}
    			if(noiseHeight<minH){
    				minH = noiseHeight;
    			}else if(noiseHeight>maxH){
    				maxH = noiseHeight;
    			}
    			gen[x,y] = noiseHeight;
    		}
    	}
    	for(int y=0;y<chunkSize;y++){
    		for(int x=0;x<chunkSize;x++){
    			//gen[x,y] = Mathf.InverseLerp(minH,maxH,gen[x,y]);
    			float normalizedHeight = (gen[x,y]+1)/(2f*maxPossibleH/1.75f);
    			gen[x,y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
    		}
    	}
    	return gen;
    }

    void Start(){
    	//DrawMapInEditor();
    }

    void Update(){
    	bool process = false;
    	lock(mdtiQueue){
    		process = (mdtiQueue.Count>0);
    	}
    	while(process){
    		MapThreadInfo<MapData> threadInfo;
    		lock(mdtiQueue){
    			threadInfo = mdtiQueue.Dequeue();
    			process = (mdtiQueue.Count>0);
    		}
    		threadInfo.callback(threadInfo.parameter);
    	}
    	lock(mdtiQueue){
    		process = (mstiQueue.Count>0);
    	}
    	while(process){
    		MapThreadInfo<MeshData> threadInfo;
    		lock(mstiQueue){
    			threadInfo = mstiQueue.Dequeue();
    			process = (mstiQueue.Count>0);
    		}
    		threadInfo.callback(threadInfo.parameter);
    	}
    }

    public void RequestMapData(Vector2 center,Action<MapData> callback){
    	ThreadStart threadStart = delegate{
    		MapDataThread(center,callback);
    	};
    	new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center,Action<MapData> callback){
    	MapData mapData = GenerateMapData(center);
    	lock(mdtiQueue){
    		mdtiQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));
    	}
    }

    public void RequestMeshData(MapData mapData,int lod,Action<MeshData> callback){
    	ThreadStart threadStart = delegate{
    		MeshDataThread(mapData,lod,callback);
    	};
    	new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData,int lod,Action<MeshData> callback){
    	MeshData meshData = GenerateMesh(mapData.heightMap,lod);
    	lock(mstiQueue){
    		mstiQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
    	}
    }

    public void DrawMapInEditor(){
    	NoiseRenderer renderer = FindObjectOfType<NoiseRenderer>();
    	MapData mapData = GenerateMapData(Vector2.zero);
    	if(drawMode == DrawMode.NoiseMap){
    		renderer.DrawNoiseMap(mapData.heightMap);
    	}else if(drawMode == DrawMode.ColorMap){
    		renderer.DrawColorMap(chunkSize,chunkSize,mapData.colorMap);
    	}else if(drawMode==DrawMode.MeshMap){
    		renderer.DrawMesh(GenerateMesh(mapData.heightMap,PreviewLOD),chunkSize,chunkSize,mapData.colorMap);
    	}
    }

    MapData GenerateMapData(Vector2 center){
    	float[,] gen = generateNoise(center);
    	Color[] colors = new Color[chunkSize*chunkSize];
    	for(int y=0;y<chunkSize;y++){
    		for(int x=0;x<chunkSize;x++){
    			float currentHeight = gen[x,y];
    			for(int i=0;i<regions.Length;i++){
    				if(currentHeight>=regions[i].height){
    					colors[y*chunkSize+x] = regions[i].color;
    				}else{
    					break;
    				}
    			}
    		}
    	}
    	return new MapData(gen,colors);
    }

    MeshData GenerateMesh(float[,] heightMap,int lod){
    	int width = heightMap.GetLength(0);
    	int height = heightMap.GetLength(1);
    	AnimationCurve curve = new AnimationCurve(elevationCurve.keys);
    	float topLeftX = (width-1)/-2.0f;
    	float topLeftZ = (height-1)/2.0f;
    	int meshSimplificationLevel = (lod==0)? 1 : lod*2;
    	int verticesPerLine = (width-1)/meshSimplificationLevel+1;
    	MeshData meshData = new MeshData(verticesPerLine,verticesPerLine);
    	int vertexIndex = 0;
    	for(int y=0;y<height;y+=meshSimplificationLevel){
    		for(int x=0;x<width;x+=meshSimplificationLevel){

    			meshData.vertices[vertexIndex] = 
    				new Vector3(topLeftX+x,
    					elevation*curve.Evaluate(heightMap[x,y]),
    					topLeftZ-y);

    			meshData.uvs[vertexIndex] = new Vector2(
    				x/(float)width,y/(float)height);

    			if(x<width-1 && y<height-1){
    				meshData.addTriangle(vertexIndex,vertexIndex+verticesPerLine+1,
    					vertexIndex+verticesPerLine);
    				meshData.addTriangle(vertexIndex+verticesPerLine+1,vertexIndex,
    					vertexIndex+1);
    			}
    			vertexIndex++;
    		}
    	}
    	meshData.BakeNormals();
    	return meshData;
    }

    void OnValidate(){
    	//if(width<1){width=1;}
    	//if(height<1){height=1;}
    	if(lacunarity<1){lacunarity=1;}
    	if(octaves<0){octaves=0;}
    }

    struct MapThreadInfo<T>{
    	public readonly Action<T> callback;
    	public readonly T parameter;

    	public MapThreadInfo(Action<T> cll,T param){
    		callback = cll;
    		parameter = param;
    	}
    }
}


[System.Serializable]
public struct TerrainType{
	public string name;
	public Color color;
	public float height;
}


public struct MapData{
	public float[,] heightMap;
	public Color[] colorMap;

	public MapData(float[,] hm,Color[] cm){
		heightMap = hm;
		colorMap = cm;
	}
}

public class MeshData{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;
	public Vector3[] bakedNormals;
	int triangleIndex;

	public MeshData(int width,int height){
		vertices = new Vector3[width*height];
		triangles = new int[6*(width-1)*(height-1)];
		uvs = new Vector2[width*height];
	}

	public void addTriangle(int a,int b,int c){
		triangles[triangleIndex] = a;
		triangles[triangleIndex+1] = b;
		triangles[triangleIndex+2] = c;
		triangleIndex += 3;
	}

	Vector3[] calculateNormals(){
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length/3;
		for(int i=0;i<triangleCount;i++){
			int  normalTriangleIndex = i*3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex+1];
			int vertexIndexC = triangles[normalTriangleIndex+2];
			Vector3 triangleNormal = surfaceNormalFromIndices(
				vertexIndexA,vertexIndexB,vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}
		for(int i=0;i<vertexNormals.Length;i++){
			vertexNormals[i].Normalize();
		}
		return vertexNormals;
	}

	Vector3 surfaceNormalFromIndices(int inda,int indb,int indc){
		Vector3 va = vertices[inda];
		Vector3 vb = vertices[indb];
		Vector3 vc = vertices[indc];
		Vector3 sideAB = vb-va;
		Vector3 sideAC = vc-va;
		return Vector3.Cross(sideAB,sideAC).normalized;
	}

	public void BakeNormals(){
		bakedNormals = calculateNormals();
	}

	public Mesh Build(){
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		//mesh.RecalculateNormals();
		mesh.normals = bakedNormals;
		return mesh;
	}

}