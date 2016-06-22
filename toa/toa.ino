// Toilet Indicator (c) Christian Nilsson & Chrstoffer Ramqvist 2016
// BSD license

#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
#include <avr/power.h>
#endif

#define NEOPXPIN       5
#define NUMPIXELS      8
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, NEOPXPIN, NEO_GRB + NEO_KHZ800);

int delayval = 200;  // Delay for a period of time (in milliseconds).
unsigned long timeChanged = 0;
int toa1PreviousState = HIGH;
int toa2PreviousState = HIGH;
uint32_t oldPixColor = 0;

void setup() {
  pinMode(8, INPUT_PULLUP);
  pinMode(9, INPUT_PULLUP);
  pixels.begin();
}

void loop() {
  unsigned long currentTime = millis();

  // get lock status LOW if occupied, or HIGH if free
  int toa1 = digitalRead(8);
  int toa2 = digitalRead(9);

  if (toa1 != toa1PreviousState || toa2 != toa2PreviousState) {
    timeChanged = currentTime;
    toa1PreviousState = toa1;
    toa2PreviousState = toa2;
  }

  // Timed out after 3h, lower brightness
  bool timeout = (currentTime - timeChanged > (3 * 60 * 60 * 1000));
  uint32_t pixColor = toa1 && toa2 ?
    pixels.Color(0, timeout ? 1 : 150, 0) :
    (!toa1 && !toa2 ? pixels.Color(255, 0, 0) :
    pixels.Color(timeout ? 4 : 64, timeout ? 4 : 64, 0) );

  if (pixColor != oldPixColor) {
    pixels.setPixelColor(0, pixColor);
    pixels.show();
  }

  delay(delayval);
}
