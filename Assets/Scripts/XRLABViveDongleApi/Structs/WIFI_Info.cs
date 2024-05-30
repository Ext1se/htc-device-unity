using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace VIVE_Trackers
{
    [Serializable]
    public struct WIFI_Info
    {
        public string host_ssid;// = "test_5G";
        public string host_passwd;// = "testtest";
        public string country;
        public int host_freq;// = 5240;

        static WIFI_Info Default => new WIFI_Info { host_ssid = "test_5G", host_passwd = "testtest", host_freq = 5240, country = "US" };

        internal static WIFI_Info Load()
        {
            if (File.Exists(Path.Combine(Application.persistentDataPath, "wifi_info.json")))
                return JsonConvert.DeserializeObject<WIFI_Info>(File.ReadAllText("wifi_info.json"));
            else return Default;
        }
        internal void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "wifi_info.json"), json);
        }
    }
}
