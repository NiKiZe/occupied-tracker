/*  ESP8266-01 layout
 *  Loc. Ant.
 *  |        GIO3      RX  o o  VCC
 *  =  +     FL_EN   GIO0  o o  GIO16   RST (timer connected to reset)
 *  =  |     TX      GIO2  o o  CH_PD
 *  =--+              GND  o o  TX      GIO1
 *
 *  GPIO0 is pulled low on boot to enable serial firmware update.
 *
 *  Use GIO0 as Pixel output (add resistor), that way no pull down is active on boot(?)
 *  If TX is moved to GIO2, then GIO1 can be used as input
 *  on the other hand so can GIO2? (check more on the UART usage?)
 *  GIO2 MUST not be low on boot (UART mode is initialized)
 *
 *  Inputs available:
 *  GIO1/2 & GIO3
 */

#include <Arduino.h>
#include <ESP8266WiFi.h>          //https://github.com/esp8266/Arduino

//needed for library
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include "WiFiManager.h"          //https://github.com/tzapu/WiFiManager

#include <TimeLib.h>              //https://github.com/PaulStoffregen/Time
#include <WiFiUdp.h>              // used by ntp (indirectly pulled in by OTA and others)
#include "FS.h"

#include <ArduinoOTA.h>
#include <ESP8266HTTPUpdateServer.h>

#include <ESP8266HTTPClient.h>

#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
#include <avr/power.h>
#endif

#define NEOPXPIN       0
#define NUMPIXELS      1
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, NEOPXPIN, NEO_GRB + NEO_KHZ800);

/* --- toa ---- */
const int delayval = 200;  // Delay for a period of time (in milliseconds).
unsigned long timeChanged = 0;
time_t fullSend = 0;
#define NUMPINS 1
const int toaPins[NUMPINS] = {3};
bool prevState[NUMPINS];
time_t lastStateChange[NUMPINS];
uint32_t idxToRoomMap[NUMPINS];
// 3 hours in millis
const unsigned long TIMEOUT = (3 * 60 * 60 * 1000L);
String passcode;
/* --- /toa --- */

ESP8266WebServer http(80);
ESP8266HTTPUpdateServer httpUpdater(true);

const char* update_path = "/flashfw";
const char* update_username = "firmware";
const char* update_password = "update";



/*-------- NTP code ----------*/
// NTP Servers:
static const char ntpServerName[] = "ntp.se";

WiFiUDP ntpUdp;

const int NTP_PACKET_SIZE = 48; // NTP time is in the first 48 bytes of message
byte packetBuffer[NTP_PACKET_SIZE]; //buffer to hold incoming & outgoing packets

// send an NTP request to the time server at the given address
void sendNTPpacket(WiFiUDP &udp, IPAddress &address)
{
  // set all bytes in the buffer to 0
  memset(packetBuffer, 0, NTP_PACKET_SIZE);
  // Initialize values needed to form NTP request
  packetBuffer[0] = 0b11100011;   // LI, Version, Mode
  packetBuffer[1] = 0;     // Stratum, or type of clock
  packetBuffer[2] = 6;     // Polling Interval
  packetBuffer[3] = 0xEC;  // Peer Clock Precision
  // 8 bytes of zero for Root Delay & Root Dispersion
  packetBuffer[12] = 49;
  packetBuffer[13] = 0x4E;
  packetBuffer[14] = 49;
  packetBuffer[15] = 52;
  // all NTP fields have been given values, now
  // you can send a packet requesting a timestamp:
  udp.beginPacket(address, 123); //NTP requests are to port 123
  udp.write(packetBuffer, NTP_PACKET_SIZE);
  udp.endPacket();
}

