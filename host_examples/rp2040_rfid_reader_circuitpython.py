"""
RFID Reader for RP2040 (Raspberry Pi Pico) using CircuitPython
Reads MFRC522 reader and sends UID to serial port

Wiring for RP2040:
  MFRC522 3.3V  -> Pico 3.3V (pin 36)
  MFRC522 GND   -> Pico GND (pin 38)
  MFRC522 CS    -> Pico GPIO 17 (pin 22)
  MFRC522 MOSI  -> Pico GPIO 19 (pin 25)
  MFRC522 MISO  -> Pico GPIO 16 (pin 21)
  MFRC522 SCK   -> Pico GPIO 18 (pin 24)
  MFRC522 IRQ   -> Not required (polling mode)

Required libraries:
  - adafruit-circuitpython-mfrc522
"""

import board
import busio
import digitalio
import time
from adafruit_mfrc522 import MFRC522

# Initialize SPI and RFID reader
spi = busio.SPI(board.GP18, MOSI=board.GP19, MISO=board.GP16)
cs_pin = digitalio.DigitalInOut(board.GP17)
cs_pin.switch_to_output()

rfid = MFRC522(spi, cs_pin)

print("RFID Ready")

# Main loop
while True:
    try:
        # Check for card
        uid = rfid.read_passive_target(timeout=0.1)
        
        if uid is not None:
            # Format UID as hex string
            uid_hex = ''.join('{:02X}'.format(b) for b in uid)
            
            # Print to serial
            print("UID: {}".format(uid_hex))
            print("Type: MIFARE Classic")
            
            # Debounce delay
            time.sleep(1.5)
    except Exception as e:
        print("Error: {}".format(e))
    
    time.sleep(0.1)
