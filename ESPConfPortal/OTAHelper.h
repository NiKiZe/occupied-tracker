
#include <ArduinoOTA.h>
#include <ESP8266WebServer.h>
#include <ESP8266HTTPUpdateServer.h>

#include <FS.h> // Needed to cleanly shutdown SPIFFS

ESP8266HTTPUpdateServer httpUpdater(true);

const char* update_path = "/flashfw";
const char* update_username = "firmware";
const char* update_password = "update";

void OTASetup(ESP8266WebServer *server) {
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

  httpUpdater.setup(server, update_path, update_username, update_password);
  Serial.print(millis());
  Serial.printf(" HTTPUpdateServer ready! Open http://%s%s in your browser and login with username '%s' and password '%s'\n", WiFi.localIP().toString().c_str(), update_path, update_username, update_password);
}

