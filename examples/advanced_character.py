#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Unity MCP Example - Advanced Character Creation
"""
from unity_mcp_client import UnityMCPClient, Colors, CharacterPresets

def main():
    # Create client
    client = UnityMCPClient(host="localhost", port=8080)
    print("Connected to Unity MCP server")
    
    # Create a scene with characters
    print("Creating a scene with advanced characters...")
    
    # Ground plane
    ground = client.create_object("PLANE", "Ground", [0, 0, 0], [0, 0, 0], [20, 1, 20])
    client.set_material("Ground", color=Colors.rgb(100, 100, 100))
    
    # Create lighting
    main_light = client.create_light("directional", [2, 10, 2], 1.0, Colors.rgb(255, 240, 230))
    
    # Create characters using presets
    
    # 1. Security guard
    guard_preset = CharacterPresets.SECURITY_GUARD.copy()
    guard_preset["position"] = [-3, 0, 0]
    guard = client.create_improved_character(**guard_preset)
    
    # 2. Businessman
    business_preset = CharacterPresets.BUSINESSMAN.copy()
    business_preset["position"] = [0, 0, 0]
    business_preset["height"] = 1.75
    business = client.create_improved_character(**business_preset)
    
    # 3. Create diverse civilians
    for i in range(5):
        position = [i * 2 - 4, 0, 3]
        
        civilian_preset = CharacterPresets.CIVILIAN.copy()
        civilian_preset["position"] = position
        
        # Vary skin tones
        if i % 3 == 0:
            civilian_preset["skinColor"] = Colors.SKIN_LIGHT
        elif i % 3 == 1:
            civilian_preset["skinColor"] = Colors.SKIN_MEDIUM
        else:
            civilian_preset["skinColor"] = Colors.SKIN_DARK
            
        # Vary body types
        civilian_preset["height"] = 1.6 + (i * 0.1)
        civilian_preset["bodyType"] = 0.3 + (i * 0.15)
        
        civilian = client.create_improved_character(**civilian_preset)
    
    # 4. Create a soldier with a weapon
    soldier_preset = CharacterPresets.SOLDIER.copy()
    soldier_preset["position"] = [-5, 0, 5]
    soldier_preset["weaponType"] = "Rifle"
    soldier = client.create_improved_character(**soldier_preset)
    
    # 5. Custom character with specific parameters
    custom = client.create_improved_character(
        character_type="human",
        position=[5, 0, 5],
        outfit_type="Military",
        has_weapon=True,
        weapon_type="Pistol",
        skin_color=Colors.rgb(120, 80, 70, 1.0),
        height=2.0,
        body_type=0.9,
        hair_color=Colors.rgb(20, 20, 20, 1.0),
        hair_style="Long"
    )
    
    # Set up camera
    client.create_camera("MainCamera", [0, 10, -15], field_of_view=60)
    client.set_active_camera("MainCamera")
    
    print("Character scene created successfully! Check Unity to see the results.")
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