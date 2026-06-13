import paho.mqtt.client as mqtt
import time
import random
import json
import signal
import threading
from datetime import datetime, timezone

# ----------------------------
# CONFIGURATION
# ----------------------------

BROKER_ADDRESS = "localhost"   # change to your MQTT broker IP if needed = 192.168.11.84
BROKER_PORT = 1883
SHELTER_CODES = ["ILS002", "DVOR003", "GP001"]
PUBLISH_INTERVAL_SECONDS = 2
QOS_LEVEL = 1
CONNECT_TIMEOUT_SECONDS = 10
PUBLISH_TIMEOUT_SECONDS = 5

running = True
connected = threading.Event()

# -----------------------------
# Sensor Data Generator
# -----------------------------
# def generate_sensor_data(shelter_code):
#     """Generate one sensor reading for the given shelter"""
#     return {
#         "RecordedTime": datetime.now(timezone.utc).isoformat(),
#         "ShelterCode": shelter_code,
#         "Temperature": round(random.uniform(20.0, 37.0), 1),
#         "Humidity": round(random.uniform(40.0, 85.0), 1),
#         "SmokeDetected": random.random() < 0.05,       # 5% chance
#         "IntrusionDetected": random.random() < 0.03,   # 3% chance
#     }




# ----------------------------
# SHUTDOWN HANDLER
# ----------------------------

def signal_handler(sig, frame):
    global running
    print("\nStopping MQTT simulator...")
    running = False


    # # Keep main thread alive
    # while True:
    #     time.sleep(30)
    
signal.signal(signal.SIGINT, signal_handler)


# ----------------------------
# MQTT CALLBACKS
# ----------------------------

def on_connect(client, userdata, flags, reason_code, properties=None):
    if reason_code == 0:
        connected.set()
        print("Connected to MQTT broker successfully.\n")
    else:
        print(f"Connection failed. Return code: {reason_code}")


def on_disconnect(client, userdata, *args):
    connected.clear()
    print("Disconnected from MQTT broker.")


# ----------------------------
# ENVIRONMENT SIMULATION
# ----------------------------

def utc_timestamp():
    return (
        datetime.now(timezone.utc)
        .isoformat(timespec="milliseconds")
        .replace("+00:00", "Z")
    )


def generate_environment_payload(shelter_code):
    temperature = round(random.uniform(18.0, 35.0), 1)
    humidity = round(random.uniform(35.0, 85.0), 1)

    # Mostly normal, occasional simulated faults
    smoke_detected = random.random() < 0.05       # 5% chance
    intrusion_detected = random.random() < 0.05   # 5% chance

    return json.dumps({
        "shelterCode": shelter_code,
        "temperature": temperature,
        "humidity": humidity,
        "smokeDetected": smoke_detected,
        "intrusionDetected": intrusion_detected,
        "timeStamp": utc_timestamp()
    })


# ----------------------------
# STABILIZER SIMULATION
# ----------------------------

def generate_stabilizer_payload(shelter_code):
    # Input voltage varies more because it represents mains supply
    input_voltage = round(random.uniform(205.0, 250.0), 1)

    # Output voltage is more stable because the stabilizer regulates it
    output_voltage = round(random.uniform(218.0, 232.0), 1)

    current = round(random.uniform(3.0, 22.0), 2)
    frequency = round(random.uniform(49.5, 50.5), 1)
    load_percentage = round(random.uniform(20.0, 88.0), 0)

    # Occasional fault spikes
    if random.random() < 0.05:
        input_voltage = round(random.choice([
            random.uniform(160.0, 179.0),
            random.uniform(261.0, 280.0)
        ]), 1)

    if random.random() < 0.04:
        output_voltage = round(random.choice([
            random.uniform(190.0, 199.0),
            random.uniform(246.0, 255.0)
        ]), 1)

    if random.random() < 0.04:
        load_percentage = round(random.uniform(91.0, 100.0), 0)

    return json.dumps({
        "shelterCode": shelter_code,
        "inputVoltage": input_voltage,
        "outputVoltage": output_voltage,
        "current": current,
        "frequency": frequency,
        "loadPercentage": load_percentage,
        "timeStamp": utc_timestamp()
    })


def publish_payload(topic, payload):
    if not connected.is_set():
        print(f"  Skipped:   {topic} (MQTT client is disconnected)")
        return False

    result = client.publish(topic, payload, qos=QOS_LEVEL)

    if result.rc != mqtt.MQTT_ERR_SUCCESS:
        print(f"  Failed:    {topic} (MQTT error code {result.rc})")
        return False

    try:
        result.wait_for_publish(timeout=PUBLISH_TIMEOUT_SECONDS)
    except RuntimeError as error:
        print(f"  Failed:    {topic} ({error})")
        return False

    if not result.is_published():
        print(f"  Failed:    {topic} (broker acknowledgement timed out)")
        return False

    print(f"  Published: {topic}")
    print(f"  Payload:   {payload}")
    return True


# ----------------------------
# MQTT SETUP
# ----------------------------

try:
    client = mqtt.Client(
        callback_api_version=mqtt.CallbackAPIVersion.VERSION2,
        client_id="RSMS-Stabilizer-Simulator"
    )
except AttributeError:
    client = mqtt.Client(client_id="RSMS-Stabilizer-Simulator")
client.on_connect = on_connect
client.on_disconnect = on_disconnect

print("Connecting to MQTT broker...")
client.connect(BROKER_ADDRESS, BROKER_PORT, 60)
client.loop_start()

if not connected.wait(CONNECT_TIMEOUT_SECONDS):
    client.loop_stop()
    client.disconnect()
    raise TimeoutError(
        f"Could not connect to MQTT broker at "
        f"{BROKER_ADDRESS}:{BROKER_PORT} within {CONNECT_TIMEOUT_SECONDS} seconds."
    )

print("Starting combined shelter simulator...")
print("Publishing environment and stabilizer data.")
print("------------------------------------------")


# ----------------------------
# MAIN LOOP
# ----------------------------

while running:
    for shelter_code in SHELTER_CODES:

        environment_topic = f"shelters/{shelter_code}/environment"
        stabilizer_topic = f"shelters/{shelter_code}/stabilizer"

        environment_payload = generate_environment_payload(shelter_code)
        stabilizer_payload = generate_stabilizer_payload(shelter_code)

        print(f"Shelter: {shelter_code}")
        publish_payload(environment_topic, environment_payload)
        publish_payload(stabilizer_topic, stabilizer_payload)
        print("------------------------------------------")

    time.sleep(PUBLISH_INTERVAL_SECONDS)


# ----------------------------
# CLEAN EXIT
# ----------------------------

client.loop_stop()
client.disconnect()

print("Simulator stopped.")
