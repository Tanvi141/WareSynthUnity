using UnityEngine;
using System;
using System.Collections.Generic;         //Allows us to use Lists.
using Random = UnityEngine.Random;         //Tells Random to use the Unity Engine random number generator.
using System.IO;
using System.Threading.Tasks;

struct WareHouseConfig
{
    public int num_cols;
    public int num_racks_per_col;
    public int num_rack_per_cell;
    public float corridor_width;
}

struct BoxProperties
{
    public float x;
    public float y;
    public float z;
}

struct CameraPosition
{
    public float x;
    public float z;
    public float y;
    public int j;
    public int k;
    public int rack_num;
    string rack_in_focus;
}
public class BoardManager : MonoBehaviour
{
    //manually populated
    public GameObject[] boxes;
    public GameObject topShelfObj;
    public GameObject shelfObj;
    public GameObject botShelfObj;
    public GameObject warehouseObj;
    public GameObject invisibleObject;
    public GameObject floorObject;
    public GameObject wallObject;
    public Texture2D[] textures_wall;
    // public Texture2D[] textures_wall;
    public Texture2D[] textures_shelf;

    //holder variables
    private Transform rackHolder;
    private Transform lightHolder;
    private Transform shelfHolder;
    private Transform wareHolder;

    private BoxProperties[] boxTrack;
    private CameraPosition[] cameraPoses;
    private WareHouseConfig warehouse_params;
    private GameObject cameraObj;
    private string save_path;

    public float shelf_x;
    public float shelf_y;
    public float shelf_z;

    public float bounding_x;
    public float bounding_y;
    public float bounding_z;

    private int imNum;
    private int stacks = 0;

	
	void changeCameraParam()
    {    
        float sizeX, sizeY;
        int width, height;
     
        Camera camera = cameraObj.GetComponent<Camera>();
        int resWidth = camera.pixelWidth; //Screen.width;
        int resHeight = camera.pixelHeight; //Screen.height;
        float f = 24.0f;

        float ax = 600.0f;
        float ay = 600.0f;
        float px = resWidth/2;
        float py = resHeight/2;
     
        sizeX = f * resWidth / ax;
        sizeY = f * resHeight / ay;

        //PlayerSettings.defaultScreenWidth = width;
        //PlayerSettings.defaultScreenHeight = height;

        float shiftX = -(px - resWidth / 2.0f) / resWidth;
        float shiftY = (py - resHeight / 2.0f) / resHeight;

        cameraObj.GetComponent<Camera>().sensorSize = new Vector2(sizeX, sizeY);     // in mm, mx = 1000/x, my = 1000/y
        cameraObj.GetComponent<Camera>().focalLength = f;                            // in mm, ax = f * mx, ay = f * my
        cameraObj.GetComponent<Camera>().lensShift = new Vector2(shiftX, shiftY);    // W/2,H/w for (0,0), 1.0 shift in full W/H in image plane

        //K = new float[3,3] {{ax,    0,  px}, {0,    ay, py}, {0,    0,  1}};

	}

    Vector2 getScreenPoint(Camera c, Vector3 position){
		Vector3 screenPos = c.WorldToScreenPoint(position);
		screenPos.y = (float)Screen.height - screenPos.y;
		return screenPos;
	}

	
    float get_x_size(GameObject obj)
    {
        MeshFilter MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
        return MeshObj.sharedMesh.bounds.size.x * obj.transform.localScale.x;
    }

    void addInvis(GameObject box, int box_num, Transform boxHolder)
    {

        for (int z = -1; z <= 1; z += 1)
        {
            Transform pointsHolder = new GameObject("PointsInside_" + z + box.name).transform;
            pointsHolder.transform.SetParent(boxHolder);

            for (float x = box.transform.position.x - boxTrack[box_num].x / 2.0f; x < box.transform.position.x + boxTrack[box_num].x / 2.0f; x += 0.02f)
            {
                for (float y = box.transform.position.y; y < box.transform.position.y + boxTrack[box_num].y; y += 0.02f)
                {
                    GameObject invis = Instantiate(invisibleObject) as GameObject;
                    //GameObject invis = new GameObject();

                    invis.transform.position = new Vector3(x, y, box.transform.position.z + z * boxTrack[box_num].z / 2.0f);

                    invis.transform.SetParent(pointsHolder);
                    invis.name = "Point_" + x + "_" + y + "_" + z;
                }
            }
        }
    }

    void addInvisToShelf(GameObject shelf)
    {

        for (float z = -1; z <= 1; z += 1)
        {
            Transform pointsHolder = new GameObject("PointsInside_" + z + shelf.name).transform;
            pointsHolder.transform.SetParent(shelfHolder);
            for (float x = shelf.transform.position.x - shelf_x / 2.0f; x < shelf.transform.position.x + shelf_x / 2.0f; x += 0.04f)
            {
                for (float y = shelf.transform.position.y; y <= shelf.transform.position.y + shelf_y - 0.02 * 4; y += 0.04f)
                {
                    GameObject invis = Instantiate(invisibleObject) as GameObject;
                    //GameObject invis = new GameObject();

                    invis.transform.position = new Vector3(x, y, shelf.transform.position.z + z * shelf_z / 2.0f);

                    invis.transform.SetParent(pointsHolder);
                    invis.name = "Point_" + x + "_" + y + "_" + z;
                }
            }
        }
    }
    Vector3 newScale(Vector3 Size, Vector3 newSize)
    {
        Vector3 rescale;
        rescale.x = newSize.x / Size.x;
        rescale.y = newSize.z / Size.z;
        rescale.z = newSize.y / Size.y;
        return rescale;
    }


