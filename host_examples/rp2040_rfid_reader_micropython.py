"""
RFID Reader for RP2040 (Raspberry Pi Pico) using MicroPython
Reads MFRC522 reader and sends UID to serial port

Wiring for RP2040:
  MFRC522 3.3V  -> Pico 3.3V (pin 36)
  MFRC522 GND   -> Pico GND (pin 38)
  MFRC522 CS    -> Pico GPIO 17 (pin 22)
  MFRC522 MOSI  -> Pico GPIO 19 (pin 25)
  MFRC522 MISO  -> Pico GPIO 16 (pin 21)
  MFRC522 SCK   -> Pico GPIO 18 (pin 24)
  MFRC522 IRQ   -> Not required (polling mode)
"""

import machine
import utime
from mfrc522 import MFRC522

# Initialize SPI and RFID reader
spi = machine.SPI(0, baudrate=1000000, polarity=0, phase=0,
                  sck=machine.Pin(18),
                  mosi=machine.Pin(19),
                  miso=machine.Pin(16))

cs_pin = machine.Pin(17, machine.Pin.OUT)
rdr = MFRC522(spi, cs_pin)

print("RFID Ready")

# Main loop
while True:
    try:
        # Check for card and read UID
        rdr.init()
        (stat, tag_type) = rdr.request(rdr.REQALL)
        
        if stat == rdr.OK:
            (stat, raw_uid) = rdr.anticoll()
            
            if stat == rdr.OK:
                # Format UID as hex string
                uid_hex = ''.join('{:02X}'.format(b) for b in raw_uid[:4])
                
                # Print to serial
                print("UID: {}".format(uid_hex))
                print("Type: {}".format(tag_type))
                
                # Debounce delay
                utime.sleep(1.5)
    except:
        pass
    
    utime.sleep(0.1)
