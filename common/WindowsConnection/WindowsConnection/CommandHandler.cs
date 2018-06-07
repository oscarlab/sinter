using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sintering {
  class CommandHandler {
    Dictionary<string , object> serviceCodes;
    Dictionary<object , string> serviceCodesRev;
    Type type;

    ConnectionHandler connectionHandler;
    BlockingCollection<Sinter> messageQueue;

    IWinCommands actuator; // could be a server or a client

    public CommandHandler(ConnectionHandler connectionHandler , BlockingCollection<Sinter> messageQueue, IWinCommands actuator) {
      // load service-code key-values
      serviceCodes = Config.getConfig("service_code");
      if (serviceCodes != null) {
        serviceCodesRev = serviceCodes.ToDictionary(kp => kp.Value , kp => kp.Key);
      } else {
        Console.WriteLine("Unable to load service_codes dictionary");
      }
      //type = GetType();
      type = typeof(IWinCommands);

      this.connectionHandler = connectionHandler;
      this.messageQueue = messageQueue;
      this.actuator = actuator;
    }

    Thread exeThread;
    public void StartCommandHandling() {
      exeThread = new Thread(CommandExecutor);
      exeThread.Start();
    }

    public void StopCommandHandling()
    {
      ShouldStop = true;
    }

    public bool ShouldStop
    {
      set
      {
        _shouldStop = value;
      }
    }

    private volatile bool _shouldStop = false;

    private void CommandExecutor() {
      string requested_service, invoking_method_name;
      MethodInfo invoking_method;

      while (!_shouldStop) {
        try {
          Sinter sinter = messageQueue.Take();
          if (serviceCodesRev.TryGetValue(sinter.HeaderNode.ServiceCode , out requested_service)) {
            invoking_method_name = "execute_" + requested_service.Trim();
            invoking_method = type.GetMethod(invoking_method_name);
            if (invoking_method != null) {
              invoking_method.Invoke(actuator , new Sinter [] { sinter });
            } else {
              Console.WriteLine("invoke error: " + invoking_method_name + " doesn't exist");
            }
          } else {
            Console.WriteLine("invoke error: " + sinter.HeaderNode.ServiceCode + " doesn't exist");
          }
          invoking_method = null;
        }
        catch (Exception ex) {
          Console.WriteLine(ex.Message);
        }
      }
    }
  }
}
