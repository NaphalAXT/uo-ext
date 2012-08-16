using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;


namespace UOExt
{
    public static class Config
    {
        /// <summary>
        /// Where current code is running. Autodetect, do not touch.
        /// </summary>
        private static bool m_inRunUO;

        /// <summary>
        /// This is encapsulation for UOExt protocol.
        /// </summary>
        private static byte m_encapsulationHeader = 0xFF;

        /// <summary>
        /// Client plugins folder. It will be filled with absolute path, if needed.
        /// </summary>
        private static string m_clientPluginsPath = @"UOExt\Plugins\Client";

        /// <summary>
        /// File with plugins init order. File format is:
        /// [DllName], [PluginNumber | PluginName]
        /// Example:
        /// ; This is comment. You can set ';' char only at start of a line.
        /// HelloWorld.plg, 0
        /// MyCoolPlugin.plg, 0
        /// MyOtherCoolPlugin.plg, 0
        /// MyCoolPlugin.plg, 1
        /// </summary>
        private static string m_pluginsInitOrderFile = @"UOExt\Plugins\Client\Order.cfg";

        /// <summary>
        /// Server plugins folder. It will be filled with absolute path, if needed.
        /// </summary>
        private static string m_serverPluginsPath = @"UOExt\Plugins\Server";

        /// <summary>
        /// Where is UOExt.GUI.dll located. This dll will deliver to client before any other plugins.
        /// </summary>
        private static string m_UOExtGUIPath = @"UOExt\UOExt.GUI.dll";

        /// <summary>
        /// Whereis is UOExt.dll located.
        /// </summary>
        private static string m_UOExtPath = @"UOExt\UOExt.dll";

        /// <summary>
        /// If code is Standalone - this is IP for server to Listen. If code inside RunUO and ExternalServerInRunUO == true, than this is IP to route UOExt during support detection phase.
        /// </summary>
        private static string m_ip = null;

        /// <summary>
        /// If code is Standalone - this is port for server to Listen. If code inside RunUO and ExternalServerInRunUO == true, than this is port to route UOExt during support detection phase.
        /// </summary>
        private static int m_port = 62942;

        /// <summary>
        /// IP for game server.
        /// </summary>
        private static string m_gameip = "127.0.0.1";

        /// <summary>
        /// Port for game server.
        /// </summary>
        private static int m_gameport = 2593;

        /// <summary>
        /// Send game ip and port on update stage
        /// </summary>
        private static bool m_sendgameip = false;

        /// <summary>
        /// Tells client that server wants encrypted connection
        /// </summary>
        private static bool m_gameencrypted = false;

        /// <summary>
        /// Autodetect.
        /// </summary>
        private static bool m_Unix = false;

        /// <summary>
        /// Autodetect.
        /// </summary>
        private static bool m_64bit = false;

        /// <summary>
        /// Autogenerated from config above.
        /// </summary>
        private static byte[] m_configpacket = null;

        public static bool InRunUO { get { return m_inRunUO; } }
        public static byte EncapsulationHeader { get { return m_encapsulationHeader; } }
        public static string ClientPluginsPath { get { return m_clientPluginsPath; } }
        public static string ServerPluginsPath { get { return m_serverPluginsPath; } }
        public static string PluginInitOrderFile { get { return m_pluginsInitOrderFile; } }
        public static string UOExtGUIPath { get { return m_UOExtGUIPath; } }
        public static string UOExtPath { get { return m_UOExtPath; } }

        public static string IP { get { return m_ip; } }
        public static int Port { get { return m_port; } }

        public static bool HasGameAddress { get { return m_sendgameip; } }
        public static string GameIP { get { return m_gameip; } }
        public static int GamePort { get { return m_gameport; } }
        public static bool GameEncrypted { get { return m_gameencrypted; } }

        public static bool IsUnix { get { return m_Unix; } set { m_Unix = value; } }
        public static bool Is64Bit { get { return m_64bit; } set { m_64bit = value; } }

        public static byte[] ConfigurationPacket { get { return m_configpacket; } }

        private static void ConfigurePaths()
        {
            m_clientPluginsPath = Path.IsPathRooted(m_clientPluginsPath) ? m_clientPluginsPath
                      : Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, m_clientPluginsPath);
            m_serverPluginsPath = Path.IsPathRooted(m_serverPluginsPath) ? m_serverPluginsPath
                      : Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, m_serverPluginsPath);
            m_pluginsInitOrderFile = Path.IsPathRooted(m_pluginsInitOrderFile) ? m_pluginsInitOrderFile
                      : Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, m_pluginsInitOrderFile);
            m_UOExtPath = Path.IsPathRooted(m_UOExtPath) ? m_UOExtPath
                      : Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, m_UOExtPath);
            m_UOExtGUIPath = Path.IsPathRooted(m_UOExtGUIPath) ? m_UOExtGUIPath
                      : Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, m_UOExtGUIPath);
        }

        private static void InternalConfigure()
        {
#if Framework_4_0
		    m_64bit = Environment.Is64BitProcess;
#else
            m_64bit = (IntPtr.Size == 8);	//Returns the size for the current /process/
#endif
            int platform = (int)Environment.OSVersion.Platform;
            m_Unix = (platform == 4 || platform == 128);

            ConfigurePaths();
            CompileConfig();
        }
        /// <summary>
        /// This is hack for RunUO. RunUO will call any Configure method it finds in classes.
        /// </summary>
        public static void Configure()
        {
            m_inRunUO = true;
            InternalConfigure();
        }

        /// <summary>
        /// This code will run under Standalone version.
        /// </summary>
        public static void Standalone()
        {
            m_inRunUO = false;
            InternalConfigure();
        }

        private static void CompileConfig()
        {
            int size = 1;
            byte flags = 0;

            if (InRunUO)
            {
                flags += 1;
            }
            else
            {
                size += 1;
                if (GameEncrypted)
                    flags += 2;
                if (HasGameAddress)
                {
                    size += 6;
                    flags += 4;
                }
            }

            m_configpacket = new byte[size];
            MemoryStream stream = new MemoryStream(m_configpacket);
            BinaryWriter writer = new BinaryWriter(stream);

            
            writer.Write((byte)flags);
            if (!InRunUO)
            {
                writer.Write((byte)EncapsulationHeader);
                if (HasGameAddress)
                {
                    writer.Write((byte[])(Dns.GetHostEntry(GameIP).AddressList[0].GetAddressBytes()));
                    writer.Write((short)GamePort);
                }
            }
        }
    }
}
