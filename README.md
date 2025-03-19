# Unity MCP (Model Control Protocol)

A simple yet powerful protocol for controlling Unity scenes through a TCP connection. This open-source project enables external applications to create, manipulate, and control Unity objects programmatically.

## Features

- Create and manipulate 3D objects in Unity scenes
- Apply materials and configure lighting
- Create detailed characters with customization options
- Add particle effects and physics properties
- Control cameras and audio
- Execute custom C# code in Unity
- Simple JSON-RPC communication protocol

## Components

1. **UnityMCPServer.cs** - Unity component that creates a TCP server and handles commands
2. **unity_mcp_client.py** - Python client for communicating with the Unity server
3. **Examples** - Demo scenes showing different features and capabilities

## Detailed Setup Guide

### Unity Setup

1. Create a new Unity project or open your existing project
2. Copy the `unitymcpserver.cs` file into your project's Assets folder
   - You can create a new folder like `Assets/Scripts/` to keep things organized
3. In your Unity scene, create an empty GameObject
   - GameObject → Create Empty
   - Rename it to "MCPServer"
4. Attach the `UnityMCPServer` component to the GameObject
   - Select the GameObject in the Hierarchy
   - Click "Add Component" in the Inspector
   - Type "UnityMCPServer" and select it
5. Configure settings in the Inspector (optional)
   - Port: Default is 8080, change if needed
6. Make sure this GameObject is included in all scenes where you want to use MCP
   - Consider making it a Prefab for easy reuse
   - You can also use DontDestroyOnLoad to keep it persistent between scenes

### Important: Starting Unity Before Using MCP

The UnityMCPServer only accepts connections when Unity is in Play mode:

1. Open your Unity project with the UnityMCPServer component attached
2. Press the Play button to enter Play mode
3. Now the server is active and accepting connections
4. **Keep Unity open and in Play mode** while using Claude or your Python client

### Python Client Setup

1. Ensure you have Python 3.6+ installed
2. Copy the `unity_mcp_client.py` file to your project
3. Install required packages:
   ```bash
   pip install uuid
   ```
4. For a proper installation, you can use the included setup.py:
   ```bash
   pip install -e .
   ```
   
   > **Note:** Before publishing to PyPI or sharing, be sure to update the URL in setup.py to your actual repository URL.

## Connecting with Claude or Other AI Tools

When working with Claude or other AI assistants that use the Unity MCP:

1. Start your Unity project and enter Play mode first
2. Make sure the "MCPServer" GameObject is in your scene
3. Keep Unity running while interacting with the AI
4. The AI will connect to Unity through the MCP protocol
5. If the connection fails, check that:
   - Unity is in Play mode
   - The MCPServer component is active
   - The port settings match (default: 8080)
   - No firewall is blocking the connection

## Usage

### Using the Python Client

```python
from unity_mcp_client import UnityMCPClient, Colors

# Create client connection
client = UnityMCPClient(host="localhost", port=8080)

# Create a simple cube
cube = client.create_object("CUBE", "MyCube", [0, 1, 0])

# Apply a material
client.set_material("MyCube", color=Colors.BLUE)

# Add physics
client.add_rigidbody("MyCube", mass=2.0)

# Create a light
client.create_light("point", [5, 5, 5], intensity=2.0, color=Colors.rgb(255, 220, 150))

# Create a character
client.create_improved_character("human", [0, 0, 3], "Formal")
```

### Full Scene Creation Example

Here's how to create a complete scene:

```python
from unity_mcp_client import UnityMCPClient, Colors, CharacterPresets

# Create client
client = UnityMCPClient()

# Create environment
floor = client.create_object("PLANE", "Floor", [0, 0, 0], [0, 0, 0], [20, 1, 20])
client.set_material("Floor", "FloorMaterial", Colors.rgb(30, 30, 30))

# Create walls
wall1 = client.create_object("CUBE", "Wall1", [0, 2, 10], [0, 0, 0], [20, 4, 0.5])
client.set_material("Wall1", color=Colors.rgb(200, 200, 200))

wall2 = client.create_object("CUBE", "Wall2", [0, 2, -10], [0, 0, 0], [20, 4, 0.5])
client.set_material("Wall2", color=Colors.rgb(200, 200, 200))

# Lighting
main_light = client.create_light("directional", [5, 10, 5], 1.0, Colors.rgb(255, 245, 230))
accent_light = client.create_light("point", [-5, 2, 5], 2.0, Colors.rgb(255, 150, 100))

# Create characters
character = client.create_improved_character("human", [0, 0, 0], "Casual")

# Set up camera
camera = client.create_camera("mainCamera", [0, 5, -10], None, 60)
client.set_active_camera("mainCamera")
```

