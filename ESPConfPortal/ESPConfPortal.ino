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

#include "FS.h"

#include <ArduinoOTA.h>
#include <ESP8266HTTPUpdateServer.h>

#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
#include <avr/power.h>
#endif

#define NEOPXPIN       0
#define NUMPIXELS      8
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, NEOPXPIN, NEO_GRB + NEO_KHZ800);

ESP8266WebServer http(80);
ESP8266HTTPUpdateServer httpUpdater(true);

const char* update_path = "/flashfw";
const char* update_username = "firmware";
const char* update_password = "update";

void configModeCallback (WiFiManager *myWiFiManager) {
  Serial.println("Entered config mode");
  Serial.println(WiFi.softAPIP());
  //if you used auto generated SSID, print it
  Serial.println(myWiFiManager->getConfigPortalSSID());
}

#define DBG_OUTPUT_PORT Serial

void handleFileList() {
  pixels.setPixelColor(7, pixels.Color(0,0,16));
  pixels.show();
  if(!http.hasArg("dir")) {http.send(500, "text/plain", "BAD ARGS"); return;}
  
  String path = http.arg("dir");
  DBG_OUTPUT_PORT.println("handleFileList: " + path);
  Dir dir = SPIFFS.openDir(path);
  path = String();

  String output = "[";
  while(dir.next()){
    File entry = dir.openFile("r");
    if (output != "[") output += ',';
    bool isDir = false;
    output += "{\"type\":\"";
    output += (isDir)?"dir":"file";
    output += "\",\"name\":\"";
    output += String(entry.name()).substring(1);
    output += "\",\"size\":" + entry.size();
    output += "}";
    entry.close();
  }

  FSInfo fs_info;
  SPIFFS.info(fs_info);
  output += "{totalBytes:" + String(fs_info.totalBytes);
  output += ",usedBytes:" + String(fs_info.usedBytes);
  output += ",blockSize:" + String(fs_info.blockSize);
  output += ",pageSize:" + String(fs_info.pageSize);
  output += ",maxOpenFiles:" + String(fs_info.maxOpenFiles);
  output += ",maxPathLength:" + String(fs_info.maxPathLength);
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
    Serial.println("OTA Start");
  });
  ArduinoOTA.onEnd([]() {
    Serial.println("\nOTA End");
    SPIFFS.end();
  });
  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.printf("OTA Progress: %u%%\r", (progress / (total / 100)));
  });
  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("OTA Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("OTA Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("OTA Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("OTA Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("OTA Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("OTA End Failed");
  });
  ArduinoOTA.begin();
}

bool loadPixelData() {
  if (!SPIFFS.exists("/pixelstate"))
    return false;
  File f = SPIFFS.open("/pixelstate", "r");
  if (!f)    
    return false;
  Serial.print("Loading pixeldata from file ... ");
  uint32_t i = 0;
  while(f.available() >=3 && i < NUMPIXELS) {
    uint32_t c = pixels.Color(f.read(), f.read(), f.read());
    char out[8];
    sprintf(out, "%06x\n", c);
    Serial.print(out);
    pixels.setPixelColor(i, c);
    i++;
  }
  f.close();
  Serial.println(" Done");
  return true;
}

bool savePixelData() {
  File f = SPIFFS.open("/pixelstate", "w");
  if (!f)
    return false;
  Serial.print("Saving pixeldata to file ... ");
  for (uint32_t i = 0; i < NUMPIXELS; i++) {
    uint32_t c = pixels.getPixelColor(i);
    char out[8];
    sprintf(out, "%06x\n", c);
    Serial.print(out);
    f.write((byte)(c >>  8)); // r
    f.write((byte)(c >> 16)); // g
    f.write((byte)(c >>  0)); // b
  }
  Serial.println(" Done");
  return true;
}

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
    Serial.println("failed to connect and hit timeout");
    // todo schedule retry
  } 
}

void setupHttp() {
  http.on("/", HTTP_GET, [](){
    http.send(200, "text/html", "result");
  });
  //list directory
  http.on("/list", HTTP_GET, handleFileList);
  //called when the url is not defined here
  //use it to load content from SPIFFS
  http.onNotFound([](){
    pixels.setPixelColor(7, pixels.Color(64,0,0));
    pixels.show();
    http.send(404, "text/plain", "FileNotFound " + http.uri());
  });

  //get heap status, analog input value and all GPIO statuses in one json call
  http.on("/all", HTTP_GET, [](){
    pixels.setPixelColor(7, pixels.Color(0,16,0));
    pixels.show();
    String json = "{";
    json += "\"heap\":"+String(ESP.getFreeHeap());
    json += ",\n \"analog\":"+String(analogRead(A0));
    json += ",\n \"gpio\":"+String((uint32_t)(((GPI | GPO) & 0xFFFF) | ((GP16I & 0x01) << 16)), HEX);
    json += ",\n \"time\":\""+timeString()+"\"";

    // https://github.com/esp8266/Arduino/blob/master/cores/esp8266/Esp.cpp#L364
    json += ",\n \"resetreason_nr\":"+String(ESP.getResetInfoPtr()->reason);
    json += ",\n \"resetreason\":"+ESP.getResetReason();
    json += ",\n \"resetinfo\":\""+ESP.getResetInfo()+"\"";
    json += "}";
    http.send(200, "text/json", json);
    json = String();
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

  http.on("/save", HTTP_GET, [](){
    //SPIFFS.end();
    http.send(savePixelData() ? 200 : 500, "text/plain", "");
  });

}

void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);

  WiFi.printDiag(Serial);
  Serial.printf("Connecting to last saved %s\n", WiFi.SSID().c_str());

  setupWifi();
  
  Serial.print("Free sketch size: ");
  Serial.println((ESP.getFreeSketchSpace() - 0x1000) & 0xFFFFF000);
  
  //if you get here you have connected to the WiFi
  Serial.println("Connected");

  pixels.begin();

  SPIFFS.begin();
  if (!loadPixelData()) {
    Serial.println("No pixeldata, using default");
    pixels.setPixelColor(0, pixels.Color(16,16,16));
    pixels.setPixelColor(2, pixels.Color(16,16,16));
    pixels.setPixelColor(4, pixels.Color(16,16,16));
    pixels.setPixelColor(6, pixels.Color(16,16,16));
  }
  pixels.show();

  DBG_OUTPUT_PORT.setDebugOutput(true);

  DBG_OUTPUT_PORT.println("");
  DBG_OUTPUT_PORT.print("Connected! IP address: ");
  DBG_OUTPUT_PORT.println(WiFi.localIP());
  
  setupHttp();

  OTASetup();
  httpUpdater.setup(&http, update_path, update_username, update_password);
  Serial.printf("HTTPUpdateServer ready! Open http://%s%s in your browser and login with username '%s' and password '%s'\n", WiFi.localIP().toString().c_str(), update_path, update_username, update_password);

  http.begin();
  DBG_OUTPUT_PORT.println("HTTP server started");
}

void loop() {
  // put your main code here, to run repeatedly:
  http.handleClient();
  ArduinoOTA.handle();
}
