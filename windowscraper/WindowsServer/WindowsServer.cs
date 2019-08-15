/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using WindowsScraper;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Sintering;

namespace WindowsServer {

  public static class ServerConfiguration{
    public const int SERVER_PORT = 6832;
    public const string CERTIFICATE_FILE = @"SinterServer.pfx"; //The path of x.509 certificate file
    public const string DEFAULT_PASSCODE = "123456";
  }

  public class SinterServer
  {
    private static int port = ServerConfiguration.SERVER_PORT;
    private static List<ClientHandler> clients = new List<ClientHandler>();
    private static TcpListener serverSocket;
    private static X509Certificate serverCertificate = new X509Certificate2(ServerConfiguration.CERTIFICATE_FILE, ServerConfiguration.DEFAULT_PASSCODE, X509KeyStorageFlags.MachineKeySet);
    private static log4net.ILog log = log4net.LogManager.GetLogger("Server");
    public static void StartServer()
    {
      log4net.GlobalContext.Properties["LogFileName"] = Path.GetTempPath() + @"\sinterserver.log"; //log file path
      log4net.GlobalContext.Properties["XMLFileName"] = Path.GetTempPath() + @"\sinterserver.xml"; //xml log file path
      MsgUtil.StartLogger();

      SslStream sslStream = null; 
      serverSocket = new TcpListener(IPAddress.Any, port);
      TcpClient clientSocket = default(TcpClient);
      int counter = 0;
      WindowsScraper.WindowsScraper scraper = null;

#if !DEBUG
      Random rnd = new Random();
      string passcode = rnd.Next(1, 999999).ToString();
#else
      string passcode = ServerConfiguration.DEFAULT_PASSCODE;
#endif

      serverSocket.Start();
      log.Info("Sinter Scrapper Started");
      log.InfoFormat("Passcode = {0}", passcode);
      Console.WriteLine("\nLet the Sinter proxy client know this passcode : {0}", passcode);

      try
      {
        while (true)
        {

          counter += 1;
          clientSocket = serverSocket.AcceptTcpClient();
          sslStream = ProcessClient(clientSocket);
          if (sslStream == null)
          {
            log.Info("Client No:" + counter + " rejected!");
            clientSocket.Close();
          }
          else
          {
                        log.Info("Client No:" + counter + " connected!");
                        if (scraper == null)
                        {
                            scraper = new WindowsScraper.WindowsScraper(passcode);
                        }
                        else
                        {
                            scraper.execute_stop_scraping(); //restart scraper
                        }
                        
                        ClientHandler client = new ClientHandler(scraper, clientSocket, "" + counter, sslStream);
                        clients.Add(client);
           }
        }
      }
      catch (SocketException e)
      {
        log.Error("SocketException: {0}", e);
      }
      catch (ArgumentException e)
      {
        log.Error("ArgumentException: {0}", e);
      }
    }

    public static void StopServer()
    {
      serverSocket.Stop();
      foreach (ClientHandler client in clients)
      {
        client.StopHandling();
      }

      clients.Clear();
    }

    private static SslStream ProcessClient(TcpClient client)
    {
      // A client has connected. Create the 
      // SslStream using the client's network stream.
      SslStream sslStream = new SslStream(
          client.GetStream(), false);
      // Authenticate the server but don't require the client to authenticate.
      try
      {

        log.InfoFormat("[SSL] server cert was issued to {0} and is valid from {1} until {2}.",
        serverCertificate.Subject,
        serverCertificate.GetEffectiveDateString(),
        serverCertificate.GetExpirationDateString());

        sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, enabledSslProtocols:SslProtocols.Tls12, checkCertificateRevocation: true);

        // Display the properties and settings for the authenticated stream.
        log.InfoFormat("[SSL] Protocol: {0}", sslStream.SslProtocol);

        DisplaySecurityLevel(sslStream);
        //DisplaySecurityServices(sslStream);
        //DisplayCertificateInformation(sslStream);
        //DisplayStreamProperties(sslStream);
      }
      catch (Exception e)
      {
        log.ErrorFormat("Exception: {0}", e.Message);
        if (e.InnerException != null)
        {
          log.ErrorFormat("Inner exception: {0}", e.InnerException.Message);
        }
        log.Error("Authentication failed - closing the connection.");
        sslStream.Close();
        return null;
      }

      return sslStream;

    }

    /* development code - need to move to log file */
    static void DisplaySecurityLevel(SslStream stream)
    {
      log.InfoFormat("[SSL] Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
      log.InfoFormat("[SSL] Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
      log.InfoFormat("[SSL] Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
    }
    static void DisplaySecurityServices(SslStream stream)
    {
      log.InfoFormat("[SSL] Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
      log.InfoFormat("[SSL] IsSigned: {0}", stream.IsSigned);
      log.InfoFormat("[SSL] Is Encrypted: {0}", stream.IsEncrypted);
    }
    static void DisplayStreamProperties(SslStream stream)
    {
      log.InfoFormat("[SSL] Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
      log.InfoFormat("[SSL] Can timeout: {0}", stream.CanTimeout);
    }
    static void DisplayCertificateInformation(SslStream stream)
    {
      log.InfoFormat("[SSL] Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

      X509Certificate localCertificate = stream.LocalCertificate;
      if (stream.LocalCertificate != null)
      {
        log.InfoFormat("[SSL] Local cert was issued to {0} and is valid from {1} until {2}.",
            localCertificate.Subject,
            localCertificate.GetEffectiveDateString(),
            localCertificate.GetExpirationDateString());
      }
      else
      {
        log.InfoFormat("[SSL] Local certificate is null.");
      }
      // Display the properties of the client's certificate.
      X509Certificate remoteCertificate = stream.RemoteCertificate;
      if (stream.RemoteCertificate != null)
      {
        log.InfoFormat("[SSL] Remote cert was issued to {0} and is valid from {1} until {2}.",
            remoteCertificate.Subject,
            remoteCertificate.GetEffectiveDateString(),
            remoteCertificate.GetExpirationDateString());
      }
      else
      {
        log.InfoFormat("[SSL] Remote certificate is null.");
      }
    }
    /* development code - need to move to log file */
  }
}