    float get_rand_val(float small, float big, float box_lim, float margin = 0)
    {
        // //Debug.Log("passed" + small + " " + box_lim/2.0f + " " + margin);
        float lower = small + box_lim / 2.0f + margin;
        float upper = big - box_lim / 2.0f - margin;
        if (lower < upper)
            return Random.Range(lower, upper);
        else
            return (lower + upper) / 2.0f;
    }

    void placeObjectsOnShelfOld(GameObject shelf)
    {

        //if (Random.Range(0.0f, 1.0f) > 0.85f) return;
        
        int inds = 0;

        float shelf_len = shelf_x;
        float x_small = shelf.transform.position.x - shelf_len / 2.0f + Random.Range(0.1f, 0.2f);
        float x_big = shelf.transform.position.x + shelf_len / 2.0f - Random.Range(0.1f, 0.2f);

        int direction = Random.Range(0, 2);
        //// //Debug.Log(x_small+" " +x_big);
        while (x_small < x_big)
        {

            //place box
            int box_num = Random.Range(0, boxes.Length);
            ////Debug.Log(boxTrack[box_num].x + " " + boxTrack[box_num].y + " " + boxTrack[box_num].z);
            //0.4
            float x_scale = Random.Range(0.2f/boxTrack[box_num].x, 0.4f/boxTrack[box_num].x);
            Vector3 rescale = new Vector3(
                x_scale, 
                Random.Range(0.2f/boxTrack[box_num].y, x_scale), 
                Random.Range(0.75f, 0.95f));

            float box_x = boxTrack[box_num].x*rescale.x;

            if (x_small + box_x > x_big)
            {
                break;
            }

            float box_y = boxTrack[box_num].y*rescale.z;

            // //Debug.Log("y-pos= " + shelf.transform.position.y + " " + box_y + " " + shelf_y);

            float y_small = shelf.transform.position.y - box_y;
            float y_big = shelf.transform.position.y + shelf_y - 0.5f; //ADJUST AS PER MODEL
            // //Debug.Log("box_num " + box_num + " " + y_small + " " + y_big);
            float z_pos = shelf.transform.position.z;
            
            float z_low = -(shelf_z)/4.0f + boxTrack[box_num].z*rescale.z/2.0f ;
            float z_high = +(shelf_z)/4.0f - boxTrack[box_num].z*rescale.z/2.0f;

            if (z_low < z_high){
                z_pos += Random.Range(z_low, z_high);
            } 

            while (y_small + box_y < y_big)
            {
                //place the box with a probability
                if (Random.Range(0.0f, 1.0f) >= 0.15f)
                {
                    // //Debug.Log("box_num " + box_num + " " + y_small + " " + y_big);
                    y_small += box_y;

                    
                    GameObject boxInstance = Instantiate(boxes[box_num]) as GameObject;
                    boxInstance.isStatic = true;
                    if (direction == 0)
                        boxInstance.transform.position = new Vector3(x_small + box_x / 2.0f, y_small, z_pos);
                    else
                        boxInstance.transform.position = new Vector3(x_big - box_x / 2.0f, y_small, z_pos);
                    
                    boxInstance.transform.Rotate(0, 0, Random.Range(-7, 7));

                    //string boxName = rackHolder.name+"_"+shelf.name+"_"+boxInstance.name.Substring(0, 5)+"_"+inds;
                    string boxName = boxInstance.name.Substring(0, boxInstance.name.Length) + " stack " + stacks;
                    //boxInstance.transform.Rotate(90, 0, 0);
                    boxInstance.transform.SetParent(shelfHolder);
                    boxInstance.name = boxName;
                    inds += 1;
                    boxInstance.AddComponent<Rigidbody>();
                    boxInstance.AddComponent<BoxCollider>();
                    float H, S, V;
                    Color.RGBToHSV(boxInstance.GetComponent<Renderer>().material.GetColor("_Color"), out H, out S, out V);
                    // //Debug.Log("H: " + H + " S: " + S + " V: " + V);
                    int variable = Random.Range(0, 1);
                    boxInstance.transform.localScale = rescale;
                    boxInstance.GetComponent<Renderer>().material.color = UnityEngine.Random.ColorHSV(H, H + 0.9f, S + 0.1f, S + 0.1f, V - 0.3f, V - 0.3f);
                    boxInstance.GetComponent<Rigidbody>().useGravity = false; //hackymethod
                    boxInstance.GetComponent<Rigidbody>().isKinematic = true; //hackymethod

                    Transform boxHolder = new GameObject("Box_" + inds + "_" + boxInstance.name).transform;
                    boxHolder.transform.SetParent(shelfHolder);

                    boxInstance.transform.SetParent(boxHolder);
                    addInvis(boxInstance, box_num, boxHolder);

                }
                else
                {
                    break;
                }
                // break; //Uncomment to disallow box stacking
            }
            stacks += 1;

            if (direction == 0)
                x_small = x_small + box_x + Random.Range(0.03f, 0.14f);
            else
                x_big = x_big - box_x - Random.Range(0.04f, 0.16f);

        }
    }

