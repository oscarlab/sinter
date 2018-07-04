# Sinter

* An Accessible, Cross-Platform Remote Desktop Protocol

**Warning: This is alpha software - it is released for testing and development purposes, but is not ready for end-user adoption.  Use at your own risk!**

## What is Sinter?

Sinter is an innovative platform for remote access that supports
screen readers and other assistive software.  Sinter is portable
across operating systems (currently Mac and Windows).  For example, a
Mac user can connect to a Windows remote desktop and read remote
Windows apps using VoiceOver (the default screen reader for Mac OS X).

Sinter currently supports applications including Windows Explorer, Mac Finder, Notepad/TextEdit, Calculator, and MS Word.
We plan to increase this list over time.

Currently, Sinter runs as a stand-alone application on the client and
server.  We eventually plan to make Sinter a plug-in for Remote
Desktop Protocol (RDP) and other remote-desktop systems.

## How to build Sinter?

Sinter consists of two major entities:
* **Scraper** - constructs the semantic hierarchy of UI elements representing an app in a *remote* Windows or Mac machine, and ships that hierarchy as an XML to the Proxy.
* **Proxy** - renders UI elements natively from the received XML in a *local* Window or Mac machine.

|Scenario| Remote   |  Local  |   Scraper       | Proxy          |
|:------:|:--------:|:-------:|:---------------:|:--------------:|
|       1| Windows  | Windows | `windowscraper` |`windowsproxy`  |
|       2| Windows  | Mac     | `windowscraper` |`osxproxy`      |
|       3| Mac      | Mac     | `osxscraper`     |`osxproxy`      |
|       4| Mac      | Windows | `osxscraper`     |`windowsproxy`  |

### Requirements for `windowscraper` and `windowsproxy`
* *Windows 7* or later
* *Visual Studio 2017* IDE or later
* *.Net Framework 4.6* or later
* *.Net desktop development* workload to build WPF, Windows Froms, and console applications using C#

### Building `windowscraper`
* Go to `sinter/windowscraper` directory and open the solution `WindowsScraper.sln` in Visual Studio IDE.
* After loading the solution, open `Solution Explorer` from the `View` menu.
* Build the entire solution by choosing `Build Solution` from `Build` menu.
* In `Solution Explorer`, right-click on `WindowsScraperTest` project as choose "Set as Startup Project", if it is not set already.

### Building `windowsproxy`
* Go to `sinter/windowsproxy` directory and open the solution `WindowsProxyClient.sln` in Visual Studio IDE.
* After loading the solution, open `Solution Explorer` from the `View` menu.
* Build the entire solution by choosing `Build Solution` from the `Build` menu.
* In `Solution Explorer`, right-click on `WindowsProxy` project and choose "Set as Startup Project", if it is not set already.

### Requirements for `osxscraper` and `osxproxy`

* *Mac OS 10.10* or later
* *Xcode 9.0* IDE or later

### Building `osxscraper`
* Go to `sinter/osxscraper` directory and open the project `OSXScraper.xcodeproj` in Xcode
* After loading the project in Xcode, choose `Build` from `Product` menu.

### Building `osxproxy`
* Go to `sinter/osxproxy` directory and open the project `OSXProxy.xcodeproj` in Xcode
* After loading the project in Xcode, choose `Build` from `Product` menu.

## How to run an application over Sinter?

* In your remote machine, run the scraper. If the remote machine is a Mac, choose  `osxscraper`;  if it is a Windows, then `windowscraper`. 
* In your local machine,  run the proxy ( `osxproxy` for Mac,  `windowscraper` for Windows). But before running the proxy, you need to provide the IP address of the remote machine in the config file (In `osxproxy`,  the name of the config file is `sinter/osxproxy/OSXProxy/Settings.plist`; in `windowscraper`, it is `sinter/windowsproxy/src/config.xml`).
* When the proxy application is running, click on `Connect` button to connect with the remote machine.
* After successful connection, you need to click on the `Load Processes` button to get a list of applications running in the remote machine. This list is rendered as a List-View in the proxy application.
* To run any of those remote application locally, double click on the application's name in the list.

## How is Sinter Licensed?

Sinter is available under a dual GPL/commercial license.  In other words,
anyone can use Sinter for free, so long as they share their code (ideally back to this project)
in accordance with the GPL.

Commercial licensing is also available; contact Don Porter <porter@cs.unc.edu> for more information.

## How to contribute to Sinter?

We would love your help!  To contribute code to the project, please
create pull request on the github page.

Note that we require all patches to be signed off by the author,
indicating that you are authorized to contribute this code to the
project.  By contributing code to this project, you are also agreeing
that your code may be distributed under _both_ the GPL and a
commercial license without compensation.

Pull Requests must pass our unit tests (CI coming soon), and be approved by
at least one maintainer before merging.

## Contact

For help, please file bug reports on the github page using the issue tracker.
        <https://github.com/oscarlab/sinter/issues>

We will also be adding additional documentation on the project wiki and website
over time.

This project is currently maintained by:
  - __Syed Masum Billah__ <sbillah@cs.stonybrook.edu>
  - __Don Porter__ <porter@cs.unc.edu>
