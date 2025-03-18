import json
import socket
import logging
from typing import Dict, Any, Optional

import asyncio

logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class UnityClient:
    """TCP client for communicating with Unity."""
    
    def __init__(self, host: str = "localhost", port: int = 8080):
        """
        Initialize the UnityClient class.
        
        Args:
            host: Host address of the Unity TCP server
            port: Port number of the Unity TCP server
        """
        self.host = host
        self.port = port
        self.socket: Optional[socket.socket] = None
        self.connected = False
        logger.info(f"Unity client initialized, target: {host}:{port}")
    
    async def connect(self) -> None:
        """Connect to the Unity TCP server."""
        try:
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            await asyncio.get_event_loop().run_in_executor(None, lambda: self.socket.connect((self.host, self.port)))
            self.connected = True
            logger.info(f"Connected to Unity server: {self.host}:{self.port}")
        except Exception as e:
            logger.error(f"Error connecting to Unity server: {e}")
            raise ConnectionError(f"Could not connect to Unity server: {e}")
    
    async def disconnect(self) -> None:
        """Close the connection with the Unity TCP server."""
        if self.socket and self.connected:
            try:
                await asyncio.get_event_loop().run_in_executor(None, lambda: self.socket.close())
                self.socket = None
                self.connected = False
                logger.info("Connection to Unity server closed")
            except Exception as e:
                logger.error(f"Error closing connection: {e}")
    
    async def send_command(self, method: str, params: Optional[Dict[str, Any]] = None) -> Any:
        """
        Send a command to the Unity server and receive a response.
        
        Args:
            method: Name of the method to call
            params: Parameters to send to the method
            
        Returns:
            Response from the server
            
        Raises:
            ConnectionError: If cannot connect to the server
            Exception: If the server returns an error
        """
        if not self.connected or self.socket is None:
            await self.connect()
        
        # If params is null, create an empty dict
        params = params or {}
        
        # If params is not a dict or contains "params" key, process it
        if isinstance(params, dict) and "params" in params:
            params = params.get("params", {})
        
        # Create a request in JSON-RPC format
        request = {
            "jsonrpc": "2.0",
            "method": method,
            "params": params,
            "id": str(id(params)) if params else "0"
        }
        
        # Log the parameters being sent
        logger.debug(f"Request sent to Unity: {json.dumps(request, indent=2)}")
        
        try:
            # Encode the request in JSON format and send
            json_data = json.dumps(request).encode('utf-8')
            await asyncio.get_event_loop().run_in_executor(None, lambda: self.socket.sendall(json_data))
            logger.debug(f"Request sent: {json_data.decode('utf-8')}")
            
            # Wait for response
            response_data = await asyncio.get_event_loop().run_in_executor(None, lambda: self.socket.recv(4096))
            response = json.loads(response_data.decode('utf-8'))
            logger.debug(f"Response received: {response}")
            
            # Error checking
            if "error" in response:
                error_msg = response.get("error", {}).get("message", "Unknown error")
                logger.error(f"Unity error: {error_msg}")
                raise Exception(f"Unity error: {error_msg}")
            
            return response.get("result")
        except json.JSONDecodeError as e:
            logger.error(f"Response is not in JSON format: {e}")
            raise Exception(f"Response is not in JSON format: {e}")
        except socket.error as e:
            logger.error(f"Socket error: {e}")
            await self.disconnect()
            raise ConnectionError(f"Connection to Unity lost: {e}")
        except Exception as e:
            logger.error(f"Error sending command: {e}")
            raise

# For testing
if __name__ == "__main__":
    async def test():
        client = UnityClient()
        await client.connect()
        result = await client.send_command("get_scene_info", {})
        print(result)
        await client.disconnect()

    asyncio.run(test())