    void print_annotations(Camera c, GameObject go, Transform searchIn, string ann_filename, string pos_text){
		
		string text = go.name + ", " + pos_text;
		
		text += ", " + go.transform.position.x;
		text += ", " + go.transform.position.y;
		text += ", " + go.transform.position.z;
		text += ", " + go.transform.rotation.x;
		text += ", " + go.transform.rotation.y;
		text += ", " + go.transform.rotation.z;
		text += ", " + go.transform.localScale.x;
		text += ", " + go.transform.localScale.y;
		text += ", " + go.transform.localScale.z;
		
		
		text += ", " + c.transform.position.x;
		text += ", " + c.transform.position.y;
		text += ", " + c.transform.position.z;
		text += ", " + c.transform.rotation.x;
		text += ", " + c.transform.rotation.y;
		text += ", " + c.transform.rotation.z;
	
		for(int z = -1; z<=1; z+=1){
		
			foreach (Transform nextchild in searchIn){	
				//// //Debug.Log(nextchild.gameObject.name);
		  		if (nextchild.gameObject.name == "PointsInside_"+z+go.name)
		  		{
					float []vis_bounds = IsTargetVisible(c, go, z, nextchild.gameObject);
					text += ", " + z;  
					for(int i = 0; i < 13; i++){ 
						text += ", " + vis_bounds[i];
					}
				}
			}
		}

		
		File.AppendAllText(ann_filename, text+"\n");
	}

    float[] IsTargetVisible(Camera c,GameObject go, int z, GameObject inVisPointHolder){
		
		float []minX = {float.MaxValue, float.MaxValue}; //full object, in view
		float []minY = {float.MaxValue, float.MaxValue};
		float []minZ = {float.MaxValue, float.MaxValue};
		float []maxX = {float.MinValue, float.MinValue};
		float []maxY = {float.MinValue, float.MinValue};
		float []maxZ = {float.MinValue, float.MinValue};
		
		int side_tracker = 0;
		
		//GameObject inVisPointHolder = GameObject.Find("PointsInside_"+z+go.name);
		
		foreach (Transform child in inVisPointHolder.transform){
	  		int flag = 0;
	  		if(IsObjectVisibleFrustum(c, child.gameObject)){
	  			flag = 1;
	  			side_tracker = 1;
	  		}
	  		
	  		for(int i=0; i<=flag; i++){
	  			if (minX[i] > child.position.x)  minX[i] = child.position.x;
	  			if (maxX[i] < child.position.x)  maxX[i] = child.position.x;
	  			if (minY[i] > child.position.y)  minY[i] = child.position.y;
	  			if (maxY[i] < child.position.y)  maxY[i] = child.position.y;
	  			if (minZ[i] > child.position.z)  minZ[i] = child.position.z;
	  			if (maxZ[i] < child.position.z)  maxZ[i] = child.position.z;
	  		}
		}
		
		float[] bounds_vis = new float[13];
		bounds_vis[0] = side_tracker;
		
		bounds_vis[1] = minX[1];
		bounds_vis[2] = maxX[1];
		bounds_vis[3] = minY[1];
		bounds_vis[4] = maxY[1];
		bounds_vis[5] = minZ[1];
		bounds_vis[6] = maxZ[1];
		
		bounds_vis[7] = minX[0];
		bounds_vis[8] = maxX[0];
		bounds_vis[9] = minY[0];
		bounds_vis[10] = maxY[0];
		bounds_vis[11] = minZ[0];
		bounds_vis[12] = maxZ[0];
		
		//// //Debug.Log(go.name +" "+ z +" X PERCENTAGE IS "+ (maxX[1] - minX[1])/(maxX[0] - minX[0]) +"    Y PERCENTAGE IS"+ (maxY[1] - minY[1])/(maxY[0] - minY[0]));
		
		return bounds_vis;
		
	}


    bool IsObjectVisibleFrustum(Camera c, GameObject go)
    {
        float[] bounds_vis = new float[2];

        var planes = GeometryUtility.CalculateFrustumPlanes(c);
        var point = go.transform.position; //center of the object
        foreach (var plane in planes)
        {
            if (plane.GetDistanceToPoint(point) < 0)
                return false;
        }

        return true;
    }

