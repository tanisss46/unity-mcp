using UnityEngine;
using System;
using System.Collections.Generic;

public partial class UnityMCPServer : MonoBehaviour
{
    [Serializable]
    private class CreateLegoCharacterParams
    {
        public string name = "LegoCharacter";
        public float[] position = new float[] { 0, 0, 0 };
        public float[] rotation = new float[] { 0, 0, 0 };
        public float scale = 1.0f;
        public float[] body_color = new float[] { 1, 0, 0, 1 };  // Red by default
        public float[] head_color = new float[] { 1, 1, 0, 1 };  // Yellow by default
    }

    private ObjectResult CreateLegoCharacter(string paramsJson)
    {
        var parameters = JsonUtility.FromJson<CreateLegoCharacterParams>(paramsJson);
        
        if (parameters == null)
        {
            Debug.LogError("CreateLegoCharacter: Parameters are null");
            throw new Exception("CreateLegoCharacter: Parameters are null");
        }
        
        // Create parent object for the LEGO character
        GameObject character = new GameObject(parameters.name);
        Vector3 position = parameters.position != null && parameters.position.Length >= 3 
            ? new Vector3(parameters.position[0], parameters.position[1], parameters.position[2]) 
            : Vector3.zero;
        character.transform.position = position;
        
        Vector3 rotation = parameters.rotation != null && parameters.rotation.Length >= 3 
            ? new Vector3(parameters.rotation[0], parameters.rotation[1], parameters.rotation[2]) 
            : Vector3.zero;
        character.transform.eulerAngles = rotation;
        
        float scale = parameters.scale > 0 ? parameters.scale : 1.0f;
        
        // Create body (cube)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.parent = character.transform;
        body.transform.localPosition = new Vector3(0, 0, 0);
        body.transform.localScale = new Vector3(1.5f * scale, 2f * scale, 1f * scale);
        
        // Create head (cylinder)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        head.name = "Head";
        head.transform.parent = character.transform;
        head.transform.localPosition = new Vector3(0, 1.25f * scale, 0);
        head.transform.localScale = new Vector3(1f * scale, 0.5f * scale, 1f * scale);
        
        // Create stud on top of head (optional - classic Lego feature)
        GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stud.name = "HeadStud";
        stud.transform.parent = head.transform;
        stud.transform.localPosition = new Vector3(0, 0.5f, 0);
        stud.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
        
        // Create left arm (cube)
        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftArm.name = "LeftArm";
        leftArm.transform.parent = character.transform;
        leftArm.transform.localPosition = new Vector3(-1f * scale, 0.2f * scale, 0);
        leftArm.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
        
        // Create right arm (cube)
        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightArm.name = "RightArm";
        rightArm.transform.parent = character.transform;
        rightArm.transform.localPosition = new Vector3(1f * scale, 0.2f * scale, 0);
        rightArm.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
        
        // Create left leg (cube)
        GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.name = "LeftLeg";
        leftLeg.transform.parent = character.transform;
        leftLeg.transform.localPosition = new Vector3(-0.5f * scale, -1.25f * scale, 0);
        leftLeg.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
        
        // Create right leg (cube)
        GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.name = "RightLeg";
        rightLeg.transform.parent = character.transform;
        rightLeg.transform.localPosition = new Vector3(0.5f * scale, -1.25f * scale, 0);
        rightLeg.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
        
        // Apply materials using the most basic shader to avoid rendering issues
        ApplyUnlitMaterial(body, "BodyMaterial", parameters.body_color);
        ApplyUnlitMaterial(leftArm, "LeftArmMaterial", parameters.body_color);
        ApplyUnlitMaterial(rightArm, "RightArmMaterial", parameters.body_color);
        ApplyUnlitMaterial(leftLeg, "LeftLegMaterial", parameters.body_color);
        ApplyUnlitMaterial(rightLeg, "RightLegMaterial", parameters.body_color);
        ApplyUnlitMaterial(head, "HeadMaterial", parameters.head_color);
        ApplyUnlitMaterial(stud, "StudMaterial", parameters.head_color);
        
        Debug.Log($"Created LEGO character: {character.name}");
        
        return new ObjectResult
        {
            success = true,
            name = character.name,
            position = new float[] { character.transform.position.x, character.transform.position.y, character.transform.position.z },
            rotation = new float[] { character.transform.eulerAngles.x, character.transform.eulerAngles.y, character.transform.eulerAngles.z },
            scale = new float[] { scale, scale, scale },
            active = character.activeSelf
        };
    }
    
    // Helper method to apply a material with the Unlit/Color shader which is the most basic shader
    private void ApplyUnlitMaterial(GameObject obj, string materialName, float[] color)
    {
        if (obj == null)
        {
            Debug.LogError($"ApplyUnlitMaterial: Object is null");
            return;
        }
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"ApplyUnlitMaterial: No renderer component found on {obj.name}");
            return;
        }
        
        // Try multiple shaders in order of simplicity
        Material newMaterial = null;
        
        // First try Unlit/Color which is the most basic
        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader != null)
        {
            newMaterial = new Material(unlitShader);
            Debug.Log($"Using Unlit/Color shader for {obj.name}");
        }
        // Fallback to Diffuse
        else
        {
            Shader diffuseShader = Shader.Find("Diffuse");
            if (diffuseShader != null)
            {
                newMaterial = new Material(diffuseShader);
                Debug.Log($"Using Diffuse shader for {obj.name}");
            }
            // Last resort: Standard (though this might cause the purple issue)
            else
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    newMaterial = new Material(standardShader);
                    Debug.Log($"Using Standard shader for {obj.name}");
                }
                else
                {
                    // Ultimate fallback: default
                    newMaterial = new Material(Shader.Find("Default-Material"));
                    Debug.Log($"Using Default-Material shader for {obj.name}");
                }
            }
        }
        
        // Set the material color
        if (color != null && color.Length >= 3)
        {
            float r = color[0];
            float g = color[1];
            float b = color[2];
            float a = color.Length >= 4 ? color[3] : 1.0f;
            
            Color materialColor = new Color(r, g, b, a);
            newMaterial.color = materialColor;
            Debug.Log($"Set {obj.name} color to ({r}, {g}, {b}, {a})");
        }
        else
        {
            newMaterial.color = Color.white;
        }
        
        // Apply the material
        renderer.material = newMaterial;
        renderer.material.name = materialName;
    }
    
    // Add this case to your ExecuteMethod switch statement
    // case "create_lego_character":
    //     return CreateLegoCharacter(paramsJson);
}
