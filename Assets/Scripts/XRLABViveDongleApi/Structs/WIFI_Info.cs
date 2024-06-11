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
#if UNITY_ANDROID
            if (File.Exists(Path.Combine(Application.persistentDataPath, "wifi_info.json")))
            {
                var json = File.ReadAllText(Path.Combine(Application.persistentDataPath, "wifi_info.json"));
                return JsonConvert.DeserializeObject<WIFI_Info>(json); 
            }
#else
            if (File.Exists("wifi_info.json"))
            {
                var json = File.ReadAllText("wifi_info.json");
                return JsonConvert.DeserializeObject<WIFI_Info>(json);
            }
#endif
            else return Default;
        }
        internal void Save()
        {
            var json = JsonConvert.SerializeObject(this);
#if UNITY_ANDROID
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "wifi_info.json"), json); 
#else
            File.WriteAllText("wifi_info.json", json);
#endif
        }
    }
}
