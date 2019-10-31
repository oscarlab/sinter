# Sinter Developer Note

This tutorial documents some configuration and setup for developing Sinter. 


## Config for Windows instances

### 1. `Scraper_config.xml` under `windowscraper/WindowsScraperLib/`

By default, only Calculator is supported. The support list is configured by `program_type`.
Currently Metro type applications (windows 10 Calculator) is not supported. 

```
<scraper program_type="Calculator calc1 calc" metro_support="false" ></scraper>
```


### 2. `Server_config.xml` under `windowscraper/WindowsServer/`

Port and log file name/location can be changed as wish:
```
<server port="6832" logfolder="%TEMP%" logfile="sinterserver.log" xml_logfile="sinterserver.xml" cert="SinterServer.pfx"></server>
```

There is a default self-signed certificate file included. 

The openssl commands to generate the self-signed certificate file: (use password '123456' when prompt)
```
openssl req -x509 -sha256 -nodes -days 1095 -newkey rsa:4096 -keyout privateKey.key -out certificate.crt
openssl pkcs12 -export -out SinterServer.pfx -inkey privateKey.key -in certificate.crt
```

### 3. `proxyconfig.xml` under `windowsproxy/src/`

Default Server IP/Port and log file name/location can be changed as wish:
```
<proxy server_ip="192.168.125.203" server_port="6832" logfolder="%TEMP%" logfile="sinterproxy.log" xml_logfile="sinterproxy.xml"></proxy>
```


### 4. `log4net.config` under `common/WindowsConnection/WindowsConnection`

logger level can be configured for Console/FileAppender


&nbsp;
&nbsp;
## Config for macOS instances

### 1.`Settings.plist` under `osxscraper/OSXScraper/`

```
<dict>
    <key>support_apps</key>
    <array>
        <string>Calculator</string>
    </array>
    <key>port</key>
    <integer>6832</integer>
    <key>default_passcode</key>
    <integer>123456</integer>
    <key>certificate_filename</key>
    <string>osxsinter.p12</string>
    <key>certificate_passcode</key>
    <string>123456</string>
</dict>
```

To update certificate file when needed, 
use password '123456' when prompt or modify the value in Settings.plist

The openssl commands to generate the self-signed certificate file: 
```
openssl req -x509 -sha256 -nodes -days 1095 -newkey rsa:4096 -keyout privatekey.pem -out certificate.pem
openssl pkcs12 -inkey privatekey.pem -in certificate.pem -export -out osxsinter.p12
```

### 2.`Settings.plist` under `osxproxy/OSXProxy/`

Default Server IP/Port can be changed as wish:

```
<dict>
    <key>port</key>
    <real>6832</real>
    <key>server_ip</key>
    <string>192.168.125.191</string>
</dict>
```
