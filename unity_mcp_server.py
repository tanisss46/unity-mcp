import asyncio
import json
import logging
from typing import Dict, Any, List

from mcp.server import Server
import mcp.types as types
from mcp.server.stdio import stdio_server

from unity_client import UnityClient
from config import UNITY_HOST, UNITY_PORT, MCP_SERVER_NAME, UNITY_COMMANDS, UNITY_OBJECT_TYPES

# Configure logging
logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Create Unity client
unity = UnityClient(host=UNITY_HOST, port=UNITY_PORT)

# MCP Server
app = Server(MCP_SERVER_NAME)

@app.list_tools()
async def list_tools() -> List[types.Tool]:
    """Return the list of all available MCP tools."""
    tools = []
    
    # Iterate through all commands in the config
    for command_name, command_info in UNITY_COMMANDS.items():
        # Create input schema
        input_schema = {"type": "object", "properties": {}}
        required_params = []
        
        # Add parameters to schema if they exist
        if "params" in command_info:
            for param_name, param_info in command_info["params"].items():
                # Handle special cases for types
                if param_info["type"] == "array":
                    if "minItems" in param_info and "maxItems" in param_info:
                        input_schema["properties"][param_name] = {
                            "type": "array",
                            "items": {"type": "number"},
                            "minItems": param_info.get("minItems", 0),
                            "maxItems": param_info.get("maxItems", 0)
                        }
                    else:
                        input_schema["properties"][param_name] = {
                            "type": "array",
                            "items": {"type": "number"}
                        }
                elif param_info["type"] == "string" and param_name == "type" and command_name == "create_object":
                    # Special case for create_object type enum
                    input_schema["properties"][param_name] = {
                        "type": "string",
                        "enum": UNITY_OBJECT_TYPES
                    }
                else:
                    input_schema["properties"][param_name] = {"type": param_info["type"]}
                
                # Add to required parameters if essential
                if param_name in ["type", "object_name", "name", "code"] and command_name != "get_scene_info":
                    required_params.append(param_name)
        
        # Add required parameters if any
        if required_params:
            input_schema["required"] = required_params
        
        # Create and add the tool
        tools.append(types.Tool(
            name=command_name,
            description=command_info["description"],
            inputSchema=input_schema
        ))
    
    return tools

@app.call_tool()
async def call_tool(name: str, arguments: Dict[str, Any]) -> List[types.TextContent]:
    """Call the MCP tool and send the corresponding command to the Unity server."""
    try:
        logger.debug(f"Tool being called: {name}, parameters: {arguments}")
        
        # Some tools (like get_scene_info) don't require parameters
        # So we skip parameter validation for get_scene_info
        if name != "get_scene_info" and name in UNITY_COMMANDS:
            # Check if there are required parameters
            if "params" in UNITY_COMMANDS[name]:
                required_params = [param for param, info in UNITY_COMMANDS[name]["params"].items() 
                                if param in ["type", "object_name", "name", "code"]]
                
                # Check if parameters are null or empty
                if not arguments and required_params:
                    logger.error(f"Parameters for {name} are empty or null")
                    raise ValueError(f"Parameters for {name} are empty or null")
                
                # Check required parameters
                for param in required_params:
                    if param not in arguments or not arguments[param]:
                        logger.error(f"Missing or empty '{param}' parameter for {name}")
                        raise ValueError(f"Missing or empty '{param}' parameter for {name}")
                
                # Check array parameters
                for param, info in UNITY_COMMANDS[name]["params"].items():
                    if info["type"] == "array" and param in arguments and arguments[param] is not None:
                        expected_length = 3  # Default for position, rotation, scale
                        if param == "color":
                            expected_length = 3 if len(arguments[param]) < 4 else 4  # RGB or RGBA
                        
                        if not isinstance(arguments[param], list) or len(arguments[param]) not in [expected_length, expected_length + 1]:
                            logger.error(f"'{param}' parameter for {name} should be an array with {expected_length} or {expected_length+1} elements: {arguments[param]}")
                            raise ValueError(f"'{param}' parameter should be an array with appropriate elements")
        
        # If arguments is None, create an empty dictionary
        if arguments is None:
            arguments = {}
        
        # Call the Unity command (asynchronous call)
        result = await unity.send_command(name, arguments)
        
        # Log and return the result
        logger.info(f"Response from Unity: {result}")
        result_text = json.dumps(result, indent=2)
        return [types.TextContent(type="text", text=result_text)]
    except Exception as e:
        error_message = f"Error calling tool {name}: {str(e)}"
        logger.error(error_message)
        return [types.TextContent(type="text", text=error_message)]

@app.list_resources()
async def list_resources() -> List[types.Resource]:
    """Return the list of all resources supported by the MCP server."""
    return [
        types.Resource(uri="scene://current", name="Current Scene", description="Active Unity scene information"),
        types.Resource(uriTemplate="object://{name}", name="Object Information", description="Information about a specific object in the scene"),
        types.Resource(uriTemplate="material://{name}", name="Material Information", description="Information about a specific material")
    ]

@app.read_resource()
async def read_resource(uri: str) -> types.ReadResourceResult:
    """Read the requested resource."""
    try:
        logger.info(f"Reading resource: {uri}")
        
        if uri == "scene://current":
            scene_info = await unity.send_command("get_scene_info", {})
            logger.info(f"get_scene_info result: {scene_info}")
            return types.ReadResourceResult(
                contents=[types.ResourceContent(uri=uri, text=json.dumps(scene_info, indent=2), mimeType="application/json")]
            )
        elif uri.startswith("object://"):
            object_name = uri.replace("object://", "")
            object_info = await unity.send_command("get_object_info", {"object_name": object_name})
            logger.info(f"get_object_info result: {object_info}")
            return types.ReadResourceResult(
                contents=[types.ResourceContent(uri=uri, text=json.dumps(object_info, indent=2), mimeType="application/json")]
            )
        elif uri.startswith("material://"):
            material_name = uri.replace("material://", "")
            return types.ReadResourceResult(
                contents=[types.ResourceContent(uri=uri, text=json.dumps({"name": material_name, "info": "Material details not implemented yet"}, indent=2), mimeType="application/json")]
            )
        
        raise Exception(f"Unknown URI: {uri}")
    except Exception as e:
        error_message = f"Error reading resource ({uri}): {str(e)}"
        logger.error(error_message)
        raise Exception(error_message)

async def main():
    """Main program."""
    try:
        # Establish the Unity connection (asynchronous call)
        await unity.connect()
        
        # Start the MCP server using stdio
        logger.info("Starting MCP server...")
        async with stdio_server() as streams:
            await app.run(
                streams[0],
                streams[1], 
                app.create_initialization_options()
            )
    except Exception as e:
        logger.error(f"Main program error: {e}")
    finally:
        # Close the Unity connection (asynchronous call)
        await unity.disconnect()

if __name__ == "__main__":
    asyncio.run(main())