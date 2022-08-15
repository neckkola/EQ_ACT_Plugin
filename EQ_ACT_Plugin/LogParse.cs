// EQ_ACT_Plugin ~ LogParse.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
using Advanced_Combat_Tracker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EQ_ACT_Plugin.EQ_ACT_Plugin;
using System.Diagnostics;
using System.Data;

namespace EQ_ACT_Plugin
{

    public static class LogParse
    {
        
        /// <summary>
        /// This takes a log line and determines the date/time from it.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static DateTime ParseLogDateTime(string message)
        {
            DateTime ret = DateTime.MinValue;

            if (message == null || message.Length < 25)
                return ret;

            if (message[0] == '[' && message[25] == ']')
            {
                if (!DateTime.TryParseExact(message.Substring(1, 24), "ddd MMM dd HH:mm:ss yyyy", null, 0, out ret))
                    return DateTime.MinValue;
            }

            return ret;
        }


        public static bool ParseZone(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.ZoneChange.Match(logInfo.logLine);
            if (m.Success)
            {
                string zonename = m.Groups["zone"].Success ? m.Groups["zone"].Value : "";

                if (!string.IsNullOrWhiteSpace(zonename))
                    ActGlobals.oFormActMain.ChangeZone(zonename);

                return true;
            }

            return false;
        }

        public static bool ParseDamage(LogLineEventArgs logInfo)
        {
            if (_LastNonMeleeSkillused.ContainsKey("critical"))
            {
                Match m = RegexCache.Damage.Match(logInfo.logLine);
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
//                string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
                string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                string skill = "melee";
                string item;
                if (_LastNonMeleeSkillused.TryGetValue("critical", out item))
                {
                    string[] elements = item.Split(',');
                    string amount = elements[1];

                    SwingTypeEnum swingType = SwingTypeEnum.Melee;

                    MasterSwing ms = new MasterSwing((int)swingType, true, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);

                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                        ActGlobals.oFormActMain.AddCombatAction(ms);

                    _LastNonMeleeSkillused.Clear();

                    return true;
                }
                return false;
            }
            else
            {
                Match m = RegexCache.Damage.Match(logInfo.logLine);
                if (!m.Success)
                    m = RegexCache.DamagePassive.Match(logInfo.logLine);
                if (m.Success)
                {
                    string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                    string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
                    string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
                    string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                    Globals.rolling_target = target;
                    string skill = "melee";
                    SwingTypeEnum swingType = SwingTypeEnum.Melee;
                    if (m.Groups["type"].Success)
                    {
                        skill = CleanupSkill(m.Groups["type"].Value);

                        if (skill == "non-melee")
                        {
                            if (_LastNonMeleeSkillused.ContainsKey(actor))
                                skill = _LastNonMeleeSkillused[actor];

                            swingType = SwingTypeEnum.NonMelee;
                        }
                    }


                    //MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, "", target);
                    MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);

                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                        ActGlobals.oFormActMain.AddCombatAction(ms);

                    return true;
                }
                return false;
            }
        }
        public static bool BotParseDamageCrit(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.BotDamageCrit.Match(logInfo.logLine);
            if (m.Success)
            {
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                //string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
                string skill = "non-melee";
                string target = Globals.rolling_target;
                string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
                string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                bool critcal = m.Groups["critical"].Success ? true : false;

                SwingTypeEnum swingType = SwingTypeEnum.NonMelee;

                //MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, "", target);
                MasterSwing ms = new MasterSwing((int)swingType, true, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);

                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                    ActGlobals.oFormActMain.AddCombatAction(ms);

                return true;
            }

            return false;
        }
        public static bool BotParseHeal(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.BotHeal.Match(logInfo.logLine);
            if (m.Success)
            {
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
                string skill = "melee";
                //string target = Globals.rolling_target;
                string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
                string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                //bool critcal = m.Groups["critical"].Success ? true : false;

                SwingTypeEnum swingType = SwingTypeEnum.Healing;

                //MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, "", target);
                MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, "heal", actor, "heal", target);

                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                    ActGlobals.oFormActMain.AddCombatAction(ms);

                return true;
            }

