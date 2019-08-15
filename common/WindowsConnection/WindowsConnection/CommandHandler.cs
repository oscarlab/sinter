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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;


namespace Sintering {
  class CommandHandler {
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Command");
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
        log.Error("Unable to load service_codes dictionary");
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
          log.InfoFormat("[Sinter recv] service code/sub_code = {0}/{1} ", sinter.HeaderNode.ServiceCode, sinter.HeaderNode.SubCode);
          if (serviceCodesRev.TryGetValue(sinter.HeaderNode.ServiceCode , out requested_service)) {
            invoking_method_name = "execute_" + requested_service.Trim();
            if (actuator.bPasscodeVerified == true 
                || requested_service.Equals(@"verify_passcode")) {
              invoking_method = type.GetMethod(invoking_method_name);
              if (invoking_method != null)
              {
                invoking_method.Invoke(actuator, new Sinter[] { sinter });
              }
              else
              {
                log.Error("invoke error: " + invoking_method_name + " doesn't exist");
              }
            }
            else{
              log.Warn("passcode not verified yet, ignore the msg!");
            }
          } else {
            log.Error("invoke error: " + sinter.HeaderNode.ServiceCode + " doesn't exist");
          }
          invoking_method = null;
        }
        catch (Exception ex) {
          log.Error(ex.Message);
        }
      }
    }
  }
}
