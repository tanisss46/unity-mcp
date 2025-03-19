#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import json
import socket
import uuid

class UnityMCPClient:
    """
    Unity Model Control Protocol (MCP) client for interacting with Unity scene
    Enhanced version with support for improved characters, advanced materials and other visual improvements
    """
    
    def __init__(self, host="localhost", port=8080):
        """Initialize the Unity MCP client"""
        self.host = host
        self.port = port
        self.id_counter = 0
    
    def send_request(self, method, params=None):
        """Send a JSON-RPC request to the Unity MCP server"""
        # Create a unique ID for this request
        req_id = str(uuid.uuid4())
        self.id_counter += 1
        
        # Build the request
        request = {
            "jsonrpc": "2.0",
            "method": method,
            "params": params or {},
            "id": req_id
        }
        
        # Convert to JSON
        request_json = json.dumps(request)
        
        # Send to server
        try:
            # Create socket
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                # Connect to server
                s.connect((self.host, self.port))
                
                # Send data
                s.sendall(request_json.encode('utf-8'))
                
                # Receive response
                chunks = []
                while True:
                    chunk = s.recv(4096)
                    if not chunk:
                        break
                    chunks.append(chunk)
                
                # Combine chunks and decode
                response_data = b''.join(chunks).decode('utf-8')
                
            # Parse response
            response = json.loads(response_data)
            
            # Check for errors
            if "error" in response:
                raise Exception(f"Error: {response['error']['message']}")
            
            # Return result if it exists
            if "result" in response:
                return json.loads(response["result"])
            
            return response
            
        except Exception as e:
            print(f"Error communicating with Unity MCP server: {e}")
            return None

    # Basic Scene Management
    def get_scene_info(self):
        """Get information about the current Unity scene"""
        return self.send_request("get_scene_info")
    
    def get_object_info(self, object_name):
        """Get information about a specific GameObject"""
        params = {"object_name": object_name}
        return self.send_request("get_object_info", params)
    
    # Basic Object Creation and Manipulation
    def create_object(self, obj_type, name=None, location=None, rotation=None, scale=None):
        """Create a primitive GameObject in the scene"""
        params = {"type": obj_type}
        if name:
            params["name"] = name
        if location:
            params["location"] = location
        if rotation:
            params["rotation"] = rotation
        if scale:
            params["scale"] = scale
        return self.send_request("create_object", params)
    
    def modify_object(self, name, location=None, rotation=None, scale=None, visible=None):
        """Modify an existing GameObject's properties"""
        params = {"name": name}
        if location:
            params["location"] = location
        if rotation:
            params["rotation"] = rotation
        if scale:
            params["scale"] = scale
        if visible is not None:
            params["visible"] = visible
        return self.send_request("modify_object", params)
    
    def delete_object(self, name):
        """Delete a GameObject from the scene"""
        params = {"name": name}
        return self.send_request("delete_object", params)
    
    def set_material(self, object_name, material_name=None, color=None):
        """Set basic material properties on an object"""
        params = {"object_name": object_name}
        if material_name:
            params["material_name"] = material_name
        if color:
            params["color"] = color
        return self.send_request("set_material", params)
    
    # Advanced Object Creation
    def create_improved_character(self, character_type, position, outfit_type="Casual", 
                                 has_weapon=False, weapon_type="None", skin_color=None, 
                                 height=1.8, body_type=0.5, hair_color=None, hair_style="Short"):
        """
        Create an advanced character with detailed model and materials
        
        Args:
            character_type (str): Type of character (human, soldier)
            position (list): [x, y, z] position
            outfit_type (str): Outfit type (Casual, Formal, Military)
            has_weapon (bool): Whether the character has a weapon
            weapon_type (str): Type of weapon (None, Pistol, Rifle)
            skin_color (list): [r, g, b, a] skin color
            height (float): Character height multiplier
            body_type (float): Body type (0.0 = thin, 1.0 = muscular)
            hair_color (list): [r, g, b, a] hair color
            hair_style (str): Hair style (Short, Long)
            
        Returns:
            dict: Result information about the created character
        """
        params = {
            "characterType": character_type,
            "position": position,
            "outfitType": outfit_type,
            "hasWeapon": has_weapon,
            "weaponType": weapon_type,
            "height": height,
            "bodyType": body_type,
            "hairStyle": hair_style
        }
        
        if skin_color:
            params["skinColor"] = skin_color
        
        if hair_color:
            params["hairColor"] = hair_color
            
        return self.send_request("improved_character", params)
    
    # Environment Creation
    def create_terrain(self, width, length, height=100, heightmap=None):
        """Create a terrain in the scene"""
        params = {
            "width": width,
            "length": length,
            "height": height
        }
        if heightmap:
            params["heightmap"] = heightmap
        return self.send_request("create_terrain", params)
    
    def create_water(self, width, length, height=0):
        """Create a water surface in the scene"""
        params = {
            "width": width,
            "length": length,
            "height": height
        }
        return self.send_request("create_water", params)
    
    def create_vegetation(self, veg_type, position, scale=1.0, color=None):
        """Create vegetation (tree, bush, rock) in the scene"""
        params = {
            "type": veg_type,
            "position": position,
            "scale": scale
        }
        if color:
            params["color"] = color
        return self.send_request("create_vegetation", params)
    
    def create_skybox(self, sky_type="day", color=None):
        """Create a skybox in the scene"""
        params = {
            "type": sky_type
        }
        if color:
            params["color"] = color
        return self.send_request("create_skybox", params)
    
    # Lighting
    def create_light(self, light_type="point", position=None, intensity=1.0, color=None, 
                    range=10.0, spot_angle=30.0, shadows=True, shadow_strength=0.8):
        """
        Create a light in the scene with advanced properties
        
        Args:
            light_type (str): Type of light (directional, point, spot)
            position (list): [x, y, z] position
            intensity (float): Light intensity
            color (list): [r, g, b, a] light color
            range (float): Light range (for point and spot lights)
            spot_angle (float): Spot angle (for spot lights)
            shadows (bool): Whether the light casts shadows
            shadow_strength (float): Shadow strength
            
        Returns:
            dict: Result information about the created light
        """
        params = {
            "lightType": light_type,
            "intensity": intensity,
            "range": range,
            "spotAngle": spot_angle,
            "shadows": shadows,
            "shadowStrength": shadow_strength
        }
        
        if position:
            params["position"] = position
            
        if color:
            params["color"] = color
            
        return self.send_request("create_light", params)
    
    # Effects
    def create_particle_system(self, effect_type="fire", position=None, scale=1.0, 
                              color=None, duration=5.0, looping=True):
        """Create a particle system with various effect types"""
        params = {
            "effectType": effect_type,
            "scale": scale,
            "duration": duration,
            "looping": looping
        }
        
        if position:
            params["position"] = position
            
        if color:
            params["startColor"] = color
            
        return self.send_request("create_particle_system", params)
    
    def set_post_processing(self, effect, intensity=1.0):
        """Configure post-processing effects"""
        params = {
            "effect": effect,
            "intensity": intensity
        }
        return self.send_request("set_post_processing", params)
    
    # Physics
    def add_rigidbody(self, object_name, mass=1.0, use_gravity=True):
        """Add physics properties to an object"""
        params = {
            "object_name": object_name,
            "mass": mass,
            "use_gravity": use_gravity
        }
        return self.send_request("add_rigidbody", params)
    
    def apply_force(self, object_name, force, mode="force"):
        """Apply a force to a rigidbody object"""
        params = {
            "object_name": object_name,
            "force": force,
            "mode": mode
        }
        return self.send_request("apply_force", params)
    
    def create_joint(self, object1, object2, joint_type="fixed"):
        """Create a joint between two objects"""
        params = {
            "object1": object1,
            "object2": object2,
            "joint_type": joint_type
        }
        return self.send_request("create_joint", params)
    
    # Camera
    def create_camera(self, camera_type="main", position=None, target=None, 
                     field_of_view=60.0, background_color=None):
        """Create a camera in the scene"""
        params = {
            "camera_type": camera_type,
            "fieldOfView": field_of_view
        }
        
        if position:
            params["position"] = position
            
        if target:
            params["target"] = target
            
        if background_color:
            params["backgroundColor"] = background_color
            
        return self.send_request("create_camera", params)
    
    def set_active_camera(self, camera_name):
        """Set the active camera in the scene"""
        params = {
            "camera_name": camera_name
        }
        return self.send_request("set_active_camera", params)
    
    def set_camera_properties(self, camera_name, field_of_view=None, near_clip=None, far_clip=None):
        """Set properties of a camera"""
        params = {
            "camera_name": camera_name
        }
        
        if field_of_view:
            params["field_of_view"] = field_of_view
            
        if near_clip:
            params["near_clip"] = near_clip
            
        if far_clip:
            params["far_clip"] = far_clip
            
        return self.send_request("set_camera_properties", params)
    
    # Audio
    def play_sound(self, sound_type, position=None, volume=1.0):
        """Play a sound effect"""
        params = {
            "sound_type": sound_type,
            "volume": volume
        }
        
        if position:
            params["position"] = position
            
        return self.send_request("play_sound", params)
    
    def create_audio_source(self, object_name, audio_type, loop=False, volume=1.0, 
                           pitch=1.0, spatial_blend=0.0):
        """Add an audio source to an object"""
        params = {
            "object_name": object_name,
            "audio_type": audio_type,
            "loop": loop,
            "volume": volume,
            "pitch": pitch,
            "spatialBlend": spatial_blend
        }
        return self.send_request("create_audio_source", params)
    
    # Custom Code Execution
    def execute_unity_code(self, code):
        """Execute custom C# code in Unity"""
        params = {
            "code": code
        }
        return self.send_request("execute_unity_code", params)


