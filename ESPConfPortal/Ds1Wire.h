
// http://www.pjrc.com/teensy/td_libs_OneWire.html
#include <OneWire.h>

OneWire  ds(2);  // on pin 10 (a 4.7K resistor is necessary)

void printb(uint8_t b) {
  Serial.print(b >> 4,HEX);
  Serial.print(b & 0x0f,HEX);
}

void printba(uint8_t* bs, int count) {
  for(int i = 0; i < count; i++) {
    Serial.write(' ');
    printb(bs[i]);
  }
}

void ds182xConvert(OneWire ow, byte * addr) {
  ow.reset();
  ow.select(addr);
  ow.write(0x44, 1);        // start conversion, with parasite power on at the end
}
byte ds182xPresent(OneWire ow, byte * addr) {
  byte present = ow.reset();
  ow.select(addr);
  ow.write(0xBE);         // Read Scratchpad
  return present;
}

float getTemprature(byte * data, byte type_s) {
  // Convert the data to actual temperature
  // because the result is a 16 bit signed integer, it should
  // be stored to an "int16_t" type, which is always 16 bits
  // even when compiled on a 32 bit processor.
  int16_t raw = (data[1] << 8) | data[0];
  if (type_s) {
    raw = raw << 3; // 9 bit resolution default
    if (data[7] == 0x10) {
      // "count remain" gives full 12 bit resolution
      raw = (raw & 0xFFF0) + 12 - data[6];
    }
    return (float)raw / 16.0;
  } else {
    byte cfg = (data[4] & 0x60);
    // at lower res, the low bits are undefined, so let's zero them
    if (cfg == 0x00) raw = raw & ~7;  // 9 bit resolution, 93.75 ms
    else if (cfg == 0x20) raw = raw & ~3; // 10 bit res, 187.5 ms
    else if (cfg == 0x40) raw = raw & ~1; // 11 bit res, 375 ms
    //// default is 12 bit resolution, 750 ms conversion time
    return (float)raw * 0.0625;
  }
}

void dodsTempRead(byte * addr, byte type_s) {
  /*
  ds.select(addr);
  ds.write(0xB4, 1);        // Read parasite power mode
  byte pmode = ds.read();
  printb(pmode);
  */

  // TODO be smart about this!
  ds182xConvert(ds, addr);

  delay(800);     // maybe 750ms is enough, maybe not
  // we might do a ds.depower() here, but the reset will take care of it.

  byte present = ds182xPresent(ds, addr);

  printb(present);
  Serial.print(" : ");
  byte data[12];
  for ( byte i = 0; i < 9; i++) {   // we need 9 bytes is returned we need 2 first
    data[i] = ds.read();
    printb(data[i]);
    Serial.print(" ");
  }
  if (OneWire::crc8(data, 8) != data[8]) {
      Serial.print("CRC is not valid! expected: ");
      printb(OneWire::crc8(data, 8));
  }

  float c = getTemprature(data, type_s);
  Serial.print("  Temp = ");
  Serial.print(c);
  Serial.print(" C");
}

uint8_t ds2413Read(byte * addr) {
  ds.reset();
  ds.select(addr);
  ds.write(0xF5);  // ACCESS_READ

  uint8_t result = ds.read();  // get register results
  printb(result);
  bool ok = (~result & 0x0f) == (result >> 4); // compare nibbles
  result &= 0x0f; // clear inverted values

  // reset

  // return ok = result : -1
  Serial.print(ok ? " ok " : " compl err ");
  Serial.print(" result: ");
  Serial.print(result, BIN);
  return result;
}

void dods2413Read(byte * addr) {
  ds2413Read(addr);
}

void handleOneWireLoop() {
  byte addr[8];
  if ( !ds.search(addr)) {
    ds.reset_search();
    Serial.println("W reset.");
    return;
  }

  Serial.print("W");
  printba(addr, 8);
  Serial.print("  ");

  if (OneWire::crc8(addr, 7) != addr[7]) {
      Serial.print("CRC is not valid! expected: ");
      printb(OneWire::crc8(addr, 7));
      Serial.print('\n');
      return;
  }

  // the first ROM byte indicates chip
  switch (addr[0]) {
    case 0x01:
      Serial.print("  DS2401   Serial Number");
      break;
    case 0x10:
      Serial.print("  DS18S20  ");  // or old DS1820
      dodsTempRead(addr, 1);
      break;
    case 0x28:
      Serial.print("  DS18B20  ");
      dodsTempRead(addr, 0);
      break;
    case 0x22:
      Serial.print("  DS1822   ");
      dodsTempRead(addr, 0);
      break;
    case 0x3a:
      Serial.print("  DS2413   Dual Switch  ");
      dods2413Read(addr);
      break;
    case 0x85:
      Serial.print("  2100H    Dual Switch  ");
      dods2413Read(addr);
      break;
    case 0x86:
      Serial.print("  CX2413 Occupy Detect  ");
      dods2413Read(addr);
      break;
    default:
      Serial.print("Device is not of known family.");
  }
  Serial.print('\n');
}