### Running the Included Examples

We've included ready-to-use examples in the `examples/` directory:

1. Make sure Unity is running with the UnityMCPServer active
2. Run an example script:
   ```bash
   python examples/basic_scene.py
   ```
3. Watch as the scene is created in real-time in Unity

## Troubleshooting

### Unity Side

- **Server not starting**: Make sure the UnityMCPServer component is attached to an active GameObject
- **Connection refused**: Check if Unity is in Play mode
- **Objects not appearing**: Check your scene's camera position, objects might be created out of view

### Python Side

- **Connection error**: Make sure Unity is running and in Play mode
- **Name conflicts**: If creating objects fails, the name might already be taken. Use unique names
- **Parameter errors**: Check the parameter types and formats (arrays for positions, etc.)

## API Reference

### Basic Objects

- `create_object(obj_type, name, location, rotation, scale)` - Create a 3D primitive
- `modify_object(name, location, rotation, scale, visible)` - Update object properties
- `delete_object(name)` - Remove an object from the scene
- `set_material(object_name, material_name, color)` - Apply a material to an object

### Characters

- `create_improved_character(character_type, position, outfit_type, has_weapon, weapon_type, ...)` - Create a detailed character

### Environment

- `create_terrain(width, length, height, heightmap)` - Create a terrain
- `create_water(width, length, height)` - Create a water surface
- `create_skybox(sky_type, color)` - Create a skybox

### Lighting & Effects

- `create_light(light_type, position, intensity, color, ...)` - Create a light
- `create_particle_system(effect_type, position, scale, ...)` - Create particle effects

### Physics

- `add_rigidbody(object_name, mass, use_gravity)` - Add physics to an object
- `apply_force(object_name, force, mode)` - Apply physics forces

### Camera & Audio

- `create_camera(camera_type, position, target, field_of_view, ...)` - Create a camera
- `set_active_camera(camera_name)` - Set the active camera
- `play_sound(sound_type, position, volume)` - Play a sound effect
- `create_audio_source(object_name, audio_type, loop, volume, ...)` - Add an audio source

### Utilities

- `get_scene_info()` - Get information about the current scene
- `get_object_info(object_name)` - Get information about a specific object
- `execute_unity_code(code)` - Execute custom C# code in Unity

## Helper Classes

### Colors

Predefined colors and utilities for working with colors:

```python
# Using predefined colors
Colors.RED    # [1.0, 0.0, 0.0, 1.0]
Colors.BLUE   # [0.0, 0.0, 1.0, 1.0]
Colors.GREEN  # [0.0, 1.0, 0.0, 1.0]

# Using RGB (0-255 range)
Colors.rgb(255, 100, 50)  # Converts to [1.0, 0.39, 0.19, 1.0]

# Interpolating between colors
Colors.lerp(Colors.RED, Colors.BLUE, 0.5)  # Purple: [0.5, 0.0, 0.5, 1.0]
```

### Character Presets

Predefined character configurations:

```python
# Using a preset
gunman = CharacterPresets.GUNMAN.copy()
gunman["position"] = [0, 0, 5]
client.create_improved_character(**gunman)
```

### Material Presets

Predefined material configurations:

```python
# Using a material preset 
metal = MaterialPresets.METAL.copy()
metal["albedoColor"] = Colors.GOLD
# Use with advanced material API
```

## Using with LLMs (Like Claude)

When using UnityMCP with Large Language Models (LLMs) like Claude:

1. Start Unity and make sure the UnityMCPServer is active (in Play mode)
2. In your conversation with the LLM, explain that Unity is running with the MCP server
3. The LLM can generate Python code using the unity_mcp_client to create scenes
4. Execute the generated code to see the results in Unity
5. Share feedback with the LLM to refine the scene

Example workflow:
1. Ask Claude: "Create a simple village scene in Unity"
2. Claude generates Python code using unity_mcp_client
3. Run the code while Unity is in Play mode
4. Tell Claude: "The houses are too large, make them smaller"
5. Claude generates updated code that modifies the scene

## Screenshots and Documentation

> **Note to Repo Owner:** Consider adding screenshots of examples created with UnityMCP to make the README more visual. You can add images to the `images/` directory and reference them in this README with Markdown syntax: `![Description](images/screenshot.png)`

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
