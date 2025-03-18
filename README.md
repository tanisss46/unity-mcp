# Unity MCP Server

A Model-Context Protocol (MCP) server implementation for Unity, enabling communication and object manipulation within Unity environments through a standard interface.

## Overview

Unity MCP Server establishes a TCP-based communication protocol between Unity and external applications. It allows creation, modification, and deletion of objects in Unity scenes, as well as retrieval of scene information, all through a simple JSON-RPC based API.

## Features

- **Object Manipulation**: Create, modify, and delete 3D objects in Unity
- **Material Management**: Apply materials and colors to objects
- **Scene Information**: Retrieve detailed information about the current scene
- **JSON-RPC Interface**: Clean, standardized communication protocol
- **TCP Socket Communication**: Reliable communication between Unity and external applications

## Components

The project consists of the following key components:

1. **UnityMCPServer.cs**: Unity component that handles TCP connections and executes commands
2. **unity_mcp_server.py**: Python server implementing the MCP protocol
3. **unity_client.py**: Python client for communicating with the Unity server
4. **config.py**: Configuration settings for the MCP server

## Installation

### Prerequisites

- Unity 2019.1 or newer
- Python 3.8 or newer
- Required Python packages: `asyncio`, `fastapi`, `asgi_tools`

### Setup

1. **Unity Setup**:
   - Add the `UnityMCPServer.cs` script to a GameObject in your scene
   - Configure the port (default: 8080) in the Inspector if needed

2. **Python Setup**:
   ```bash
   # Create a virtual environment (optional but recommended)
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   
   # Install required packages
   pip install asyncio fastapi asgi_tools
   ```

## Usage

### Starting the Server

1. **Start Unity**: Open your Unity project with the UnityMCPServer component
2. **Start the MCP Server**:
   ```bash
   python -m unity_mcp_server
   ```

### Commands and API Reference

The MCP server supports the following commands:

#### get_scene_info

Retrieves detailed information about the current Unity scene.

```python
result = await unity_client.send_command("get_scene_info", {})
```

#### get_object_info

Gets properties of a specific object.

```python
result = await unity_client.send_command("get_object_info", {
    "object_name": "Cube1"
})
```

#### create_object

Creates a new object in the Unity scene.

```python
result = await unity_client.send_command("create_object", {
    "type": "CUBE",  # CUBE, SPHERE, CYLINDER, PLANE, EMPTY, CAPSULE, QUAD
    "name": "MyCube",
    "location": [0, 1, 0],
    "rotation": [0, 45, 0],
    "scale": [1, 1, 1]
})
```

#### modify_object

Modifies an existing object's properties.

```python
result = await unity_client.send_command("modify_object", {
    "name": "MyCube",
    "location": [0, 2, 0],
    "rotation": [0, 90, 0],
    "scale": [2, 2, 2],
    "visible": True
})
```

#### delete_object

Deletes an object from the scene.

```python
result = await unity_client.send_command("delete_object", {
    "name": "MyCube"
})
```

#### set_material

Assigns a material and/or color to an object.

```python
result = await unity_client.send_command("set_material", {
    "object_name": "MyCube",
    "material_name": "MyMaterial",  # Optional
    "color": [1.0, 0.0, 0.0]  # RGB values (0-1)
})
```

#### execute_unity_code

Executes custom C# code in Unity (limited functionality for security reasons).

```python
result = await unity_client.send_command("execute_unity_code", {
    "code": "Debug.Log(\"Hello from Python!\");"
})
```

## Example

Here's a complete example of creating and manipulating objects:

```python
import asyncio
from unity_client import UnityClient

async def main():
    # Connect to Unity
    client = UnityClient(host="localhost", port=8080)
    await client.connect()
    
    try:
        # Get scene info
        scene_info = await client.send_command("get_scene_info", {})
        print(f"Scene name: {scene_info['name']}")
        
        # Create a cube
        cube = await client.send_command("create_object", {
            "type": "CUBE",
            "name": "PythonCube",
            "location": [0, 1, 0]
        })
        print(f"Created cube: {cube['name']}")
        
        # Apply a red material
        await client.send_command("set_material", {
            "object_name": "PythonCube",
            "color": [1.0, 0.0, 0.0]
        })
        
        # Move the cube
        await client.send_command("modify_object", {
            "name": "PythonCube",
            "location": [0, 2, 0],
            "rotation": [0, 45, 0]
        })
        
        # Get object info
        object_info = await client.send_command("get_object_info", {
            "object_name": "PythonCube"
        })
        print(f"Object position: {object_info['position']}")
        
    finally:
        # Disconnect
        await client.disconnect()

if __name__ == "__main__":
    asyncio.run(main())
```

## Architecture

The system uses a client-server architecture:

1. **Unity (TCP Server)**: 
   - Listens for incoming connections
   - Processes JSON-RPC commands
   - Executes operations in the Unity environment

2. **Python MCP Server (Middleware)**:
   - Implements the MCP protocol
   - Routes commands to the Unity server
   - Handles command validation and error handling

3. **Python Client**:
   - Provides a simple API for applications to communicate with Unity
   - Handles connection management and command formatting

## JSON-RPC Protocol

All communication uses the JSON-RPC 2.0 protocol. Requests take the form:

```json
{
    "jsonrpc": "2.0",
    "method": "create_object",
    "params": {
        "type": "CUBE",
        "name": "MyCube"
    },
    "id": "1"
}
```

Responses take the form:

```json
{
    "jsonrpc": "2.0",
    "result": {
        "success": true,
        "name": "MyCube",
        "position": [0, 1, 0],
        "rotation": [0, 0, 0],
        "scale": [1, 1, 1],
        "active": true
    },
    "id": "1"
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- Unity Technologies for providing the Unity game engine
- The MCP protocol specification
