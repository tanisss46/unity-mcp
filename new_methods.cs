// Terrain Generation methods
[Serializable]
private class CreateTerrainParams
{
    public int width = 100;
    public int length = 100;
    public float height = 20;
    public float[] heightMapValues;  // Optional heightmap data
}

private ObjectResult CreateTerrain(CreateTerrainParams parameters)
{
    if (parameters == null)
    {
        Debug.LogError("CreateTerrain: Parameters are null");
        throw new Exception("CreateTerrain: Parameters are null");
    }
    
    // Create a new GameObject for the terrain
    GameObject terrainObject = new GameObject("GeneratedTerrain");
    
    // Add Terrain component
    Terrain terrain = terrainObject.AddComponent<Terrain>();
    TerrainCollider terrainCollider = terrainObject.AddComponent<TerrainCollider>();
    
    // Set up TerrainData
    TerrainData terrainData = new TerrainData();
    terrainData.heightmapResolution = 129;  // Must be 2^n + 1
    terrainData.size = new Vector3(parameters.width, parameters.height, parameters.length);
    
    // Apply heightmap if provided
    if (parameters.heightMapValues != null && parameters.heightMapValues.Length > 0)
    {
        int resolution = Mathf.FloorToInt(Mathf.Sqrt(parameters.heightMapValues.Length));
        if (resolution * resolution == parameters.heightMapValues.Length)
        {
            // Convert 1D array to 2D
            float[,] heightmap = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heightmap[y, x] = parameters.heightMapValues[y * resolution + x];
                }
            }
            terrainData.SetHeights(0, 0, heightmap);
        }
        else
        {
            Debug.LogError("CreateTerrain: HeightMapValues must be a square array");
        }
    }
    
    // Apply the TerrainData to the Terrain and Collider
    terrain.terrainData = terrainData;
    terrainCollider.terrainData = terrainData;
    
    Debug.Log($"Created terrain: {terrainObject.name}, Width: {parameters.width}, Length: {parameters.length}, Height: {parameters.height}");
    
    return new ObjectResult
    {
        success = true,
        name = terrainObject.name,
        position = new float[] { terrainObject.transform.position.x, terrainObject.transform.position.y, terrainObject.transform.position.z },
        rotation = new float[] { terrainObject.transform.eulerAngles.x, terrainObject.transform.eulerAngles.y, terrainObject.transform.eulerAngles.z },
        scale = new float[] { terrainObject.transform.localScale.x, terrainObject.transform.localScale.y, terrainObject.transform.localScale.z },
        active = terrainObject.activeSelf
    };
}

// Water and Environment
[Serializable]
private class CreateWaterParams
{
    public float width = 50;
    public float length = 50;
    public float height = 0;
    public float[] color = new float[] { 0, 0.5f, 1, 0.7f };  // Blue with transparency
}

private ObjectResult CreateWater(CreateWaterParams parameters)
{
    if (parameters == null)
    {
        Debug.LogError("CreateWater: Parameters are null");
        throw new Exception("CreateWater: Parameters are null");
    }
    
    // Create a plane for the water
    GameObject waterObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
    waterObject.name = "Water";
    
    // Set position and scale
    waterObject.transform.position = new Vector3(0, parameters.height, 0);
    waterObject.transform.localScale = new Vector3(parameters.width/10f, 1, parameters.length/10f);
    
    // Create and apply water material
    Material waterMaterial = new Material(Shader.Find("Standard"));
    waterMaterial.color = new Color(
        parameters.color[0], 
        parameters.color[1], 
        parameters.color[2], 
        parameters.color.Length >= 4 ? parameters.color[3] : 1f
    );
    
    // Set material properties for a water-like appearance
    waterMaterial.SetFloat("_Glossiness", 0.9f);  // High glossiness for reflection
    
    // Apply material
    Renderer renderer = waterObject.GetComponent<Renderer>();
    renderer.material = waterMaterial;
    
    Debug.Log($"Created water: {waterObject.name}, Width: {parameters.width}, Length: {parameters.length}, Height: {parameters.height}");
    
    return new ObjectResult
    {
        success = true,
        name = waterObject.name,
        position = new float[] { waterObject.transform.position.x, waterObject.transform.position.y, waterObject.transform.position.z },
        rotation = new float[] { waterObject.transform.eulerAngles.x, waterObject.transform.eulerAngles.y, waterObject.transform.eulerAngles.z },
        scale = new float[] { waterObject.transform.localScale.x, waterObject.transform.localScale.y, waterObject.transform.localScale.z },
        active = waterObject.activeSelf
    };
}

