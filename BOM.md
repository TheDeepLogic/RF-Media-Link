# RFID Scanner BOM

## Core Electronics
- 1x ESP32-C3 dev board (USB-C, 3.3V I/O)
- 1x RC522 RFID reader module (MFRC522, 3.3V)
- 1x (optional) OLED display: SSD1306 72×40 ER, I2C (software I2C pins: SCL=GPIO6, SDA=GPIO5; reset not used)
- 1x Custom PCB or perfboard for mounting ESP32-C3, RC522, and display
- 1x USB-C cable (to host PC)
- 1x USB-C breakout/adapter if needed for a convenient panel mount

## Passives & Interconnects
- Pin headers / sockets as needed for ESP32-C3, RC522, and display
- Jumper wires or short traces on PCB for signal routing
- Solder and flux

## Enclosure & Hardware
- 1x 3D-printed shell/enclosure
- Optional standoffs/screws for securing the PCB and display inside the enclosure

## Power & Wiring (per esp32c3_rfid_reader_with_display.ino)
- RC522: SS→GPIO10, SCK→GPIO9, MOSI→GPIO8, MISO→GPIO7, 3V3→3V3, GND→GND
- Display (SSD1306 72×40 ER, SW I2C): SCL→GPIO6, SDA→GPIO5, RESET not used (U8X8_PIN_NONE)
- ESP32-C3 powered via USB-C; RC522 powered from the ESP32 3V3 pin; common ground

## Serial Output Format
- On tag read, device sends over USB CDC (virtual COM):
  - "UID: XX XX XX XX"
  - "Type: <PICC type>"

## Notes
- Any equivalent RC522 board, ESP32-C3 dev board, or SSD1306 I2C display that matches the voltage/pin expectations can be substituted.
- Ensure the RC522 is powered from 3.3V (not 5V) and grounds are common.
- Main host script consuming serial data: main.py.