    void SetupWarehouse(){
     	
     	lightHolder = new GameObject("Lights").transform;
     	lightHolder.transform.SetParent(wareHolder);
     	
     	//placing floor
     	// GameObject obj = Instantiate(floorObject) as GameObject;
		// obj.isStatic = true;
		// obj.transform.localScale = new Vector3(1f, 1f, 1f);
		int texture_to_choose = Random.Range(0, textures_wall.Length);
        // texture_to_choose = 2;
        // obj.transform.position = new Vector3(0f, -4f, 0f);
		// obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];

        GameObject obj = Instantiate(floorObject) as GameObject;
		MeshFilter MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
		// bounding_x =  MeshObj.sharedMesh.bounds.size.x*Math.Abs(obj.transform.localScale.x)*3.5f/3f;
		// bounding_y =  MeshObj.sharedMesh.bounds.size.y*Math.Abs(obj.transform.localScale.y)*3.5f/3f;
		// bounding_z =  MeshObj.sharedMesh.bounds.size.z*Math.Abs(obj.transform.localScale.z);
        bounding_x =  MeshObj.sharedMesh.bounds.size.x*Math.Abs(obj.transform.localScale.x);
		bounding_y =  MeshObj.sharedMesh.bounds.size.y*Math.Abs(obj.transform.localScale.y);
		bounding_z =  MeshObj.sharedMesh.bounds.size.z*Math.Abs(obj.transform.localScale.z);

		obj.isStatic = true;
		obj.transform.localScale = new Vector3(1f, 1f, 1f);
		obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
        obj.transform.position = new Vector3(0f, -1.35f, 8f);
	    obj.transform.SetParent(wareHolder);
        // //Debug.Log("XYZ" + bounding_x + " " + bounding_y + " " + bounding_z);
        // float x_ceil = 0f;
        // float z_ceil = 8f;
        // for (float i = -4; i < 1; i = i + 0.99f)
        // {
        //     for (float j = -2; j < 1; j= j + 0.99f){
        //         obj = Instantiate(floorObject) as GameObject;
		//         obj.isStatic = true;
		//         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		//         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		//         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
        //         obj.transform.position = new Vector3(x_ceil + i*bounding_x, -0.249f, z_ceil + j*35f*bounding_z);
	    //         obj.transform.SetParent(wareHolder);
        //     }
        // }
	    

	    //placing ceil
        obj = Instantiate(floorObject) as GameObject;
		obj.isStatic = true;
		obj.transform.localScale = new Vector3(1f, 1f, 1f);
		obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
        obj.transform.position = new Vector3(0f, 11f, 8f);
	    obj.transform.SetParent(wareHolder);
        // float x_floor = 0f;
        // float z_floor = 8f;
        // for (float i = -4; i < 1; i = i + 0.99f)
        // {
        //     for (float j = -2; j < 1; j= j + 0.99f){
        //         obj = Instantiate(floorObject) as GameObject;
		//         obj.isStatic = true;
		//         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		//         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		//         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
        //         obj.transform.position = new Vector3(x_floor + i*bounding_x, 8f, z_floor + j*35f*bounding_z);
	    //         obj.transform.SetParent(wareHolder);
        //     }
        // }
	    
		//placing wall	
	    texture_to_choose = Random.Range(0, textures_wall.Length);
		// texture_to_choose = 5;
        // //Debug.Log(texture_to_choose);
	    
			
	    for(int n=0; n<4; n++){
	    	obj = Instantiate(floorObject) as GameObject;
	    	obj.transform.localScale = new Vector3(1f, 1f, 1f);
	    	obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
			obj.GetComponent<Renderer>().material.SetFloat("_Smoothness", 1.0f);
			obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
			obj.isStatic = true;	
			float H, S, V;
			// Color.RGBToHSV(obj.GetComponent<Renderer>().material.GetColor("_Color"), out H, out S, out V);
			// if (texture_to_choose >= 2)
				// obj.GetComponent<Renderer>().material.color = UnityEngine.Random.ColorHSV(H, H, S, S, 0.6f, 0.6f); 
		
			
			if(n==0){
				obj.transform.Rotate(0, 90, 0);
				// obj.transform.position = new Vector3(bounding_x/2.0f, 2.149f, 0);
                obj.transform.position = new Vector3(10, 2.149f, 0);
			    // obj.transform.SetParent(wareHolder);
                
                // float z_behind = 0f;
                // float y_behind = 2.149f;
                // for (float i = -1; i < 4; i = i + 1f)
                // {
                //     for (float j = -1; j < 2; j= j + 1f)
                //     {
                //         obj = Instantiate(floorObject) as GameObject;
		        //         obj.isStatic = true;
				//         obj.transform.Rotate(0, 90, 0);
		        //         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		        //         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		        //         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
                //         obj.transform.position = new Vector3(4.5f, y_behind + j*bounding_y, z_behind + i*bounding_x);
	            //         obj.transform.SetParent(wareHolder);
                //     }
                // }

			}
			else if(n==1){
				obj.transform.Rotate(0, -90, 0);
				// obj.transform.position = new Vector3(-bounding_x/2.0f, 2.149f, 0);
                obj.transform.position = new Vector3(-30f, 2.149f, 0);
			    // obj.transform.SetParent(wareHolder);

                // float z_behind = 0f;
                // float y_behind = 2.149f;
                // for (float i = -1; i < 4; i = i + 1f)
                // {
                //     for (float j = -1; j < 2; j= j + 1f)
                //     {
                //         obj = Instantiate(floorObject) as GameObject;
		        //         obj.isStatic = true;
				//         obj.transform.Rotate(0, -90, 0);
		        //         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		        //         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		        //         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
                //         obj.transform.position = new Vector3(-25, y_behind + j*bounding_y, z_behind + i*bounding_x);
	            //         obj.transform.SetParent(wareHolder);
                //     }
                // }
			}
			else if(n==2){
				obj.transform.Rotate(90, 0, 0);
				// obj.transform.position = new Vector3(0, 2.149f, bounding_x/2.0f);
                obj.transform.position = new Vector3(0f, 2.149f, 11f);
			    // obj.transform.SetParent(wareHolder);
               
                // float x_behind = 0f;
                // float y_behind = 2.149f;
                // for (float i = -4; i < 2; i = i + 1f)
                // {
                //     for (float j = -1; j < 2; j= j + 1f)
                //     {
                //         obj = Instantiate(floorObject) as GameObject;
		        //         obj.isStatic = true;
				//         obj.transform.Rotate(90, 0, 0);
		        //         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		        //         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		        //         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
                //         obj.transform.position = new Vector3(x_behind + i*bounding_x, y_behind + j*bounding_y, 11f);
	            //         obj.transform.SetParent(wareHolder);
                //     }
                // }
                
			}
			else if(n==3){
				obj.transform.Rotate(-90, 0, 0);
                obj.transform.position = new Vector3(0f, 2.149f, -11);
                
                // float x_behind = 0f;
                // float y_behind = 2.149f;
                // for (float i = -4; i < 2; i = i + 1f)
                // {
                //     for (float j = -1; j < 2; j= j + 1f)
                //     {
                //         obj = Instantiate(floorObject) as GameObject;
		        //         obj.isStatic = true;
				//         obj.transform.Rotate(-90, 0, 0);
		        //         obj.transform.localScale = new Vector3(3.5f, 3.5f, 0.1f);
		        //         obj.GetComponent<Renderer>().material.mainTexture = textures_wall[texture_to_choose];
		        //         obj.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);
                //         obj.transform.position = new Vector3(x_behind + i*bounding_x, y_behind + j*bounding_y, -7f);
	            //         obj.transform.SetParent(wareHolder);
                //     }
                // }
		    }
			
			obj.transform.SetParent(wareHolder);
				
	    }
		place_lights();
		changeCameraParam();
     }
     
     
    void place_lights_position(Vector3 postion, float range, float intensity)
    {
        GameObject lightGameObject = new GameObject("Point Light");
        Light lightComp = lightGameObject.AddComponent<Light>();
        lightComp.color = Color.white;
        lightComp.intensity = intensity;
        lightComp.range = range;
        lightComp.shadows = LightShadows.Soft;
        lightGameObject.transform.position = postion;
        lightGameObject.transform.SetParent(lightHolder);
    }

