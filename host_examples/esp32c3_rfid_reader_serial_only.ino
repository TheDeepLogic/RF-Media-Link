#include <Arduino.h>
#include <SPI.h>
#include <MFRC522v2.h>
#include <MFRC522DriverSPI.h>
#include <MFRC522DriverPinSimple.h>
#include <MFRC522Debug.h>

// ---- RC522 SPI pins ----
// Connect RC522 module pins:
//   RC522 3.3V  -> ESP32-C3 3.3V
//   RC522 GND   -> ESP32-C3 GND
//   RC522 CS    -> GPIO 10
//   RC522 MOSI  -> GPIO 3
//   RC522 MISO  -> GPIO 2
//   RC522 SCK   -> GPIO 1
// (adjust if using different wiring)

constexpr uint8_t PIN_SS   = 10;  // Chip Select
constexpr uint8_t PIN_SCK  = 1;   // Serial Clock
constexpr uint8_t PIN_MOSI = 3;   // Master Out, Slave In
constexpr uint8_t PIN_MISO = 2;   // Master In, Slave Out

MFRC522DriverPinSimple ss_pin(PIN_SS);
MFRC522DriverSPI driver(ss_pin);
MFRC522 mfrc522(driver);

void setup() {
  // Serial communication to PC (USB)
  Serial.begin(115200);
  delay(200);
  
  // Initialize RFID reader
  SPI.begin(PIN_SCK, PIN_MISO, PIN_MOSI, PIN_SS);
  mfrc522.PCD_Init();
  delay(50);
  
  Serial.println("RFID Ready");
}

void loop() {
  // Check if a card is present
  if (!mfrc522.PICC_IsNewCardPresent()) {
    return;
  }
  
  // Try to read the card
  if (!mfrc522.PICC_ReadCardSerial()) {
    return;
  }
  
  // Build UID string (hex format, no spaces)
  char uidStr[24];
  char *p = uidStr;
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    sprintf(p, "%02X", mfrc522.uid.uidByte[i]);
    p += 2;
  }
  *p = '\0';
  
  // Send UID to serial
  Serial.print("UID: ");
  Serial.println(uidStr);
  
  // Get and send card type
  MFRC522::PICC_Type type = mfrc522.PICC_GetType(mfrc522.uid.sak);
  char typeName[16];
  strncpy_P(typeName, (PGM_P)MFRC522Debug::PICC_GetTypeName(type), sizeof(typeName));
  typeName[sizeof(typeName)-1] = '\0';
  
  Serial.print("Type: ");
  Serial.println(typeName);
  
  // Halt the card
  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();
  
  // Debounce delay
  delay(1500);
}