time_t getNtpTime()
{
  ntpUdp.begin(12388);  // local port to listen for ntp UDP packets
  IPAddress ntpServerIP; // NTP server's ip address

  Serial.print(millis());
  Serial.print(" Local ntp port: ");
  Serial.print(ntpUdp.localPort());

  while (ntpUdp.parsePacket() > 0) ; // discard any previously received packets
  Serial.print(" NTP Request ");
  // get a random server from the pool
  WiFi.hostByName(ntpServerName, ntpServerIP);
  Serial.print(String(ntpServerName) + ": ");
  Serial.println(ntpServerIP);
  sendNTPpacket(ntpUdp, ntpServerIP);
  uint32_t beginWait = millis();
  while (millis() - beginWait < 1500) {
    int size = ntpUdp.parsePacket();
    if (size >= NTP_PACKET_SIZE) {
      Serial.print(millis());
      Serial.println(" NTP Response");
      ntpUdp.read(packetBuffer, NTP_PACKET_SIZE);  // read packet into the buffer
      ntpUdp.stop();
      unsigned long secsSince1900;
      // convert four bytes starting at location 40 to a long integer
      secsSince1900 =  (unsigned long)packetBuffer[40] << 24;
      secsSince1900 |= (unsigned long)packetBuffer[41] << 16;
      secsSince1900 |= (unsigned long)packetBuffer[42] << 8;
      secsSince1900 |= (unsigned long)packetBuffer[43];
      return secsSince1900 - 2208988800UL; // add 70 years in seconds
    }
  }
  Serial.print(millis());
  ntpUdp.stop();
  Serial.println(" No NTP Response :-(");
  return 0; // return 0 if unable to get the time
}
/*-------- NTP code END ------*/



void configModeCallback (WiFiManager *myWiFiManager) {
  Serial.println("Entered config mode");
  Serial.println(WiFi.softAPIP());
  //if you used auto generated SSID, print it
  Serial.println(myWiFiManager->getConfigPortalSSID());
}

#define DBG_OUTPUT_PORT Serial

void handleFileList() {
  if(!http.hasArg("dir")) {http.send(500, "text/plain", "BAD ARGS"); return;}
  
  String path = http.arg("dir");
  DBG_OUTPUT_PORT.println("handleFileList: " + path);
  Dir dir = SPIFFS.openDir(path);
  path = String();

  String output = "[";
  while(dir.next()){
    File entry = dir.openFile("r");
    bool isDir = false;
    output += "{\"type\":\"";
    output += (isDir)?"dir":"file";
    output += "\",\"name\":\"";
    output += String(entry.name()).substring(1);
    output += "\",\"size\":" + String(entry.size());
    output += "},\n";
    entry.close();
  }

  FSInfo fs_info;
  SPIFFS.info(fs_info);
  output += "{totalBytes:" + String(fs_info.totalBytes);
  output += ",\nusedBytes:" + String(fs_info.usedBytes);
  output += ",\nblockSize:" + String(fs_info.blockSize);
  output += ",\npageSize:" + String(fs_info.pageSize);
  output += ",\nmaxOpenFiles:" + String(fs_info.maxOpenFiles);
  output += ",\nmaxPathLength:" + String(fs_info.maxPathLength);
  output += "}";

  output += "]";
  http.send(200, "text/json", output);
}

