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
using System.Linq;
using System.IO;
using log4net.Config;

namespace Sintering
{
    public static class States
    {
        public const uint DISABLED = 0x1;
        public const uint SELECTED = 0x2;
        public const uint FOCUSED = 0x4;
        public const uint PRESSED = 0x8;
        public const uint CHECKED = 0x10;
        public const uint MIXED = 0x20;
        public const uint READONLY = 0x40;
        public const uint DEFAULT = 0x100;
        public const uint EXPANDED = 0x200;
        public const uint COLLAPSED = 0x400;
        public const uint BUSY = 0x800;
        public const uint INVISIBLE = 0x8000;
        public const uint OFFSCREEN = 0x10000; //65536
        public const uint FOCUSABLE = 0x100000; //1048576
        public const uint SELECTABLE = 0x200000;
        public const uint LINKED = 0x400000;
        public const uint VISITED = 0x800000;
        public const uint PROTECTED = 0x20000000;
        public const uint HASPOPUP = 0x40000000;
    }

    public static class MsgUtil
    {

        static Dictionary<string, int> serviceCodes;
        static Dictionary<int, string> serviceCodesRev;
        static log4net.ILog log = log4net.LogManager.GetLogger("MsgUtil");

        static MsgUtil()
        {

            FileInfo fi1 = new FileInfo("log4net.config");
            XmlConfigurator.Configure(fi1);

            // loading the service_code dictionary
            Dictionary<string, object> serviceCodesTemp = Config.getConfig("service_code");
            if (serviceCodesTemp != null)
            {
                serviceCodes = serviceCodesTemp.ToDictionary(pair => pair.Key, pair => (int)pair.Value);
                serviceCodesRev = serviceCodes.ToDictionary(pair => pair.Value, pair => pair.Key);
                log.Debug("Successfully load serviceCodes");
            }
            else
            {
                log.Error("Unable to load service_code dictionary");
            }
        }

        public static void StartLogger()
        {

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
            return BuildHeader(service_code, sub_code, target_id, null, null, null, null);
        }

        public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1)
        {
            return BuildHeader(service_code, sub_code, target_id, null, data1, null, null);
        }

        public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1, string data2)
        {
            return BuildHeader(service_code, sub_code, target_id, null, data1, data2, null);
        }

        public static Header BuildHeader(int service_code, int sub_code, string target_id, string data1, string data2, string data3)
        {
            return BuildHeader(service_code, sub_code, target_id, null, data1, data2, data3);
        }

        public static Header BuildHeader(int service_code, int sub_code, string target_id, List<string[]> target_id_list, string data1, string data2, string data3)
        {
            Header header = BuildHeader(service_code, sub_code);
            header.ParamsInfo = new Params
            {
                TargetId = target_id,
                TargetIdList = target_id_list,
                Data1 = data1,
                Data2 = data2,
                Data3 = data3,
            };

            return header;
        }
        #endregion

    }
}
