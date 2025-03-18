#!/usr/bin/env python3
import json
import socket
import time

# Unity MCP Server bağlantı bilgileri
HOST = 'localhost'
PORT = 8080

def send_command(method, params=None):
    """Unity MCP Server'a JSON-RPC komutu gönderir"""
    request_id = str(int(time.time()))
    
    # JSON-RPC 2.0 formatında istek oluştur
    request = {
        "jsonrpc": "2.0",
        "method": method,
        "id": request_id
    }
    
    # Parametreler varsa ekle
    if params:
        request["params"] = params
    
    request_str = json.dumps(request)
    
    # Sunucuya bağlan
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        sock.connect((HOST, PORT))
        print(f"Bağlandı: {HOST}:{PORT}")
        
        # İsteği gönder
        sock.sendall(request_str.encode('utf-8'))
        print(f"Gönderilen istek: {request_str}")
        
        # Yanıtı al
        response = sock.recv(4096)
        response_str = response.decode('utf-8')
        print(f"Alınan yanıt: {response_str}")
        
        # JSON yanıtını ayrıştır
        try:
            response_json = json.loads(response_str)
            return response_json
        except json.JSONDecodeError:
            print(f"Hata: Geçersiz JSON yanıtı: {response_str}")
            return {"error": "Geçersiz JSON yanıtı"}
            
    except Exception as e:
        print(f"Hata: {e}")
        return {"error": str(e)}
    finally:
        sock.close()

def create_lego_character():
    """Basit bir LEGO karakteri oluştur"""
    print("LEGO karakteri oluşturuluyor...")
    
    # Kamera ayarla
    camera_params = {
        "position": [0, 0, 5],
        "rotation": [0, 0, 0]
    }
    send_command("create_camera", camera_params)
    
    # Objeler için temel renkler
    body_color = [1.0, 0.0, 0.0, 1.0]  # Kırmızı
    head_color = [1.0, 1.0, 0.0, 1.0]  # Sarı
    
    # Gövde (Küp)
    body_params = {
        "type": "CUBE",
        "name": "LegoBody",
        "location": [0, 0, 0],
        "scale": [1.5, 2.0, 1.0]
    }
    body_result = send_command("create_object", body_params)
    
    # Materyal ayarla - Gövde
    body_material = {
        "object_name": "LegoBody",
        "color": body_color
    }
    send_command("set_material", body_material)
    
    # Baş (Silindir)
    head_params = {
        "type": "CYLINDER",
        "name": "LegoHead",
        "location": [0, 1.25, 0],
        "scale": [1.0, 0.5, 1.0]
    }
    head_result = send_command("create_object", head_params)
    
    # Materyal ayarla - Baş
    head_material = {
        "object_name": "LegoHead",
        "color": head_color
    }
    send_command("set_material", head_material)
    
    # Üst çıkıntı (kafa üzerindeki LEGO mandalı)
    stud_params = {
        "type": "CYLINDER",
        "name": "LegoStud",
        "location": [0, 1.6, 0],
        "scale": [0.3, 0.2, 0.3]
    }
    stud_result = send_command("create_object", stud_params)
    
    # Materyal ayarla - Üst çıkıntı
    stud_material = {
        "object_name": "LegoStud",
        "color": head_color
    }
    send_command("set_material", stud_material)
    
    # Sol kol
    left_arm_params = {
        "type": "CUBE",
        "name": "LegoLeftArm",
        "location": [-1.0, 0.2, 0],
        "scale": [0.5, 1.5, 0.5]
    }
    left_arm_result = send_command("create_object", left_arm_params)
    
    # Materyal ayarla - Sol kol
    left_arm_material = {
        "object_name": "LegoLeftArm",
        "color": body_color
    }
    send_command("set_material", left_arm_material)
    
    # Sağ kol
    right_arm_params = {
        "type": "CUBE",
        "name": "LegoRightArm",
        "location": [1.0, 0.2, 0],
        "scale": [0.5, 1.5, 0.5]
    }
    right_arm_result = send_command("create_object", right_arm_params)
    
    # Materyal ayarla - Sağ kol
    right_arm_material = {
        "object_name": "LegoRightArm",
        "color": body_color
    }
    send_command("set_material", right_arm_material)
    
    # Sol bacak
    left_leg_params = {
        "type": "CUBE",
        "name": "LegoLeftLeg",
        "location": [-0.5, -1.5, 0],
        "scale": [0.5, 1.5, 0.5]
    }
    left_leg_result = send_command("create_object", left_leg_params)
    
    # Materyal ayarla - Sol bacak
    left_leg_material = {
        "object_name": "LegoLeftLeg",
        "color": body_color
    }
    send_command("set_material", left_leg_material)
    
    # Sağ bacak
    right_leg_params = {
        "type": "CUBE",
        "name": "LegoRightLeg",
        "location": [0.5, -1.5, 0],
        "scale": [0.5, 1.5, 0.5]
    }
    right_leg_result = send_command("create_object", right_leg_params)
    
    # Materyal ayarla - Sağ bacak
    right_leg_material = {
        "object_name": "LegoRightLeg",
        "color": body_color
    }
    send_command("set_material", right_leg_material)
    
    print("LEGO karakter oluşturuldu!")

if __name__ == "__main__":
    create_lego_character()
