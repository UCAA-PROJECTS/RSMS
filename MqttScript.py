import json
import time
import random
import threading
import paho.mqtt.client as mqtt

# -----------------------------
# Configuration
# -----------------------------
BROKER = "localhost"   # MQTT broker host
PORT = 1883
PUBLISH_INTERVAL = 3   # seconds

SHELTERS = [
    "ILS002",
    "DVOR003",
    "GP001"
]

# -----------------------------
# Sensor Data Generator
# -----------------------------
def generate_sensor_data(shelter_code):
    """Generate one sensor reading for the given shelter"""
    return {
        "ShelterCode": shelter_code,
        "Temperature": round(random.uniform(20.0, 37.0), 1),
        "Humidity": round(random.uniform(40.0, 85.0), 1),
        "SmokeDetected": random.random() < 0.05,       # 5% chance
        "IntrusionDetected": random.random() < 0.03,   # 3% chance
    }

# -----------------------------
# Shelter Publisher Thread
# -----------------------------
def run_shelter_simulator(shelter_code):
    client = mqtt.Client(client_id=f"{shelter_code}_Simulator")
    client.connect(BROKER, PORT)
    client.loop_start()

    topic = f"shelters/{shelter_code}"

    print(f"🚀 Started simulator for {shelter_code}")

    while True:
        payload = generate_sensor_data(shelter_code)
        json_payload = json.dumps(payload)
        client.publish(topic, json_payload)
        print(f"[{shelter_code}] Published → {payload}")  # now prints Python dict style
        time.sleep(PUBLISH_INTERVAL)

# -----------------------------
# Main
# -----------------------------
if __name__ == "__main__":
    print("=== Raspberry Pi MQTT Simulator Started ===")

    # Run one thread per shelter
    for shelter in SHELTERS:
        thread = threading.Thread(
            target=run_shelter_simulator,
            args=(shelter,),
            daemon=True
        )
        thread.start()

    # Keep main thread alive
    while True:
        time.sleep(1)