// Vegetation and Nature Objects
[Serializable]
private class CreateVegetationParams
{
    public string type; // tree, bush, rock, etc.
    public float[] position = new float[] { 0, 0, 0 };
    public float scale = 1.0f;
    public float[] color;  // Optional color
}

private ObjectResult CreateVegetation(CreateVegetationParams parameters)
{
    if (parameters == null || string.IsNullOrEmpty(parameters.type))
    {
        Debug.LogError("CreateVegetation: Parameters are null or type is empty");
        throw new Exception("CreateVegetation: Parameters are null or type is empty");
    }
    
    GameObject vegetation = null;
    
    switch (parameters.type.ToLower())
    {
        case "tree":
            // Create tree trunk
            vegetation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vegetation.name = "Tree";
            vegetation.transform.localScale = new Vector3(parameters.scale * 0.5f, parameters.scale * 5f, parameters.scale * 0.5f);
            
            // Create foliage
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "TreeFoliage";
            foliage.transform.parent = vegetation.transform;
            foliage.transform.localPosition = new Vector3(0, 1f, 0);
            foliage.transform.localScale = new Vector3(3f, 2f, 3f);
            
            // Set materials
            Material trunkMaterial = new Material(Shader.Find("Standard"));
            if (trunkMaterial.shader == null)
            {
                Debug.LogError("Standard shader not found for trunk");
                trunkMaterial = new Material(Shader.Find("Diffuse"));
                if (trunkMaterial.shader == null)
                {
                    Debug.LogError("Fallback shader also not found");
                }
            }
            trunkMaterial.color = new Color(0.5f, 0.3f, 0.1f); // Brown
            vegetation.GetComponent<Renderer>().material = trunkMaterial;
            Debug.Log($"Set tree trunk material color to: {trunkMaterial.color}");
            
            Material foliageMaterial = new Material(Shader.Find("Standard"));
            if (foliageMaterial.shader == null)
            {
                Debug.LogError("Standard shader not found for foliage");
                foliageMaterial = new Material(Shader.Find("Diffuse"));
            }
            foliageMaterial.color = new Color(0.1f, 0.6f, 0.1f); // Green
            foliage.GetComponent<Renderer>().material = foliageMaterial;
            Debug.Log($"Set tree foliage material color to: {foliageMaterial.color}");
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                foliageMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
            }
            break;
            
        case "bush":
            vegetation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vegetation.name = "Bush";
            vegetation.transform.localScale = new Vector3(parameters.scale, parameters.scale * 0.7f, parameters.scale);
            
            Material bushMaterial = new Material(Shader.Find("Standard"));
            if (bushMaterial.shader == null)
            {
                Debug.LogError("Standard shader not found for bush");
                bushMaterial = new Material(Shader.Find("Diffuse"));
                if (bushMaterial.shader == null)
                {
                    Debug.LogError("Fallback shader also not found for bush");
                }
            }
            bushMaterial.color = new Color(0.1f, 0.5f, 0.1f); // Green
            vegetation.GetComponent<Renderer>().sharedMaterial = null; // Clear any existing material
            vegetation.GetComponent<Renderer>().material = bushMaterial;
            Debug.Log($"Set bush material color to: {bushMaterial.color}");
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                bushMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
                Debug.Log($"Applied custom color to bush: R={parameters.color[0]}, G={parameters.color[1]}, B={parameters.color[2]}");
            }
            break;
            
        case "rock":
            vegetation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vegetation.name = "Rock";
            vegetation.transform.localScale = new Vector3(parameters.scale, parameters.scale * 0.8f, parameters.scale);
            vegetation.transform.rotation = Quaternion.Euler(
                UnityEngine.Random.Range(0f, 30f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 30f)
            );
            
            Material rockMaterial = new Material(Shader.Find("Standard"));
            if (rockMaterial.shader == null)
            {
                Debug.LogError("Standard shader not found for rock");
                rockMaterial = new Material(Shader.Find("Diffuse"));
                if (rockMaterial.shader == null)
                {
                    Debug.LogError("Fallback shader also not found for rock");
                }
            }
            rockMaterial.color = new Color(0.5f, 0.5f, 0.5f); // Gray
            vegetation.GetComponent<Renderer>().sharedMaterial = null; // Clear any existing material
            vegetation.GetComponent<Renderer>().material = rockMaterial;
            Debug.Log($"Set rock material color to: {rockMaterial.color}");
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                rockMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
                Debug.Log($"Applied custom color to rock: R={parameters.color[0]}, G={parameters.color[1]}, B={parameters.color[2]}");
            }
            break;
            
        default:
            Debug.LogError($"CreateVegetation: Unknown vegetation type: {parameters.type}");
            throw new Exception($"CreateVegetation: Unknown vegetation type: {parameters.type}");
    }
    
    // Set position
    if (parameters.position != null && parameters.position.Length == 3)
        vegetation.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
    
    Debug.Log($"Created vegetation: {vegetation.name}, Type: {parameters.type}, Position: {vegetation.transform.position}");
    
    return new ObjectResult
    {
        success = true,
        name = vegetation.name,
        position = new float[] { vegetation.transform.position.x, vegetation.transform.position.y, vegetation.transform.position.z },
        rotation = new float[] { vegetation.transform.eulerAngles.x, vegetation.transform.eulerAngles.y, vegetation.transform.eulerAngles.z },
        scale = new float[] { vegetation.transform.localScale.x, vegetation.transform.localScale.y, vegetation.transform.localScale.z },
        active = vegetation.activeSelf
    };
}

