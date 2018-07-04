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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using WindowsScraper;
using Sintering;


namespace WindowsServer {

  public class SinterServer
  {
    private static int port = 6832;
    private static List<ClientHandler> clients = new List<ClientHandler>();
    private static TcpListener serverSocket;

    public static void StartServer()
    {

      serverSocket = new TcpListener(IPAddress.Any, port);
      TcpClient clientSocket = default(TcpClient);
      int counter = 0;

      serverSocket.Start();
      Console.WriteLine("Server Started");

      try
      {
        while (true)
        {
          //TcpClient NATSocket = new TcpClient("127.0.0.1", 14141);
          /* String message = "Hello Public Server, This is first packet from Scrapper for ist hole punch";
          Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
          NetworkStream stream = NATSocket.GetStream();

          // Send the message to the connected PublicServer. 
          stream.Write(data, 0, data.Length);
          Console.WriteLine("Sent: {0}", message);*/

          counter += 1;
          clientSocket = serverSocket.AcceptTcpClient();
          Console.WriteLine("Client No:" + counter + " started!");

          ClientHandler client = new ClientHandler(new WindowsScraper.WindowsScraper(), clientSocket, "" + counter);
          clients.Add(client);
        }
      }
      catch (SocketException e)
      {
        Console.WriteLine("SocketException: {0}", e);
      }
      catch (ArgumentException e)
      {
        Console.WriteLine("ArgumentException: {0}", e);
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
  }
}
