using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMotion : MonoBehaviour
{
	public float speed = 1;
	public GameObject explosion;
	public Transform campos;
	const float hlimit = 100;
	Rigidbody rb;
	float prevSpeed = -1;
	Vector3 rot;
	GUIStyle style=new GUIStyle();
	
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        style.normal.textColor = Color.black;
    }

    // Update is called once per frame
    void Update()
    {
    	bool uplimit = gameObject.transform.position.y>hlimit;
    	speed += Input.GetAxis("Acc")*Time.deltaTime*10f;
    	float yaw = Input.GetAxis("Horizontal")*Time.deltaTime*10;
    	float pitch = Input.GetAxis("Vertical")*Time.deltaTime*10;
    	if(speed<1){speed=1;}
    	if(speed>25){speed=25;}
    	if(pitch<-80){pitch=-80;}
    	if(pitch>80){pitch=80;}
    	if(uplimit){
    		pitch=1;
    	}
    	//Quaternion q = Quaternion.Euler( -pitch,yaw,0 );
    	//dir = q*dir;
        //gameObject.transform.position += dir*speed;
        if(speed!=prevSpeed || pitch!=0 || yaw!=0){
        	prevSpeed = speed;
        	rot += new Vector3(pitch,yaw,-speed*yaw);
        	gameObject.transform.Rotate(pitch,yaw,-speed*yaw);
        	Vector3 dir = gameObject.transform.forward;
        	rb.velocity = dir*speed*10;
        }

        //if(campos.position.y<gameObject.transform.position.y){
        //	campos.position += new Vector3(
        //		0,2*(gameObject.transform.position.y-campos.position.y),0);
        //}
        
        //if(uplimit){
        //	gameObject.transform.position += new Vector3(0,hlimit-gameObject.transform.position.y,0);
        //}
        //gameObject.transform.rotation = Quaternion.Euler(-90,0,0);
    }

    void OnGUI(){
    	string status = "[Pitch:"+rot.x+"][Yaw:"+rot.y+"]Roll:"+rot.z+"]";
    	GUI.Label(new Rect(20,20,300,50),status,style);
    }

    void OnCollisionEnter(){
    	Debug.Log("Collision!!!");
    	rb.isKinematic = true;
    	rb.velocity = new Vector3(0,0,0);
    	rb.angularVelocity = new Vector3(0,0,0);
    	explosion.SetActive(true);
    }


}
