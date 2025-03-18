using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.SceneManagement;
#endif

public class UnityMCPServer : MonoBehaviour
{
    [SerializeField] private int port = 8080;
    private TcpListener server;
    private bool isRunning;

    // JSON definitions
    [Serializable]
    private class JsonRpcRequest
    {
        public string jsonrpc = "2.0";
        public string method;
        public string paramsJson; // Parameters as JSON
        public string id;
    }

    [Serializable]
    private class JsonRpcResponse
    {
        public string jsonrpc = "2.0";
        public string result; // Result as JSON
        public string id;
    }

    [Serializable]
    private class JsonRpcErrorResponse
    {
        public string jsonrpc = "2.0";
        public JsonRpcError error;
        public string id;
    }

    [Serializable]
    private class JsonRpcError
    {
        public int code;
        public string message;
        public string data;
    }

    // Parameter classes
    [Serializable]
    private class CreateObjectParams
    {
        public string type;
        public string name;
        public float[] location;
        public float[] rotation;
        public float[] scale;
    }

    [Serializable]
    private class ModifyObjectParams
    {
        public string name;
        public float[] location;
        public float[] rotation;
        public float[] scale;
        public bool? visible;
    }

    [Serializable]
    private class GetObjectInfoParams
    {
        public string object_name;
    }

    [Serializable]
    private class SetMaterialParams
    {
        public string object_name;
        public string material_name;
        public float[] color;
    }

    [Serializable]
    private class DeleteObjectParams
    {
        public string name;
    }

    [Serializable]
    private class ExecuteCodeParams
    {
        public string code;
    }

    // New parameter classes for added features
    [Serializable]
    private class CreateTerrainParams
    {
        public int width;
        public int length;
        public float height;
        public float[,] heightmap;
    }

    [Serializable]
    private class CreateWaterParams
    {
        public float width;
        public float length;
        public float height;
    }

    [Serializable]
    private class CreateVegetationParams
    {
        public string type;
        public float[] position;
        public float scale;
        public float[] color;
    }

    [Serializable]
    private class CreateSkyboxParams
    {
        public string type;
        public float[] color;
    }

    [Serializable]
    private class CreateCharacterParams
    {
        public string characterType;
        public float[] position;
    }

    [Serializable]
    private class SetAnimationParams
    {
        public string object_name;
        public string animation;
    }

    [Serializable]
    private class SetCharacterControllerParams
    {
        public string object_name;
        public float speed;
        public float jump_height;
    }

    [Serializable]
    private class CreateVehicleParams
    {
        public string vehicleType;
        public float[] position;
    }

    [Serializable]
    private class SetVehiclePropertiesParams
    {
        public string object_name;
        public float top_speed;
        public float acceleration;
    }

    [Serializable]
    private class CreateLightParams
    {
        public string lightType;
        public float[] position;
        public float intensity;
        public float[] color;
    }

    [Serializable]
    private class CreateParticleSystemParams
    {
        public string effectType;
        public float[] position;
        public float scale;
    }

    [Serializable]
    private class SetPostProcessingParams
    {
        public string effect;
        public float intensity;
    }

    [Serializable]
    private class AddRigidbodyParams
    {
        public string object_name;
        public float mass;
        public bool use_gravity;
    }

    [Serializable]
    private class ApplyForceParams
    {
        public string object_name;
        public float[] force;
        public string mode;
    }

    [Serializable]
    private class CreateJointParams
    {
        public string object1;
        public string object2;
        public string joint_type;
    }

    [Serializable]
    private class CreateCameraParams
    {
        public string camera_type;
        public float[] position;
        public string target;
    }

    [Serializable]
    private class SetActiveCameraParams
    {
        public string camera_name;
    }

    [Serializable]
    private class SetCameraPropertiesParams
    {
        public string camera_name;
        public float field_of_view;
        public float near_clip;
        public float far_clip;
    }

    [Serializable]
    private class PlaySoundParams
    {
        public string sound_type;
        public float[] position;
        public float volume;
    }

    [Serializable]
    private class CreateAudioSourceParams
    {
        public string object_name;
        public string audio_type;
        public bool loop;
        public float volume;
    }

    void Start()
    {
        StartServer();
    }

    private async void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            isRunning = true;
            Debug.Log($"Unity MCP Server started, port: {port}");
            
            while (isRunning)
            {
                var client = await server.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // Fire and forget
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Server error: {e.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Debug.Log($"Received message: {message}");

                    var response = ProcessJsonRpcMessage(message);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Client handling error: {e.Message}. Client connection closed.");
        }
        finally
        {
            client.Close();
            Debug.Log("Client connection closed.");
        }
    }

    private string ProcessJsonRpcMessage(string message)
    {
        try
        {
            // Process JSON-RPC message - manual JSON parsing
            JsonRpcRequest request = new JsonRpcRequest();
            
            // Extract key values from JSON in a simple way
            var jsonObj = SimpleJsonParse(message);
            request.jsonrpc = GetJsonValue(jsonObj, "jsonrpc");
            request.method = GetJsonValue(jsonObj, "method");
            request.id = GetJsonValue(jsonObj, "id");
            
            // Get params object as JSON
            request.paramsJson = GetJsonObjectRaw(message, "params");
            Debug.Log($"Extracted paramsJson: {request.paramsJson}");
            
            // Call method and get result
            var resultObj = ExecuteMethod(request.method, request.paramsJson);
            
            // Create JSON-RPC response
            var response = new JsonRpcResponse
            {
                jsonrpc = "2.0",
                id = request.id,
                result = JsonUtility.ToJson(resultObj)
            };
            
            return JsonUtility.ToJson(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON-RPC processing error: {e.Message}");
            var errorResponse = new JsonRpcErrorResponse
            {
                jsonrpc = "2.0",
                id = null,
                error = new JsonRpcError
                {
                    code = -32603,
                    message = e.Message
                }
            };
            
            return JsonUtility.ToJson(errorResponse);
        }
    }

    private Dictionary<string, string> SimpleJsonParse(string json)
    {
        var result = new Dictionary<string, string>();
        int i = 0;
        
        // Skip any whitespace or opening braces
        while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == '{'))
            i++;
            
