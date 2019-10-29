# Sinter User Tutorial 

This tutorial introduces how to use Sinter Scraper and Proxy applications

## 0. Download the Sinter

Sinter Server: 

- Windows:  [SinterServer.msi](Rel/Latest/Server/SinterServer.msi)
- MacOS:    [SinterServer.dmg](Rel/Latest/Server/SinterServer.dmg)

Sinter Proxy:

- Windows:  [SinterProxy.msi](Rel/Latest/Proxy/SinterProxy.msi)
- MacOS:    [SinterProxy.dmg](Rel/Latest/Proxy/SinterProxy.dmg)


## 1. Install Scraper on Server

1. For Windows: execute [SinterServer.msi](Rel/Latest/Server/SinterServer.msi) to install the Scraper Server.

    The default installation folder is `C:\Program Files (x86)\Oscar Lab\Sinter Server\`

2. For macOS: execute [SinterServer.dmg](Rel/Latest/Server/SinterServer.dmg) 

## 2. Run Scraper on Server

1. For Windows: open the `WindowsScraper.exe` in installation folder, or

2. For Mac: open `SinterServer.app` on mac. 

    Since it is a development release, you might need to go to “Security & Privacy” setting in your mac to allow the application to run.

3. The Sinter Scraper starts. remember the passcode and give to the Proxy so it can connect from another machine. The passcode is randomly generated everytime when scraper starts.

4. The default port is 6832. 

    If you would like to configure the port for windows sinter, modify the port value in `Server_config.xml` in the installation folder.

5. Open the Calculator program

    __[Note] The current version only supports Calculator of windows 7. If you are using windows 10, you may need to first download the windows7 calculator and install on windows 10 first.__

6. The Scraper server is ready to be connected by proxy client. 


## 3. Install Sinter Proxy on client computer

1. For Windows: execute [SinterProxy.msi](Rel/Latest/Proxy/SinterProxy.msi) to install the Sinter Proxy.

    The default installation folder is `C:\Program Files (x86)\Oscar Lab\Sinter Proxy\`

2. For macOS: execute [SinterProxy.dmg](Rel/Latest/Proxy/SinterProxy.dmg)

## 4. Run Sinter Proxy

1. For Windows: open the `WindowsProxy.exe` in installation folder, or

2. For Mac: open `SinterProxy.app` on mac. 

    Since it is a development release, you might need to go to “Security & Privacy” setting in your mac to allow the application to run.

3. Fill the IP and Passcode of the Scraper Server, click 'Connect' 

4. Click 'Show Remote Applications'

5. Double click on the process name (ex. Calculator) to render

6. Now you can operate calculator from proxy side