# Color Helper Class
class Colors:
    """Helper class with predefined colors and color utilities"""
    
    # Basic colors
    RED = [1.0, 0.0, 0.0, 1.0]
    GREEN = [0.0, 1.0, 0.0, 1.0]
    BLUE = [0.0, 0.0, 1.0, 1.0]
    YELLOW = [1.0, 1.0, 0.0, 1.0]
    CYAN = [0.0, 1.0, 1.0, 1.0]
    MAGENTA = [1.0, 0.0, 1.0, 1.0]
    BLACK = [0.0, 0.0, 0.0, 1.0]
    WHITE = [1.0, 1.0, 1.0, 1.0]
    GRAY = [0.5, 0.5, 0.5, 1.0]
    
    # Custom colors
    ORANGE = [1.0, 0.5, 0.0, 1.0]
    PURPLE = [0.5, 0.0, 0.5, 1.0]
    PINK = [1.0, 0.75, 0.8, 1.0]
    BROWN = [0.5, 0.25, 0.0, 1.0]
    DARK_GREEN = [0.0, 0.5, 0.0, 1.0]
    NAVY_BLUE = [0.0, 0.0, 0.5, 1.0]
    
    # Skin tones
    SKIN_LIGHT = [0.99, 0.82, 0.73, 1.0]
    SKIN_MEDIUM = [0.95, 0.74, 0.6, 1.0]
    SKIN_DARK = [0.55, 0.37, 0.23, 1.0]
    
    # Material colors
    GOLD = [1.0, 0.84, 0.0, 1.0]
    SILVER = [0.75, 0.75, 0.75, 1.0]
    BRONZE = [0.8, 0.5, 0.2, 1.0]
    STEEL = [0.5, 0.5, 0.55, 1.0]
    
    @staticmethod
    def rgb(r, g, b, a=1.0):
        """
        Create a color from RGB values (0-255 range)
        
        Args:
            r (int): Red (0-255)
            g (int): Green (0-255)
            b (int): Blue (0-255)
            a (float): Alpha (0.0-1.0)
            
        Returns:
            list: [r, g, b, a] color values normalized to 0.0-1.0
        """
        return [r/255.0, g/255.0, b/255.0, a]
    
    @staticmethod
    def lerp(color1, color2, t):
        """
        Linear interpolate between two colors
        
        Args:
            color1 (list): First color [r, g, b, a]
            color2 (list): Second color [r, g, b, a]
            t (float): Interpolation factor (0.0-1.0)
            
        Returns:
            list: Interpolated color
        """
        result = []
        for i in range(min(len(color1), len(color2))):
            result.append((1-t) * color1[i] + t * color2[i])
        return result


