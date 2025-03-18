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
    
    tools.append(types.Tool(
        name="get_scene_info",
        description=UNITY_COMMANDS["get_scene_info"]["description"],
        inputSchema={"type": "object", "properties": {}}
    ))
    
    tools.append(types.Tool(
        name="get_object_info",
        description=UNITY_COMMANDS["get_object_info"]["description"],
        inputSchema={
            "type": "object",
            "properties": {"object_name": {"type": "string"}},
            "required": ["object_name"]
        }
    ))
    
    tools.append(types.Tool(
        name="create_object",
        description=UNITY_COMMANDS["create_object"]["description"],
        inputSchema={
            "type": "object",
            "properties": {
                "type": {"type": "string", "enum": UNITY_OBJECT_TYPES},
                "name": {"type": "string"},
                "location": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3},
                "rotation": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3},
                "scale": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3}
            },
            "required": ["type"]
        }
    ))
    
    tools.append(types.Tool(
        name="modify_object",
        description=UNITY_COMMANDS["modify_object"]["description"],
        inputSchema={
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "location": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3},
                "rotation": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3},
                "scale": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 3},
                "visible": {"type": "boolean"}
            },
            "required": ["name"]
        }
    ))
    
    tools.append(types.Tool(
        name="delete_object",
        description=UNITY_COMMANDS["delete_object"]["description"],
        inputSchema={
            "type": "object",
            "properties": {"name": {"type": "string"}},
            "required": ["name"]
        }
    ))
    
    tools.append(types.Tool(
        name="set_material",
        description=UNITY_COMMANDS["set_material"]["description"],
        inputSchema={
            "type": "object",
            "properties": {
                "object_name": {"type": "string"},
                "material_name": {"type": "string"},
                "color": {"type": "array", "items": {"type": "number"}, "minItems": 3, "maxItems": 4}
            },
            "required": ["object_name"]
        }
    ))
    
    tools.append(types.Tool(
        name="execute_unity_code",
        description=UNITY_COMMANDS["execute_unity_code"]["description"],
        inputSchema={
            "type": "object",
            "properties": {"code": {"type": "string"}},
            "required": ["code"]
        }
    ))
    
    return tools

@app.call_tool()
async def call_tool(name: str, arguments: Dict[str, Any]) -> List[types.TextContent]:
    """Call the MCP tool and send the corresponding command to the Unity server."""
    try:
        logger.debug(f"Tool being called: {name}, parameters: {arguments}")
        
        # Some tools (like get_scene_info) don't require parameters
        # So we skip parameter validation for get_scene_info
        if name != "get_scene_info":
            # Check if parameters are null or empty
            if arguments is None or not arguments:
                logger.error(f"Parameters for {name} are empty or null")
                raise ValueError(f"Parameters for {name} are empty or null")
        
        # If arguments is None, create an empty dictionary
        if arguments is None:
            arguments = {}
        
        # Convert string format to JSON (e.g., "create_object type=CUBE name=TestCube...")
        if isinstance(arguments, str):
            logger.warning(f"String parameter detected, converting to JSON: {arguments}")
            try:
                # Simple string parser (e.g., convert key=value format to JSON)
                params = {}
                parts = arguments.split()
                method = parts[0]
                for part in parts[1:]:
                    if '=' in part:
                        key, value = part.split('=', 1)
                        if value.startswith('[') and value.endswith(']'):
                            # Convert list format (e.g., [0,1,0]) to JSON list
                            value = eval(value)  # Use with caution, consider using an alternative parser
                        params[key] = value
                arguments = {"method": method, "params": params}
            except Exception as e:
                logger.error(f"Error converting string to JSON: {e}")
                raise ValueError(f"Error converting string to JSON: {e}")

        # Special validation for create_object
        if name == "create_object":
            if "type" not in arguments or not arguments["type"]:
                logger.error("Missing or empty 'type' parameter for create_object")
                raise ValueError("Missing or empty 'type' parameter for create_object")
            for key in ["location", "rotation", "scale"]:
                if key in arguments and arguments[key] is not None:
                    if not isinstance(arguments[key], list) or len(arguments[key]) != 3:
                        logger.error(f"'{key}' parameter for create_object should be a numeric array with 3 elements: {arguments[key]}")
                        raise ValueError(f"'{key}' parameter should be a numeric array with 3 elements")
        
        # Special validation for execute_unity_code
        if name == "execute_unity_code":
            if "code" not in arguments or not arguments["code"]:
                logger.error("Missing or empty 'code' parameter for execute_unity_code")
                raise ValueError("Missing or empty 'code' parameter for execute_unity_code")
        
        # Special validation for get_object_info
        if name == "get_object_info":
            if "object_name" not in arguments or not arguments["object_name"]:
                logger.error("Missing or empty 'object_name' parameter for get_object_info")
                raise ValueError("Missing or empty 'object_name' parameter for get_object_info")
        
        # Special validation for set_material
        if name == "set_material":
            if "object_name" not in arguments or not arguments["object_name"]:
                logger.error("Missing or empty 'object_name' parameter for set_material")
                raise ValueError("Missing or empty 'object_name' parameter for set_material")
            if "color" in arguments and arguments["color"] is not None:
                if not isinstance(arguments["color"], list) or len(arguments["color"]) not in [3, 4]:
                    logger.error(f"'color' parameter for set_material should be a numeric array with 3 (RGB) or 4 (RGBA) elements: {arguments['color']}")
                    raise ValueError("Color parameter should be a numeric array with 3 (RGB) or 4 (RGBA) elements")

        # Special validation for delete_object
        if name == "delete_object":
            if "name" not in arguments or not arguments["name"]:
                logger.error("Missing or empty 'name' parameter for delete_object")
                raise ValueError("Missing or empty 'name' parameter for delete_object")

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