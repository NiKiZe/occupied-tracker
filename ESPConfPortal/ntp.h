
/*
 * Provides functions for getting time from NTP server
 */

#include <ESP8266WiFi.h>          //https://github.com/esp8266/Arduino - needed for WiFi.hostByName
#include <WiFiUdp.h>              // used by ntp (indirectly pulled in by OTA and others)

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