# Character Presets
class CharacterPresets:
    """Predefined character configurations"""
    
    GUNMAN = {
        "characterType": "human",
        "outfitType": "Military",
        "hasWeapon": True,
        "weaponType": "Rifle",
        "skinColor": [0.8, 0.6, 0.5, 1.0],
        "height": 1.85,
        "bodyType": 0.7
    }
    
    CIVILIAN = {
        "characterType": "human",
        "outfitType": "Casual",
        "hasWeapon": False,
        "skinColor": [0.9, 0.75, 0.65, 1.0],
        "height": 1.75,
        "bodyType": 0.5
    }
    
    BUSINESSMAN = {
        "characterType": "human",
        "outfitType": "Formal",
        "hasWeapon": False,
        "skinColor": [0.85, 0.7, 0.6, 1.0],
        "height": 1.8,
        "bodyType": 0.6
    }
    
    SECURITY_GUARD = {
        "characterType": "human",
        "outfitType": "Formal",
        "hasWeapon": True,
        "weaponType": "Pistol",
        "skinColor": [0.7, 0.5, 0.4, 1.0],
        "height": 1.9,
        "bodyType": 0.65
    }
    
    SOLDIER = {
        "characterType": "soldier",
        "outfitType": "Military",
        "hasWeapon": True,
        "weaponType": "Rifle",
        "skinColor": [0.75, 0.6, 0.45, 1.0],
        "height": 1.85,
        "bodyType": 0.8
    }


# Material Presets
class MaterialPresets:
    """Predefined material configurations"""
    
    METAL = {
        "materialType": "Standard",
        "albedoColor": [0.8, 0.8, 0.8, 1.0],
        "smoothness": 0.8,
        "metallic": 1.0
    }
    
    PLASTIC = {
        "materialType": "Standard",
        "albedoColor": [1.0, 1.0, 1.0, 1.0],
        "smoothness": 0.7,
        "metallic": 0.0
    }
    
    WOOD = {
        "materialType": "Standard",
        "albedoColor": [0.7, 0.5, 0.3, 1.0],
        "smoothness": 0.3,
        "metallic": 0.0
    }
    
    GLASS = {
        "materialType": "Standard",
        "albedoColor": [0.9, 0.9, 0.9, 0.5],
        "smoothness": 0.95,
        "metallic": 0.0,
        "useTransparency": True
    }
    
    EMISSIVE = {
        "materialType": "Standard",
        "albedoColor": [1.0, 1.0, 1.0, 1.0],
        "smoothness": 0.5,
        "metallic": 0.0,
        "emissionColor": [1.0, 1.0, 1.0],
        "emissionIntensity": 1.5
    } 