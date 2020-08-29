using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;

namespace Minecraft_Launch_Server_Linux
{
    class Program
    {
        /// <summary>
        ///  Listener für die Verbindung mit dem Client, restarTime in Sekunden für den Fall eines kritischen Fehlers
        ///  error = Hilfsvariable, um zu beginn nicht die Konsolenzeile zu clearen (siehe Restart-Methode)
        /// </summary>
        private static Socket listener;
        private static string neewsfeed = "";
        private static int restartTime = 30;
        private static bool error = false;
        private static int port = 43333;
        private static string dateipfad = "update.zip";
        private static string Serverversion = "1";
        static void Main(string[] args)
        {
            if (!LaunchServer())
            {
                restart();
            }
            Console.ReadKey();
        }

        public static void create_config()
        {
            if (!File.Exists("serverconfig.txt"))
            {
                SetConsoleColor(1);
                Console.WriteLine("Erstelle Konfigurationsdatei...");
                File.WriteAllText("serverconfig.txt", "dateipfad=update.zip\r\nserverversion=1\r\nnewsfeed=Wilkommen!");
            }
        }
        public static void set_config()
        {
            SetConsoleColor(1);
            Console.WriteLine("Lese Konfigurationsdatei...");
            string text = File.ReadAllText("serverconfig.txt");
            string[] config = text.Trim().Split('=', '\n');
            for (int i = 0; i < config.Length; i += 2)
            {
                switch (config[i].Trim())
                {
                    case "dateipfad":
                        Console.WriteLine("Setze Dateipfad für die Updatedatei auf '" + config[i + 1].Trim() + "'");
                        dateipfad = config[i + 1].Trim();
                        break;
                    case "serverversion":
                        Console.WriteLine("Setze aktuelle Serverversion auf: '" + config[i + 1].Trim() + "'");
                        Serverversion = config[i + 1].Trim();
                        break;
                    case "newsfeed":
                        Console.WriteLine("Setze Neewsfeed auf: '" + config[i + 1].Trim() + "'");
                        neewsfeed = config[i + 1].Trim();
                        break;

                }
            }
        }
        /// <summary>
        /// Bei einem kritischen Fehler wird dise Methode ausgeführt,
        /// Die Methode zählt von der restartTime runter und restartet dann den Server
        /// </summary>
        private static void restart()
        {
            Timer t = new Timer(1000);
            Timer refresh = new Timer(300);
            t.Elapsed += (s, e) => { restartTime--; };
            refresh.Elapsed += (s, e) =>
            {
                if (error)
                    ClearCurrentConsoleLine();
                SetConsoleColor(3);
                Console.Write("Ein kritischer Fehler ist aufgetreten, restart in..." + restartTime + "s");
                SetConsoleColor(1);
                error = true;
                if (restartTime == 0)
                {
                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
                    Environment.Exit(0);
                }
            };
            refresh.Start();
            t.Start();
        }
        /// <summary>
        /// Methode wird für die Restart-Methode benötigt.
        /// Sie löscht die aktuelle Zeile, so ist es möglich, die Zeit bis zum Restarten in einer Zeile auszugeben.
        /// </summary>
        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
        /// <summary>
        /// Die Methode konfiguriert und startet den Listener
        /// </summary>
        /// <returns>true: Wenn erfolgreich; false: Bei einem Fehler</returns>
        private static bool LaunchServer()
        {

            SetConsoleColor(0);
            Console.WriteLine("TheMineZone Launchserver v. 1.0 by Cyberta (https://www.fiverr.com/cyberta)");
            SetConsoleColor(1);
            Console.WriteLine("Du kannst den Server jederzeit schließen, indem du eine beliebe Eingabe in das Konsolenfenster machst!");
            create_config();
            set_config();
            string ip = "[Unbekannt]";
            try
            {
                IPAddress ipAddr = IPAddress.Any;
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);
                ip = localEndPoint.ToString();
                listener = new Socket(ipAddr.AddressFamily,
                             SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                listener.Listen(60);
                if (!StartAccept())
                    return false;
            }
            catch (SocketException e)
            {
                SetConsoleColor(3);
                Console.WriteLine("Eine Socketexception ist aufgetreten: " + e.Message);
                SetConsoleColor(1);
                return false;
            }
            catch (Exception e)
            {
                SetConsoleColor(3);
                Console.WriteLine("Ein Fehler ist aufgetreten: " + e.Message);
                SetConsoleColor(1);
                return false;
            }
            SetConsoleColor(1);
            Console.WriteLine("Endpunkt für " + ip + " erfolgreich gestartet");
            return true;
        }
        /// <summary>
        /// Setzt die Hintergrund- und Vordergrundfarbe der Konsole 
        /// aus einem definierten Farbschema von 0 bis 3
        /// </summary>
        /// <param name="color">0: grüner Hintergrund, gelbe Schrift; 1: schwarzer Hintergrund, weiße Schrift; 2: gelber Hintergrund, dunkelblaue Schrift; 3: roter Hintergrund, schwarze Schrift</param>
        private static void SetConsoleColor(int color)
        {
            switch (color)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
            }
        }
        /// <summary>
        /// Erstellt einen neuen asynchronen Verbindungseingang, damit mehrere Anfragen asynchron (gleichzeitig) bearbeitet werden können
        /// </summary>
        /// <returns>True: Falls das erstellen erfolgreich war, false bei einer Exception</returns>
        private static bool StartAccept()
        {
            try
            {              
                listener.BeginAccept(HandleAsyncConnection, listener);
                return true;
            }
            catch (Exception e)
            {
                SetConsoleColor(3);
                Console.WriteLine("Ein Fehler ist aufgetreten: " + e.Message);
                SetConsoleColor(1);
                return false;
            }
        }
        /// <summary>
        /// Bearbeitet eine Client-Anfrage d.h.:
        /// Die Methode sendet dem Nutzer die aktuelle Version des Packs
        /// gegebenfalls, falls der Client dies anfragt, sendet diese Methode das aktuelle Pack als ZIP-Datei
        /// </summary>
        /// <param name="res">Parameter, um den Zustand zu tracken</param>
        private static void HandleAsyncConnection(IAsyncResult res)
        {
            if (!StartAccept())
            {
                restart();
            }
            string ip = "[Unbekannt]";
            try
            {
                Socket client = listener.EndAccept(res);
                ip = client.RemoteEndPoint.ToString();
                byte[] bytes = new Byte[256];
                string data = null;

                int numByte = client.Receive(bytes);
                data = Encoding.ASCII.GetString(bytes, 0, numByte);
                if (!(data.IndexOf("GetVersion") > -1 || data.IndexOf("Update") > -1 || data.IndexOf("GetNewsfeed") > -1))
                {
                    SetConsoleColor(2);
                    Console.WriteLine("Ungültiger Verbindungseingang von " + client.RemoteEndPoint.ToString() + " mit '" + data + "'");
                    SetConsoleColor(1);
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    return;
                }
                SetConsoleColor(1);
                Console.WriteLine("Eingehende Verbindung von " + client.RemoteEndPoint.ToString() + " mit '" + data + "'");
                if (data.IndexOf("GetVersion") > -1)
                {
                    SetConsoleColor(1);
                    Console.WriteLine("Sende aktuelle Serverversion '{0}' an " + client.RemoteEndPoint.ToString(), Serverversion);
                    client.Send(System.Text.Encoding.ASCII.GetBytes(Serverversion));
                }
                else if (data.IndexOf("Update") > -1)
                {
                    SetConsoleColor(1);
                    string[] path = dateipfad.Split('.');
                    string newPath = path[0] + (int.Parse(data.Trim().Split('=')[1]) + 1).ToString() + ".zip";
                    Console.WriteLine("Sende Updatedateien {0} an " + client.RemoteEndPoint.ToString(), newPath);
                    if (File.Exists(newPath))
                        client.SendFile(newPath);
                    else
                    {
                        SetConsoleColor(2);
                        Console.WriteLine("Die angeforderte Datei " + newPath + " von " + client.RemoteEndPoint.ToString() + " existiert nicht");
                    }
                }
                else if(data.IndexOf("GetNewsfeed") > -1)
                {
                    SetConsoleColor(1);
                    Console.WriteLine("Sende Newsfeed '{0} an " + client.RemoteEndPoint.ToString(), neewsfeed);
                    client.Send(System.Text.Encoding.ASCII.GetBytes(neewsfeed));

                }
                SetConsoleColor(1);
                Console.WriteLine("Schließe Verbindung mit " + client.RemoteEndPoint.ToString());
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (SocketException e)
            {
                SetConsoleColor(2);
                Console.WriteLine("Die Verbindung zu " + ip + " wurde auf unerwartete Weise getrennt: " + e.Message);
                SetConsoleColor(1);
            }
            catch (Exception e)
            {
                SetConsoleColor(3);
                Console.WriteLine("Ein unerwarteter Fehler im Zusammenhang mit " + ip + " ist aufgetreten: " + e.Message);
                SetConsoleColor(1);
            }
        }
    }
}
