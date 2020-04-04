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
    [Info("Working Man", "pilate/mothball187", "0.0.4")]
    [Description("Limit playtime per user")]

    class WorkingMan : CovalencePlugin
    {
        [PluginReference]
        private Plugin GUIAnnouncements, TimeOfDay;

        private DynamicConfigFile timeData;
        private PluginConfig config;
        private int dayWarningThreshold1 = 0;
        private int dayWarningThreshold2 = 0;
        private int weekWarningThreshold1 = 0;
        private int weekWarningThreshold2 = 0;
        private int WARNING1_INTERVAL = 5;
        private int WARNING2_INTERVAL = 1;
        private bool countTime = true;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Reset"] = "Your timer has been reset for {0} and week {1}",
                ["ResetError"] = "Error handling reset command",
                ["SetDayTimer"] = "{0} day timer has been set to {1}",
                ["SetDayTimerError"] = "Could not find player with name {0}",
                ["SetDayTimerError2"] = "Error handling setdaytimer command",
                ["SetWeekTimer"] = "{0} week timer has been set to {1} minutes for the week of {2}",
                ["SetWeekTimerError"] = "Could not find player with name {0}",
                ["SetWeekTimerError2"] = "Error handling setweektimer command",
                ["SetDayLimit"] = "Time limit has been set to {0} minutes per day",
                ["SetDayLimitError"] = "Error handling setdaylimit command",
                ["SetWeekLimit"] = "Time limit has been set to {0} minutes per week",
                ["SetWeekLimitError"] = "Error handling setweeklimit command",
                ["SetWeekStartDay"] = "Week start day has been updated to {0}",
                ["SetWeekStartDayError"] = "Error handling setweekstartday command",
                ["SetTimeNights"] = "Time nights option set to {0}",
                ["SetTimeNightsError"] = "Error handling settimenights command",
                ["ResetDefaults"] = "Default config reloaded",
                ["SetWarn1"] = "Warning threshold 1 has been set to {0} minutes",
                ["SetWarn1Error"] = "Error handling setwarn1 command",
                ["SetWarn2"] = "Warning threshold 2 has been set to {0} minutes",
                ["SetWarn2Error"] = "Error handling setwarn2 command",
                ["GiveTimeDay"] = "{0} has been given {1} minutes for {2}",
                ["GiveTimeDayError"] = "Could not find player with name {0}",
                ["GiveTimeDayError2"] = "Error handling givetimeday command",
                ["GiveTimeWeek"] = "{0} has been given {1} minutes for week {2}",
                ["GiveTimeWeekError"] = "Could not find player with name {0}",
                ["GiveTimeWeekError2"] = "Error handling givetimeweek command",
                ["CheckTimer1"] = "You have been playing for {0} in this 24-hour period ({1}), you have {2} left.",
                ["CheckTimer2"] = "There is {0} remaining until the next day cycle begins.",
                ["CheckTimer3"] = "You have been playing for {0} this week ({1}), you have {2} left.",
                ["CheckTimer4"] = "There is {0} remaining until the next week cycle begins.",
                ["CheckTimerError"] = "Error handling checktimer command",
                ["PlayerDayWarning"] = "WARNING: You have been playing for {0} in this 24-hour period ({1}), you have {2} left!",
                ["PlayerWeekWarning"] = "WARNING: You have been playing for {0} this week ({1}), you have {2} left!",
                ["Kick"] = "Played time exceeds limit",
                ["LoginDay"] = "There is {0} remaining until the next day cycle begins.",
                ["LoginWeek"] = "There is {0} remaining until the next week cycle begins."
            }, this);
        }

        [Command("workingman.reset")]
        private void ResetTimer(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();
            timeData[player.Id, today] = 0;
            timeData[player.Id, week] = 0;
            player.Message(string.Format(lang.GetMessage("Reset", this, player.Id), today, week));
           
        }

        [Command("workingman.setdaytimer")]
        private void SetDayTimer(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            string playerId = players.FindPlayer(args[0]).Id;
            string today = DateTime.Now.ToString("MM/dd/yyyy");

            if(playerId != null){
                Int32 time;
                if(!Int32.TryParse(args[1], out time))
                {
                    player.Message(lang.GetMessage("SetDayTimerError2", this, player.Id));
                    return;
                }

                timeData[playerId, today] = time;
                player.Message(string.Format(lang.GetMessage("SetDayTimer", this, player.Id), args[0], args[1]));
            }
            else
                player.Message(string.Format(lang.GetMessage("SetDayTimerError", this, player.Id), args[0]));

        }

        [Command("workingman.setweektimer")]
        private void SetWeekTimer(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            string playerId = players.FindPlayer(args[0]).Id;
            string week = WeekOfYear();

            if(playerId != null){
                Int32 time;
                if(!Int32.TryParse(args[1], out time))
                {
                    player.Message(lang.GetMessage("SetWeekTimerError2", this, player.Id));
                    return;
                }

                timeData[playerId, week] = time;
                player.Message(string.Format(lang.GetMessage("SetWeekTimer", this, player.Id), args[0], args[1], week));
            }
            else
                player.Message(string.Format(lang.GetMessage("SetWeekTimerError", this, player.Id), args[0]));
        }

        [Command("workingman.setdaylimit")]
        private void SetDayLimit(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            Int32 limit;
            if(!Int32.TryParse(args[0], out limit))
            {
                player.Message(lang.GetMessage("SetDayLimitError", this, player.Id));
                return;
            }

            config.minutesPerDay = limit;
            SaveConfig();
            ResetWarningThresholds();
            player.Message(string.Format(lang.GetMessage("SetDayLimit", this, player.Id), args[0]));
        }

        [Command("workingman.setweeklimit")]
        private void SetWeekLimit(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            Int32 limit;
            if(!Int32.TryParse(args[0], out limit))
            {
                player.Message(lang.GetMessage("SetWeekLimitError", this, player.Id));
                return;
            }

            config.minutesPerWeek = limit;
            SaveConfig();
            ResetWarningThresholds();
            player.Message(string.Format(lang.GetMessage("SetWeekLimit", this, player.Id), args[0]));
        }

        [Command("workingman.setweekstartday")]
        private void SetWeekStartDay(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            Int32 weekday;
            if(!Int32.TryParse(args[0], out weekday))
            {
                player.Message(lang.GetMessage("SetWeekStartDayError", this, player.Id));
                return;
            }

            config.dayOfWeek = weekday;
            SaveConfig();
            player.Message(string.Format(lang.GetMessage("SetWeekStartDay", this, player.Id), args[0]));
        }

        [Command("workingman.settimenights")]
        private void SetTimeNights(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            bool timeNights;
            if(!Boolean.TryParse(args[0], out timeNights))
            {
                player.Message(lang.GetMessage("SetTimeNightsError", this, player.Id));
                return;
            }

            config.timeNights = timeNights;
            SaveConfig();
            player.Message(string.Format(lang.GetMessage("SetTimeNights", this, player.Id), timeNights));    
        }

        [Command("workingman.resetdefaults")]
        private void ResetDefaults(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            LoadDefaultConfig();
            player.Message(lang.GetMessage("ResetDefaults", this, player.Id));
        }

        [Command("workingman.setwarn1")]
        private void SetWarningThreshold1(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            Int32 warn;
            if(!Int32.TryParse(args[0], out warn))
            {
                player.Message(lang.GetMessage("SetWarn1Error", this, player.Id));
                return;
            }

            config.warningThreshold1 = warn;
            SaveConfig();
            ResetWarningThresholds();
            player.Message(string.Format(lang.GetMessage("SetWarn1", this, player.Id), args[0]));
        }

        [Command("workingman.setwarn2")]
        private void SetWarningThreshold2(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            Int32 threshold;
            if(!Int32.TryParse(args[0], out threshold))
            {
                player.Message(lang.GetMessage("SetWarn2Error", this, player.Id));
                return;
            }

            config.warningThreshold2 = threshold;
            SaveConfig();
            ResetWarningThresholds();
            player.Message(string.Format(lang.GetMessage("SetWarn2", this, player.Id), args[0]));
        }

        [Command("workingman.givetimeday")]
        private void GiveTimeDay(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string playerId = players.FindPlayer(args[0]).Id;
            if(playerId == null)
            {
                player.Message(string.Format(lang.GetMessage("GiveTimeDayError", this, player.Id), args[0]));
                return;
            }

            Int32 time;
            if(!Int32.TryParse(args[1], out time))
            {
                player.Message(lang.GetMessage("GiveTimeDayError2", this, player.Id));
                return;
            }

            timeData[playerId, today] = (int)timeData[playerId, today] - time;
            player.Message(string.Format(lang.GetMessage("GiveTimeDay", this, player.Id), args[0], args[1], today));
        }

        [Command("workingman.givetimeweek")]
        private void GiveTimeWeek(IPlayer player, string command, string[] args)
        {
            if(!player.IsAdmin)
                return;

            string week = WeekOfYear();
            string playerId = players.FindPlayer(args[0]).Id;
            if(playerId == null)
            {
                player.Message(string.Format(lang.GetMessage("GiveTimeWeekError", this, player.Id), args[0]));
                return;
            }

            Int32 time;
            if(!Int32.TryParse(args[1], out time))
            {
                player.Message(lang.GetMessage("GiveTimeWeekError2", this, player.Id));
                return;
            }

            timeData[playerId, week] = (int)timeData[playerId, week] - time;
            player.Message(string.Format(lang.GetMessage("GiveTimeWeek", this, player.Id), args[0], args[1], week));
        }

        [Command("checktimer")]
        private void CheckTimer(IPlayer player, string command, string[] args)
        {
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();

            if(config.minutesPerDay > 0){
                player.Message(string.Format(lang.GetMessage("CheckTimer1", this, player.Id), 
                    FormatTimeSpan((int)timeData[player.Id, today]), today, FormatTimeSpan(config.minutesPerDay - (int)timeData[player.Id, today])));
                player.Message(string.Format(lang.GetMessage("CheckTimer2", this, player.Id), FormatTimeSpan((long)TimeTilNextDayCycle().TotalMinutes)));
            }

            if(config.minutesPerWeek > 0){
                player.Message(string.Format(lang.GetMessage("CheckTimer3", this, player.Id), 
                    FormatTimeSpan((int)timeData[player.Id, week]), week, FormatTimeSpan(config.minutesPerWeek - (int)timeData[player.Id, week])));
                player.Message(string.Format(lang.GetMessage("CheckTimer4", this, player.Id), FormatTimeSpan2((long)TimeTilNextWeekCycle().TotalMinutes)));
            }
        }

        private void OnTimeSunset()
        {
            if(!(bool)config.timeNights)
                countTime = false;
        }

        private void OnTimeSunrise()
        {
            countTime = true;
        }

        private void Unload()
        {
            timeData.Save();
        }

        private void OnServerSave()
        {
            timeData.Save();
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

        private string FormatTimeSpan(long minutes)
        {
            TimeSpan t = TimeSpan.FromMinutes( minutes );
            string answer = string.Format("{0:D2}h:{1:D2}m", 
                t.Hours, 
                t.Minutes);
            return answer;
        }

        private string FormatTimeSpan2(long minutes)
        {
            TimeSpan t = TimeSpan.FromMinutes( minutes );
            string answer = string.Format("{0:D2}d:{1:D2}h:{2:D2}m",
                t.Days, 
                t.Hours, 
                t.Minutes);
            return answer;
        }

        class PluginConfig
        {
            public long minutesPerDay { get; set; }
            public long minutesPerWeek { get; set; }
            public long warningThreshold1 { get; set; }
            public long warningThreshold2 { get; set; }
            public int  dayOfWeek { get; set; }
            public bool timeNights { get; set; }
        }

        private void Init()
        {
            TimeZoneInfo.ClearCachedData();
            timeData = Interface.Oxide.DataFileSystem.GetDatafile("WorkingMan/timeData");
            config = Config.ReadObject<PluginConfig>();
            dayWarningThreshold1 = (int)config.minutesPerDay - (int)config.warningThreshold1;
            dayWarningThreshold2 = (int)config.minutesPerDay - (int)config.warningThreshold2;
            weekWarningThreshold1 = (int)config.minutesPerWeek - (int)config.warningThreshold1;
            weekWarningThreshold2 = (int)config.minutesPerWeek - (int)config.warningThreshold2;

            timer.Every(60f, UpdateLoop);
        }

        private void MsgPlayer(BasePlayer player, string msg)
        {
            player.ChatMessage(msg);
            if(GUIAnnouncements != null)
                GUIAnnouncements?.Call("CreateAnnouncement", msg, "Purple", "Yellow", player);
        }

        private void UpdateLoop()
        {
            if(!countTime)
                return;

            int dayTime, weekTime;
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();
            List<string> kick = new List<string>();

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (timeData[player.UserIDString, today] == null)
                    timeData[player.UserIDString, today] = 0;

                if (timeData[player.UserIDString, week] == null)
                    timeData[player.UserIDString, week] = 0;

                dayTime = (int)timeData[player.UserIDString, today] + 1;
                timeData[player.UserIDString, today] = dayTime;

                weekTime = (int)timeData[player.UserIDString, week] + 1;
                timeData[player.UserIDString, week] = weekTime;

                if(config.minutesPerDay > 0)
                {
                    string msg = string.Format(lang.GetMessage("PlayerDayWarning", this, player.UserIDString), 
                            FormatTimeSpan(dayTime), today, FormatTimeSpan(config.minutesPerDay - dayTime));
                    if(dayTime >= dayWarningThreshold2)
                    {
                        if(dayTime % WARNING2_INTERVAL == 0)
                            MsgPlayer(player, msg);

                    }
                    else if(dayTime >= dayWarningThreshold1)
                    {
                        if(dayTime % WARNING1_INTERVAL == 0)
                            MsgPlayer(player, msg);
                    }
                }

                if(config.minutesPerWeek > 0)
                {
                    string msg = string.Format(lang.GetMessage("PlayerWeekWarning", this, player.UserIDString), 
                            FormatTimeSpan(weekTime), week, FormatTimeSpan(config.minutesPerWeek - weekTime));
                    if(weekTime >= weekWarningThreshold2)
                    {
                        if(weekTime % WARNING2_INTERVAL == 0)
                            MsgPlayer(player, msg);

                    }
                    else if(weekTime >= weekWarningThreshold1)
                    {
                        if(weekTime % WARNING1_INTERVAL == 0)
                            MsgPlayer(player, msg);
                    }
                }

                if ((config.minutesPerDay > 0 && dayTime >= config.minutesPerDay) || (config.minutesPerWeek > 0 && weekTime >= config.minutesPerWeek)) {
                    kick.Add(player.UserIDString);
                }

            }

            foreach(string playerId in kick)
            {
                var player = covalence.Players.FindPlayer(playerId);
                player.Kick(lang.GetMessage("Kick", this, playerId));
            }
        }

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            config = new PluginConfig();
            config.minutesPerDay = 6 * 60; //6 hours
            config.minutesPerWeek = 0;
            config.warningThreshold1 = 30;
            config.warningThreshold2 = 10;
            config.dayOfWeek = 4; // Thursday
            config.timeNights = true;
            ResetWarningThresholds();
            SaveConfig();
        }

        private object CanClientLogin(Network.Connection connection)
        {
            int dayCount, weekCount;
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            string week = WeekOfYear();
            string id = connection.userid.ToString();

            if (timeData[id, today] == null)
                timeData[id, today] = 0;

            if (timeData[id, week] == null)
                timeData[id, week] = 0;

            dayCount = (int)timeData[id, today];
            weekCount = (int)timeData[id, week];
            if (config.minutesPerDay > 0 && dayCount != null && dayCount >= config.minutesPerDay)
            {
                string error = string.Format(lang.GetMessage("LoginDay", this, id), FormatTimeSpan((long)TimeTilNextDayCycle().TotalMinutes));
                return error;
            }
            else if(config.minutesPerWeek > 0 && weekCount != null && weekCount >= config.minutesPerWeek)
            {
                string error = string.Format(lang.GetMessage("LoginWeek", this, id), FormatTimeSpan2((long)TimeTilNextWeekCycle().TotalMinutes));
                return error;
            }

            return true;
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void ResetWarningThresholds()
        {
            if((int)config.minutesPerDay > 0){
                dayWarningThreshold1 = (int)config.minutesPerDay - (int)config.warningThreshold1;
                dayWarningThreshold2 = (int)config.minutesPerDay - (int)config.warningThreshold2;
            }

            if((int)config.minutesPerWeek > 0){
                weekWarningThreshold1 = (int)config.minutesPerWeek - (int)config.warningThreshold1;
                weekWarningThreshold2 = (int)config.minutesPerWeek - (int)config.warningThreshold2;
            }
        }

    }
}