void OTASetup() {
  // Port defaults to 8266
  // ArduinoOTA.setPort(8266);

  // Hostname defaults to esp8266-[ChipID]
  // ArduinoOTA.setHostname("myesp8266");

  // No authentication by default
  ArduinoOTA.setPassword(update_password);

  ArduinoOTA.onStart([]() {
    Serial.println(String(millis()) + " OTA Start");
  });
  ArduinoOTA.onEnd([]() {
    Serial.println(String(millis()) + " \nOTA End");
    SPIFFS.end();
  });
  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.print(millis());
    Serial.printf(" OTA Progress: %u%%\r", (progress / (total / 100)));
  });
  ArduinoOTA.onError([](ota_error_t error) {
    Serial.print(millis());
    Serial.printf(" OTA Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("OTA Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("OTA Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("OTA Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("OTA Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("OTA End Failed");
  });
  ArduinoOTA.begin();
}


bool loadRoomMap() {
  if (!SPIFFS.exists("/roommap"))
    return false;
  File f = SPIFFS.open("/roommap", "r");
  if (!f)    
    return false;
  Serial.print(String(millis()) + " Loading roomMap from file ... ");
  const uint32_t _s = sizeof(uint32_t);
  uint32_t i = 0;
  uint8_t out[_s];
  while((uint32_t)f.available() >= _s && i < NUMPINS) {
    Serial.print("\nAvailable: " + String(f.available()) + " ");
    f.readBytes((char *) out, _s);
    Serial.printf("out: 0x%02x%02x%02x%02x ", out[3], out[2], out[1], out[0]);
    idxToRoomMap[i] = out[0] | (out[1] << 8) | (out[2] << 16) | (out[3] << 24);
    Serial.print(String(i) + " : " + String(idxToRoomMap[i]));
    i++;
  }
  f.close();
  Serial.println("\n" + String(millis()) + " Done");
  return true;
}
bool saveRoomMap() {
  File f = SPIFFS.open("/roommap", "w");
  if (!f)    
    return false;
  Serial.print(String(millis()) + " Saving roomMap to file ... ");
  for (uint32_t i = 0; i < NUMPINS; i++) {
    Serial.print("\n" + String(i) + " : " + String(idxToRoomMap[i]) + " ");
    f.write((byte) (idxToRoomMap[i] >>  0) & 0xff);
    f.write((byte) (idxToRoomMap[i] >>  8) & 0xff);
    f.write((byte) (idxToRoomMap[i] >> 16) & 0xff);
    f.write((byte) (idxToRoomMap[i] >> 24) & 0xff);
    Serial.printf("out: 0x%02x%02x%02x%02x",
      (idxToRoomMap[i] >> 24) & 0xff,
      (idxToRoomMap[i] >> 16) & 0xff,
      (idxToRoomMap[i] >>  8) & 0xff,
      (idxToRoomMap[i] >>  0) & 0xff);
  }
  f.close();
  Serial.println("\n" + String(millis()) + " Done");
  return true;
}


bool loadPasscode() {
  if (!SPIFFS.exists("/passcode"))
    return false;
  File f = SPIFFS.open("/passcode", "r");
  if (!f)    
    return false;
  Serial.print(String(millis()) + " Loading passcode from file ... ");
  passcode = f.readStringUntil('\n');
  passcode.replace("\n", "");
  f.close();
  Serial.println(passcode + " " + String(millis()) + " Done");
  return true;
}
bool savePasscode() {
  File f = SPIFFS.open("/passcode", "w");
  if (!f)    
    return false;
  Serial.print(String(millis()) + " Saving passcode " + passcode + " to file ... ");
  f.print(passcode + "\n");
  f.close();
  Serial.println(String(millis()) + " Done");
  return true;
}


bool loadPixelData() {
  if (!SPIFFS.exists("/pixelstate"))
    return false;
  File f = SPIFFS.open("/pixelstate", "r");
  if (!f)    
    return false;
  Serial.print(String(millis()) + " Loading pixeldata from file ... ");
  uint32_t i = 0;
  while(f.available() >=3 && i < NUMPIXELS) {
    uint32_t c = pixels.Color(f.read(), f.read(), f.read());
    Serial.printf("%06x\n", c);
    pixels.setPixelColor(i, c);
    i++;
  }
  f.close();
  Serial.println(String(millis()) + " Done");
  return true;
}
bool savePixelData() {
  File f = SPIFFS.open("/pixelstate", "w");
  if (!f)
    return false;
  Serial.print(String(millis()) + " Saving pixeldata to file ... ");
  for (uint32_t i = 0; i < NUMPIXELS; i++) {
    uint32_t c = pixels.getPixelColor(i);
    Serial.printf("%06x\n", c);
    f.write((byte)(c >>  8)); // r
    f.write((byte)(c >> 16)); // g
    f.write((byte)(c >>  0)); // b
  }
  Serial.println(String(millis()) + " Done");
  return true;
}

String timeString() {
  char out[20];
  sprintf(out, "%i-%02i-%02i %02i:%02i:%02i", year(), month(), day(), hour(), minute(), second());
  return String(out);
}

/*String ntpTimeString() {
  time_t ntpTime = getNtpTime();
  tmElements_t tm;
  breakTime(ntpTime, tm); 
  //time_t mkt = makeTime(tm);
  char out[20];
  sprintf(out, "%i-%02i-%02i %02i:%02i:%02i", year(ntpTime), tm.Month, tm.Day, tm.Hour, tm.Minute, tm.Second);
  return String(out);
}*/

void setupWifi() {
  //Local intialization. Once its business is done, there is no need to keep it around
  WiFiManager wifiManager;
  //reset settings - if reboot was done by external reset then reset settings GPIO16/RST pin
  //if (ESP.getResetInfoPtr()->reason == REASON_EXT_SYS_RST)
  //  wifiManager.resetSettings();

  //set callback that gets called when connecting to previous WiFi fails, and enters Access Point mode
  wifiManager.setAPCallback(configModeCallback);
  
  //fetches ssid and pass and tries to connect
  //if it does not connect it starts an access point
  //and goes into a blocking loop awaiting configuration
  if(!wifiManager.autoConnect()) {
    Serial.println(String(millis()) + " failed to connect and hit timeout");
    // todo schedule retry
  } 
}

void setupHttp() {
  http.on("/", HTTP_GET, [](){
    http.send(200, "text/html", "no index, try /all");
  });
  //list directory
  http.on("/list", HTTP_GET, handleFileList);
  //called when the url is not defined here
  //use it to load content from SPIFFS
  http.onNotFound([](){
    http.send(404, "text/plain", "FileNotFound " + http.uri());
  });

  //get heap status, analog input value and all GPIO statuses in one json call
  http.on("/all", HTTP_GET, [](){
    time_t cnow = now();
    String json = "{";
    json += "\"heap\":"+String(ESP.getFreeHeap());
    json += ",\n \"analog\":"+String(analogRead(A0));
    json += ",\n \"gpio\":"+String((uint32_t)(((GPI | GPO) & 0xFFFF) | ((GP16I & 0x01) << 16)), HEX);
    json += ",\n \"millis\":"+String(millis());
    json += ",\n \"time\":\""+timeString()+"\"";
    json += ",\n \"unix\":"+String(cnow);

    // https://github.com/esp8266/Arduino/blob/master/cores/esp8266/Esp.cpp#L364
    json += ",\n \"resetreason_nr\":"+String(ESP.getResetInfoPtr()->reason);
    json += ",\n \"resetreason\":\""+ESP.getResetReason()+"\"";
    json += ",\n \"resetinfo\":\""+ESP.getResetInfo()+"\"";

    json += ",\n \"rooms\":[";
    for (int i = 0; i < NUMPINS; i++) {
      if (i > 0) json += ",";
      json += "[" + String(i) + "," + String(toaPins[i]) + "," +
        String(idxToRoomMap[i]) + "," +
        String(prevState[i]) + "," + String(lastStateChange[i]) + "," +
        String(cnow - lastStateChange[i]) + "]";
    }
    json += "]";

    json += "}";
    http.send(200, "text/json", json);
    json = String();
  });

  http.on("/set", HTTP_GET, [](){
    String result;
    String _passcode = http.arg("passcode");
    _passcode.trim();
    if(_passcode.length() > 0) {
      passcode = _passcode;
      result += "Passcode changed new length " + String(passcode.length()) + "\n";
    }

    int _idx = http.arg("idx").toInt();
    int _id = http.arg("id").toInt();
    if (0 <= _idx && _idx < NUMPINS && _id > 0) {
      result += "Updating room id for index " + String(_idx) + " from old " + String(idxToRoomMap[_idx]) + " to new " + String(_id) + "\n";
      idxToRoomMap[_idx] = _id;
    }
    if(result.length() == 0) {
      http.send(500, "text/html", "No params given<br>"
        "use ?passcode= to change passcode<br>"
        "use ?idx=n&id=n to change the external id for internal pins<br>"
        "visit /save/passcode and /save/roommap respectively to actually save changes to persistent storage<br>");
      return;
    }
    
    Serial.print(String(millis()) + " " + result);
    http.send(200, "text/plain", result);
  });
  http.on("/px", HTTP_GET, [](){
    int r = http.arg("r").toInt();
    int g = http.arg("g").toInt();
    int b = http.arg("b").toInt();
    pixels.setPixelColor(http.arg("px").toInt(),
        pixels.Color(r,g,b));
    pixels.show();
    char out[7];
    sprintf(out, "%02x%02x%02x", r, g, b);
    http.send(200, "text/html", "<body bgcolor=#" + String(out) + " />");
  });

  http.on("/save/roommap", HTTP_GET, [](){
    http.send(saveRoomMap() ? 200 : 500, "text/plain", "");
  });
  http.on("/save/passcode", HTTP_GET, [](){
    http.send(savePasscode() ? 200 : 500, "text/plain", "");
  });

  http.on("/time", HTTP_GET, [](){
    http.send(200, "text/plain", timeString());
  });

}

void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);

  WiFi.printDiag(Serial);
  Serial.print(millis());
  Serial.printf(" Connecting to last saved %s\n", WiFi.SSID().c_str());

  setupWifi();
  
  //if you get here you have connected to the WiFi
  Serial.print(String(millis()) + " Connected, Free sketch size: ");
  Serial.println((ESP.getFreeSketchSpace() - 0x1000) & 0xFFFFF000);

  pixels.begin();

  SPIFFS.begin();

  DBG_OUTPUT_PORT.setDebugOutput(true);

  DBG_OUTPUT_PORT.println("");
  DBG_OUTPUT_PORT.print(String(millis()) + " Connected! IP address: ");
  DBG_OUTPUT_PORT.println(WiFi.localIP());
  
  setupHttp();

  OTASetup();
  httpUpdater.setup(&http, update_path, update_username, update_password);
  Serial.print(millis());
  Serial.printf(" HTTPUpdateServer ready! Open http://%s%s in your browser and login with username '%s' and password '%s'\n", WiFi.localIP().toString().c_str(), update_path, update_username, update_password);

  http.begin();
  DBG_OUTPUT_PORT.println(String(millis()) + " HTTP server started");
  setSyncProvider(getNtpTime);
  setSyncInterval(30 * 60);

  /* --- toa ---- */
  Serial.print(String(millis()));
  uint32_t roombase = (ESP.getChipId() & 0xffffff) << 8;
  Serial.printf(" roombase: %08x\n", roombase);
  // Ensure we are not used to something else
  //pinMode(3, FUNCTION3);
  for (int i = 0; i < NUMPINS; i++) {
    pinMode(toaPins[i], INPUT);
    prevState[i] = HIGH;
    lastStateChange[i] = 0;
    // predefine room map just in case
    idxToRoomMap[i] = roombase + i + 1;
    Serial.print(String(i) + " : " + String(idxToRoomMap[i]));
    Serial.printf(" %08x\n", idxToRoomMap[i]);
  }
  loadRoomMap();
  loadPasscode();
  /* --- /toa --- */
}

