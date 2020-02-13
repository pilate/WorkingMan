using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Working Man", "pilate/mothball187", "0.0.1")]
    [Description("Limit playtime per user")]

    class WorkingMan : CovalencePlugin
    {
        [PluginReference]
        Plugin GUIAnnouncements;

        private DynamicConfigFile timeData;
        private PluginConfig config;
        private int warningThreshold1, warningThreshold2;

        [Command("workingman.reset")]
        private void ResetTimer(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string today = DateTime.Now.ToString("MM/dd/yyyy");
                    timeData[player.Id, today] = 0;
                    player.Message(string.Format("Your timer has been reset for {0}", today));
                }
                catch{
                    player.Message("Error handling reset command");
                }
            }
            else
            {
                player.Message("This command is restricted to admins only");
            }
        }

        [Command("workingman.settimer")]
        private void SetTimer(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string playerId = FindPlayerIDByName(args[0]);
                    string today = DateTime.Now.ToString("MM/dd/yyyy");

                    /*
                    if(Int32.TryParse(args[0]))
                        playerId = args[0];
                    else{
                        
                        foreach (var player in BasePlayer.activePlayerList){
                            if(String.Compare(player.Name, args[0]) == 0)
                            {
                                playerId = player.UserIDString;
                                break;
                            }

                        }
                        
                        
                    }
                    */

                    if(playerId != null){
                        timeData[playerId, today] = Int32.Parse(args[1]);
                        player.Message(string.Format("{0} timer has been set to {1}", args[0], args[1]));
                    }
                    else
                        player.Message(string.Format("Could not find player with name {0}", args[0]));
                }
                catch{
                    player.Message("Error handling settimer command");
                }
            }
            else
            {
                player.Message("This command is restricted to admins only");
            }
        }

        [Command("workingman.setlimit")]
        private void SetTimeLimit(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    config.secondsPerDay = Int32.Parse(args[0]);
                    SaveConfig();
                    ResetWarningThresholds();
                    player.Message(string.Format("Time limit has been set to {0} seconds per day", args[0]));
                }
                catch{
                    player.Message("Error handling setlimit command");
                }
            }
        }

        [Command("workingman.resetdefaults")]
        private void ResetDefaults(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                LoadMyDefaultConfig();
                SaveConfig();
                player.Message("Default config reloaded");
            }
        }

        [Command("workingman.setwarn1")]
        private void SetWarningThreshold1(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    config.warningThreshold1 = Int32.Parse(args[0]);
                    SaveConfig();
                    ResetWarningThresholds();
                    player.Message(string.Format("Warning threshold 1 has been set to {0} seconds", args[0]));
                }
                catch{
                    player.Message("Error handling setwarn1 command");
                }
            }
        }

        [Command("workingman.setwarn2")]
        private void SetWarningThreshold2(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    config.warningThreshold2 = Int32.Parse(args[0]);
                    SaveConfig();
                    ResetWarningThresholds();
                    player.Message(string.Format("Warning threshold 2 has been set to {0} seconds", args[0]));
                }
                catch{
                    player.Message("Error handling setwarn2 command");
                }
            }
        }

        [Command("workingman.givetime")]
        private void GiveTime(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string today = DateTime.Now.ToString("MM/dd/yyyy");
                    string playerId = FindPlayerIDByName(args[0]);
                    if(playerId != null){
                        timeData[playerId, today] = (int)timeData[playerId, today] - Int32.Parse(args[1]);
                        player.Message(string.Format("{0} has been given {1} seconds", args[0], args[1]));
                    }
                    else
                        player.Message(string.Format("Could not find player with name {0}", args[0]));
                }
                catch{
                    player.Message("Error handling givetime command");
                }
            }
            else
            {
                player.Message("This command is restricted to admins only");
            }
        }

        [Command("checktimer")]
        private void CheckTimer(IPlayer player, string command, string[] args)
        {
            try{
                string today = DateTime.Now.ToString("MM/dd/yyyy");
                player.Message(string.Format("You have been playing for {0} in this 24-hour period ({1}), you have {2} left.", 
                    FormatTimeSpan((int)timeData[player.Id, today]), today, FormatTimeSpan(config.secondsPerDay - (int)timeData[player.Id, today])));
            }
            catch{
                player.Message("Error handling checktimer command");
            }

        }

        private string FindPlayerIDByName(string name)
        {
            ulong userID;
            if (name.Length == 17 && ulong.TryParse(name, out userID))
                return name;

            var players = covalence.Players.FindPlayers(name).ToList();
            if(players.Count > 1)
            {
                return null;
            }

            if(players.Count == 1)
            {
                return players[0].Id;
            }

            return null;
        }

        private string FormatTimeSpan(long seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds( seconds );
            string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", 
                t.Hours, 
                t.Minutes, 
                t.Seconds);
            return answer;
        }

        class PluginConfig
        {
            public long secondsPerDay { get; set; }
            public long warningThreshold1 { get; set; }
            public long warningThreshold2 { get; set; }
        }

        private void Init()
        {
            timeData = Interface.Oxide.DataFileSystem.GetDatafile("WorkingMan/timeData");
            //timeData.Clear();
            //timeData.Save();
            config = Config.ReadObject<PluginConfig>();
            warningThreshold1 = (int)Config["secondsPerDay"] - (int)Config["warningThreshold1"];
            warningThreshold2 = (int)Config["secondsPerDay"] - (int)Config["warningThreshold2"];

            timer.Every(1f, UpdateLoop);
            timer.Every(10f, SaveLoop);
            timer.Every(60f, Warn2Loop);
            timer.Every(300f, Warn1Loop);
        }

        private void SaveLoop()
        {
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            timeData.Save();
        }

        private void UpdateLoop()
        {
            int curTime;
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            Dictionary<string, int> kick = new Dictionary<string, int>();

            //foreach (var player in players.Connected)
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (timeData[player.UserIDString, today] == null)
                {
                    timeData[player.UserIDString, today] = 0;
                }

                curTime = (int)timeData[player.UserIDString, today] + 1;
                timeData[player.UserIDString, today] = curTime;

                if (curTime > config.secondsPerDay) {
                    kick[player.UserIDString] = curTime;
                }

            }

            foreach(KeyValuePair<string, int> kvp in kick)
            {
                var player = covalence.Players.FindPlayer(kvp.Key);
                player.Kick($"Played time exceeds limit of {config.secondsPerDay} seconds within a 24-hour period");
            }
        }

        //TODO: see about having the warning be a banner or something more prominent in addition to the message
        private void Warn2Loop()
        {
            int curTime;
            string today = DateTime.Now.ToString("MM/dd/yyyy");

            //foreach (var player in players.Connected)
            foreach (var player in BasePlayer.activePlayerList)
            {
                curTime = (int)timeData[player.UserIDString, today];
                if(curTime == null)
                    curTime = 0;

                if(curTime >= warningThreshold2){
                    string msg = string.Format("WARNING: You have been playing for {0} in this 24-hour period ({1}), you have {2} left!", 
                        FormatTimeSpan(curTime), today, FormatTimeSpan(config.secondsPerDay - curTime));
                    player.ChatMessage(msg);
                    GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                }
            }
        }

        private void Warn1Loop()
        {
            int curTime;
            string today = DateTime.Now.ToString("MM/dd/yyyy");

            //foreach (var player in players.Connected)
            foreach (var player in BasePlayer.activePlayerList)
            {
                curTime = (int)timeData[player.UserIDString, today];
                if(curTime == null)
                    curTime = 0;

                if(curTime >= warningThreshold1 && curTime < warningThreshold2){
                    string msg = string.Format("WARNING: You have been playing for {0} in this 24-hour period ({1}), you have {2} left!", 
                        FormatTimeSpan(curTime), today, FormatTimeSpan(config.secondsPerDay - curTime));
                    player.ChatMessage(msg);
                    GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["secondsPerDay"] = 14400;
            Config["warningThreshold1"] = 30 * 60;
            Config["warningThreshold2"] = 10 * 60;
            ResetWarningThresholds();
        }

        private void LoadMyDefaultConfig()
        {
            config.secondsPerDay = 14400;
            config.warningThreshold1 = 30 * 60;
            config.warningThreshold2 = 10 * 60;
            ResetWarningThresholds();
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void ResetWarningThresholds()
        {
            warningThreshold1 = (int)Config["secondsPerDay"] - (int)Config["warningThreshold1"];
            warningThreshold2 = (int)Config["secondsPerDay"] - (int)Config["warningThreshold2"];
        }

        // private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        // void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

    }
}
