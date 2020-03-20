using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseRenderer : MonoBehaviour
{
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;


	public static Texture2D GenTexture(int width,int height,Color[] colors){
		Texture2D texture = new Texture2D(width,height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colors);
    	texture.Apply();
    	return texture;
	}

	void renderTexture(int width,int height,Color[] colors){
		Texture2D texture = GenTexture(width,height,colors);
    	Renderer textureRender = GetComponent<Renderer>();
    	textureRender.sharedMaterial.mainTexture = texture;
    	textureRender.transform.localScale = new Vector3(width,1,height);
	}

    public void DrawNoiseMap(float[,] noiseMap){
    	int width = noiseMap.GetLength(0);
    	int height = noiseMap.GetLength(1);
    	Color[] colors = new Color[width*height];
    	for(int y=0;y<height;y++){
    		for(int x=0;x<width;x++){
    			colors[y*width+x] = Color.Lerp(Color.black,Color.white,noiseMap[x,y]);
    		}
    	}
    	renderTexture(width,height,colors);
    }

    public void DrawColorMap(int w,int h,Color[] colors){
    	renderTexture(w,h,colors);
    }

    public void DrawMesh(MeshData meshData,int w,int h,Color[] colors){
    	meshFilter.sharedMesh = meshData.Build();
    	meshRenderer.sharedMaterial.mainTexture = GenTexture(w,h,colors);
    }

}
