using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
	const float scale = 1f;
    public Transform viewer;
    public Material mapMaterial;
    public LODInfo[] detailLevels;

    public static float maxViewDist;
    public static Vector2 viewerPos;
    static NoiseGen mapGenerator;

    const int chunkSize = 240;
    const float viewerMoveThreshForChunkUpd = 25.0f;
    const float sqrviewerMoveThreshForChunkUpd = viewerMoveThreshForChunkUpd*viewerMoveThreshForChunkUpd;
    Vector2 oldViewerPos;
    int chunkVisibleInViewDist;
    Dictionary<Vector2,TerrainChunk> chunksDict = new Dictionary<Vector2,TerrainChunk>();
    static List<TerrainChunk> chunksVisited = new List<TerrainChunk>();

    void Start(){
    	maxViewDist = detailLevels[detailLevels.Length-1].lodThreshold;
    	chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist/chunkSize);
    	mapGenerator = FindObjectOfType<NoiseGen>();
    	UpdateVisibleChunks();
    }

    void Update(){
    	viewerPos = new Vector2(viewer.position.x,viewer.position.z)/scale;
    	if((viewerPos-oldViewerPos).sqrMagnitude>sqrviewerMoveThreshForChunkUpd){
    		oldViewerPos = viewerPos;
    		UpdateVisibleChunks();
    	}
    }

    void UpdateVisibleChunks(){
    	for(int i=0;i<chunksVisited.Count;i++){
    		chunksVisited[i].SetVisible(false);
    	}
    	chunksVisited.Clear();
    	int curChunkCoordX = Mathf.RoundToInt(viewerPos.x/chunkSize);
    	int curChunkCoordY = Mathf.RoundToInt(viewerPos.y/chunkSize);
    	for(int yoff=-chunkVisibleInViewDist;yoff<=chunkVisibleInViewDist;yoff++){
    		for(int xoff=-chunkVisibleInViewDist;xoff<=chunkVisibleInViewDist;xoff++){
    			Vector2 viewedChunkCoord = new Vector2(
    				curChunkCoordX+xoff,curChunkCoordY+yoff);
    			if(chunksDict.ContainsKey(viewedChunkCoord)){
    				chunksDict[viewedChunkCoord].UpdateChunk();
    			}else{
    				chunksDict.Add(viewedChunkCoord,
    					new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform,mapMaterial));
    			}
    		}
    	}
    }

public class TerrainChunk{

	GameObject meshObject;
	Vector2 position;
	Bounds bounds;
	MeshRenderer meshRenderer;
	MeshFilter meshFilter;
	MeshCollider meshCollider;

	LODInfo[] detailLevels;
	LODMesh[] lodMeshes;
	LODMesh collisionLODMesh;

	MapData mapData;
	bool mapDataReceived;
	int prevLODIndex = -1;


	public TerrainChunk(Vector2 coord,int size,LODInfo[] detl,Transform parent,Material material){
		detailLevels = detl;
		position = coord*size;
		bounds = new Bounds(position,Vector2.one*size);
		Vector3 pos3D = new Vector3(position.x,0,position.y);
		meshObject = new GameObject("TerrainChunk");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshObject.transform.position = pos3D*scale;
		meshObject.transform.parent = parent;
		meshObject.transform.localScale = Vector3.one*scale;
		meshRenderer.material = material;
		SetVisible(false);
		lodMeshes = new LODMesh[detailLevels.Length];
		for(int i=0;i<detailLevels.Length;i++){
			lodMeshes[i] = new LODMesh(detailLevels[i].lod,UpdateChunk);
			if(detailLevels[i].useForCollider){
				collisionLODMesh = lodMeshes[i];
			}
		}
		mapGenerator.RequestMapData(position,OnMapDataReceived);
	}

	void OnMapDataReceived(MapData mapData){
		this.mapData = mapData;
		mapDataReceived = true;

		Texture2D texture = NoiseRenderer.GenTexture(241,241,mapData.colorMap);
		meshRenderer.material.mainTexture = texture;

		UpdateChunk();
	}

	public void UpdateChunk(){
		if(mapDataReceived){
			float dist = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
			bool visible = dist<=maxViewDist;
			if(visible){
				int lodIndex = 0;
				for(int i=0;i<detailLevels.Length-1;i++){
					if(dist>detailLevels[i].lodThreshold){
						lodIndex = i+1;
					}else{
						break;
					}
				}
				if(lodIndex != prevLODIndex){
					LODMesh lodMesh = lodMeshes[lodIndex];
					if(lodMesh.hasMesh){
						prevLODIndex = lodIndex;
						meshFilter.mesh = lodMesh.mesh;
					}else if(!lodMesh.hasReqMesh){
						lodMesh.RequestMesh(mapData);
					}
				} 
				if(lodIndex==0){
					if(collisionLODMesh.hasMesh){
						meshCollider.sharedMesh = collisionLODMesh.mesh;
					}else if(!collisionLODMesh.hasReqMesh){
						collisionLODMesh.RequestMesh(mapData);
					}
				}
				chunksVisited.Add(this);
			}
			SetVisible(visible);
		}
	}

	public void SetVisible(bool visible){
		meshObject.SetActive(visible);
	}

	public bool IsVisible(){
		return meshObject.activeSelf;
	}

}

public class LODMesh{
	public Mesh mesh;
	public bool hasReqMesh,hasMesh;
	int lod;
	System.Action updcallback;

	public LODMesh(int l,System.Action cll){
		lod = l;
		updcallback = cll;
	}

	void OnMeshDataReceived(MeshData meshData){
		mesh = meshData.Build();
		hasMesh = true;
		updcallback();
	}

	public void RequestMesh(MapData mapData){
		hasReqMesh = true;
		mapGenerator.RequestMeshData(mapData,lod,OnMeshDataReceived);
	}

}

[System.Serializable]
public struct LODInfo{
	public int lod;
	public float lodThreshold;
	public bool useForCollider;
}

}