    void place_lights()
    {

        for (float i = -24; i < 4; i = i + 1.0f)
        {
            for (float j = 0; j < 7.5; j = j + 1.0f)
            {
                bool place = true;
                if (Random.Range(0.0f, 1.0f) < -0.1f)
                {
                    place = false;
                }
                if (place == true)
                {

                    float intensity_low = Random.Range(0.3f, 0.35f);
                    float range_low = Random.Range(20f, 30f); //Random.Range(4.0f, 17.0f);

                    place_lights_position(new Vector3(i, j, -9.8f), range_low, intensity_low);
                }
            }

        }

        for (float i = -24f; i < 4f; i = i + 1.0f)
        {
            for (float j = -4f; j < 9.85f; j = j + 1.0f)
            {
                bool place = true;
                if (Random.Range(0.0f, 1.0f) < -0.1f)
                {
                    place = false;
                }
                if (place == true)
                {

                    float intensity_low = Random.Range(0.4f, 0.45f);
                    float range_low = Random.Range(20f, 30f); //Random.Range(4.0f, 17.0f);

                    place_lights_position(new Vector3(i, 9f, -j), range_low, intensity_low);
                }
            }

        }

        // for (float i = -24; i < 4; i = i + 1.0f)
        // {
        //     for (float j = 0; j < 7.5; j = j + 1.0f)
        //     {
        //         bool place = true;
        //         if (Random.Range(0.0f, 1.0f) < -0.1f)
        //         {
        //             place = false;
        //         }
        //         if (place == true)
        //         {

        //             float intensity_low = Random.Range(1f, 1.3f);
        //             float range_low = Random.Range(3f, 4f); //Random.Range(4.0f, 17.0f);

        //             place_lights_position(new Vector3(i, j, -5.76f), range_low, intensity_low);
        //         }
        //     }

        // }

        // for (float i = 0.89f; i < 6.87f; i = i + 1.0f)
        // {
        //     for (float j = -5.76f; j < 1.21f; j = j + 1.0f)
        //     {
        //         bool place = true;
        //         if (Random.Range(0.0f, 1.0f) < -0.1f)
        //         {
        //             place = false;
        //         }
        //         if (place == true)
        //         {

        //             float intensity_low = Random.Range(1f, 1.5f);
        //             float range_low = Random.Range(3.0f, 3.5f); //Random.Range(4.0f, 17.0f);

        //             place_lights_position(new Vector3(-24.0f, i, j), range_low, intensity_low);
        //         }
        //     }
        // }

        // for (float i = 0.89f; i < 6.87f; i = i + 1.0f)
        // {
        //     for (float j = -5.76f; j < 1.21f; j = j + 1.0f)
        //     {
        //         bool place = true;
        //         if (Random.Range(0.0f, 1.0f) < -0.1f)
        //         {
        //             place = false;
        //         }
        //         if (place == true)
        //         {

        //             float intensity_low = Random.Range(1f, 1.5f);
        //             float range_low = Random.Range(3.0f, 3.5f); //Random.Range(4.0f, 17.0f);

        //             place_lights_position(new Vector3(4.0f, i, j), range_low, intensity_low);
        //         }
        //     }
        // }
    }

    void GetTwoImages_path(CameraPosition camPos)
    {
    	Camera camera = cameraObj.GetComponent<Camera>();
    	  //Bot 2
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(0.99f, 1.09f),
            camPos.z - 2.35f + Random.Range(-3.0f, -2.36f)
            );
        ExtractDataPoint(camPos);       

