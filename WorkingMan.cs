using System;
using System.Globalization;
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
        private Plugin GUIAnnouncements;

        private DynamicConfigFile timeData;
        private PluginConfig config;
        private int dayWarningThreshold1 = 0;
        private int dayWarningThreshold2 = 0;
        private int weekWarningThreshold1 = 0;
        private int weekWarningThreshold2 = 0;
        private int WARNING1_INTERVAL = 300;
        private int WARNING2_INTERVAL = 60;

        [Command("workingman.reset")]
        private void ResetTimer(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string today = DateTime.Now.ToString("MM/dd/yyyy");
                    string week = WeekOfYear();
                    timeData[player.Id, today] = 0;
                    timeData[player.Id, week] = 0;
                    player.Message(string.Format("Your timer has been reset for {0} and week {1}", today, week));
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

        [Command("workingman.setdaytimer")]
        private void SetDayTimer(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string playerId = FindPlayerIDByName(args[0]);
                    string today = DateTime.Now.ToString("MM/dd/yyyy");

                    if(playerId != null){
                        timeData[playerId, today] = Int32.Parse(args[1]);
                        player.Message(string.Format("{0} day timer has been set to {1}", args[0], args[1]));
                    }
                    else
                        player.Message(string.Format("Could not find player with name {0}", args[0]));
                }
                catch{
                    player.Message("Error handling setdaytimer command");
                }
            }
            else
            {
                player.Message("This command is restricted to admins only");
            }
        }

        [Command("workingman.setweektimer")]
        private void SetWeekTimer(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string playerId = FindPlayerIDByName(args[0]);
                    string week = WeekOfYear();

                    if(playerId != null){
                        timeData[playerId, week] = Int32.Parse(args[1]);
                        player.Message(string.Format("{0} week timer has been set to {1} for the week of {2}", args[0], args[1], week));
                    }
                    else
                        player.Message(string.Format("Could not find player with name {0}", args[0]));
                }
                catch{
                    player.Message("Error handling setweektimer command");
                }
            }
            else
            {
                player.Message("This command is restricted to admins only");
            }
        }

        [Command("workingman.setdaylimit")]
        private void SetDayLimit(IPlayer player, string command, string[] args)
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

        [Command("workingman.setweeklimit")]
        private void SetWeekLimit(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    config.secondsPerWeek = Int32.Parse(args[0]);
                    SaveConfig();
                    ResetWarningThresholds();
                    player.Message(string.Format("Time limit has been set to {0} seconds per week", args[0]));
                }
                catch{
                    player.Message("Error handling setlimit command");
                }
            }
        }

        [Command("workingman.setweekstartday")]
        private void SetWeekStartDay(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    config.dayOfWeek = Int32.Parse(args[0]);
                    SaveConfig();
                    player.Message(string.Format("Week start day has been updated to {0}", args[0]));
                }
                catch{
                    player.Message("Error handling setweekstartday command");
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

        [Command("workingman.givetimeday")]
        private void GiveTimeDay(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string today = DateTime.Now.ToString("MM/dd/yyyy");
                    string playerId = FindPlayerIDByName(args[0]);
                    if(playerId != null){
                        timeData[playerId, today] = (int)timeData[playerId, today] - Int32.Parse(args[1]);
                        player.Message(string.Format("{0} has been given {1} seconds for {2}", args[0], args[1], today));
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

        [Command("workingman.givetimeweek")]
        private void GiveTimeWeek(IPlayer player, string command, string[] args)
        {
            if(player.IsAdmin)
            {
                try{
                    string week = WeekOfYear();
                    string playerId = FindPlayerIDByName(args[0]);
                    if(playerId != null){
                        timeData[playerId, week] = (int)timeData[playerId, week] - Int32.Parse(args[1]);
                        player.Message(string.Format("{0} has been given {1} seconds for {2}", args[0], args[1], week));
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
                string week = WeekOfYear();

                if(config.secondsPerDay > 0){
                    player.Message(string.Format("You have been playing for {0} in this 24-hour period ({1}), you have {2} left.", 
                        FormatTimeSpan((int)timeData[player.Id, today]), today, FormatTimeSpan(config.secondsPerDay - (int)timeData[player.Id, today])));
                    player.Message(string.Format("There is {0} remaining until the next day cycle begins.", FormatTimeSpan((long)TimeTilNextDayCycle().TotalSeconds)));
                }

                if(config.secondsPerWeek > 0){
                    player.Message(string.Format("You have been playing for {0} this week ({1}), you have {2} left.", 
                        FormatTimeSpan((int)timeData[player.Id, week]), week, FormatTimeSpan(config.secondsPerWeek - (int)timeData[player.Id, week])));
                    player.Message(string.Format("There is {0} remaining until the next week cycle begins.", FormatTimeSpan2((long)TimeTilNextWeekCycle().TotalSeconds)));
                }
            }
            catch{
                player.Message("Error handling checktimer command");
            }

        }

        private string WeekOfYear()
        {
            CultureInfo cul = CultureInfo.CurrentCulture;    

            int firstDayWeek = cul.Calendar.GetWeekOfYear(    
                 DateTime.Now,    
                 CalendarWeekRule.FirstDay,    
                 (System.DayOfWeek)config.dayOfWeek);

            return DateTime.Now.ToString("yyyy") + "-" + firstDayWeek.ToString();
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int) day - (int) start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        private TimeSpan TimeTilNextWeekCycle()
        {
            TimeSpan untilMidnight = GetNextWeekday(DateTime.Today, (System.DayOfWeek)config.dayOfWeek) - DateTime.Now;
            return untilMidnight;
        }

        private TimeSpan TimeTilNextDayCycle()
        {
            TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
            return untilMidnight;
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

        private string FormatTimeSpan2(long seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds( seconds );
            string answer = string.Format("{0:D2}d:{1:D2}h:{2:D2}m:{3:D2}s",
                t.Days, 
                t.Hours, 
                t.Minutes, 
                t.Seconds);
            return answer;
        }

        class PluginConfig
        {
            public long secondsPerDay { get; set; }
            public long secondsPerWeek { get; set; }
            public long warningThreshold1 { get; set; }
            public long warningThreshold2 { get; set; }
            public int  dayOfWeek { get; set; }
        }

        private void Init()
        {
            TimeZoneInfo.ClearCachedData();
            timeData = Interface.Oxide.DataFileSystem.GetDatafile("WorkingMan/timeData");
            config = Config.ReadObject<PluginConfig>();
            dayWarningThreshold1 = (int)config.secondsPerDay- (int)config.warningThreshold1;
            dayWarningThreshold2 = (int)config.secondsPerDay - (int)config.warningThreshold2;
            weekWarningThreshold1 = (int)config.secondsPerWeek - (int)config.warningThreshold1;
            weekWarningThreshold2 = (int)config.secondsPerWeek - (int)config.warningThreshold2;

            timer.Every(1f, UpdateLoop);
            timer.Every(10f, SaveLoop);
        }

        private void SaveLoop()
        {
            timeData.Save();
        }

        private void UpdateLoop()
        {
            int dayTime, weekTime;
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();
            List<string> kick = new List<string>();

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (timeData[player.UserIDString, today] == null)
                {
                    timeData[player.UserIDString, today] = 0;
                }

                if (timeData[player.UserIDString, week] == null)
                {
                    timeData[player.UserIDString, week] = 0;
                }

                dayTime = (int)timeData[player.UserIDString, today] + 1;
                timeData[player.UserIDString, today] = dayTime;

                weekTime = (int)timeData[player.UserIDString, week] + 1;
                timeData[player.UserIDString, week] = weekTime;

                if(config.secondsPerDay > 0)
                {
                    if(dayTime >= dayWarningThreshold2)
                    {
                        if(dayTime % WARNING2_INTERVAL == 0)
                        {
                            string msg = string.Format("WARNING: You have been playing for {0} in this 24-hour period ({1}), you have {2} left!", 
                            FormatTimeSpan(dayTime), today, FormatTimeSpan(config.secondsPerDay - dayTime));
                            player.ChatMessage(msg);
                            if(GUIAnnouncements != null)
                                GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                        }

                    }
                    else if(dayTime >= dayWarningThreshold1)
                    {
                        if(dayTime % WARNING1_INTERVAL == 0)
                        {
                            string msg = string.Format("WARNING: You have been playing for {0} in this 24-hour period ({1}), you have {2} left!", 
                            FormatTimeSpan(dayTime), today, FormatTimeSpan(config.secondsPerDay - dayTime));
                            player.ChatMessage(msg);
                            if(GUIAnnouncements != null)
                                GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                        }
                    }
                }

                if(config.secondsPerWeek > 0)
                {
                    if(weekTime >= weekWarningThreshold2)
                    {
                        if(weekTime % 60 == 0)
                        {
                            string msg = string.Format("WARNING: You have been playing for {0} this week ({1}), you have {2} left!", 
                            FormatTimeSpan(weekTime), week, FormatTimeSpan(config.secondsPerWeek - weekTime));
                            player.ChatMessage(msg);
                            GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                        }

                    }
                    else if(weekTime >= weekWarningThreshold1)
                    {
                        if(weekTime % 300 == 0)
                        {
                            string msg = string.Format("WARNING: You have been playing for {0} this week ({1}), you have {2} left!", 
                            FormatTimeSpan(weekTime), week, FormatTimeSpan(config.secondsPerWeek - weekTime));
                            player.ChatMessage(msg);
                            GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
                        }
                    }
                }

                if ((config.secondsPerDay > 0 && dayTime > config.secondsPerDay) || (config.secondsPerWeek > 0 && weekTime > config.secondsPerWeek)) {
                    kick.Add(player.UserIDString);
                }

            }

            foreach(string playerId in kick)
            {
                var player = covalence.Players.FindPlayer(playerId);
                player.Kick($"Played time exceeds limit");
            }
        }

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["secondsPerDay"] = 14400;
            Config["secondsPerWeek"] = 0;
            Config["warningThreshold1"] = 30 * 60;
            Config["warningThreshold2"] = 10 * 60;
            Config["dayOfWeek"] = 4; // Thursday
            LoadMyDefaultConfig();
            SaveConfig();
        }

        private object CanClientLogin(Network.Connection connection)
        {
            int dayCount, weekCount;
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();
            string id = connection.userid.ToString();

            dayCount = (int)timeData[id, today];
            weekCount = (int)timeData[id, week];
            Puts(string.Format("{0} attempting to connect with {1} day seconds and {2} week seconds", connection.username, dayCount, weekCount));
            if (config.secondsPerDay > 0 && dayCount != null && dayCount > config.secondsPerDay)
            {
                string error = string.Format("There is {0} remaining until the next day cycle begins.", FormatTimeSpan((long)TimeTilNextDayCycle().TotalSeconds));
                return error;
            }
            else if(config.secondsPerWeek > 0 && weekCount != null && weekCount > config.secondsPerWeek)
            {
                string error = string.Format("There is {0} remaining until the next week cycle begins.", FormatTimeSpan2((long)TimeTilNextWeekCycle().TotalSeconds));
                return error;
            }

            return true;
        }

        private void LoadMyDefaultConfig()
        {
            config.secondsPerDay = 14400;
            config.secondsPerWeek = 0;
            config.warningThreshold1 = 30 * 60;
            config.warningThreshold2 = 10 * 60;
            config.dayOfWeek = 4; // Thursday
            ResetWarningThresholds();
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void ResetWarningThresholds()
        {
            if((int)config.secondsPerDay > 0){
                dayWarningThreshold1 = (int)config.secondsPerDay - (int)config.warningThreshold1;
                dayWarningThreshold2 = (int)config.secondsPerDay - (int)config.warningThreshold2;
            }

            if((int)config.secondsPerWeek > 0){
                weekWarningThreshold1 = (int)config.secondsPerWeek - (int)config.warningThreshold1;
                weekWarningThreshold2 = (int)config.secondsPerWeek - (int)config.warningThreshold2;
            }
        }

    }
}
