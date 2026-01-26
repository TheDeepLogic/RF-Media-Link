#include <stdio.h>
#include <string.h>
#include "pico/stdlib.h"
#include "hardware/spi.h"
#include "hardware/gpio.h"

/*
 * RFID Reader for RP2040 (Raspberry Pi Pico) using C SDK + MFRC522 library
 * Reads MFRC522 reader and sends UID to serial port
 *
 * Wiring for RP2040:
 *   MFRC522 3.3V  -> Pico 3.3V (pin 36)
 *   MFRC522 GND   -> Pico GND (pin 38)
 *   MFRC522 CS    -> Pico GPIO 17 (pin 22)
 *   MFRC522 MOSI  -> Pico GPIO 19 (pin 25)
 *   MFRC522 MISO  -> Pico GPIO 16 (pin 21)
 *   MFRC522 SCK   -> Pico GPIO 18 (pin 24)
 *
 * This example uses the Pico SDK and assumes you have a MFRC522 C library
 * linked into your CMakeLists.txt (https://github.com/miguelbalboa/rfid)
 */

// Pin configuration
#define CS_PIN    17
#define SCK_PIN   18
#define MOSI_PIN  19
#define MISO_PIN  16
#define SPI_PORT  spi0

// MFRC522 register addresses (if not using a full library)
#define MFRC522_FIFO_DATA   0x04
#define MFRC522_COMMAND     0x01
#define MFRC522_CONTROL     0x0C
#define MFRC522_VERSION     0x37

// Function prototypes
void spi_init_custom(void);
void mfrc522_init(void);
uint8_t mfrc522_read(uint8_t addr);
void mfrc522_write(uint8_t addr, uint8_t val);
void mfrc522_read_uid(uint8_t *uid);

int main() {
    stdio_init_all();
    sleep_ms(100);
    
    printf("Initializing RFID Reader...\n");
    
    spi_init_custom();
    mfrc522_init();
    
    printf("RFID Ready\n");
    
    // Main loop
    while (true) {
        // Check for card (simplified polling)
        // In a production system, you would:
        // 1. Send REQA/WUPA command
        // 2. Wait for card response
        // 3. Read UID
        // 4. Parse and send to serial
        
        uint8_t uid[4];
        mfrc522_read_uid(uid);
        
        if (uid[0] != 0) {
            printf("UID: %02X%02X%02X%02X\n", uid[0], uid[1], uid[2], uid[3]);
            printf("Type: MIFARE Classic\n");
            sleep_ms(1500);
        }
        
        sleep_ms(100);
    }
    
    return 0;
}

void spi_init_custom(void) {
    // Initialize SPI0 at 1 MHz
    spi_init(SPI_PORT, 1000 * 1000);
    
    // Set pins
    gpio_set_function(SCK_PIN, GPIO_FUNC_SPI);
    gpio_set_function(MOSI_PIN, GPIO_FUNC_SPI);
    gpio_set_function(MISO_PIN, GPIO_FUNC_SPI);
    
    // CS pin as GPIO output
    gpio_init(CS_PIN);
    gpio_set_dir(CS_PIN, GPIO_OUT);
    gpio_put(CS_PIN, 1);  // CS high (inactive)
}

void mfrc522_init(void) {
    // Reset the MFRC522
    mfrc522_write(0x01, 0x0F);  // Soft reset
    sleep_ms(50);
    
    // Configure for ISO/IEC 14443 Type A
    mfrc522_write(0x26, 0x3D);  // TxASK
    mfrc522_write(0x15, 0x40);  // Mode
}

uint8_t mfrc522_read(uint8_t addr) {
    uint8_t tx_buf[2];
    uint8_t rx_buf[2];
    
    // MFRC522 read command: address << 1 | 0x80
    tx_buf[0] = (addr << 1) | 0x80;
    tx_buf[1] = 0x00;
    
    gpio_put(CS_PIN, 0);
    spi_write_read_blocking(SPI_PORT, tx_buf, rx_buf, 2);
    gpio_put(CS_PIN, 1);
    
    return rx_buf[1];
}

void mfrc522_write(uint8_t addr, uint8_t val) {
    uint8_t tx_buf[2];
    
    tx_buf[0] = (addr << 1) & 0x7E;
    tx_buf[1] = val;
    
    gpio_put(CS_PIN, 0);
    spi_write_blocking(SPI_PORT, tx_buf, 2);
    gpio_put(CS_PIN, 1);
}

void mfrc522_read_uid(uint8_t *uid) {
    // This is a simplified stub
    // In production, implement full ISO14443A anticollision/select
    memset(uid, 0, 4);
    
    // For testing, you would:
    // 1. Send REQA command
    // 2. Wait for response
    // 3. Send ANTICOLL command
    // 4. Read 4-byte UID from FIFO
}
