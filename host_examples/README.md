# RFID Reader Host Examples

These are reference implementations for RFID readers that communicate with RetroNFC via serial connection. Each example implements the same protocol: send `UID: {hex_string}` when a tag is scanned.

## Protocol

The reader should:
1. Connect to the NFC/RFID reader hardware
2. When a tag is detected, read the UID
3. Send the UID to serial in the format: `UID: {hex_string}`
4. Example output: `UID: 041234567890AB`

## Available Examples

### ESP32-C3 Arduino (with display)
- **File**: `esp32c3_rfid_reader_with_display.ino`
- **Platform**: Arduino IDE
- **Features**: Serial output + OLED display feedback
- **Default Pins**:
  - OLED SCL (SW I2C): GPIO 6
  - OLED SDA (SW I2C): GPIO 5
  - RFID CS (SPI): GPIO 10
  - RFID SCK: GPIO 9
  - RFID MOSI: GPIO 8
  - RFID MISO: GPIO 7

### ESP32-C3 Arduino (serial only)
- **File**: `esp32c3_rfid_reader_serial_only.ino`
- **Platform**: Arduino IDE
- **Features**: Serial output only, minimal dependencies
- **Default Pins**:
  - RFID CS (SPI): GPIO 10
  - RFID SCK: GPIO 1
  - RFID MOSI: GPIO 3
  - RFID MISO: GPIO 2

### RP2040 MicroPython
- **File**: `rp2040_rfid_reader.py`
- **Platform**: MicroPython (Raspberry Pi Pico)
- **Features**: Serial output, simple Python implementation
- **Default Pins**:
  - RFID CS: GPIO 17
  - RFID MOSI: GPIO 19
  - RFID MISO: GPIO 16
  - RFID SCK: GPIO 18

### RP2040 CircuitPython
- **File**: `rp2040_rfid_reader_circuitpython.py`
- **Platform**: CircuitPython (Raspberry Pi Pico)
- **Features**: Serial output using CircuitPython libraries
- **Default Pins**:
  - RFID CS: GPIO 17
  - RFID MOSI: GPIO 19
  - RFID MISO: GPIO 16
  - RFID SCK: GPIO 18

### RP2040 C/C++
- **File**: `rp2040_rfid_reader.c`
- **Platform**: Raspberry Pi Pico SDK
- **Features**: Native C implementation
- **Default Pins**:
  - RFID CS: GPIO 17
  - RFID MOSI: GPIO 19
  - RFID MISO: GPIO 16
  - RFID SCK: GPIO 18

## Pin Configuration

Each implementation documents the pin assignments in comments at the top of the file. Common wire colors for standard MFRC522 modules:

- **3.3V** (red) - Power
- **GND** (black) - Ground
- **MOSI** (yellow) - Master Out, Slave In
- **MISO** (green) - Master In, Slave Out
- **SCK** (blue) - Serial Clock
- **CS/NSS** (white) - Chip Select / Slave Select

Adjust pins in each file to match your wiring.

## Serial Connection to PC

Connect the microcontroller to your PC via USB. The serial port will appear as:
- **Windows**: COM3, COM4, etc. (check Device Manager)
- **Linux**: /dev/ttyUSB0, /dev/ttyACM0, etc.
- **macOS**: /dev/tty.usbserial-*, /dev/tty.usbmodem*, etc.

Update `config.json` in RetroNFC with your serial port:
```json
{
  "serial_port": "COM9",
  "serial_baud": 115200
}
```

## Testing

Before using with RetroNFC:
1. Upload the example code to your microcontroller
2. Open a serial monitor (Arduino IDE, PuTTY, minicom, etc.)
3. Scan an NFC tag
4. Verify you see output like: `UID: 041234567890AB`

Once verified, start RetroNFC and scan tags normally.

## Customization

- **Baud Rate**: Default is 115200. Change in code if needed (update `config.json` to match)
- **RFID Reader**: These examples use MFRC522 readers. Adapt the hardware library if using a different reader
- **Serial Timing**: Add delays if tags aren't reading reliably (typically 100-200ms between reads)

## Troubleshooting

- **No output on serial**: Check wiring, especially SPI pins (MOSI, MISO, SCK, CS)
- **Wrong UID format**: Verify the RFID library is reading UID as hex string
- **Frequent disconnects**: May need capacitors on 3.3V line or better power supply
- **Serial port permission denied (Linux)**: Use `sudo usermod -a -G dialout $USER` then logout/login

## Adding New Examples

To contribute another platform:
1. Create a new file: `{platform}_rfid_reader.{ext}`
2. Document pins at the top
3. Implement the `UID: {hex_string}` protocol
4. Update this README with platform info