// Character Creation
[Serializable]
private class CreateCharacterParams
{
    public string type; // human, robot, etc.
    public float[] position = new float[] { 0, 0, 0 };
    public float scale = 1.0f;
    public float[] color;  // Optional color for customization
}

private ObjectResult CreateCharacter(CreateCharacterParams parameters)
{
    if (parameters == null || string.IsNullOrEmpty(parameters.type))
    {
        Debug.LogError("CreateCharacter: Parameters are null or type is empty");
        throw new Exception("CreateCharacter: Parameters are null or type is empty");
    }
    
    GameObject character = null;
    
    switch (parameters.type.ToLower())
    {
        case "human":
            // Create a simple humanoid character
            character = new GameObject("Human");
            
            // Create body parts
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.parent = character.transform;
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.parent = character.transform;
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            // Create limbs
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftArm.transform.parent = character.transform;
            leftArm.transform.localPosition = new Vector3(-0.35f, 1.1f, 0);
            leftArm.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
            leftArm.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightArm.transform.parent = character.transform;
            rightArm.transform.localPosition = new Vector3(0.35f, 1.1f, 0);
            rightArm.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
            rightArm.transform.rotation = Quaternion.Euler(0, 0, -90);
            
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftLeg.transform.parent = character.transform;
            leftLeg.transform.localPosition = new Vector3(-0.15f, 0.5f, 0);
            leftLeg.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            
            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightLeg.transform.parent = character.transform;
            rightLeg.transform.localPosition = new Vector3(0.15f, 0.5f, 0);
            rightLeg.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            
            // Set default materials
            Material skinMaterial = new Material(Shader.Find("Standard"));
            skinMaterial.color = new Color(0.9f, 0.7f, 0.5f); // Skin tone
            
            Material clothesMaterial = new Material(Shader.Find("Standard"));
            clothesMaterial.color = new Color(0.2f, 0.2f, 0.8f); // Blue clothes
            
            // Apply materials
            head.GetComponent<Renderer>().material = skinMaterial;
            body.GetComponent<Renderer>().material = clothesMaterial;
            leftArm.GetComponent<Renderer>().material = clothesMaterial;
            rightArm.GetComponent<Renderer>().material = clothesMaterial;
            leftLeg.GetComponent<Renderer>().material = clothesMaterial;
            rightLeg.GetComponent<Renderer>().material = clothesMaterial;
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                clothesMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
            }
            
            // Add a CharacterController component
            character.AddComponent<CharacterController>();
            
            break;
            
        case "robot":
            // Create a simple robot character
            character = new GameObject("Robot");
            
            // Create body parts
            GameObject robotBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotBody.transform.parent = character.transform;
            robotBody.transform.localPosition = new Vector3(0, 1f, 0);
            robotBody.transform.localScale = new Vector3(0.7f, 1f, 0.4f);
            
            GameObject robotHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotHead.transform.parent = character.transform;
            robotHead.transform.localPosition = new Vector3(0, 1.7f, 0);
            robotHead.transform.localScale = new Vector3(0.5f, 0.4f, 0.4f);
            
            // Eyes
            GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.transform.parent = robotHead.transform;
            leftEye.transform.localPosition = new Vector3(-0.15f, 0, 0.21f);
            leftEye.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            
            GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.transform.parent = robotHead.transform;
            rightEye.transform.localPosition = new Vector3(0.15f, 0, 0.21f);
            rightEye.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            
            // Create limbs
            GameObject robotLeftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotLeftArm.transform.parent = character.transform;
            robotLeftArm.transform.localPosition = new Vector3(-0.5f, 1.1f, 0);
            robotLeftArm.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            
            GameObject robotRightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotRightArm.transform.parent = character.transform;
            robotRightArm.transform.localPosition = new Vector3(0.5f, 1.1f, 0);
            robotRightArm.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            
            GameObject robotLeftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotLeftLeg.transform.parent = character.transform;
            robotLeftLeg.transform.localPosition = new Vector3(-0.2f, 0.4f, 0);
            robotLeftLeg.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            
            GameObject robotRightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robotRightLeg.transform.parent = character.transform;
            robotRightLeg.transform.localPosition = new Vector3(0.2f, 0.4f, 0);
            robotRightLeg.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            
            // Set default materials
            Material robotMaterial = new Material(Shader.Find("Standard"));
            robotMaterial.color = new Color(0.7f, 0.7f, 0.7f); // Gray metal
            
            Material eyeMaterial = new Material(Shader.Find("Standard"));
            eyeMaterial.color = new Color(1f, 0, 0); // Red eyes
            eyeMaterial.EnableKeyword("_EMISSION");
            eyeMaterial.SetColor("_EmissionColor", new Color(1f, 0, 0));
            
            // Apply materials
            robotBody.GetComponent<Renderer>().material = robotMaterial;
            robotHead.GetComponent<Renderer>().material = robotMaterial;
            robotLeftArm.GetComponent<Renderer>().material = robotMaterial;
            robotRightArm.GetComponent<Renderer>().material = robotMaterial;
            robotLeftLeg.GetComponent<Renderer>().material = robotMaterial;
            robotRightLeg.GetComponent<Renderer>().material = robotMaterial;
            leftEye.GetComponent<Renderer>().material = eyeMaterial;
            rightEye.GetComponent<Renderer>().material = eyeMaterial;
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                robotMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
            }
            
            // Add a CharacterController component
            character.AddComponent<CharacterController>();
            
            break;
            
        default:
            Debug.LogError($"CreateCharacter: Unknown character type: {parameters.type}");
            throw new Exception($"CreateCharacter: Unknown character type: {parameters.type}");
    }
    
    // Set position and scale
    if (parameters.position != null && parameters.position.Length == 3)
        character.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
    
    character.transform.localScale = new Vector3(parameters.scale, parameters.scale, parameters.scale);
    
    Debug.Log($"Created character: {character.name}, Type: {parameters.type}, Position: {character.transform.position}");
    
    return new ObjectResult
    {
        success = true,
        name = character.name,
        position = new float[] { character.transform.position.x, character.transform.position.y, character.transform.position.z },
        rotation = new float[] { character.transform.eulerAngles.x, character.transform.eulerAngles.y, character.transform.eulerAngles.z },
        scale = new float[] { character.transform.localScale.x, character.transform.localScale.y, character.transform.localScale.z },
        active = character.activeSelf
    };
}

