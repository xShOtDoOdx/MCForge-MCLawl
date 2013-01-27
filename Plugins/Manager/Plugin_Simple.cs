/*
	Copyright 2011 MCForge
		
	Dual-licensed under the	Educational Community License, Version 2.0 and
	the GNU General Public License, Version 3 (the "Licenses"); you may
	not use this file except in compliance with the Licenses. You may
	obtain a copy of the Licenses at
	
	http://www.opensource.org/licenses/ecl2.php
	http://www.gnu.org/licenses/gpl-3.0.html
	
	Unless required by applicable law or agreed to in writing,
	software distributed under the Licenses are distributed on an "AS IS"
	BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
	or implied. See the Licenses for the specific language governing
	permissions and limitations under the Licenses.
*/
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace MCForge
{
    public abstract class Plugin_Simple
    {
        public abstract void Load(bool startup);
        public abstract void Unload(bool shutdown);
        public abstract string name { get; }
        public abstract string creator { get; }
        public abstract string MCForge_Version { get; }
        /// <summary>
        /// Load a simple plugin
        /// </summary>
        /// <param name="pluginname">The filepath to load</param>
        /// <param name="startup">Whether the server is starting up or not</param>
        /// <returns>Whether the plugin loaded or not</returns>
        public static bool Load(string pluginname, bool startup)
        {
            String creator = "";
            object instance = null;
            Assembly lib = null;
            using (FileStream fs = File.Open(pluginname, FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read = 0;
                    while ((read = fs.Read(buffer, 0, 1024)) > 0)
                        ms.Write(buffer, 0, read);
                    lib = Assembly.Load(ms.ToArray());
                    ms.Close();
                    ms.Dispose();
                }
                fs.Close();
                fs.Dispose();
            }
            try
            {
                foreach (Type t in lib.GetTypes())
                {
                    if (t.BaseType == typeof(Plugin_Simple))
                    {
                        instance = Activator.CreateInstance(lib.GetTypes()[0]);
                        break;
                    }
                }
            }
            catch { }
            if (instance == null)
            {
                Server.s.Log("The plugin " + pluginname + " couldnt be loaded!");
                return false;
            }
            String plugin_version = ((Plugin_Simple)instance).MCForge_Version;
            if (plugin_version != "" && new Version(plugin_version) != Server.Version)
            {
                Server.s.Log("This plugin (" + ((Plugin_Simple)instance).name + ") isnt compatible with this version of MCForge!");
                Thread.Sleep(1000);
                if (Server.unsafe_plugin)
                {
                    Server.s.Log("Will attempt to load!");
                    goto here;
                }
                else
                    return false;
            }
        here:
            Plugin.all_simple.Add((Plugin_Simple)instance);
            creator = ((Plugin_Simple)instance).creator;
            ((Plugin_Simple)instance).Load(startup);
            Server.s.Log("Plugin: " + ((Plugin_Simple)instance).name + " loaded...");
            Thread.Sleep(1000);
            return true;
        }
    }
}
