using System.Collections.Generic;
using PlistCS;

namespace Sintering
{
  public static class Config {
    private static Dictionary<string , object> configs, winConfig;
    private static bool isLoaded = false;

    public static void plParser() {
      string loc = "RoleMapping.plist";
      configs = (Dictionary<string , object>)Plist.readPlist(loc);

      loc = "Windows.plist";
      winConfig = (Dictionary<string, object>)Plist.readPlist(loc);

      isLoaded = true;
    }

    public static Dictionary<string, object> getConfig(string config_key) {
      if (!isLoaded)
        plParser();

      if (configs.TryGetValue(config_key, out object item))
        return (Dictionary<string, object>)item;

      return null;
    }

    public static Dictionary<string, object> getWinConfig(string config_key)
    {
      if (!isLoaded)
        plParser();

      if (winConfig.TryGetValue(config_key, out object item))
        return (Dictionary<string, object>)item;

      return null;
    }
  }
}
