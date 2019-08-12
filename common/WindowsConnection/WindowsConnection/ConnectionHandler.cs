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
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Net.Security;

namespace Sintering {

  public class ConnectionHandler {
    public TcpClient clientSocket;
    public Stream networkStream; //SSL Implementation
    string clientId;
#if DEBUG
    string xmlFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//sinterxml.txt";
#endif

    XmlSerializer serializer = new XmlSerializer(typeof(Sinter));
    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
    XmlWriterSettings settings = new XmlWriterSettings() {
      Encoding = new UTF8Encoding(false) ,
      OmitXmlDeclaration = true ,
      Indent = true ,
    };

    BlockingCollection<Sinter> messageQueue;
    public ConnectionHandler(TcpClient clientSocket , string clientId, BlockingCollection<Sinter> messageQueue) {
      this.clientSocket = clientSocket;
      this.clientId = clientId;

      // get network stream for reading, writing
      networkStream = clientSocket.GetStream();

      // initiate shared message queue
      this.messageQueue = messageQueue;

      RequestedProcessId = 0;

      // initialize xml writer
      ns.Add("" , "");
      serializer.UnknownNode += new XmlNodeEventHandler(Serializer_UnknownNode);
      serializer.UnknownAttribute += new XmlAttributeEventHandler(Serializer_UnknownAttribute);

#if DEBUG
      using (StreamWriter sw = File.CreateText(xmlFilePath))
      {
         sw.WriteLine(DateTime.Now.ToLongTimeString());
      }
#endif

    }

    public ConnectionHandler(TcpClient clientSocket, string clientId, BlockingCollection<Sinter> messageQueue, SslStream sslStream)
    {
      this.clientSocket = clientSocket;
      this.clientId = clientId;

      // get network stream for reading, writing
      networkStream = sslStream;

      // initiate shared message queue
      this.messageQueue = messageQueue;

      RequestedProcessId = 0;

      // initialize xml writer
      ns.Add("", "");
      serializer.UnknownNode += new XmlNodeEventHandler(Serializer_UnknownNode);
      serializer.UnknownAttribute += new XmlAttributeEventHandler(Serializer_UnknownAttribute);
    }

    Thread ctThread;
    public void StartConnectionHandling() {
      ctThread = new Thread(ReceiveMessage);
      ctThread.Start();
    }

    public void StopConnectionHandling()
    {
      ShouldStop = true;
    }

    public int RequestedProcessId {get; set;}

    public void SendMessage(Sinter sinter) {
      using (MemoryStream ms = new MemoryStream()) {
        using (XmlWriter writer = XmlWriter.Create(ms , settings)) {

          if (_shouldStop) return;
          
          // add timestamp
          sinter.HeaderNode.Timestamp = DateTime.Now.ToShortTimeString();

          // add process id
          sinter.HeaderNode.Process = RequestedProcessId.ToString();
          
          // serialize
          serializer.Serialize(writer , sinter , ns);
        }
#if DEBUG
       try
       {
                    byte[] bytesToFile = ms.ToArray();
                    string filestring = Encoding.ASCII.GetString(bytesToFile);
                    using (StreamWriter sw = File.AppendText(xmlFilePath))
                    {
                        sw.WriteLine(filestring);
                    }
        }
        catch (Exception e)
        {
          Console.WriteLine("Exception: {0}", e);
          return;
        }
#endif
        
        try
        {
          networkStream.Write(ms.GetBuffer(), 0, (int)ms.Length);
        }
        catch (Exception e)
        {
          Console.WriteLine("Exception: {0}", e);
          this.StopConnectionHandling();
          return;
        }
        networkStream.Flush();

        // Debug statement
        Console.WriteLine("sent: " + (int)ms.Length + " bytes");
        Console.WriteLine("[Sinter sent] service code/sub_code = {0}/{1}", sinter.HeaderNode.ServiceCode, sinter.HeaderNode.SubCode);
      }

      // Debug statement
      //SaveMessageInTextFile(sinter);
    }

    public void SaveMessageInTextFile(Sinter sinter) {
      using (TextWriter WriteFileStream = new StreamWriter("messages.xml", true)) {
        serializer.Serialize(WriteFileStream, sinter);
      }        
    }

    public bool ShouldStop {
      set {
        _shouldStop = value;
      }
    }

    private volatile bool _shouldStop = false;

    private void ReceiveMessage() {
      byte [] bytesFrom = new byte [(clientSocket.ReceiveBufferSize + 10)];
      string dataFromClient = "";
      bool quit = false;
      int bytesRead = 0;

      try
      {
        while (!_shouldStop && clientSocket.Connected)
        {
          bytesRead = networkStream.Read(bytesFrom, 0, clientSocket.ReceiveBufferSize);

          quit = (bytesRead <= 0);
          if (quit) break;

          dataFromClient += Encoding.UTF8.GetString(bytesFrom, 0, bytesRead);

          string excess = "";
          ExtractSinterMessage(dataFromClient, out excess);
          dataFromClient = excess;
        }
      }
      catch (IOException ex)
      {
         Console.WriteLine(ex.Message);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
      
      // Terminate
      StopClient();
    }

    private void StopClient() {
      if (clientSocket.Connected) {
        networkStream.Close();
        clientSocket.Close();
      }
    }

    // XML headers and trailers
    private static string xmlHeader = "<sinter>";
    private static string xmlTrailer = "</sinter>";

    private void ExtractSinterMessage(string whole , out string excess) {
      int index = -1;
      whole = whole.Trim();

      while (!_shouldStop && whole.Length > 0) {
        // Check if XML Header is present
        if (!whole.StartsWith(xmlHeader)) {
          break;
        }

        // Check if XML trailer is also present
        index = whole.IndexOf(xmlTrailer);
        if (index == -1) {
          break;
        }

        // Both header and trailer are found -- all good
        string sinterXml = whole.Substring(0 , index + xmlTrailer.Length);
        whole = whole.Substring(index + xmlTrailer.Length);
        AddToMessageQueue(sinterXml);
        whole = whole.Trim();
      }
      excess = whole;
    }

    private void AddToMessageQueue(string xml) {

#if DEBUG
      using (StreamWriter sw = File.AppendText(xmlFilePath))
      {
            sw.WriteLine(xml);
      }
#endif
      using (XmlReader reader = XmlReader.Create(new StringReader(xml))) {
        Sinter sinter = (Sinter)serializer.Deserialize(reader);
        messageQueue.Add(sinter);
      }
    }

    // error handling
    protected void Serializer_UnknownNode(object sender , XmlNodeEventArgs e) {
      Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
    }

    protected void Serializer_UnknownAttribute(object sender , XmlAttributeEventArgs e) {
      XmlAttribute attr = e.Attr;
      Console.WriteLine("Unknown attribute " + attr.Name + "='" + attr.Value + "'");
    }
  }
}