// Vehicle Creation
[Serializable]
private class CreateVehicleParams
{
    public string type; // car, truck, airplane, etc.
    public float[] position = new float[] { 0, 0, 0 };
    public float scale = 1.0f;
    public float[] color;  // Optional color for customization
}

private ObjectResult CreateVehicle(CreateVehicleParams parameters)
{
    if (parameters == null || string.IsNullOrEmpty(parameters.type))
    {
        Debug.LogError("CreateVehicle: Parameters are null or type is empty");
        throw new Exception("CreateVehicle: Parameters are null or type is empty");
    }
    
    GameObject vehicle = null;
    
    switch (parameters.type.ToLower())
    {
        case "car":
            // Create a simple car
            vehicle = new GameObject("Car");
            
            // Create body
            GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carBody.transform.parent = vehicle.transform;
            carBody.transform.localPosition = new Vector3(0, 0.5f, 0);
            carBody.transform.localScale = new Vector3(2f, 0.5f, 4f);
            
            // Create cabin
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.transform.parent = vehicle.transform;
            cabin.transform.localPosition = new Vector3(0, 1f, -0.5f);
            cabin.transform.localScale = new Vector3(1.8f, 0.5f, 2f);
            
            // Create wheels
            GameObject wheel1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel1.transform.parent = vehicle.transform;
            wheel1.transform.localPosition = new Vector3(-1.1f, 0.3f, 1f);
            wheel1.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            wheel1.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            GameObject wheel2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel2.transform.parent = vehicle.transform;
            wheel2.transform.localPosition = new Vector3(1.1f, 0.3f, 1f);
            wheel2.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            wheel2.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            GameObject wheel3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel3.transform.parent = vehicle.transform;
            wheel3.transform.localPosition = new Vector3(-1.1f, 0.3f, -1f);
            wheel3.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            wheel3.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            GameObject wheel4 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel4.transform.parent = vehicle.transform;
            wheel4.transform.localPosition = new Vector3(1.1f, 0.3f, -1f);
            wheel4.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            wheel4.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            // Set default materials
            Material bodyMaterial = new Material(Shader.Find("Standard"));
            bodyMaterial.color = new Color(0.8f, 0.2f, 0.2f); // Red car
            
            Material windowMaterial = new Material(Shader.Find("Standard"));
            windowMaterial.color = new Color(0.2f, 0.3f, 0.8f, 0.7f); // Blue tinted windows
            
            Material wheelMaterial = new Material(Shader.Find("Standard"));
            wheelMaterial.color = new Color(0.1f, 0.1f, 0.1f); // Black tires
            
            // Apply materials
            carBody.GetComponent<Renderer>().material = bodyMaterial;
            cabin.GetComponent<Renderer>().material = windowMaterial;
            wheel1.GetComponent<Renderer>().material = wheelMaterial;
            wheel2.GetComponent<Renderer>().material = wheelMaterial;
            wheel3.GetComponent<Renderer>().material = wheelMaterial;
            wheel4.GetComponent<Renderer>().material = wheelMaterial;
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                bodyMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
            }
            
            // Add a Rigidbody component for physics
            vehicle.AddComponent<Rigidbody>();
            
            break;
            
        case "airplane":
            // Create a simple airplane
            vehicle = new GameObject("Airplane");
            
            // Create body
            GameObject planeBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            planeBody.transform.parent = vehicle.transform;
            planeBody.transform.localPosition = new Vector3(0, 0, 0);
            planeBody.transform.localScale = new Vector3(1f, 1f, 3f);
            planeBody.transform.rotation = Quaternion.Euler(0, 0, 90);
            
            // Create wings
            GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWing.transform.parent = vehicle.transform;
            leftWing.transform.localPosition = new Vector3(-2f, 0, 0);
            leftWing.transform.localScale = new Vector3(3f, 0.1f, 1f);
            
            GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWing.transform.parent = vehicle.transform;
            rightWing.transform.localPosition = new Vector3(2f, 0, 0);
            rightWing.transform.localScale = new Vector3(3f, 0.1f, 1f);
            
            // Create tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.transform.parent = vehicle.transform;
            tail.transform.localPosition = new Vector3(0, 0.5f, -1.5f);
            tail.transform.localScale = new Vector3(1f, 1f, 0.1f);
            
            // Set default materials
            Material planeMaterial = new Material(Shader.Find("Standard"));
            planeMaterial.color = new Color(0.8f, 0.8f, 0.8f); // White airplane
            
            // Apply materials
            planeBody.GetComponent<Renderer>().material = planeMaterial;
            leftWing.GetComponent<Renderer>().material = planeMaterial;
            rightWing.GetComponent<Renderer>().material = planeMaterial;
            tail.GetComponent<Renderer>().material = planeMaterial;
            
            // Apply custom color if provided
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                planeMaterial.color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
            }
            
            // Add a Rigidbody component for physics
            vehicle.AddComponent<Rigidbody>();
            
            break;
            
        default:
            Debug.LogError($"CreateVehicle: Unknown vehicle type: {parameters.type}");
            throw new Exception($"CreateVehicle: Unknown vehicle type: {parameters.type}");
    }
    
    // Set position and scale
    if (parameters.position != null && parameters.position.Length == 3)
        vehicle.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
    
    vehicle.transform.localScale = new Vector3(parameters.scale, parameters.scale, parameters.scale);
    
    Debug.Log($"Created vehicle: {vehicle.name}, Type: {parameters.type}, Position: {vehicle.transform.position}");
    
    return new ObjectResult
    {
        success = true,
        name = vehicle.name,
        position = new float[] { vehicle.transform.position.x, vehicle.transform.position.y, vehicle.transform.position.z },
        rotation = new float[] { vehicle.transform.eulerAngles.x, vehicle.transform.eulerAngles.y, vehicle.transform.eulerAngles.z },
        scale = new float[] { vehicle.transform.localScale.x, vehicle.transform.localScale.y, vehicle.transform.localScale.z },
        active = vehicle.activeSelf
    };
}

