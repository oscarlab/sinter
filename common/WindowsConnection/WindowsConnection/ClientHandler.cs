using System.Collections.Concurrent;
using System.Net.Sockets;

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

    public void SendMessage(Sinter sinter) {
      connectionHandler.SendMessage(sinter);
    }

    public void StopHandling() {
      commandHandler.StopCommandHandling();
      connectionHandler.StopConnectionHandling();
    }
  }
}
