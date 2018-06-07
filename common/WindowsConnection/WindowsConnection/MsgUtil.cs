using System;
using System.Collections.Generic;
using System.Linq;

namespace Sintering
{
  public static class MsgUtil
  {

    static Dictionary<string, int> serviceCodes;
    static Dictionary<int, string> serviceCodesRev;

    static MsgUtil() {

      // loading the service_code dictionary
      Dictionary<string, object> serviceCodesTemp = Config.getConfig("service_code");
      if (serviceCodesTemp != null)
      {
        serviceCodes = serviceCodesTemp.ToDictionary(pair => pair.Key, pair => (int)pair.Value);
        serviceCodesRev = serviceCodes.ToDictionary(pair => pair.Value, pair => pair.Key);
      }
      else
      {
        Console.WriteLine("Unable to load service_code dictionary");
      }
    }

    #region Build Header helper
    // see messages.xlsx for individual message detail

    public static Header BuildHeader(int service_code)
    {
      return BuildHeader(service_code, service_code * 100);
    }

    public static Header BuildHeader(int service_code, int sub_code)
    {
      Header header = new Header
      {
        ServiceCode = service_code,
        SubCode = sub_code,        
      };

      return header;
    }

    public static Header BuildHeader(int service_code, int sub_code, string target_id)
    {
      return BuildHeader(service_code, sub_code, target_id, null, null, null);
    }

    public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1)
    {
      return BuildHeader(service_code, sub_code, target_id, data1, null, null);
    }

    public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1, string data2)
    {
      return BuildHeader(service_code, sub_code, target_id, data1, data2, null);
    }

    public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1, string data2, string data3)
    {
      Header header = BuildHeader(service_code, sub_code);
      header.ParamsInfo = new Params
      {
        TargetId = target_id,
        Data1 = data1,
        Data2 = data2,
        Data3 = data3,
      };

      return header;
    }

    #endregion

  }
}