// Environment Effects (Weather, Time of Day)
[Serializable]
private class SetEnvironmentEffectsParams
{
    public string effect_type; // weather, timeofday
    public string effect_name; // rain, snow, fog, day, night, etc.
    public float intensity = 1.0f;
}

private ObjectResult SetEnvironmentEffects(SetEnvironmentEffectsParams parameters)
{
    if (parameters == null || string.IsNullOrEmpty(parameters.effect_type) || string.IsNullOrEmpty(parameters.effect_name))
    {
        Debug.LogError("SetEnvironmentEffects: Parameters are null or incomplete");
        throw new Exception("SetEnvironmentEffects: Parameters are null or incomplete");
    }
    
    GameObject effectsObject = GameObject.Find("EnvironmentEffects");
    
    // Create environment effects container if it doesn't exist
    if (effectsObject == null)
    {
        effectsObject = new GameObject("EnvironmentEffects");
    }
    
    switch (parameters.effect_type.ToLower())
    {
        case "weather":
            // Handle different weather types
            switch (parameters.effect_name.ToLower())
            {
                case "rain":
                    // Create or find rain particle system
                    GameObject rainSystem = GameObject.Find("RainEffect");
                    if (rainSystem == null)
                    {
                        rainSystem = new GameObject("RainEffect");
                        rainSystem.transform.parent = effectsObject.transform;
                        
                        // Add particle system
                        ParticleSystem rainParticles = rainSystem.AddComponent<ParticleSystem>();
                        var main = rainParticles.main;
                        main.startSpeed = 10f * parameters.intensity;
                        main.startSize = 0.1f;
                        main.startLifetime = 2f;
                        main.maxParticles = 1000;
                        
                        // Set emission rate
                        var emission = rainParticles.emission;
                        emission.rateOverTime = 500f * parameters.intensity;
                        
                        // Set shape
                        var shape = rainParticles.shape;
                        shape.shapeType = ParticleSystemShapeType.Box;
                        shape.scale = new Vector3(50f, 1f, 50f);
                        shape.position = new Vector3(0, 20f, 0);
                        
                        // Create a material for the particles
                        var renderer = rainSystem.GetComponent<ParticleSystemRenderer>();
                        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                        renderer.material.color = new Color(0.7f, 0.7f, 1f, 0.5f);
                    }
                    else
                    {
                        // Update existing rain system
                        ParticleSystem rainParticles = rainSystem.GetComponent<ParticleSystem>();
                        var main = rainParticles.main;
                        main.startSpeed = 10f * parameters.intensity;
                        
                        var emission = rainParticles.emission;
                        emission.rateOverTime = 500f * parameters.intensity;
                    }
                    break;
                    
                case "fog":
                    // Set global fog
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.8f, 0.8f, 0.8f);
                    RenderSettings.fogDensity = 0.02f * parameters.intensity;
                    break;
                    
                case "clear":
                    // Remove all weather effects
                    GameObject rainObj = GameObject.Find("RainEffect");
                    if (rainObj != null)
                        GameObject.Destroy(rainObj);
                        
                    RenderSettings.fog = false;
                    break;
                    
                default:
                    Debug.LogError($"SetEnvironmentEffects: Unknown weather effect: {parameters.effect_name}");
                    throw new Exception($"SetEnvironmentEffects: Unknown weather effect: {parameters.effect_name}");
            }
            break;
            
        case "timeofday":
            // Handle different time of day settings
            GameObject lightObject = GameObject.Find("DirectionalLight");
            
            // Create directional light if it doesn't exist
            if (lightObject == null)
            {
                lightObject = new GameObject("DirectionalLight");
                lightObject.transform.parent = effectsObject.transform;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            
            Light directionalLight = lightObject.GetComponent<Light>();
            
            switch (parameters.effect_name.ToLower())
            {
                case "day":
                    // Bright day settings
                    directionalLight.intensity = 1.0f * parameters.intensity;
                    directionalLight.color = new Color(1f, 0.95f, 0.85f);
                    RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
                    lightObject.transform.rotation = Quaternion.Euler(50f, 30f, 0);
                    break;
                    
                case "sunset":
                    // Sunset/sunrise settings
                    directionalLight.intensity = 0.7f * parameters.intensity;
                    directionalLight.color = new Color(1f, 0.5f, 0.2f);
                    RenderSettings.ambientLight = new Color(0.5f, 0.3f, 0.2f);
                    lightObject.transform.rotation = Quaternion.Euler(10f, 30f, 0);
                    break;
                    
                case "night":
                    // Night settings
                    directionalLight.intensity = 0.2f * parameters.intensity;
                    directionalLight.color = new Color(0.1f, 0.1f, 0.3f);
                    RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.2f);
                    lightObject.transform.rotation = Quaternion.Euler(-30f, 30f, 0);
                    break;
                    
                default:
                    Debug.LogError($"SetEnvironmentEffects: Unknown time of day setting: {parameters.effect_name}");
                    throw new Exception($"SetEnvironmentEffects: Unknown time of day setting: {parameters.effect_name}");
            }
            break;
            
        default:
            Debug.LogError($"SetEnvironmentEffects: Unknown effect type: {parameters.effect_type}");
            throw new Exception($"SetEnvironmentEffects: Unknown effect type: {parameters.effect_type}");
    }
    
    Debug.Log($"Set environment effect: {parameters.effect_type} - {parameters.effect_name}, Intensity: {parameters.intensity}");
    
    return new ObjectResult
    {
        success = true,
        name = "EnvironmentEffects",
        position = new float[] { effectsObject.transform.position.x, effectsObject.transform.position.y, effectsObject.transform.position.z },
        rotation = new float[] { effectsObject.transform.eulerAngles.x, effectsObject.transform.eulerAngles.y, effectsObject.transform.eulerAngles.z },
        scale = new float[] { effectsObject.transform.localScale.x, effectsObject.transform.localScale.y, effectsObject.transform.localScale.z },
        active = effectsObject.activeSelf
    };
}

