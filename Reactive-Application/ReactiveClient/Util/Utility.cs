using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveClient.Util
{
    public class Utility
    {
        public static int GetSettingValue(string settingName)
        {
            int retValue = 0;
            if(!string.IsNullOrEmpty(settingName))
            {
                string settingStr = ConfigurationManager.AppSettings[settingName];
                int outValue = 0;
                if (int.TryParse(settingStr, out outValue))
                {
                    retValue = outValue;
                }
            }
            return retValue;
        }
        
    }
}
