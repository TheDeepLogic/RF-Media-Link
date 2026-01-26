#include <Arduino.h>
#include <SPI.h>
#include <U8x8lib.h>

#include <MFRC522v2.h>
#include <MFRC522DriverSPI.h>
#include <MFRC522DriverPinSimple.h>
#include <MFRC522Debug.h>

// ---- YOUR TINY SCREEN (unchanged) ----
U8X8_SSD1306_72X40_ER_SW_I2C u8x8(
  /* clock=*/ 6,
  /* data=*/  5,
  /* reset=*/ U8X8_PIN_NONE
);

// ---- RC522 SPI pins (unchanged) ----
//   RC522 SDA/SS -> GPIO 10
//   RC522 SCK    -> GPIO 9
//   RC522 MOSI   -> GPIO 8
//   RC522 MISO   -> GPIO 7
//   3.3V / GND as usual
constexpr uint8_t PIN_SS   = 10;
constexpr uint8_t PIN_SCK  = 9;
constexpr uint8_t PIN_MOSI = 8;
constexpr uint8_t PIN_MISO = 7;

MFRC522DriverPinSimple ss_pin(PIN_SS);
MFRC522DriverSPI driver(ss_pin);
MFRC522 mfrc522(driver);

void setup() {
  // USB serial only – no extra UART, no pin mapping
  Serial.begin(115200);
  delay(200);
  Serial.println("Booting...");

  // Display
  u8x8.begin();
  u8x8.setFlipMode(1);  // Rotate 180 degrees
  u8x8.setFont(u8x8_font_chroma48medium8_r);
  u8x8.clear();
  u8x8.drawString(0, 0, "BOOT");

  // RFID
  SPI.begin(PIN_SCK, PIN_MISO, PIN_MOSI, PIN_SS);
  mfrc522.PCD_Init();
  delay(50);

  u8x8.clear();
  u8x8.drawString(0, 0, "READY");
  u8x8.drawString(0, 2, "SCAN");

  Serial.println("RFID Ready");
}

void loop() {
  if (!mfrc522.PICC_IsNewCardPresent()) return;
  if (!mfrc522.PICC_ReadCardSerial())   return;

  // ---- UID ----
  char uidStr[24];
  char *p = uidStr;
  for (byte i = 0; i < mfrc522.uid.size && i < 4; i++) {
    sprintf(p, "%02X", mfrc522.uid.uidByte[i]);
    p += 2;
    if (i < mfrc522.uid.size - 1) *p++ = ' ';
  }
  *p = '\0';

  // ---- Type ----
  MFRC522::PICC_Type type = mfrc522.PICC_GetType(mfrc522.uid.sak);
  char typeName[16];
  strncpy_P(typeName, (PGM_P)MFRC522Debug::PICC_GetTypeName(type), sizeof(typeName));
  typeName[sizeof(typeName)-1] = '\0';
  typeName[9] = '\0'; // fit 72px width

  // Send serial data immediately (before slow display operations)
  Serial.print("UID: ");
  Serial.println(uidStr);
  Serial.print("Type: ");
  Serial.println(typeName);

  // Now update display
  u8x8.clear();
  u8x8.drawString(0, 0, "TAG");
  u8x8.drawString(0, 2, uidStr);
  u8x8.drawString(0, 4, typeName);

  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();

  delay(1500);

  u8x8.clear();
  u8x8.drawString(0, 0, "READY");
  u8x8.drawString(0, 2, "SCAN");
}