        //Top 2
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(2f, 2.05f),
            camPos.z - 2.35f + Random.Range(-3.0f, -2.7f)
            );
        ExtractDataPoint(camPos);     
		//All 3
        cameraObj.transform.position = new Vector3(
            camPos.x + Random.Range(-0.2f, 0.2f),
            camPos.y - 0.02f + Random.Range(1.54f, 1.57f),
            camPos.z - 2.35f + Random.Range(-4.78f, -4.68f)
            );
        ExtractDataPoint(camPos);
        
       float x = -7.088681f;
       float y = 1.566397f;
       float z = -6.737548f;
       
       //-7.089, 1.576, -8.555
		while (z > -8.555){		
			cameraObj.transform.position = new Vector3(x, y, z);
			ExtractDataPoint(camPos);
			z -= 1.0f/48.0f;
		}
		
       
       // -8.46, 1.576, -8.555
       while (x > -8.46){		
			cameraObj.transform.position = new Vector3(x, y, z);
			ExtractDataPoint(camPos);
			x -= 1.0f/48.0f;
		}
		
		// -6.86, 1.576, -8.555
       while (x < -6.86){		
			cameraObj.transform.position = new Vector3(x, y, z);
			ExtractDataPoint(camPos);
			x += 1.0f/48.0f;
		}
		
       //  -6.86, 1.576, -7.2
       while (z < -7.2){		
			cameraObj.transform.position = new Vector3(x, y, z);
			ExtractDataPoint(camPos);
			z += 1.0f/48.0f;
		}
		
  
    }
    
    void GetTwoImages(CameraPosition camPos)
    {
        Camera camera = cameraObj.GetComponent<Camera>();
        //Bot 1
        // //Debug.Log(camPos.x + " " + camPos.y + " " + camPos.z);
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(0.311f, 0.385f),
            camPos.z - 2.35f + Random.Range(-0.733f, -0.396f)
            );
        ExtractDataPoint(camPos);
        return;
        // Mid 1
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(1.6f, 1.42f),
            camPos.z - 2.35f + Random.Range(-0.733f, -0.396f)
            );
        ExtractDataPoint(camPos);
        // return;

        //Top 1
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(2.56f, 2.76f),
            camPos.z - 2.35f + Random.Range(-0.733f, -0.396f)
            );
        ExtractDataPoint(camPos);

        //Bot 2
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(0.99f, 1.09f),
            camPos.z - 2.35f + Random.Range(-3.0f, -2.36f)
            );
        ExtractDataPoint(camPos);       

        //Top 2
        cameraObj.transform.position = new Vector3(
            camPos.x + + Random.Range(-0.1f, 0.1f),
            camPos.y - 0.02f + Random.Range(2f, 2.05f),
            camPos.z - 2.35f + Random.Range(-3.0f, -2.7f)
            );
        ExtractDataPoint(camPos);     

        //All 3
        cameraObj.transform.position = new Vector3(
            camPos.x + Random.Range(-0.2f, 0.2f),
            camPos.y - 0.02f + Random.Range(1.54f, 1.57f),
            camPos.z - 2.35f + Random.Range(-4.78f, -4.68f)
            );
        ExtractDataPoint(camPos);     
    }

    	string util(Camera c, GameObject go){
		string text = "";
		for (int i=0; i<5; i+=1){	
			for(int j=0; j<5; j+=1){
				for(int k=0; k<5; k+=1){
					Vector3 trans = new Vector3(go.transform.position.x + Random.Range(-0.01f*i,0.01f*i), 
					go.transform.position.y + Random.Range(-0.01f*j,0.01f*j), 
					go.transform.position.z + Random.Range(-0.01f*k,0.01f*k));
					
					Vector3 screenPos = getScreenPoint(c, trans);
					
					if(screenPos.x >= 0.0f && screenPos.x <= cameraObj.GetComponent<Camera>().pixelWidth && screenPos.y >= 0.0f && screenPos.y <= cameraObj.GetComponent<Camera>().pixelHeight){
						text += trans.x;
						text += ", " + trans.y;
						text += ", " + trans.z;
						text += ", " + screenPos.x;
						text += ", " + screenPos.y;
						text += "\n";
					}
				}
			}
		}
		
		return text;
	}

    void write_correspondences(CameraPosition camPos){
	
		//string 3d_file = string.Format(save_path +"/Correspondences/"+imNum.ToString().PadLeft(6, '0')+".txt", save_path); 
		string text = "";
		for (int x_Rack = camPos.j; x_Rack <= camPos.j; x_Rack += 1){
			
			if(x_Rack < 0 || x_Rack >= warehouse_params.num_racks_per_col) {
				continue;
			}
			
            string RackName = "Rack_" + x_Rack + "_" + camPos.k + "_" + camPos.rack_num;
			// //Debug.Log(imNum + RackName);
			GameObject mainRack = GameObject.Find(RackName);
			
            foreach (Transform child in mainRack.transform)
            {

                if (child.gameObject.name.Length > 10) //Objects on ...
                { //means holder for boxes
                    string shelfName = child.gameObject.name.Substring(9, child.gameObject.name.Length - 9);
                    foreach (Transform box_holder in child.gameObject.transform)
                    {
                        string res = box_holder.name.Substring(0, 1);
                        if (res.Equals("P"))
                            continue;

                        foreach (Transform box in box_holder.gameObject.transform)
                        {
                            res = box.name.Substring(0, 1);
                            if (res.Equals("P"))
                                continue;
      						text += util(cameraObj.GetComponent<Camera>(), box.gameObject);   

                        }
                    }
                }
                else
                {
                    string res = child.gameObject.name.Substring(0, 1);
                    if (res.Equals("P"))
                        continue;

                    foreach (Transform nextchild in mainRack.transform)
                    {
                        if (nextchild.gameObject.name == "ObjectsOn" + child.gameObject.name)
                        {
	  						text += util(cameraObj.GetComponent<Camera>(), child.gameObject);    	
                        }
                    }
                }
            }

	    }
	    
	    string ann_filename = string.Format(save_path +"/Correspondences/"+imNum.ToString().PadLeft(6, '0')+".txt", save_path);
		File.WriteAllText(ann_filename, text);
	}

    void ExtractDataPoint(CameraPosition camPos)
    {
        // //Debug.Log(cameraObj.transform.position);
        cameraObj.transform.eulerAngles = new Vector3(0, 0, 0);
        // cameraObj.transform.eulerAngles = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f,1f), Random.Range(-1f,1f));
        //Fix the position of the camera to X:-1.45 Y:0.85 Z:1 with respect to the rack.
        // cameraObj.transform.position = new Vector3(2f, 0.82f, 1.4f);
        Camera camera = cameraObj.GetComponent<Camera>();

        //annotation file
        string ann_filename = string.Format(save_path + "/Annotations/" + imNum.ToString().PadLeft(6, '0') + ".txt", save_path);
        File.WriteAllText(ann_filename, "Rack_" + camPos.j + "_" + camPos.k + "_" + camPos.rack_num + "\n");

        write_correspondences(camPos);
        //go through the three racks and extract annotation

        for (int x_Rack = camPos.j; x_Rack <= camPos.j; x_Rack += 1)
        {

            if (x_Rack < 0 || x_Rack >= warehouse_params.num_racks_per_col)
            {
                continue;
            }

            string RackName = "Rack_" + x_Rack + "_" + camPos.k + "_" + camPos.rack_num;
            // "Rack_" + j + "_" + k + "_" + rack_num;
            //Debug.Log(imNum + RackName);
            GameObject mainRack = GameObject.Find(RackName);
            foreach (Transform child in mainRack.transform)
            {

                if (child.gameObject.name.Length > 10) //Objects on ...
                { //means holder for boxes
                    string shelfName = child.gameObject.name.Substring(9, child.gameObject.name.Length - 9);
                    foreach (Transform box_holder in child.gameObject.transform)
                    {
                        string res = box_holder.name.Substring(0, 1);
                        if (res.Equals("P"))
                            continue;

                        foreach (Transform box in box_holder.gameObject.transform)
                        {
                            res = box.name.Substring(0, 1);
                            if (res.Equals("P"))
                                continue;
                            print_annotations(camera, box.gameObject, box_holder.gameObject.transform, ann_filename, RackName + ", " + shelfName);
                        }
                    }
                }
                else
                {
                    string res = child.gameObject.name.Substring(0, 1);
                    if (res.Equals("P"))
                        continue;

                    foreach (Transform nextchild in mainRack.transform)
                    {
                        if (nextchild.gameObject.name == "ObjectsOn" + child.gameObject.name)
                        {
                            print_annotations(camera, child.gameObject, nextchild, ann_filename, RackName + ", " + child.gameObject.name);
                        }
                    }
                }
            }
        }


        //take photo
        
        cameraObj.GetComponent<ImageSynthesis>().Save(save_path, imNum.ToString().PadLeft(6, '0'));

        imNum += 1;
    }

    void BoardSetup()
    {
        stacks = 0;
		
        SetupWarehouse();
        // return;
        //Screen.width = 480;
        //Screen.height = 270;
        Screen.SetResolution(480, 270, true);
        //cameraObj =  GameObject.Find("Main Camera");
        //cameraObj.GetComponent<Camera>().usePhysicalProperties = true;
        //cameraObj.GetComponent<Camera>().pixelRect = new Rect(0, 0, 480, 270);
        //cameraObj.GetComponent<Camera>().focalLength = 15;
        cameraPoses = new CameraPosition[warehouse_params.num_cols * warehouse_params.num_racks_per_col*warehouse_params.num_rack_per_cell];
        int camPos_ind = 0;

        float z = -bounding_x / 2 + warehouse_params.corridor_width;
        // z = -2.633f; //-9.779f+1.3f;
        z = 8.35f;
        Color curr_color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

        for (int k = 0; k < warehouse_params.num_cols; k++)
        {
            float x = bounding_x / 2 - 1.5f;
            x = 2f; //6.506f;
            for (int j = 0; j < warehouse_params.num_racks_per_col; j++)
            {
                int rack_num;
                for (rack_num = 0; rack_num < warehouse_params.num_rack_per_cell; rack_num++)
                {
                    float y = 0.02f; //for this shelf model
                    string RackName = "Rack_" + j + "_" + k + "_" + rack_num;
                    // //Debug.Log("Editing values for" + (k * warehouse_params.num_cols + rack_num + j*warehouse_params.num_racks_per_col) + RackName);
                    
                    cameraPoses[camPos_ind].x = x;
                    cameraPoses[camPos_ind].y = y;
                    cameraPoses[camPos_ind].z = z;
                    cameraPoses[camPos_ind].j = j;
                    cameraPoses[camPos_ind].k = k;
                    cameraPoses[camPos_ind].rack_num = rack_num;
                    camPos_ind += 1;

                    rackHolder = new GameObject(RackName).transform;
                    rackHolder.transform.SetParent(wareHolder);
                    int num_shelfs = 3;

                    for (int i = 0; i < 3; i++)
                    {

                        //place shelf
                        GameObject shelfInstance;
                        if (i == 0)
                        {
                            // //Debug.Log("1");
                            shelfInstance = Instantiate(botShelfObj, new Vector3(x, y, z), Quaternion.identity) as GameObject;
                        }
                        else if (i == num_shelfs - 1)
                        {
                            float z_light = 0f;
                           
                            shelfInstance = Instantiate(topShelfObj, new Vector3(x, y, z), Quaternion.identity) as GameObject;
                        }
                        else
                        {
                            // //Debug.Log("3");
                            shelfInstance = Instantiate(shelfObj, new Vector3(x, y, z), Quaternion.identity) as GameObject;
                        }
                        shelfInstance.isStatic = true;
                        shelfInstance.transform.Rotate(0, 0, 0);
                        shelfInstance.transform.SetParent(rackHolder);
                        shelfInstance.name = "Shelf_" + i;

                        shelfInstance.GetComponent<Renderer>().material.mainTextureScale = new Vector2(bounding_x, bounding_x);

                        // float H, S,V;
                        // shelfInstance.GetComponent<Renderer>().material.GetColor("_Color"), out H, out S, out V;
                        // // //Debug.Log("SHELVES" + "H: "  + H + " S: " + S + " V: " + V);
                        ////Debug.Log(shelfInstance.GetComponent<Renderer>().material.GetColor("_Color"));
                        shelfInstance.GetComponent<Renderer>().material.color = curr_color;
                        shelfInstance.GetComponent<Renderer>().material.mainTexture = textures_shelf[Random.Range(0, textures_shelf.Length)];
                        shelfInstance.GetComponent<Renderer>().material.SetFloat("_Metallic", 1.0f);
                        shelfInstance.GetComponent<Renderer>().material.SetFloat("_Glossiness", 0.5f);
                        shelfInstance.GetComponent<Renderer>().material.SetFloat("_Smoothness", 0.7f);
                        //shelfInstance.AddComponent<Rigidbody>();
                        //shelfInstance.GetComponent<Rigidbody>().useGravity = false;
                        shelfInstance.AddComponent<MeshCollider>();
                        shelfInstance.AddComponent<BoxCollider>();

                        y += shelf_y;
                        if (i == 0) y -= 0.225f; //for this model
                        else y -= 0.01f;
                        //// //Debug.Log(IsTargetVisible(Camera.main, shelfInstance));
                        shelfHolder = new GameObject("ObjectsOnShelf_" + i).transform;
                        shelfHolder.transform.SetParent(rackHolder);
                        placeObjectsOnShelfOld(shelfInstance);
                        addInvisToShelf(shelfInstance);

                        //// //Debug.Log(""+i+" "+j);
                    }
                    x -= shelf_x;
                }
                // //Debug.Log(rack_num);
                if(rack_num == warehouse_params.num_rack_per_cell) {
                    x -= (shelf_x + 1);
                }    
                else x -= (2 * shelf_x + 1);
            }

            z -= warehouse_params.corridor_width;
        }

        // //Debug.Log(cameraPoses.Length);
        for (int i = cameraPoses.Length-1; i >= 0 ; i--)
        {   string text = "";
            text += " " + i;
            text += " " + cameraPoses[i].x;
            text += " " + cameraPoses[i].y;
            text += " " + cameraPoses[i].z;
            text += " " + cameraPoses[i].j;
            text += " " + cameraPoses[i].k;
            //Debug.Log(text);
        }
        for (int i = cameraPoses.Length-1; i >= 0 ; i--)
        {
            GetTwoImages(cameraPoses[i]);
            break;
            // //Debug.Log( cameraPoses[i].x+ " " + cameraPoses[i].z);
            //GetTwoImages(cameraPoses[i]);
            //GetTwoImages(cameraPoses[i]);
            //GetTwoImages(cameraPoses[i]);
            // break;
        }
    }

    void InitVars()
    {

        imNum = 0;
        boxTrack = new BoxProperties[boxes.Length];
        save_path = "/home/tanvi/Desktop/Honors/RRC/data";
        // save_path = "/mnt/New Volume/RRC/data_diverse";
        // save_path = "/home/avinash/Desktop/unity_retail/data";
        // save_path = "/home/pranjali/Documents/RRC/Unity/data";
        cameraObj = GameObject.Find("Main Camera");
        cameraObj.transform.Rotate(0, 180, 0);

        string text = "";

        GameObject obj = Instantiate(shelfObj) as GameObject;
        MeshFilter MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
       
        shelf_x = MeshObj.sharedMesh.bounds.size.x * Math.Abs(obj.transform.localScale.x);
        shelf_y = MeshObj.sharedMesh.bounds.size.y * Math.Abs(obj.transform.localScale.y);
        shelf_z = MeshObj.sharedMesh.bounds.size.z * Math.Abs(obj.transform.localScale.z);
        // //Debug.Log(shelf_x + " " + shelf_y + " " + shelf_z);
        text += "Shelf_1, " + MeshObj.sharedMesh.bounds.size.x + ", " + MeshObj.sharedMesh.bounds.size.y + ", " + MeshObj.sharedMesh.bounds.size.z + "\n";
        GameObject.DestroyImmediate(obj);

        obj = Instantiate(topShelfObj) as GameObject;
        MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
        text += "Shelf_2, " + MeshObj.sharedMesh.bounds.size.x + ", " + MeshObj.sharedMesh.bounds.size.y + ", " + MeshObj.sharedMesh.bounds.size.z + "\n";
        GameObject.DestroyImmediate(obj);

		obj = Instantiate(botShelfObj) as GameObject;
        MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
        text += "Shelf_0, " + MeshObj.sharedMesh.bounds.size.x + ", " + MeshObj.sharedMesh.bounds.size.y + ", " + MeshObj.sharedMesh.bounds.size.z + "\n";
        GameObject.DestroyImmediate(obj);


        for (int i = 0; i < boxes.Length; i++)
        {
            obj = Instantiate(boxes[i]) as GameObject;
            MeshObj = (MeshFilter)obj.GetComponent("MeshFilter");
            boxTrack[i].x = MeshObj.sharedMesh.bounds.size.x;
            boxTrack[i].z = MeshObj.sharedMesh.bounds.size.y;
            boxTrack[i].y = MeshObj.sharedMesh.bounds.size.z;
            text += obj.name + ", " + boxTrack[i].x + ", " + boxTrack[i].y + ", " + boxTrack[i].z + "\n";
            GameObject.DestroyImmediate(obj);
        }


        string ann_filename = string.Format(save_path + "/dimensions.txt", save_path);
        File.WriteAllText(ann_filename, text);

        //warehouse_params =WareHouseConfig;
        warehouse_params.num_cols = 2;
        warehouse_params.num_racks_per_col = 2;
        warehouse_params.corridor_width = 8f;
        warehouse_params.num_rack_per_cell = 2;

    }


    void recursive_delete(GameObject go)
    {
        foreach (Transform child in go.transform)
        {
            recursive_delete(child.gameObject);
        }
        GameObject.DestroyImmediate(go);
    }

    //SetupScene initializes our level and calls the previous functions to lay out the game board
    public void SetupScene()
    {

        //Initialise vars
        InitVars();


        //Creates the outer walls and floor: this must be called multiple times

        int max_ware = 1;
        for (int wareNum = 1; wareNum < max_ware + 1; wareNum += 1)
        {
            GameObject wareHolderObj = new GameObject("WareHouse");
            wareHolder = wareHolderObj.transform;
            BoardSetup();
            //recursive_delete(wareHolderObj);

            //Debug.Log("Captured for warehouse " + wareNum + "/" + max_ware);
        }
    }
}