        while (i < json.Length)
        {
            // Skip whitespace
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
                
            if (i >= json.Length || json[i] == '}')
                break;
                
            // Read key
            if (json[i] != '\"')
            {
                Debug.LogError($"Expected '\"' at position {i}");
                break;
            }
            
            i++; // Skip opening quote
            int keyStart = i;
            
            while (i < json.Length && json[i] != '\"')
                i++;
                
            if (i >= json.Length)
            {
                Debug.LogError("Unexpected end of JSON while parsing key");
                break;
            }
            
            string key = json.Substring(keyStart, i - keyStart);
            i++; // Skip closing quote
            
            // Skip whitespace and colon
            while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ':'))
                i++;
                
            if (i >= json.Length)
            {
                Debug.LogError("Unexpected end of JSON while looking for value");
                break;
            }
            
            // Read value
            string value;
            
            if (json[i] == '\"')
            {
                i++; // Skip opening quote
                int valueStart = i;
                
                while (i < json.Length && json[i] != '\"')
                {
                    // Handle escaped quotes
                    if (json[i] == '\\' && i + 1 < json.Length)
                        i += 2;
                    else
                        i++;
                }
                
                if (i >= json.Length)
                {
                    Debug.LogError("Unexpected end of JSON while parsing string value");
                    break;
                }
                
                value = json.Substring(valueStart, i - valueStart);
                i++; // Skip closing quote
            }
            else if (json[i] == '{' || json[i] == '[')
            {
                // Skip complex objects/arrays for now
                int depth = 1;
                int valueStart = i;
                i++;
                
                while (i < json.Length && depth > 0)
                {
                    if (json[i] == '{' || json[i] == '[')
                        depth++;
                    else if (json[i] == '}' || json[i] == ']')
                        depth--;
                        
                    i++;
                }
                
                value = json.Substring(valueStart, i - valueStart);
            }
            else
            {
                // Number, boolean, or null
                int valueStart = i;
                
                while (i < json.Length && ",}".IndexOf(json[i]) == -1)
                    i++;
                    
                value = json.Substring(valueStart, i - valueStart).Trim();
            }
            
            result[key] = value;
            
            // Skip comma or end of object
            while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ','))
                i++;
        }
        
        return result;
    }

    private string GetJsonValue(Dictionary<string, string> jsonObj, string key)
    {
        if (jsonObj.TryGetValue(key, out string value))
            return value;
        return "";
    }

    private string GetJsonObjectRaw(string json, string key)
    {
        int keyIndex = json.IndexOf("\"" + key + "\"");
        if (keyIndex == -1)
            return "{}"; // Return empty object
            
        int colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex == -1)
            return "{}";
            
        int valueStart = colonIndex + 1;
        
        // Skip whitespace
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;
            
        if (valueStart >= json.Length)
            return "{}";
            
        // Check if value is an object, array, string, number, boolean, or null
        char startChar = json[valueStart];
        int valueEnd;
        
        if (startChar == '{' || startChar == '[')
        {
            // Object or array
            char endChar = (startChar == '{') ? '}' : ']';
            int bracketCount = 1;
            valueEnd = valueStart + 1;
            
            while (valueEnd < json.Length && bracketCount > 0)
            {
                if (json[valueEnd] == startChar)
                    bracketCount++;
                else if (json[valueEnd] == endChar)
                    bracketCount--;
                    
                valueEnd++;
            }
            
            if (bracketCount != 0)
                return "{}";
        }
        else if (startChar == '"')
        {
            // String
            valueEnd = valueStart + 1;
            while (valueEnd < json.Length && json[valueEnd] != '"')
            {
                // Handle escaped quotes
                if (json[valueEnd] == '\\' && valueEnd + 1 < json.Length)
                    valueEnd += 2;
                else
                    valueEnd++;
            }
            
            if (valueEnd >= json.Length)
                return "{}";
                
            valueEnd++; // Include closing quote
        }
        else
        {
            // Number, boolean, or null
            valueEnd = valueStart;
            while (valueEnd < json.Length && ",}]".IndexOf(json[valueEnd]) == -1)
                valueEnd++;
        }
        
        Debug.Log($"Extracted JSON value: {json.Substring(valueStart, valueEnd - valueStart)}");
        return json.Substring(valueStart, valueEnd - valueStart);
    }

    private object ExecuteMethod(string method, string paramsJson)
    {
        Debug.Log($"Executing method: {method} with params: {paramsJson}");
        
        // Special check for methods that don't require parameters, such as get_scene_info
        bool requiresParameters = method != "get_scene_info";
        
        // Parameter check (if required)
        if (requiresParameters)
        {
            if (string.IsNullOrEmpty(paramsJson) || paramsJson == "{}")
            {
                Debug.LogError($"ExecuteMethod: Missing parameters for method {method}");
                throw new Exception($"ExecuteMethod: Missing parameters for method {method}");
            }
        }
        
        switch (method)
        {
            case "get_scene_info":
                return GetSceneInfo();
            
            case "get_object_info":
                var objParams = JsonUtility.FromJson<GetObjectInfoParams>(paramsJson);
                if (objParams == null || string.IsNullOrEmpty(objParams.object_name))
                {
                    Debug.LogError("get_object_info: object_name is null or empty");
                    throw new Exception("get_object_info: object_name is null or empty");
                }
                return GetObjectInfo(objParams.object_name);
            
            case "create_object":
                var createParams = JsonUtility.FromJson<CreateObjectParams>(paramsJson);
                if (createParams == null)
                {
                    Debug.LogError("create_object: createParams is null after JSON deserialization");
                    throw new Exception("create_object: createParams is null after JSON deserialization");
                }
                Debug.Log($"CreateObject params - type: {createParams.type}, name: {createParams.name}, location: {(createParams.location != null ? string.Join(",", createParams.location) : "null")}");
                return CreateObject(createParams);
            
            case "modify_object":
                var modifyParams = JsonUtility.FromJson<ModifyObjectParams>(paramsJson);
                if (modifyParams == null || string.IsNullOrEmpty(modifyParams.name))
                {
                    Debug.LogError("modify_object: name is null or empty");
                    throw new Exception("modify_object: name is null or empty");
                }
                return ModifyObject(modifyParams);
                
            case "delete_object":
                var deleteParams = JsonUtility.FromJson<DeleteObjectParams>(paramsJson);
                if (deleteParams == null || string.IsNullOrEmpty(deleteParams.name))
                {
                    Debug.LogError("delete_object: name is null or empty");
                    throw new Exception("delete_object: name is null or empty");
                }
                return DeleteObject(deleteParams.name);
                
            case "set_material":
                var materialParams = JsonUtility.FromJson<SetMaterialParams>(paramsJson);
                if (materialParams == null || string.IsNullOrEmpty(materialParams.object_name))
                {
                    Debug.LogError("set_material: object_name is null or empty");
                    throw new Exception("set_material: object_name is null or empty");
                }
                return SetMaterial(materialParams);
                
            case "execute_unity_code":
                var codeParams = JsonUtility.FromJson<ExecuteCodeParams>(paramsJson);
                if (codeParams == null || string.IsNullOrEmpty(codeParams.code))
                {
                    Debug.LogError("execute_unity_code: code is null or empty");
                    throw new Exception("execute_unity_code: code is null or empty");
                }
                return ExecuteUnityCode(codeParams);
            
            // New methods
            case "create_terrain":
                var terrainParams = JsonUtility.FromJson<CreateTerrainParams>(paramsJson);
                if (terrainParams == null)
                {
                    Debug.LogError("create_terrain: terrainParams is null after JSON deserialization");
                    throw new Exception("create_terrain: terrainParams is null after JSON deserialization");
                }
                return CreateTerrain(terrainParams.width, terrainParams.length, terrainParams.height);
                
            case "create_water":
                var waterParams = JsonUtility.FromJson<CreateWaterParams>(paramsJson);
                if (waterParams == null)
                {
                    Debug.LogError("create_water: waterParams is null after JSON deserialization");
                    throw new Exception("create_water: waterParams is null after JSON deserialization");
                }
                return CreateWater(waterParams.width, waterParams.length, waterParams.height);
                
            case "create_vegetation":
                var vegetationParams = JsonUtility.FromJson<CreateVegetationParams>(paramsJson);
                if (vegetationParams == null || string.IsNullOrEmpty(vegetationParams.type))
                {
                    Debug.LogError("create_vegetation: vegetationParams or type is null");
                    throw new Exception("create_vegetation: vegetationParams or type is null");
                }
                return CreateVegetation(JsonUtility.ToJson(vegetationParams));
                
            case "create_skybox":
                var skyboxParams = JsonUtility.FromJson<CreateSkyboxParams>(paramsJson);
                if (skyboxParams == null || string.IsNullOrEmpty(skyboxParams.type))
                {
                    Debug.LogError("create_skybox: skyboxParams or type is null");
                    throw new Exception("create_skybox: skyboxParams or type is null");
                }
                return CreateSkybox(skyboxParams.type, skyboxParams.color);
                
            case "create_character":
                var characterParams = JsonUtility.FromJson<CreateCharacterParams>(paramsJson);
                if (characterParams == null || string.IsNullOrEmpty(characterParams.characterType))
                {
                    Debug.LogError("create_character: characterParams or characterType is null");
                    throw new Exception("create_character: characterParams or characterType is null");
                }
                Vector3 charPos = characterParams.position != null && characterParams.position.Length == 3 ?
                    new Vector3(characterParams.position[0], characterParams.position[1], characterParams.position[2]) :
                    Vector3.zero;
                return CreateCharacter(characterParams.characterType, charPos);
                
            case "set_animation":
                var animParams = JsonUtility.FromJson<SetAnimationParams>(paramsJson);
                if (animParams == null || string.IsNullOrEmpty(animParams.object_name) || string.IsNullOrEmpty(animParams.animation))
                {
                    Debug.LogError("set_animation: animParams, object_name, or animation is null");
                    throw new Exception("set_animation: animParams, object_name, or animation is null");
                }
                return SetAnimation(animParams.object_name, animParams.animation);
                
            case "set_character_controller":
                var controllerParams = JsonUtility.FromJson<SetCharacterControllerParams>(paramsJson);
                if (controllerParams == null || string.IsNullOrEmpty(controllerParams.object_name))
                {
                    Debug.LogError("set_character_controller: controllerParams or object_name is null");
                    throw new Exception("set_character_controller: controllerParams or object_name is null");
                }
                return SetCharacterController(controllerParams);
                
            case "create_vehicle":
                var vehicleParams = JsonUtility.FromJson<CreateVehicleParams>(paramsJson);
                if (vehicleParams == null || string.IsNullOrEmpty(vehicleParams.vehicleType))
                {
                    Debug.LogError("create_vehicle: vehicleParams or vehicleType is null");
                    throw new Exception("create_vehicle: vehicleParams or vehicleType is null");
                }
                Vector3 vehPos = vehicleParams.position != null && vehicleParams.position.Length == 3 ?
                    new Vector3(vehicleParams.position[0], vehicleParams.position[1], vehicleParams.position[2]) :
                    Vector3.zero;
                return CreateVehicle(vehicleParams.vehicleType, vehPos);
                
            case "set_vehicle_properties":
                var vehPropParams = JsonUtility.FromJson<SetVehiclePropertiesParams>(paramsJson);
                if (vehPropParams == null || string.IsNullOrEmpty(vehPropParams.object_name))
                {
                    Debug.LogError("set_vehicle_properties: vehPropParams or object_name is null");
                    throw new Exception("set_vehicle_properties: vehPropParams or object_name is null");
                }
                return SetVehicleProperties(vehPropParams);
                
            case "create_light":
                var lightParams = JsonUtility.FromJson<CreateLightParams>(paramsJson);
                if (lightParams == null || string.IsNullOrEmpty(lightParams.lightType))
                {
                    Debug.LogError("create_light: lightParams or lightType is null");
                    throw new Exception("create_light: lightParams or lightType is null");
                }
                return CreateLight(lightParams);
                
            case "create_particle_system":
                var particleParams = JsonUtility.FromJson<CreateParticleSystemParams>(paramsJson);
                if (particleParams == null || string.IsNullOrEmpty(particleParams.effectType))
                {
                    Debug.LogError("create_particle_system: particleParams or effectType is null");
                    throw new Exception("create_particle_system: particleParams or effectType is null");
                }
                return CreateParticleSystem(particleParams);
                
            case "set_post_processing":
                var postParams = JsonUtility.FromJson<SetPostProcessingParams>(paramsJson);
                if (postParams == null || string.IsNullOrEmpty(postParams.effect))
                {
                    Debug.LogError("set_post_processing: postParams or effect is null");
                    throw new Exception("set_post_processing: postParams or effect is null");
                }
                return SetPostProcessing(postParams);
                
            case "add_rigidbody":
                var rbParams = JsonUtility.FromJson<AddRigidbodyParams>(paramsJson);
                if (rbParams == null || string.IsNullOrEmpty(rbParams.object_name))
                {
                    Debug.LogError("add_rigidbody: rbParams or object_name is null");
                    throw new Exception("add_rigidbody: rbParams or object_name is null");
                }
                return AddRigidbody(rbParams);
                
            case "apply_force":
                var forceParams = JsonUtility.FromJson<ApplyForceParams>(paramsJson);
                if (forceParams == null || string.IsNullOrEmpty(forceParams.object_name) || forceParams.force == null)
                {
                    Debug.LogError("apply_force: forceParams, object_name, or force is null");
                    throw new Exception("apply_force: forceParams, object_name, or force is null");
                }
                return ApplyForce(forceParams);
                
            case "create_joint":
                var jointParams = JsonUtility.FromJson<CreateJointParams>(paramsJson);
                if (jointParams == null || string.IsNullOrEmpty(jointParams.object1) || string.IsNullOrEmpty(jointParams.object2))
                {
                    Debug.LogError("create_joint: jointParams, object1, or object2 is null");
                    throw new Exception("create_joint: jointParams, object1, or object2 is null");
                }
                return CreateJoint(jointParams);
                
            case "create_camera":
                var cameraParams = JsonUtility.FromJson<CreateCameraParams>(paramsJson);
                if (cameraParams == null || string.IsNullOrEmpty(cameraParams.camera_type))
                {
                    Debug.LogError("create_camera: cameraParams or camera_type is null");
                    throw new Exception("create_camera: cameraParams or camera_type is null");
                }
                return CreateCamera(cameraParams);
                
            case "set_active_camera":
                var activeCamParams = JsonUtility.FromJson<SetActiveCameraParams>(paramsJson);
                if (activeCamParams == null || string.IsNullOrEmpty(activeCamParams.camera_name))
                {
                    Debug.LogError("set_active_camera: activeCamParams or camera_name is null");
                    throw new Exception("set_active_camera: activeCamParams or camera_name is null");
                }
                return SetActiveCamera(activeCamParams.camera_name);
                
            case "set_camera_properties":
                var camPropParams = JsonUtility.FromJson<SetCameraPropertiesParams>(paramsJson);
                if (camPropParams == null || string.IsNullOrEmpty(camPropParams.camera_name))
                {
                    Debug.LogError("set_camera_properties: camPropParams or camera_name is null");
                    throw new Exception("set_camera_properties: camPropParams or camera_name is null");
                }
                return SetCameraProperties(camPropParams);
                
            case "play_sound":
                var soundParams = JsonUtility.FromJson<PlaySoundParams>(paramsJson);
                if (soundParams == null || string.IsNullOrEmpty(soundParams.sound_type))
                {
                    Debug.LogError("play_sound: soundParams or sound_type is null");
                    throw new Exception("play_sound: soundParams or sound_type is null");
                }
                return PlaySound(soundParams);
                
            case "create_audio_source":
                var audioParams = JsonUtility.FromJson<CreateAudioSourceParams>(paramsJson);
                if (audioParams == null || string.IsNullOrEmpty(audioParams.object_name))
                {
                    Debug.LogError("create_audio_source: audioParams or object_name is null");
                    throw new Exception("create_audio_source: audioParams or object_name is null");
                }
                return CreateAudioSource(audioParams);
                
            default:
                Debug.LogError($"Unknown method: {method}");
                throw new Exception($"Unknown method: {method}");
        }
    }

    // Result Classes
    [Serializable]
    private class WarningResult
    {
        public string warning;
    }

    [Serializable]
    private class ObjectResult
    {
        public bool success;
        public string name;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public bool active;
    }
    
    [Serializable]
    private class DeleteResult
    {
        public bool success;
        public string message;
    }
    
    [Serializable]
    private class MaterialResult
    {
        public bool success;
        public string objectName;
        public string materialName;
        public float[] color;
    }
    
    [Serializable]
    private class SceneInfoResult
    {
        public string name;
        public string path;
        public bool isDirty;
        public bool isLoaded;
        public int buildIndex;
        public int rootCount;
        public List<SceneObjectInfo> rootObjects;
    }
    
    [Serializable]
    private class SceneObjectInfo
    {
        public string name;
        public bool activeSelf;
        public string tag;
        public int layer;
        public int childCount;
    }
    
    [Serializable]
    private class ObjectInfoResult
    {
        public string name;
        public bool activeSelf;
        public bool activeInHierarchy;
        public string tag;
        public int layer;
        public string layerName;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public bool hasParent;
        public string parentName;
        public int childCount;
        public List<string> children;
        public List<ComponentInfo> components;
    }
    
    [Serializable]
    private class ComponentInfo
    {
        public string type;
        public bool enabled;
    }

    // Get scene info
    private SceneInfoResult GetSceneInfo()
    {
#if UNITY_2019_1_OR_NEWER
        var activeScene = SceneManager.GetActiveScene();
        var rootObjects = activeScene.GetRootGameObjects();
        
        var result = new SceneInfoResult
        {
            name = activeScene.name,
            path = activeScene.path,
            isDirty = activeScene.isDirty,
            isLoaded = activeScene.isLoaded,
            buildIndex = activeScene.buildIndex,
            rootCount = rootObjects.Length,
            rootObjects = new List<SceneObjectInfo>()
        };
        
        foreach (var obj in rootObjects)
        {
            result.rootObjects.Add(new SceneObjectInfo
            {
                name = obj.name,
                activeSelf = obj.activeSelf,
                tag = obj.tag,
                layer = obj.layer,
                childCount = obj.transform.childCount
            });
        }
        
        return result;
#else
        return new SceneInfoResult { 
            name = "Unknown", 
            path = "Unknown",
            isDirty = false,
            isLoaded = true,
            buildIndex = -1,
            rootCount = 0,
            rootObjects = new List<SceneObjectInfo>()
        };
#endif
    }

    // Get object info
    private ObjectInfoResult GetObjectInfo(string objectName)
    {
        var obj = GameObject.Find(objectName);
        
        if (obj == null)
        {
            Debug.LogError($"GetObjectInfo: Object '{objectName}' not found");
            throw new Exception($"Object '{objectName}' not found");
        }
        
        var result = new ObjectInfoResult
        {
            name = obj.name,
            activeSelf = obj.activeSelf,
            activeInHierarchy = obj.activeInHierarchy,
            tag = obj.tag,
            layer = obj.layer,
            layerName = LayerMask.LayerToName(obj.layer),
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            hasParent = obj.transform.parent != null,
            parentName = obj.transform.parent != null ? obj.transform.parent.name : "",
            childCount = obj.transform.childCount,
            children = new List<string>(),
            components = new List<ComponentInfo>()
        };
        
        // Add children
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            result.children.Add(obj.transform.GetChild(i).name);
        }
        
        // Add components
        var components = obj.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component != null)
            {
                var compInfo = new ComponentInfo
                {
                    type = component.GetType().Name
                };
                
                // Check if component is a Behaviour
                var behaviour = component as Behaviour;
                if (behaviour != null)
                {
                    compInfo.enabled = behaviour.enabled;
                }
                else
                {
                    compInfo.enabled = true; // Non-behaviours are always enabled
                }
                
                result.components.Add(compInfo);
            }
        }
        
        return result;
    }

    // Create object
    private ObjectResult CreateObject(CreateObjectParams parameters)
    {
        if (parameters == null)
        {
            Debug.LogError("CreateObject: Parameters are null");
            throw new Exception("CreateObject: Parameters are null");
        }
        
        if (string.IsNullOrEmpty(parameters.type))
        {
            Debug.LogError("CreateObject: type is null or empty");
            throw new Exception("CreateObject: type is null or empty");
        }
        
        // Object name not specified, create one
        string objectName = !string.IsNullOrEmpty(parameters.name) ? parameters.name : $"{parameters.type}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        
        // Create object
        GameObject newObject = null;
        
        switch (parameters.type.ToUpper())
        {
            case "CUBE":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
            case "SPHERE":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
            case "CYLINDER":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                break;
            case "PLANE":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                break;
            case "CAPSULE":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                break;
            case "QUAD":
                newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                break;
            case "EMPTY":
                newObject = new GameObject();
                break;
            default:
                Debug.LogError($"CreateObject: Unknown object type: {parameters.type}");
                throw new Exception($"CreateObject: Unknown object type: {parameters.type}");
        }
        
        newObject.name = objectName;
        
        // Set position, rotation and scale
        if (parameters.location != null && parameters.location.Length == 3)
            newObject.transform.position = new Vector3(parameters.location[0], parameters.location[1], parameters.location[2]);
        else
            newObject.transform.position = Vector3.zero; // Default position
        
        if (parameters.rotation != null && parameters.rotation.Length == 3)
            newObject.transform.eulerAngles = new Vector3(parameters.rotation[0], parameters.rotation[1], parameters.rotation[2]);
        else
            newObject.transform.eulerAngles = Vector3.zero; // Default rotation
        
        if (parameters.scale != null && parameters.scale.Length == 3)
            newObject.transform.localScale = new Vector3(parameters.scale[0], parameters.scale[1], parameters.scale[2]);
        else
            newObject.transform.localScale = Vector3.one; // Default scale
        
#if UNITY_2019_1_OR_NEWER
        if (!newObject.scene.IsValid() || newObject.scene != SceneManager.GetActiveScene())
        {
            SceneManager.MoveGameObjectToScene(newObject, SceneManager.GetActiveScene());
            Debug.Log($"Moved object {newObject.name} to active scene");
        }
#endif

        // Add debug log
        Debug.Log($"Created object: {newObject.name} at position {newObject.transform.position}, active: {newObject.activeSelf}");
        
        return new ObjectResult
        {
            success = true,
            name = newObject.name,
            position = new float[] { newObject.transform.position.x, newObject.transform.position.y, newObject.transform.position.z },
            rotation = new float[] { newObject.transform.eulerAngles.x, newObject.transform.eulerAngles.y, newObject.transform.eulerAngles.z },
            scale = new float[] { newObject.transform.localScale.x, newObject.transform.localScale.y, newObject.transform.localScale.z },
            active = newObject.activeSelf
        };
    }

    // Modify object
    private ObjectResult ModifyObject(ModifyObjectParams parameters)
    {
        if (parameters == null || string.IsNullOrEmpty(parameters.name))
        {
            Debug.LogError("ModifyObject: name is null or empty");
            throw new Exception("ModifyObject: name is null or empty");
        }
        
        var obj = GameObject.Find(parameters.name);
        
        if (obj == null)
        {
            Debug.LogError($"ModifyObject: Object '{parameters.name}' not found");
            throw new Exception($"Object '{parameters.name}' not found");
        }
        
        // Position, rotation, scale and visibility settings
        if (parameters.location != null && parameters.location.Length == 3)
            obj.transform.position = new Vector3(parameters.location[0], parameters.location[1], parameters.location[2]);
            
        if (parameters.rotation != null && parameters.rotation.Length == 3)
            obj.transform.eulerAngles = new Vector3(parameters.rotation[0], parameters.rotation[1], parameters.rotation[2]);
            
        if (parameters.scale != null && parameters.scale.Length == 3)
            obj.transform.localScale = new Vector3(parameters.scale[0], parameters.scale[1], parameters.scale[2]);
            
        if (parameters.visible.HasValue)
            obj.SetActive(parameters.visible.Value);
            
        Debug.Log($"Modified object: {obj.name}, position: {obj.transform.position}, active: {obj.activeSelf}");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    // Delete object
    private DeleteResult DeleteObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogError("DeleteObject: objectName is null or empty");
            throw new Exception("DeleteObject: objectName is null or empty");
        }
        
        var obj = GameObject.Find(objectName);
        
        if (obj == null)
        {
            Debug.LogError($"DeleteObject: Object '{objectName}' not found");
            throw new Exception($"Object '{objectName}' not found");
        }
        
        Destroy(obj);
        Debug.Log($"Deleted object: {objectName}");
        
        return new DeleteResult
        {
            success = true,
            message = $"Object '{objectName}' deleted successfully"
        };
    }

    // Set material
    private MaterialResult SetMaterial(SetMaterialParams parameters)
    {
        if (parameters == null || string.IsNullOrEmpty(parameters.object_name))
        {
            Debug.LogError("SetMaterial: object_name is null or empty");
            throw new Exception("SetMaterial: object_name is null or empty");
        }
        
        var obj = GameObject.Find(parameters.object_name);
        
        if (obj == null)
        {
            Debug.LogError($"SetMaterial: Object '{parameters.object_name}' not found");
            throw new Exception($"Object '{parameters.object_name}' not found");
        }
        
        var renderer = obj.GetComponent<Renderer>();
        
        if (renderer == null)
        {
            Debug.LogError($"SetMaterial: Object '{parameters.object_name}' has no Renderer component");
            throw new Exception($"Object '{parameters.object_name}' has no Renderer component");
        }
        
        // Material name specified, find and apply it
        if (!string.IsNullOrEmpty(parameters.material_name))
        {
            var material = Resources.Load<Material>(parameters.material_name);
            
            if (material != null)
            {
                renderer.material = material;
                Debug.Log($"Applied material '{parameters.material_name}' to object '{parameters.object_name}'");
            }
            else
            {
                Debug.LogWarning($"Material '{parameters.material_name}' not found, creating a new material");
                var newMaterial = new Material(Shader.Find("Standard"));
                if (newMaterial.shader == null)
                {
                    Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                    newMaterial = new Material(Shader.Find("Default-Diffuse"));
                    if (newMaterial.shader == null)
                    {
                        Debug.LogError("No compatible shaders found. Will use default material.");
                        newMaterial = new Material(Shader.Find("Unlit/Color"));
                    }
                }
                renderer.material = newMaterial;
                renderer.material.name = parameters.material_name;
            }
        }
        else
        {
            // Material name not specified and no current material, create a new material
            if (renderer.material == null)
            {
                var newMaterial = new Material(Shader.Find("Standard"));
                if (newMaterial.shader == null)
                {
                    Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                    newMaterial = new Material(Shader.Find("Default-Diffuse"));
                    if (newMaterial.shader == null)
                    {
                        Debug.LogError("No compatible shaders found. Will use default material.");
                        newMaterial = new Material(Shader.Find("Unlit/Color"));
                    }
                }
                renderer.material = newMaterial;
                renderer.material.name = $"Material_{parameters.object_name}";
            }
        }
        
        // Color specified, change material color
        if (parameters.color != null)
        {
            if (parameters.color.Length >= 3)
            {
                float r = parameters.color[0];
                float g = parameters.color[1];
                float b = parameters.color[2];
                float a = parameters.color.Length >= 4 ? parameters.color[3] : 1.0f;
                
                renderer.material.color = new Color(r, g, b, a);
                Debug.Log($"Set color of object '{parameters.object_name}' to ({r}, {g}, {b}, {a})");
            }
            else
            {
                Debug.LogError("SetMaterial: Color array must have at least 3 elements (RGB)");
                throw new Exception("SetMaterial: Color array must have at least 3 elements (RGB)");
            }
        }
        
        return new MaterialResult
        {
            success = true,
            objectName = parameters.object_name,
            materialName = renderer.material.name,
            color = new float[] { 
                renderer.material.color.r, 
                renderer.material.color.g, 
                renderer.material.color.b, 
                renderer.material.color.a 
            }
        };
    }

    // Execute custom Unity code
    private WarningResult ExecuteUnityCode(ExecuteCodeParams parameters)
    {
        if (parameters == null || string.IsNullOrEmpty(parameters.code))
        {
            Debug.LogError("ExecuteUnityCode: code is null or empty");
            throw new Exception("ExecuteUnityCode: code is null or empty");
        }
        
        Debug.LogWarning("ExecuteUnityCode: This functionality is limited for security reasons");
        
        return new WarningResult
        {
            warning = "ExecuteUnityCode: Custom code execution is not fully implemented for security reasons"
        };
    }

    // New method implementations
    
    // Create terrain
    private ObjectResult CreateTerrain(int width, int length, float height, float[,] heightmap = null)
    {
        // Create a new terrain game object
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 129;
        terrainData.size = new Vector3(width, height, length);
        
        // Apply heightmap if provided, otherwise create a flat terrain
        if (heightmap != null)
        {
            terrainData.SetHeights(0, 0, heightmap);
        }
        
        // Create the terrain game object
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "GeneratedTerrain";
        
        // Get terrain component
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        
        // Set terrain properties
        terrain.materialTemplate = new Material(Shader.Find("Standard"));
        terrain.materialTemplate.color = new Color(0.5f, 0.7f, 0.2f); // Greenish color
        
        Debug.Log($"Created terrain: {terrainObject.name}");
        
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

    // Create water
    private ObjectResult CreateWater(float width, float length, float height)
    {
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "Water";
        waterPlane.transform.position = new Vector3(0, height, 0);
        waterPlane.transform.localScale = new Vector3(width/10f, 1, length/10f);
        
        // Create water material
        Material waterMaterial = new Material(Shader.Find("Standard"));
        waterMaterial.color = new Color(0, 0.5f, 1f, 0.7f);
        waterPlane.GetComponent<Renderer>().material = waterMaterial;
        
        Debug.Log($"Created water: {waterPlane.name}");
        
        return new ObjectResult
        {
            success = true,
            name = waterPlane.name,
            position = new float[] { waterPlane.transform.position.x, waterPlane.transform.position.y, waterPlane.transform.position.z },
            rotation = new float[] { waterPlane.transform.eulerAngles.x, waterPlane.transform.eulerAngles.y, waterPlane.transform.eulerAngles.z },
            scale = new float[] { waterPlane.transform.localScale.x, waterPlane.transform.localScale.y, waterPlane.transform.localScale.z },
            active = waterPlane.activeSelf
        };
    }

    // Create vegetation
    private ObjectResult CreateVegetation(string paramsJson)
    {
        var parameters = JsonUtility.FromJson<CreateVegetationParams>(paramsJson);
        
        if (parameters == null || string.IsNullOrEmpty(parameters.type))
        {
            throw new Exception("CreateVegetation: Invalid parameters");
        }
        
        Vector3 position = parameters.position != null && parameters.position.Length >= 3 
            ? new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]) 
            : Vector3.zero;
        
        float scale = parameters.scale > 0 ? parameters.scale : 1.0f;
        
        GameObject vegetation = null;
        GameObject foliage = null;
        
        string type = parameters.type.ToLower();
        Debug.Log($"Creating vegetation of type: {type}");
        
        switch (type)
        {
            case "tree":
                // Main trunk
                vegetation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                vegetation.name = "Tree";
                vegetation.transform.position = position;
                vegetation.transform.localScale = new Vector3(scale * 0.5f, scale * 5.0f, scale * 0.5f);
                
                // Foliage (top part)
                foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "TreeFoliage";
                foliage.transform.parent = vegetation.transform;
                foliage.transform.localPosition = new Vector3(0, 1.0f, 0);
                foliage.transform.localScale = new Vector3(2.0f, 1.5f, 2.0f);
                
                // Use Unity's default materials to ensure compatibility
                Renderer trunkRenderer = vegetation.GetComponent<Renderer>();
                Renderer foliageRenderer = foliage.GetComponent<Renderer>();
                
                // Remove any existing materials
                if (trunkRenderer != null)
                {
                    Material trunkMaterial = new Material(Shader.Find("Standard"));
                    if (trunkMaterial.shader == null)
                    {
                        Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                        trunkMaterial = new Material(Shader.Find("Default-Diffuse"));
                        if (trunkMaterial.shader == null)
                        {
                            Debug.LogError("No compatible shader found for trunk");
                            trunkMaterial = new Material(Shader.Find("Unlit/Color"));
                        }
                    }
                    trunkMaterial.color = new Color(0.5f, 0.3f, 0.1f); // Brown
                    trunkRenderer.material = trunkMaterial;
                    Debug.Log($"Applied trunk material with color: {trunkMaterial.color}");
                }
                
                if (foliageRenderer != null)
                {
                    Material foliageMaterial = new Material(Shader.Find("Standard"));
                    if (foliageMaterial.shader == null)
                    {
                        Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                        foliageMaterial = new Material(Shader.Find("Default-Diffuse"));
                        if (foliageMaterial.shader == null)
                        {
                            Debug.LogError("No compatible shader found for foliage");
                            foliageMaterial = new Material(Shader.Find("Unlit/Color"));
                        }
                    }
                    foliageMaterial.color = new Color(0.1f, 0.6f, 0.1f); // Green
                    foliageRenderer.material = foliageMaterial;
                    Debug.Log($"Applied foliage material with color: {foliageMaterial.color}");
                }
                
                break;
                
            case "bush":
                vegetation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vegetation.name = "Bush";
                vegetation.transform.position = position;
                vegetation.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
                
                Renderer bushRenderer = vegetation.GetComponent<Renderer>();
                if (bushRenderer != null)
                {
                    Material bushMaterial = new Material(Shader.Find("Standard"));
                    if (bushMaterial.shader == null)
                    {
                        Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                        bushMaterial = new Material(Shader.Find("Default-Diffuse"));
                        if (bushMaterial.shader == null)
                        {
                            Debug.LogError("No compatible shader found for bush");
                            bushMaterial = new Material(Shader.Find("Unlit/Color"));
                        }
                    }
                    bushMaterial.color = new Color(0.1f, 0.5f, 0.1f); // Green
                    bushRenderer.material = bushMaterial;
                    Debug.Log($"Applied bush material with color: {bushMaterial.color}");
                }
                break;
                
            case "rock":
                vegetation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vegetation.name = "Rock";
                vegetation.transform.position = position;
                vegetation.transform.localScale = new Vector3(scale, scale * 0.8f, scale);
                vegetation.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 30f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 30f));
                
                Renderer rockRenderer = vegetation.GetComponent<Renderer>();
                if (rockRenderer != null)
                {
                    Material rockMaterial = new Material(Shader.Find("Standard"));
                    if (rockMaterial.shader == null)
                    {
                        Debug.LogWarning("Standard shader not found, using Default-Diffuse shader");
                        rockMaterial = new Material(Shader.Find("Default-Diffuse"));
                        if (rockMaterial.shader == null)
                        {
                            Debug.LogError("No compatible shader found for rock");
                            rockMaterial = new Material(Shader.Find("Unlit/Color"));
                        }
                    }
                    rockMaterial.color = new Color(0.5f, 0.5f, 0.5f); // Gray
                    rockRenderer.material = rockMaterial;
                    Debug.Log($"Applied rock material with color: {rockMaterial.color}");
                }
                break;
                
            default:
                throw new Exception($"Unknown vegetation type: {type}");
        }
        
        // Apply custom color if provided
        if (parameters.color != null && parameters.color.Length >= 3)
        {
            Renderer renderer = vegetation.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color customColor = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1f
                );
                renderer.material.color = customColor;
                Debug.Log($"Applied custom color: R={parameters.color[0]}, G={parameters.color[1]}, B={parameters.color[2]}");
                
                // Also apply color to foliage if it exists (for trees)
                if (foliage != null)
                {
                    Renderer foliageRenderer = foliage.GetComponent<Renderer>();
                    if (foliageRenderer != null && foliageRenderer.material != null)
                    {
                        foliageRenderer.material.color = customColor;
                    }
                }
            }
        }
        
        Debug.Log($"Created vegetation: {vegetation.name}");
        
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

    // Create skybox
    private ObjectResult CreateSkybox(string type, float[] color)
    {
        // Create a new object to hold the skybox settings
        GameObject skyboxObject = new GameObject("SkyboxSettings");
        
        // Create a material for the skybox
        Material skyboxMaterial = new Material(Shader.Find("Skybox/6 Sided"));
        
        // Apply different settings based on the type
        switch (type.ToLower())
        {
            case "day":
                skyboxMaterial.SetColor("_SkyTint", new Color(0.5f, 0.8f, 1f));
                break;
                
            case "night":
                skyboxMaterial.SetColor("_SkyTint", new Color(0.05f, 0.05f, 0.1f));
                break;
                
            case "sunset":
                skyboxMaterial.SetColor("_SkyTint", new Color(0.9f, 0.6f, 0.4f));
                break;
                
            case "space":
                skyboxMaterial.SetColor("_SkyTint", new Color(0.01f, 0.01f, 0.02f));
                break;
                
            default:
                throw new Exception($"Unknown skybox type: {type}");
        }
        
        // Override color if provided
        if (color != null && color.Length >= 3)
        {
            skyboxMaterial.SetColor("_SkyTint", new Color(color[0], color[1], color[2]));
        }
        
        // Apply the skybox material to the scene
        RenderSettings.skybox = skyboxMaterial;
        
        Debug.Log($"Created skybox: {type}");
        
        return new ObjectResult
        {
            success = true,
            name = skyboxObject.name,
            position = new float[] { 0, 0, 0 },
            rotation = new float[] { 0, 0, 0 },
            scale = new float[] { 1, 1, 1 },
            active = true
        };
    }

    // Create character
    private ObjectResult CreateCharacter(string characterType, Vector3 position)
    {
        GameObject character = null;
        
        switch(characterType.ToLower())
        {
            case "human":
                // Create a simple humanoid character
                character = new GameObject("Human");
                character.transform.position = position;
                
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
                
                GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                rightArm.transform.parent = character.transform;
                rightArm.transform.localPosition = new Vector3(0.35f, 1.1f, 0);
                rightArm.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
                
                GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leftLeg.transform.parent = character.transform;
                leftLeg.transform.localPosition = new Vector3(-0.15f, 0.5f, 0);
                leftLeg.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
                
                GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                rightLeg.transform.parent = character.transform;
                rightLeg.transform.localPosition = new Vector3(0.15f, 0.5f, 0);
                rightLeg.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
                
                // Add character controller component
                character.AddComponent<CharacterController>();
                
                break;
                
            case "robot":
                // Create a simple robot character
                character = new GameObject("Robot");
                character.transform.position = position;
                
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
                
                // Set materials
                Material robotMaterial = new Material(Shader.Find("Standard"));
                robotMaterial.color = new Color(0.7f, 0.7f, 0.7f);
                
                Material eyeMaterial = new Material(Shader.Find("Standard"));
                eyeMaterial.color = new Color(1f, 0, 0);
                eyeMaterial.SetFloat("_Emission", 1.0f);
                
                robotBody.GetComponent<Renderer>().material = robotMaterial;
                robotHead.GetComponent<Renderer>().material = robotMaterial;
                robotLeftArm.GetComponent<Renderer>().material = robotMaterial;
                robotRightArm.GetComponent<Renderer>().material = robotMaterial;
                robotLeftLeg.GetComponent<Renderer>().material = robotMaterial;
                robotRightLeg.GetComponent<Renderer>().material = robotMaterial;
                leftEye.GetComponent<Renderer>().material = eyeMaterial;
                rightEye.GetComponent<Renderer>().material = eyeMaterial;
                
                // Add character controller component
                character.AddComponent<CharacterController>();
                
                break;
                
            default:
                throw new Exception($"Unknown character type: {characterType}");
        }
        
        Debug.Log($"Created character: {character.name}");
        
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

    // Set animation
    private ObjectResult SetAnimation(string objectName, string animationName)
    {
        GameObject obj = GameObject.Find(objectName);
        
        if (obj == null)
        {
            throw new Exception($"Object '{objectName}' not found");
        }
        
        // For a proper implementation, we would use the Animator component
        // But for this basic implementation, we'll just log it
        Debug.Log($"Set animation '{animationName}' for object '{objectName}'");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    // Set character controller properties
    private ObjectResult SetCharacterController(SetCharacterControllerParams parameters)
    {
        GameObject obj = GameObject.Find(parameters.object_name);
        
        if (obj == null)
        {
            throw new Exception($"Object '{parameters.object_name}' not found");
        }
        
        CharacterController controller = obj.GetComponent<CharacterController>();
        
        if (controller == null)
        {
            controller = obj.AddComponent<CharacterController>();
        }
        
        // Here we would set properties for the controller
        // But we'll just log it for this basic implementation
        Debug.Log($"Set character controller properties for '{parameters.object_name}': Speed={parameters.speed}, Jump Height={parameters.jump_height}");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    // Create vehicle
    private ObjectResult CreateVehicle(string vehicleType, Vector3 position)
    {
        GameObject vehicle = null;
        
        switch(vehicleType.ToLower())
        {
            case "car":
                // Create a simple car
                vehicle = new GameObject("Car");
                vehicle.transform.position = position;
                
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
                
                // Set materials
                Material bodyMaterial = new Material(Shader.Find("Standard"));
                bodyMaterial.color = new Color(0.8f, 0.2f, 0.2f);
                
                Material windowMaterial = new Material(Shader.Find("Standard"));
                windowMaterial.color = new Color(0.2f, 0.3f, 0.8f, 0.7f);
                
                Material wheelMaterial = new Material(Shader.Find("Standard"));
                wheelMaterial.color = new Color(0.1f, 0.1f, 0.1f);
                
                carBody.GetComponent<Renderer>().material = bodyMaterial;
                cabin.GetComponent<Renderer>().material = windowMaterial;
                wheel1.GetComponent<Renderer>().material = wheelMaterial;
                wheel2.GetComponent<Renderer>().material = wheelMaterial;
                wheel3.GetComponent<Renderer>().material = wheelMaterial;
                wheel4.GetComponent<Renderer>().material = wheelMaterial;
                
                // Add rigidbody for physics
                vehicle.AddComponent<Rigidbody>();
                
                break;
                
            case "airplane":
                // Create a simple airplane
                vehicle = new GameObject("Airplane");
                vehicle.transform.position = position;
                
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
                
                // Set materials
                Material planeMaterial = new Material(Shader.Find("Standard"));
                planeMaterial.color = new Color(0.8f, 0.8f, 0.8f);
                
                planeBody.GetComponent<Renderer>().material = planeMaterial;
                leftWing.GetComponent<Renderer>().material = planeMaterial;
                rightWing.GetComponent<Renderer>().material = planeMaterial;
                tail.GetComponent<Renderer>().material = planeMaterial;
                
                // Add rigidbody for physics
                vehicle.AddComponent<Rigidbody>();
                
                break;
                
            default:
                throw new Exception($"Unknown vehicle type: {vehicleType}");
        }
        
        Debug.Log($"Created vehicle: {vehicle.name}");
        
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

    // Set vehicle properties
    private ObjectResult SetVehicleProperties(SetVehiclePropertiesParams parameters)
    {
        GameObject vehicle = GameObject.Find(parameters.object_name);
        
        if (vehicle == null)
        {
            throw new Exception($"Vehicle '{parameters.object_name}' not found");
        }
        
        // Here we would implement proper vehicle physics
        // But for this demo, we'll just log the values
        Debug.Log($"Set vehicle properties for '{parameters.object_name}': Top Speed={parameters.top_speed}, Acceleration={parameters.acceleration}");
        
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

    // Create light
    private ObjectResult CreateLight(CreateLightParams parameters)
    {
        GameObject lightObj = new GameObject(parameters.lightType + "Light");
        Light light = lightObj.AddComponent<Light>();
        
        // Set position if provided
        if (parameters.position != null && parameters.position.Length == 3)
        {
            lightObj.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
        }
        
        // Set light type
        switch (parameters.lightType.ToLower())
        {
            case "directional":
                light.type = LightType.Directional;
                break;
                
            case "point":
                light.type = LightType.Point;
                break;
                
            case "spot":
                light.type = LightType.Spot;
                break;
                
            default:
                throw new Exception($"Unknown light type: {parameters.lightType}");
        }
        
        // Set intensity if provided
        if (parameters.intensity > 0)
        {
            light.intensity = parameters.intensity;
        }
        
        // Set color if provided
        if (parameters.color != null && parameters.color.Length >= 3)
        {
            light.color = new Color(parameters.color[0], parameters.color[1], parameters.color[2]);
        }
        
        Debug.Log($"Created light: {lightObj.name}");
        
        return new ObjectResult
        {
            success = true,
            name = lightObj.name,
            position = new float[] { lightObj.transform.position.x, lightObj.transform.position.y, lightObj.transform.position.z },
            rotation = new float[] { lightObj.transform.eulerAngles.x, lightObj.transform.eulerAngles.y, lightObj.transform.eulerAngles.z },
            scale = new float[] { lightObj.transform.localScale.x, lightObj.transform.localScale.y, lightObj.transform.localScale.z },
            active = lightObj.activeSelf
        };
    }

    // Create particle system
    private ObjectResult CreateParticleSystem(CreateParticleSystemParams parameters)
    {
        GameObject particleObj = new GameObject(parameters.effectType + "Effect");
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        
        // Set position if provided
        if (parameters.position != null && parameters.position.Length == 3)
        {
            particleObj.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
        }
        
        // Configure particle system based on effect type
        var main = particles.main;
        var emission = particles.emission;
        var shape = particles.shape;
        var colorOverLifetime = particles.colorOverLifetime;
        
        switch (parameters.effectType.ToLower())
        {
            case "fire":
                main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
                main.startSize = new ParticleSystem.MinMaxCurve(0.5f * parameters.scale, 1.5f * parameters.scale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                emission.rateOverTime = 20f;
                shape.shapeType = ParticleSystemShapeType.Cone;
                break;
                
            case "smoke":
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.7f, 0.7f, 0.5f), new Color(0.3f, 0.3f, 0.3f, 0.2f));
                main.startSize = new ParticleSystem.MinMaxCurve(1f * parameters.scale, 2f * parameters.scale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 3f);
                emission.rateOverTime = 10f;
                shape.shapeType = ParticleSystemShapeType.Cone;
                break;
                
            case "water":
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.6f, 1f, 0.5f), new Color(0.2f, 0.4f, 0.8f, 0.3f));
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f * parameters.scale, 0.3f * parameters.scale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                main.gravityModifier = 1f;
                emission.rateOverTime = 50f;
                shape.shapeType = ParticleSystemShapeType.Circle;
                break;
                
            case "explosion":
                main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f * parameters.scale, 1f * parameters.scale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });
                shape.shapeType = ParticleSystemShapeType.Sphere;
                break;
                
            default:
                throw new Exception($"Unknown effect type: {parameters.effectType}");
        }
        
        Debug.Log($"Created particle system: {particleObj.name}");
        
        return new ObjectResult
        {
            success = true,
            name = particleObj.name,
            position = new float[] { particleObj.transform.position.x, particleObj.transform.position.y, particleObj.transform.position.z },
            rotation = new float[] { particleObj.transform.eulerAngles.x, particleObj.transform.eulerAngles.y, particleObj.transform.eulerAngles.z },
            scale = new float[] { particleObj.transform.localScale.x, particleObj.transform.localScale.y, particleObj.transform.localScale.z },
            active = particleObj.activeSelf
        };
    }

    // Set post-processing
    private ObjectResult SetPostProcessing(SetPostProcessingParams parameters)
    {
        // For a full implementation, this would use the Post-Processing package
        // But for this demo, we'll just create a placeholder object and log the values
        GameObject postProcessingObj = GameObject.Find("PostProcessingVolume");
        
        if (postProcessingObj == null)
        {
            postProcessingObj = new GameObject("PostProcessingVolume");
        }
        
        Debug.Log($"Set post-processing effect '{parameters.effect}' with intensity {parameters.intensity}");
        
        return new ObjectResult
        {
            success = true,
            name = postProcessingObj.name,
            position = new float[] { postProcessingObj.transform.position.x, postProcessingObj.transform.position.y, postProcessingObj.transform.position.z },
            rotation = new float[] { postProcessingObj.transform.eulerAngles.x, postProcessingObj.transform.eulerAngles.y, postProcessingObj.transform.eulerAngles.z },
            scale = new float[] { postProcessingObj.transform.localScale.x, postProcessingObj.transform.localScale.y, postProcessingObj.transform.localScale.z },
            active = postProcessingObj.activeSelf
        };
    }

    // Add rigidbody
    private ObjectResult AddRigidbody(AddRigidbodyParams parameters)
    {
        GameObject obj = GameObject.Find(parameters.object_name);
        
        if (obj == null)
        {
            throw new Exception($"Object '{parameters.object_name}' not found");
        }
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        
        rb.mass = parameters.mass;
        rb.useGravity = parameters.use_gravity;
        
        Debug.Log($"Added/modified rigidbody for '{parameters.object_name}': Mass={parameters.mass}, UseGravity={parameters.use_gravity}");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    // Apply force
    private ObjectResult ApplyForce(ApplyForceParams parameters)
    {
        GameObject obj = GameObject.Find(parameters.object_name);
        
        if (obj == null)
        {
            throw new Exception($"Object '{parameters.object_name}' not found");
        }
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            throw new Exception($"Object '{parameters.object_name}' has no Rigidbody component");
        }
        
        Vector3 force = parameters.force != null && parameters.force.Length == 3 ?
            new Vector3(parameters.force[0], parameters.force[1], parameters.force[2]) :
            Vector3.zero;
            
        ForceMode mode = ForceMode.Force;
        
        if (!string.IsNullOrEmpty(parameters.mode))
        {
            switch (parameters.mode.ToLower())
            {
                case "force":
                    mode = ForceMode.Force;
                    break;
                    
                case "impulse":
                    mode = ForceMode.Impulse;
                    break;
                    
                case "acceleration":
                    mode = ForceMode.Acceleration;
                    break;
                    
                case "velocitychange":
                    mode = ForceMode.VelocityChange;
                    break;
            }
        }
        
        rb.AddForce(force, mode);
        
        Debug.Log($"Applied force to '{parameters.object_name}': Force={force}, Mode={mode}");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    // Create joint
    private ObjectResult CreateJoint(CreateJointParams parameters)
    {
        GameObject obj1 = GameObject.Find(parameters.object1);
        GameObject obj2 = GameObject.Find(parameters.object2);
        
        if (obj1 == null)
        {
            throw new Exception($"Object '{parameters.object1}' not found");
        }
        
        if (obj2 == null)
        {
            throw new Exception($"Object '{parameters.object2}' not found");
        }
        
        // Make sure both objects have rigidbodies
        Rigidbody rb1 = obj1.GetComponent<Rigidbody>();
        if (rb1 == null)
        {
            rb1 = obj1.AddComponent<Rigidbody>();
        }
        
        Rigidbody rb2 = obj2.GetComponent<Rigidbody>();
        if (rb2 == null)
        {
            rb2 = obj2.AddComponent<Rigidbody>();
        }
        
        Joint joint = null;
        
        switch (parameters.joint_type.ToLower())
        {
            case "fixed":
                FixedJoint fixedJoint = obj1.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = rb2;
                joint = fixedJoint;
                break;
                
            case "hinge":
                HingeJoint hingeJoint = obj1.AddComponent<HingeJoint>();
                hingeJoint.connectedBody = rb2;
                joint = hingeJoint;
                break;
                
            case "spring":
                SpringJoint springJoint = obj1.AddComponent<SpringJoint>();
                springJoint.connectedBody = rb2;
                joint = springJoint;
                break;
                
            default:
                throw new Exception($"Unknown joint type: {parameters.joint_type}");
        }
        
        Debug.Log($"Created {parameters.joint_type} joint between '{parameters.object1}' and '{parameters.object2}'");
        
        return new ObjectResult
        {
            success = true,
            name = obj1.name,
            position = new float[] { obj1.transform.position.x, obj1.transform.position.y, obj1.transform.position.z },
            rotation = new float[] { obj1.transform.eulerAngles.x, obj1.transform.eulerAngles.y, obj1.transform.eulerAngles.z },
            scale = new float[] { obj1.transform.localScale.x, obj1.transform.localScale.y, obj1.transform.localScale.z },
            active = obj1.activeSelf
        };
    }

    // Create camera
    private ObjectResult CreateCamera(CreateCameraParams parameters)
    {
        GameObject cameraObj = new GameObject(parameters.camera_type + "Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        
        // Set position if provided
        if (parameters.position != null && parameters.position.Length == 3)
        {
            cameraObj.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
        }
        
        // Configure camera based on type
        switch (parameters.camera_type.ToLower())
        {
            case "main":
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 1000f;
                break;
                
            case "first_person":
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 70f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 1000f;
                break;
                
            case "third_person":
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = 50f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 1000f;
                break;
                
            default:
                throw new Exception($"Unknown camera type: {parameters.camera_type}");
        }
        
        // Set target if provided
        if (!string.IsNullOrEmpty(parameters.target))
        {
            GameObject targetObj = GameObject.Find(parameters.target);
            
            if (targetObj != null)
            {
                cameraObj.transform.LookAt(targetObj.transform);
            }
            else
            {
                Debug.LogWarning($"Camera target '{parameters.target}' not found");
            }
        }
        
        Debug.Log($"Created camera: {cameraObj.name}");
        
        return new ObjectResult
        {
            success = true,
            name = cameraObj.name,
            position = new float[] { cameraObj.transform.position.x, cameraObj.transform.position.y, cameraObj.transform.position.z },
            rotation = new float[] { cameraObj.transform.eulerAngles.x, cameraObj.transform.eulerAngles.y, cameraObj.transform.eulerAngles.z },
            scale = new float[] { cameraObj.transform.localScale.x, cameraObj.transform.localScale.y, cameraObj.transform.localScale.z },
            active = cameraObj.activeSelf
        };
    }

    // Set active camera
    private ObjectResult SetActiveCamera(string cameraName)
    {
        GameObject cameraObj = GameObject.Find(cameraName);
        
        if (cameraObj == null)
        {
            throw new Exception($"Camera '{cameraName}' not found");
        }
        
        Camera camera = cameraObj.GetComponent<Camera>();
        
        if (camera == null)
        {
            throw new Exception($"Object '{cameraName}' has no Camera component");
        }
        
        // Get all cameras and disable them
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            cam.enabled = false;
        }
        
        // Enable the specified camera
        camera.enabled = true;
        
        Debug.Log($"Set active camera to '{cameraName}'");
        
        return new ObjectResult
        {
            success = true,
            name = cameraObj.name,
            position = new float[] { cameraObj.transform.position.x, cameraObj.transform.position.y, cameraObj.transform.position.z },
            rotation = new float[] { cameraObj.transform.eulerAngles.x, cameraObj.transform.eulerAngles.y, cameraObj.transform.eulerAngles.z },
            scale = new float[] { cameraObj.transform.localScale.x, cameraObj.transform.localScale.y, cameraObj.transform.localScale.z },
            active = cameraObj.activeSelf
        };
    }

    // Set camera properties
    private ObjectResult SetCameraProperties(SetCameraPropertiesParams parameters)
    {
        GameObject cameraObj = GameObject.Find(parameters.camera_name);
        
        if (cameraObj == null)
        {
            throw new Exception($"Camera '{parameters.camera_name}' not found");
        }
        
        Camera camera = cameraObj.GetComponent<Camera>();
        
        if (camera == null)
        {
            throw new Exception($"Object '{parameters.camera_name}' has no Camera component");
        }
        
        camera.fieldOfView = parameters.field_of_view;
        camera.nearClipPlane = parameters.near_clip;
        camera.farClipPlane = parameters.far_clip;
        
        Debug.Log($"Set camera properties for '{parameters.camera_name}': FOV={parameters.field_of_view}, Near Clip={parameters.near_clip}, Far Clip={parameters.far_clip}");
        
        return new ObjectResult
        {
            success = true,
            name = cameraObj.name,
            position = new float[] { cameraObj.transform.position.x, cameraObj.transform.position.y, cameraObj.transform.position.z },
            rotation = new float[] { cameraObj.transform.eulerAngles.x, cameraObj.transform.eulerAngles.y, cameraObj.transform.eulerAngles.z },
            scale = new float[] { cameraObj.transform.localScale.x, cameraObj.transform.localScale.y, cameraObj.transform.localScale.z },
            active = cameraObj.activeSelf
        };
    }

    // Play sound
    private ObjectResult PlaySound(PlaySoundParams parameters)
    {
        // For a full implementation, this would use actual audio clips
        // But for this demo, we'll just create a placeholder object and log the values
        GameObject soundObj = new GameObject(parameters.sound_type + "Sound");
        
        // Set position if provided
        if (parameters.position != null && parameters.position.Length == 3)
        {
            soundObj.transform.position = new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]);
        }
        
        // Add audio source component
        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        
        // Configure audio source
        audioSource.volume = parameters.volume;
        audioSource.spatialBlend = parameters.position != null ? 1f : 0f; // Spatial audio if position provided
        
        // In a real implementation, we would load and play actual audio clips
        Debug.Log($"Playing sound: {parameters.sound_type}, Volume: {parameters.volume}");
        
        return new ObjectResult
        {
            success = true,
            name = soundObj.name,
            position = new float[] { soundObj.transform.position.x, soundObj.transform.position.y, soundObj.transform.position.z },
            rotation = new float[] { soundObj.transform.eulerAngles.x, soundObj.transform.eulerAngles.y, soundObj.transform.eulerAngles.z },
            scale = new float[] { soundObj.transform.localScale.x, soundObj.transform.localScale.y, soundObj.transform.localScale.z },
            active = soundObj.activeSelf
        };
    }

    // Create audio source
    private ObjectResult CreateAudioSource(CreateAudioSourceParams parameters)
    {
        GameObject obj = GameObject.Find(parameters.object_name);
        
        if (obj == null)
        {
            throw new Exception($"Object '{parameters.object_name}' not found");
        }
        
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = obj.AddComponent<AudioSource>();
        }
        
        audioSource.loop = parameters.loop;
        audioSource.volume = parameters.volume;
        
         // In a real implementation, we would load and configure actual audio clips
        Debug.Log($"Created audio source for '{parameters.object_name}': Type={parameters.audio_type}, Loop={parameters.loop}, Volume={parameters.volume}");
        
        return new ObjectResult
        {
            success = true,
            name = obj.name,
            position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
            rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
            scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
            active = obj.activeSelf
        };
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (server != null)
        {
            server.Stop();
            Debug.Log("Unity MCP Server stopped");
        }
    }
} // End of UnityMCPServer class