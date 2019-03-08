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

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.Security;

namespace Sintering
{
  public enum NodeType{
        Server,
        Client,
    }

    public class ClientHandler {

    ConnectionHandler connectionHandler;
    CommandHandler    commandHandler;

    private ConcurrentQueue<Sinter> _messageQueue = new ConcurrentQueue<Sinter>();
    BlockingCollection<Sinter> messageQueue = null;

    IWinCommands actuator;

    public ClientHandler(IWinCommands actuator, TcpClient client, string clientId) {
      messageQueue = new BlockingCollection<Sinter>(_messageQueue);
       this.actuator = actuator;
     
      connectionHandler = new ConnectionHandler(client , clientId, messageQueue);
      commandHandler = new CommandHandler(connectionHandler, messageQueue, this.actuator);

      this.actuator.connection = connectionHandler;

      connectionHandler.StartConnectionHandling();
      commandHandler.StartCommandHandling();
    }

    public ClientHandler(IWinCommands actuator, TcpClient client, string clientId, SslStream sslStream)
    {
      messageQueue = new BlockingCollection<Sinter>(_messageQueue);
      this.actuator = actuator;

      connectionHandler = new ConnectionHandler(client, clientId, messageQueue, sslStream);
      commandHandler = new CommandHandler(connectionHandler, messageQueue, this.actuator);

      this.actuator.connection = connectionHandler;

      connectionHandler.StartConnectionHandling();
      commandHandler.StartCommandHandling();
    }

    public void SendMessage(Sinter sinter) {
      connectionHandler.SendMessage(sinter);
    }

    public void StopHandling() {
      commandHandler.StopCommandHandling();
      connectionHandler.StopConnectionHandling();
    }
  }
}
