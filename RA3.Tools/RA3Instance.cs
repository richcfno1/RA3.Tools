﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RA3.Tools
{
    public class RA3Instance
    {
        private static readonly string _quickLoaderPath = "RA3.QuickLoader.exe";
        //
        public string GamePath;
        public string LaunchParamter;
        public bool UseBarLauncher;
        public List<string> Profiles
        {
            get { return GetProfilesList(); } 
        }
        //
        public readonly ResourceFolder ModFolder = new ResourceFolder(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Red Alert 3\\Mods\\");
        public readonly ResourceFolder ReplayFolder = new ResourceFolder(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Red Alert 3\\Replays\\");
        public readonly ResourceFolder MapFolder = new ResourceFolder(Environment.GetEnvironmentVariable("appdata") + "\\Red Alert 3\\Maps\\");
        public readonly ResourceFolder ProfileFolder = new ResourceFolder(Environment.GetEnvironmentVariable("appdata") + "\\Red Alert 3\\Profiles\\");
        /// <summary>  
        /// 红警3进程实例
        /// </summary>  
        /// <param name="gamePath">游戏路径（可选，为空则从注册表读取）</param>  
        public RA3Instance(string gamePath = "")
        {
            //Read GamePath
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                GamePath = Registry.GetRA3Path();
            }
            else
            {
                GamePath = gamePath;
            }
            //Check RA3.QuickLoader
            if (File.Exists($".\\{_quickLoaderPath}"))
            {
                UseBarLauncher = true;
            }
        }

        #region Check Files
        public bool IsRA3PathValid()
        {
            try
            {
                return Directory.EnumerateFiles(GamePath, "RA3_*_1.12.SkuDef").Any();
            }
            catch (Exception) { }
            return false;
        }

        public bool IsRA3FileValid()
        {
            return false;
        }
        #endregion

        #region Launch & Register
        public void Register()
        {
            //ToDo : 需要直接写入，而不是依赖RA3.reg
            try
            {
                if (File.Exists("RA3.reg"))
                {
                    string regPath = Path.GetFullPath("RA3.reg");
                    regPath = @"""" + regPath + @"""";
                    Process.Start("regedit", string.Format(" /s {0}", regPath));
                }
                //write registion here.
            }
            catch (Exception) { }
        }

        public void Launch()
        {
            var LauncherPath = Path.Combine(GamePath, "RA3.exe");
            if ((UseBarLauncher == true) && File.Exists(_quickLoaderPath))
            {
                LauncherPath = Path.Combine(Directory.GetCurrentDirectory(), _quickLoaderPath);
            }

            var ra3ProcessInfo = new ProcessStartInfo
            {
                FileName = LauncherPath,
                Arguments = LaunchParamter,
                WorkingDirectory = GamePath
            };
            Process.Start(ra3ProcessInfo);
        }

        #endregion

        #region Steam & Origin Version detection.
        //From @BSG-75 (https://github.com/BSG-75)
        public bool DoesRA3NeedSteamAppID()
        {
            var ra3Path = GamePath;
            if (ra3Path.IndexOf("steam", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            var steamAppIDPath = Path.Combine(ra3Path, "steam_appid.txt");
            return !File.Exists(steamAppIDPath) || File.ReadAllText(steamAppIDPath).Trim() != "17480";
        }

        //Abandoned
        public bool DoesRA3NeedPatchedParFile()
        {
            var tucParPath = Path.Combine(GamePath, "Data", "ra3_1.12.par");
            if (!File.Exists(tucParPath))
            {
                return false;
            }

            return !File.ReadAllBytes(tucParPath).SequenceEqual(Utility.PatchedParFile);
        }

        public void GenerateSteamAppID()
        {
            var steamAppIDPath = Path.Combine(GamePath, "steam_appid.txt");
            File.WriteAllText(steamAppIDPath, "17480");
        }

        //Abandoned
        public void GeneratePatchedParFile()
        {
            var tucParPath = Path.Combine(GamePath, "Data", "ra3_1.12.par");
            var oldFileId = 0;
            while (File.Exists($"{tucParPath}.{oldFileId}.old"))
            {
                ++oldFileId;
            }
            File.Move(tucParPath, $"{tucParPath}.{oldFileId}.old");
            File.WriteAllBytes(tucParPath, Utility.PatchedParFile);
        }

        #endregion

        #region Profile Operations
        // Parse string encoded by EA similar to UTF-8 in directory.ini
        private string ParseDirectoryString(string s)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c != '_')
                {
                    bytes.Add(Convert.ToByte(c));
                }
                else
                {
                    var hex = new char[] { s[i + 1], s[i + 2] };
                    var n = int.Parse(hex, NumberStyles.HexNumber);
                    bytes.Add(Convert.ToByte(n));
                    i += 2;
                }
            }
            return Encoding.Unicode.GetString(bytes.ToArray());
        }

        private List<string> GetProfilesList()
        {
            var original = ParseDirectoryString(File.ReadAllLines($"{ProfileFolder.Path}\\directory.ini")[0]);
            string[] directories = Directory.GetDirectories(ProfileFolder.Path);
            List<string> profiles = new List<string>();
            foreach (string profile in directories)
            {
                // verify if the profile exist in directory.ini
                if (original.Contains(Path.GetFileNameWithoutExtension(profile)))
                {
                    profiles.Add(Path.GetFileNameWithoutExtension(profile));
                }
            }
            return profiles;
        }

        public string GetCurrentProfile()
        {
            try
            {
                var original = File.ReadAllLines($"{ProfileFolder.Path}\\directory.ini")[1];
                return ParseDirectoryString(original)[15..];
            }
            catch
            {
                return null;
            }
        }

        public void DeleteSkirmishINI(string profile)
        {
            try
            {
                File.Delete($"{ProfileFolder.Path}\\{profile}\\Skirmish.ini");
            }
            catch { }
        }

        public void DeleteAllSkirmishINI()
        {
            foreach (var i in Profiles)
            {
                DeleteSkirmishINI(i);
            }
        }
        #endregion

        //ToDo:1.完善检测文件完整的函数
        //ToDo:8.软链接修改Mod,Map,Replay的位置（在ResourceFolder类中）
    }
}