void postRoomChange(int rommidx) {
  // TODO schedule retry if there is any failure
  HTTPClient http;
  String url = "http://caspecooccupancy.azurewebsites.net/Rooms/" + String(idxToRoomMap[rommidx]) +
    "/Occupancies?isOccupied=" + (prevState[rommidx] ? "false" : "true") +
    "&lastchange=" + String(lastStateChange[rommidx]) +
    "&passcode=" + passcode;
  Serial.print(String(millis()) + " start http post " + url + " ...\n");
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int result = http.POST("empty");
  Serial.print(String(millis()) + " http post result " + String(result) + " ...\n");
  http.writeToStream(&Serial);
  Serial.print("\n" + String(millis()) + " .\n");
  http.end();
}

bool checkInput() {
  unsigned long curMillis = millis();
  time_t curTime = now();

  // get lock status LOW if occupied, or HIGH if free
  bool isFreeState[NUMPINS];
  bool allFree = true;
  bool noneFree = true;
  for (int i = 0; i < NUMPINS; i++) {
    isFreeState[i] = digitalRead(toaPins[i]);

    if (!isFreeState[i])
      allFree = false;

    if (isFreeState[i])
      noneFree = false;

    if (isFreeState[i] != prevState[i]) {
      // state changed      
      timeChanged = curMillis;
      prevState[i] = isFreeState[i];
      lastStateChange[i] = curTime;
    }
  }

  // Timed out, lower brightness
  bool timeout = (curMillis - timeChanged > TIMEOUT);
  uint32_t pixColor = allFree ?
    pixels.Color(0, timeout ? 1 : 150, 0) :
    (noneFree ? pixels.Color(255, 0, 0) :
    pixels.Color(timeout ? 4 : 64, timeout ? 4 : 64, 0) );

  if (pixColor != pixels.getPixelColor(0)) {
    pixels.setPixelColor(0, pixColor);
    pixels.show();
  }

  // full send on timeout
  bool doFullSend = curTime - fullSend > (5 * 60);
  if (timeChanged == curMillis || doFullSend) {
    Serial.print("\n\n" + String(millis()) + " do callback full: " + String(doFullSend) + " curTime: " + String(curTime) + " last fullsend: " + String(fullSend) + "\n");
    for (int i = 0; i < NUMPINS; i++) {
      if (doFullSend || lastStateChange[i] == curTime) {
        Serial.print(String(millis()) + " doing room update lastStateChange: " + String(lastStateChange[i]) + "\n");
        postRoomChange(i);
      }
    }
    Serial.print(String(millis()) + " do callback done full: " + String(doFullSend) + "\n");
    if (doFullSend) fullSend = curTime;
  }

  return timeChanged == curMillis;
}

unsigned long lastInputCheck = 0;

void loop() {
  http.handleClient();
  ArduinoOTA.handle();

  unsigned long cur = millis();
  if (cur < lastInputCheck || cur - lastInputCheck >= delayval) {
    checkInput();
    lastInputCheck = millis();
  }
}
