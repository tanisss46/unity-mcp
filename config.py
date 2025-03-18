# config.py
import os
from typing import Dict, Any

# Unity connection settings
UNITY_HOST = os.environ.get("UNITY_HOST", "localhost")
UNITY_PORT = int(os.environ.get("UNITY_PORT", "8080"))

# MCP server settings
MCP_SERVER_NAME = "unity-mcp-server"
MCP_SERVER_VERSION = "1.0.0"

# Unity supported object types
UNITY_OBJECT_TYPES = ["CUBE", "SPHERE", "CYLINDER", "PLANE", "EMPTY", "CAPSULE", "QUAD"]

# Unity supported commands
UNITY_COMMANDS = {
    "get_scene_info": {
        "description": "Get details of the current Unity scene"
    },
    "get_object_info": {
        "description": "Get properties of a specific object",
        "params": {
            "object_name": {"type": "string", "description": "Name of the object to get information about"}
        }
    },
    "create_object": {
        "description": "Create a new object in the Unity scene",
        "params": {
            "type": {"type": "string", "description": "Type of object to create (CUBE, SPHERE, etc.)"},
            "name": {"type": "string", "description": "Name of the object to create"},
            "location": {"type": "array", "description": "Position of the object [x, y, z]"},
            "rotation": {"type": "array", "description": "Rotation of the object [x, y, z]"},
            "scale": {"type": "array", "description": "Scale of the object [x, y, z]"}
        }
    },
    "modify_object": {
        "description": "Modify properties of an existing object",
        "params": {
            "name": {"type": "string", "description": "Name of the object to modify"},
            "location": {"type": "array", "description": "New position [x, y, z]"},
            "rotation": {"type": "array", "description": "New rotation [x, y, z]"},
            "scale": {"type": "array", "description": "New scale [x, y, z]"},
            "visible": {"type": "boolean", "description": "Visibility of the object"}
        }
    },
    "delete_object": {
        "description": "Delete an object from the scene",
        "params": {
            "name": {"type": "string", "description": "Name of the object to delete"}
        }
    },
    "set_material": {
        "description": "Assign a material to an object",
        "params": {
            "object_name": {"type": "string", "description": "Name of the object to assign material to"},
            "material_name": {"type": "string", "description": "Name of the material (optional)"},
            "color": {"type": "array", "description": "RGB or RGBA color values"}
        }
    },
    "execute_unity_code": {
        "description": "Execute custom C# code in Unity",
        "params": {
            "code": {"type": "string", "description": "C# code to execute"}
        }
    }
}