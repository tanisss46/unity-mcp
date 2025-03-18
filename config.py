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
    },
    
    # New environment creation tools
    "create_terrain": {
        "description": "Create a terrain in the Unity scene",
        "params": {
            "width": {"type": "number", "description": "Width of the terrain"},
            "length": {"type": "number", "description": "Length of the terrain"},
            "height": {"type": "number", "description": "Maximum height of the terrain"},
            "heightmap": {"type": "array", "description": "Optional 2D array of height values"}
        }
    },
    "create_water": {
        "description": "Create a water surface in the Unity scene",
        "params": {
            "width": {"type": "number", "description": "Width of the water surface"},
            "length": {"type": "number", "description": "Length of the water surface"},
            "height": {"type": "number", "description": "Height position of the water surface"}
        }
    },
    "create_vegetation": {
        "description": "Create vegetation (trees, bushes, rocks) in the Unity scene",
        "params": {
            "type": {"type": "string", "description": "Type of vegetation (tree, bush, rock)"},
            "position": {"type": "array", "description": "Position [x, y, z] of the vegetation"},
            "scale": {"type": "number", "description": "Scale factor for the vegetation"}
        }
    },
    "create_skybox": {
        "description": "Set the skybox for the Unity scene",
        "params": {
            "type": {"type": "string", "description": "Type of skybox (day, night, sunset, space)"},
            "color": {"type": "array", "description": "Optional base color for the skybox [r, g, b]"}
        }
    },
    
    # Character and animation tools
    "create_character": {
        "description": "Create a character in the Unity scene",
        "params": {
            "characterType": {"type": "string", "description": "Type of character (human, robot)"},
            "position": {"type": "array", "description": "Position [x, y, z] where the character will be placed"}
        }
    },
    "set_animation": {
        "description": "Set animation for a character",
        "params": {
            "object_name": {"type": "string", "description": "Name of the character"},
            "animation": {"type": "string", "description": "Animation to play (idle, walk, run, jump)"}
        }
    },
    "set_character_controller": {
        "description": "Configure a character controller",
        "params": {
            "object_name": {"type": "string", "description": "Name of the character"},
            "speed": {"type": "number", "description": "Movement speed"},
            "jump_height": {"type": "number", "description": "Jump height"}
        }
    },
    
    # Vehicle tools
    "create_vehicle": {
        "description": "Create a vehicle in the Unity scene",
        "params": {
            "vehicleType": {"type": "string", "description": "Type of vehicle (car, airplane)"},
            "position": {"type": "array", "description": "Position [x, y, z] where the vehicle will be placed"}
        }
    },
    "set_vehicle_properties": {
        "description": "Configure vehicle properties",
        "params": {
            "object_name": {"type": "string", "description": "Name of the vehicle"},
            "top_speed": {"type": "number", "description": "Maximum speed"},
            "acceleration": {"type": "number", "description": "Acceleration rate"}
        }
    },
    
    # Lighting and effects tools
    "create_light": {
        "description": "Create a light in the Unity scene",
        "params": {
            "lightType": {"type": "string", "description": "Type of light (directional, point, spot)"},
            "position": {"type": "array", "description": "Position [x, y, z] of the light"},
            "intensity": {"type": "number", "description": "Light intensity"},
            "color": {"type": "array", "description": "Light color in RGB format [r, g, b]"}
        }
    },
    "create_particle_system": {
        "description": "Create a particle system for visual effects",
        "params": {
            "effectType": {"type": "string", "description": "Type of effect (fire, smoke, water, explosion)"},
            "position": {"type": "array", "description": "Position [x, y, z] of the effect"},
            "scale": {"type": "number", "description": "Scale of the effect"}
        }
    },
    "set_post_processing": {
        "description": "Configure post-processing effects",
        "params": {
            "effect": {"type": "string", "description": "Effect to configure (bloom, ambient_occlusion, depth_of_field)"},
            "intensity": {"type": "number", "description": "Effect intensity"}
        }
    },
    
    # Physics tools
    "add_rigidbody": {
        "description": "Add physics properties to an object",
        "params": {
            "object_name": {"type": "string", "description": "Name of the object"},
            "mass": {"type": "number", "description": "Mass of the object"},
            "use_gravity": {"type": "boolean", "description": "Whether gravity affects the object"}
        }
    },
    "apply_force": {
        "description": "Apply a force to a rigidbody object",
        "params": {
            "object_name": {"type": "string", "description": "Name of the object"},
            "force": {"type": "array", "description": "Force vector [x, y, z]"},
            "mode": {"type": "string", "description": "Force mode (force, impulse)"}
        }
    },
    "create_joint": {
        "description": "Create a joint between two objects",
        "params": {
            "object1": {"type": "string", "description": "First object name"},
            "object2": {"type": "string", "description": "Second object name"},
            "joint_type": {"type": "string", "description": "Type of joint (fixed, hinge, spring)"}
        }
    },
    
    # Camera tools
    "create_camera": {
        "description": "Create a new camera in the scene",
        "params": {
            "camera_type": {"type": "string", "description": "Type of camera (main, first_person, third_person)"},
            "position": {"type": "array", "description": "Position [x, y, z] of the camera"},
            "target": {"type": "string", "description": "Optional name of the object to look at"}
        }
    },
    "set_active_camera": {
        "description": "Set the active camera",
        "params": {
            "camera_name": {"type": "string", "description": "Name of the camera to activate"}
        }
    },
    "set_camera_properties": {
        "description": "Configure camera properties",
        "params": {
            "camera_name": {"type": "string", "description": "Name of the camera"},
            "field_of_view": {"type": "number", "description": "Field of view in degrees"},
            "near_clip": {"type": "number", "description": "Near clip plane distance"},
            "far_clip": {"type": "number", "description": "Far clip plane distance"}
        }
    },
    
    # Audio tools
    "play_sound": {
        "description": "Play a sound effect",
        "params": {
            "sound_type": {"type": "string", "description": "Type of sound (explosion, footstep, engine, ambient)"},
            "position": {"type": "array", "description": "Optional position [x, y, z] for spatial audio"},
            "volume": {"type": "number", "description": "Volume level (0.0 to 1.0)"}
        }
    },
    "create_audio_source": {
        "description": "Add an audio source to an object",
        "params": {
            "object_name": {"type": "string", "description": "Name of the object"},
            "audio_type": {"type": "string", "description": "Type of audio (ambient, effect, music)"},
            "loop": {"type": "boolean", "description": "Whether the audio should loop"},
            "volume": {"type": "number", "description": "Volume level (0.0 to 1.0)"}
        }
    }
}