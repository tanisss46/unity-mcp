#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Unity MCP Example - Basic Scene Creation
"""
from unity_mcp_client import UnityMCPClient, Colors

def main():
    # Create client
    client = UnityMCPClient(host="localhost", port=8080)
    print("Connected to Unity MCP server")
    
    # Create a simple scene with primitives
    print("Creating a simple scene...")
    
    # Ground plane
    ground = client.create_object("PLANE", "Ground", [0, 0, 0], [0, 0, 0], [10, 1, 10])
    client.set_material("Ground", color=Colors.rgb(50, 50, 50))
    
    # Create some objects
    cube = client.create_object("CUBE", "Cube", [0, 1, 0], [0, 45, 0], [1, 1, 1])
    client.set_material("Cube", color=Colors.BLUE)
    
    sphere = client.create_object("SPHERE", "Sphere", [2, 1, 2], [0, 0, 0], [1, 1, 1])
    client.set_material("Sphere", color=Colors.RED)
    
    cylinder = client.create_object("CYLINDER", "Cylinder", [-2, 1, 2], [0, 0, 0], [1, 1, 1])
    client.set_material("Cylinder", color=Colors.GREEN)
    
    # Add a light
    light = client.create_light("directional", [1, 3, 1], 1.0, Colors.WHITE)
    
    # Add some physics
    client.add_rigidbody("Sphere", mass=1.0)
    client.add_rigidbody("Cube", mass=2.0)
    
    # Set up a camera
    client.create_camera("MainCamera", [0, 5, -7], field_of_view=60)
    client.set_active_camera("MainCamera")
    
    print("Scene created successfully! Check Unity to see the results.")
    print("Press Ctrl+C to exit")
    
    # Keep the application running to maintain connection
    try:
        import time
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nExiting...")

if __name__ == "__main__":
    main() 