// Physics Interactions
[Serializable]
private class ApplyPhysicsParams
{
    public string object_name;
    public string action; // applyForce, addTorque, setGravity, etc.
    public float[] direction = new float[] { 0, 0, 0 };
    public float magnitude = 1.0f;
    public string mode = "Force"; // ForceMode: Force, Acceleration, Impulse, VelocityChange
}

private ObjectResult ApplyPhysics(ApplyPhysicsParams parameters)
{
    if (parameters == null || string.IsNullOrEmpty(parameters.object_name) || string.IsNullOrEmpty(parameters.action))
    {
        Debug.LogError("ApplyPhysics: Parameters are null or incomplete");
        throw new Exception("ApplyPhysics: Parameters are null or incomplete");
    }
    
    GameObject targetObject = GameObject.Find(parameters.object_name);
    
    if (targetObject == null)
    {
        Debug.LogError($"ApplyPhysics: Object '{parameters.object_name}' not found");
        throw new Exception($"ApplyPhysics: Object '{parameters.object_name}' not found");
    }
    
    // Get or add Rigidbody component
    Rigidbody rb = targetObject.GetComponent<Rigidbody>();
    if (rb == null)
    {
        rb = targetObject.AddComponent<Rigidbody>();
    }
    
    // Create force/direction vector
    Vector3 direction = new Vector3(
        parameters.direction[0],
        parameters.direction[1],
        parameters.direction[2]
    ).normalized;
    
    Vector3 forceVector = direction * parameters.magnitude;
    
    // Parse force mode
    ForceMode forceMode = ForceMode.Force;
    switch (parameters.mode.ToLower())
    {
        case "force":
            forceMode = ForceMode.Force;
            break;
        case "acceleration":
            forceMode = ForceMode.Acceleration;
            break;
        case "impulse":
            forceMode = ForceMode.Impulse;
            break;
        case "velocitychange":
            forceMode = ForceMode.VelocityChange;
            break;
    }
    
    // Apply the specified action
    switch (parameters.action.ToLower())
    {
        case "applyforce":
            rb.AddForce(forceVector, forceMode);
            Debug.Log($"Applied force {forceVector} to {parameters.object_name} with mode {forceMode}");
            break;
            
        case "addtorque":
            rb.AddTorque(forceVector, forceMode);
            Debug.Log($"Applied torque {forceVector} to {parameters.object_name} with mode {forceMode}");
            break;
            
        case "setvelocity":
            rb.velocity = forceVector;
            Debug.Log($"Set velocity of {parameters.object_name} to {forceVector}");
            break;
            
        case "setgravity":
            rb.useGravity = parameters.magnitude > 0;
            if (parameters.magnitude != 1 && parameters.magnitude > 0)
            {
                // Custom gravity factor
                rb.mass = parameters.magnitude;
            }
            Debug.Log($"Set gravity for {parameters.object_name} to {(rb.useGravity ? "On" : "Off")}, Mass: {rb.mass}");
            break;
            
        default:
            Debug.LogError($"ApplyPhysics: Unknown action: {parameters.action}");
            throw new Exception($"ApplyPhysics: Unknown action: {parameters.action}");
    }
    
    return new ObjectResult
    {
        success = true,
        name = targetObject.name,
        position = new float[] { targetObject.transform.position.x, targetObject.transform.position.y, targetObject.transform.position.z },
        rotation = new float[] { targetObject.transform.eulerAngles.x, targetObject.transform.eulerAngles.y, targetObject.transform.eulerAngles.z },
        scale = new float[] { targetObject.transform.localScale.x, targetObject.transform.localScale.y, targetObject.transform.localScale.z },
        active = targetObject.activeSelf
    };
}

