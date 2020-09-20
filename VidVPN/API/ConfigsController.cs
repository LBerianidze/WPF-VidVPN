using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidVPN.API
{
    public class ConfigsController
    {
        public List<Config> SavedConfigs = new List<Config>();
        public void Save()
        {
            string directoryName = AppDomain.CurrentDomain.BaseDirectory + "configs";
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            StreamWriter sw = new StreamWriter(directoryName + "\\configs.conf", false, Encoding.UTF8);
            serializer.Serialize(sw, SavedConfigs);
            sw.Close();
        }
        public void Load()
        {
            string directoryName = AppDomain.CurrentDomain.BaseDirectory + "configs\\configs.conf";
            if (!File.Exists(directoryName))
            {
                return;
            }
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            StreamReader sw = new StreamReader(directoryName, Encoding.UTF8);
            SavedConfigs = (List<Config>)serializer.Deserialize(sw, SavedConfigs.GetType());
            sw.Close();
        }
    }
    public class Config
    {
        public int ID { get; set; }
        public string FileName { get; set; }
        public int Version { get; set; }
        public double RunningSeconds { get; set; }
        private Stopwatch configRunTime = new Stopwatch();
        public Config(int id,string filename,int version)
        {
            this.ID = id;
            this.FileName = filename;
            this.Version = version;
        }
        public void Run()
        {
            //if (!configRunTime.IsRunning)
            {
                configRunTime.Reset();
                configRunTime.Start();
            }
        }
        public void Stop()
        {
            //if (configRunTime.IsRunning)
            {
                RunningSeconds += configRunTime.Elapsed.TotalSeconds;
                configRunTime.Reset();
            }
        }

        internal double GetRunningTime()
        {
            return /*RunningSeconds + */configRunTime.Elapsed.TotalSeconds;
        }
    }
}
