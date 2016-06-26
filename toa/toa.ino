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
#define NUMPINS 2
const int toaPins[NUMPINS] = {8, 9};
bool prevState[NUMPINS] = {HIGH, HIGH};
// 3 hours in millis
const unsigned long TIMEOUT = (3 * 60 * 60 * 1000L);

void setup() {
  for (int i = 0; i < NUMPINS; i++)
    pinMode(toaPins[i], INPUT_PULLUP);
  pixels.begin();
}

void loop() {
  unsigned long currentTime = millis();

  // get lock status LOW if occupied, or HIGH if free
  bool toa[NUMPINS];
  bool allFree = true;
  bool noneFree = true;
  for (int i = 0; i < NUMPINS; i++) {
    toa[i] = digitalRead(toaPins[i]);

    if (!toa[i])
      allFree = false;

    if (toa[i])
      noneFree = false;

    if (toa[i] != prevState[i]) {
      // state changed      
      timeChanged = currentTime;
      prevState[i] = toa[i];
    }
  }

  // Timed out, lower brightness
  bool timeout = (currentTime - timeChanged > TIMEOUT);
  uint32_t pixColor = allFree ?
    pixels.Color(0, timeout ? 1 : 150, 0) :
    (noneFree ? pixels.Color(255, 0, 0) :
    pixels.Color(timeout ? 4 : 64, timeout ? 4 : 64, 0) );

  if (pixColor != pixels.getPixelColor(0)) {
    pixels.setPixelColor(0, pixColor);
    pixels.show();
  }

  delay(delayval);
}