// Add these methods to the ExecuteMethod switch statement:
/*
case "create_terrain":
    var terrainParams = JsonUtility.FromJson<CreateTerrainParams>(paramsJson);
    if (terrainParams == null)
    {
        Debug.LogError("create_terrain: terrainParams is null after JSON deserialization");
        throw new Exception("create_terrain: terrainParams is null after JSON deserialization");
    }
    return CreateTerrain(terrainParams);
    
case "create_water":
    var waterParams = JsonUtility.FromJson<CreateWaterParams>(paramsJson);
    if (waterParams == null)
    {
        Debug.LogError("create_water: waterParams is null after JSON deserialization");
        throw new Exception("create_water: waterParams is null after JSON deserialization");
    }
    return CreateWater(waterParams);
    
case "create_vegetation":
    var vegetationParams = JsonUtility.FromJson<CreateVegetationParams>(paramsJson);
    if (vegetationParams == null || string.IsNullOrEmpty(vegetationParams.type))
    {
        Debug.LogError("create_vegetation: vegetationParams is null or type is empty");
        throw new Exception("create_vegetation: vegetationParams is null or type is empty");
    }
    return CreateVegetation(vegetationParams);
    
case "create_character":
    var characterParams = JsonUtility.FromJson<CreateCharacterParams>(paramsJson);
    if (characterParams == null || string.IsNullOrEmpty(characterParams.type))
    {
        Debug.LogError("create_character: characterParams is null or type is empty");
        throw new Exception("create_character: characterParams is null or type is empty");
    }
    return CreateCharacter(characterParams);
    
case "create_vehicle":
    var vehicleParams = JsonUtility.FromJson<CreateVehicleParams>(paramsJson);