            return false;
        }

        public static bool ParseDamageCrit(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.DamageCrit.Match(logInfo.logLine);
            if (m.Success)
            {
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";

                string data = string.Concat(actor, ",", amount);

                _LastNonMeleeSkillused.TryAdd("critical", data);

                //bool critcal = m.Groups["critical"].Success ? true : false;

                //                SwingTypeEnum swingType = SwingTypeEnum.Melee;

                //MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, "", target);
                //                MasterSwing ms = new MasterSwing((int)swingType, true, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, "crit", actor, "crit", target);

                //                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                //                    ActGlobals.oFormActMain.AddCombatAction(ms);

                return true;
            }

            return false;
        }
        private static ConcurrentDictionary<string, string> _LastDoT = new ConcurrentDictionary<string, string>();

        public static bool ParseDoTTick(LogLineEventArgs logInfo)
        {
            
            Match m = RegexCache.DamageDoTTick.Match(logInfo.logLine);
            if (!m.Success)
                return false;

            string actor = m.Groups["actorname1"].Success ? TranslateName(m.Groups["actorname1"].Value) : 
                m.Groups["actorname2"].Success ? TranslateName(m.Groups["actorname2"].Value) : "";
            string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
            string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
            //string skillname = CleanupSkill(m.Groups["type"].Success ? m.Groups["type"].Value : 
            //    m.Groups["type2"].Success ? m.Groups["type2"].Value :
            //    m.Groups["type3"].Success ? m.Groups["type3"].Value :
            //    m.Groups["type4"].Success ? m.Groups["type4"].Value : 
            //    m.Groups["type10"].Success ? m.Groups["type10"].Value : "");
            string skillname = CleanupSkill(m.Groups["spell1"].Success ? m.Groups["spell1"].Value :
                m.Groups["spell2"].Success ? m.Groups["spell2"].Value : "unkownspell");

            //if (actor == "your") actor = ActGlobals.charName;

            if (string.IsNullOrWhiteSpace(actor))
                actor = "unknown";

            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.WriteLine("Actor:" + actor);
            Debug.WriteLine("Target:" + target);
            Debug.WriteLine("Amount:" + amount);
            Debug.WriteLine("SkillName:" + skillname);

            MasterSwing ms = new MasterSwing((int)SwingTypeEnum.NonMelee, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, "non-melee", actor, skillname, target);

            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                ActGlobals.oFormActMain.AddCombatAction(ms);

            return true;
        }
        public static bool ParseMiss(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.Miss.Match(logInfo.logLine);
            if (m.Success)
            {
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
                string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                string skill = "melee";
                string misstype = m.Groups["dodge"].Success ? "dodge" :
                    m.Groups["parry"].Success ? "parry" :
                    m.Groups["riposte"].Success ? "riposte" : "";

                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
                Debug.WriteLine("Actor:" + actor);
                Debug.WriteLine("Target:" + target);
                Debug.WriteLine("SwingType:" + swingtype);
                Debug.WriteLine("MissType:" + misstype);

                //MasterSwing ms = new MasterSwing((int)SwingTypeEnum.Melee, false, misstype, Dnum.Miss, logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skillname, actor, "", target);
                MasterSwing ms = new MasterSwing((int)SwingTypeEnum.Melee, false, misstype, Dnum.Miss , logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);
                
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                    ActGlobals.oFormActMain.AddCombatAction(ms);

                return true;
            }

            return false;
        }

        public static bool ParseDeath(LogLineEventArgs logInfo)
        {
            Match m = RegexCache.Death.Match(logInfo.logLine);
            if (!m.Success)
                m = RegexCache.Death2.Match(logInfo.logLine);
            if (m.Success)
            {
                string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                string target = m.Groups["targetname"].Success ? m.Groups["targetname"].Value : "";
                string skillname = "Killing";

                MasterSwing ms = new MasterSwing((int)SwingTypeEnum.Melee, false, Dnum.Death, logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skillname, actor, "", target);

                // only log death if currently in combat.
                if (ActGlobals.oFormActMain.InCombat)
                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                        ActGlobals.oFormActMain.AddCombatAction(ms);

                return true;
            }

            return false;
        }

        public static bool CheckForRegexMatch(LogLineEventArgs logInfo, Regex regex)
        {
            Match m = regex.Match(logInfo.logLine);
            if (m.Success)
                return true;
            return false;
        }


        private static ConcurrentDictionary<string, string> _LastNonMeleeSkillused = new ConcurrentDictionary<string, string>();

        public static bool ParseNonMeleeType(LogLineEventArgs logInfo)
        {
            if (_LastNonMeleeSkillused.ContainsKey("non-melee"))
            {
                Match m = RegexCache.NonMeleeType.Match(logInfo.logLine);
                string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                string skill = "non-melee";
                string item;
                if (_LastNonMeleeSkillused.TryGetValue("non-melee", out item))
                {
                    string[] elements = item.Split(',');
                    string actor = elements[0];
                    string target = elements[1];
                    string amount = elements[2];

                    SwingTypeEnum swingType = SwingTypeEnum.NonMelee;

                    MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);

                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
                        ActGlobals.oFormActMain.AddCombatAction(ms);

                    _LastNonMeleeSkillused.Clear();

                    return true;
                }
                return true;
            }
            else
            {
                Match m = RegexCache.NonMelee.Match(logInfo.logLine);
                if (m.Success)
                {
                    string actor = m.Groups["actorname"].Success ? TranslateName(m.Groups["actorname"].Value) : "";
                    string target = m.Groups["targetname"].Success ? TranslateName(m.Groups["targetname"].Value) : "";
                    string amount = m.Groups["amount"].Success ? m.Groups["amount"].Value : "";
                    string swingtype = m.Groups["swingtype"].Success ? m.Groups["swingtype"].Value : "";
                    string skill = "non-melee";
                    string data = string.Concat(actor, ',', target, ',', amount);

                    _LastNonMeleeSkillused.TryAdd("non-melee", data);

//                    SwingTypeEnum swingType = SwingTypeEnum.NonMelee;

//                    MasterSwing ms = new MasterSwing((int)swingType, false, int.Parse(amount), logInfo.detectedTime, ActGlobals.oFormActMain.GlobalTimeSorter, skill, actor, swingtype, target);

//                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, actor, target))
//                        ActGlobals.oFormActMain.AddCombatAction(ms);

                    return true;

                }

                return false;
            }

        }
        private static string TranslateName(string name)
        {
            string ret = name;

            if (name.ToLower() == "you")
                return ActGlobals.charName;
            if (name.ToLower() == "your")
                return ActGlobals.charName;
            if (ret.ToLower().StartsWith("an "))
                ret = ret.Substring(3);
            else if (ret.ToLower().StartsWith("a "))
                ret = ret.Substring(2);

            return name;
        }

        private static string CleanupSkill(string skill)
        {
            string tmp =  (skill?.Replace(".", "") ?? "").Trim();
            if (tmp.IndexOf('<') < tmp.IndexOf('>'))
                tmp = tmp.Substring(tmp.IndexOf('<')+1, tmp.IndexOf('>') - tmp.IndexOf('<') - 1);

            return tmp;
        }
    }
}
