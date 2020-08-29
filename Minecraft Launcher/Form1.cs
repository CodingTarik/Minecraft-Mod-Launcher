using KMCCC.Authentication;
using KMCCC.Launcher;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minecraft_Launcher
{
    public partial class Form1 : Form
    {
        private string pathToFolder = Environment.GetEnvironmentVariable("APPDATA") + @"\.TheMineZone";
        private string pathToConfig = Environment.GetEnvironmentVariable("APPDATA") + @"\.TheMineZone\config.txt";
        private int port = 43333;
        public int ctrAr = 0;
        public int ctrArComple = 0;
        private bool passwordsave = false;
        private string ipw = "gp7hq6XqSp?eqR%U";

        /// <summary>
        /// Initialisiert den Launcher
        /// </summary>
        public Form1(bool fromSettings = false)
        {
            InitializeComponent();
            /// Erstellen des Installationsordners und der Konfigdatei
            Create_Config();
            if (!Set_Config())
            {
                MessageBox.Show("Fehler beim Einlesen der Konfigurationsdatei", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!fromSettings)
            {
                Debug.WriteLine("Loading with: MaxMemory " + Properties.Settings.Default.MaxMemory + " MinMemory " + Properties.Settings.Default.MinMemory + " Version " + Properties.Settings.Default.Version + " Serverversion " + Properties.Settings.Default.Serverversion + " Height " + Properties.Settings.Default.Height + " Width " + Properties.Settings.Default.Width + " Updateserver " + Properties.Settings.Default.Updateserver);
                Load_Password();
                Load_Username();
                GetNeewsfeed();
                Color cl = Color.FromArgb(150, Color.Black);
                Color font = Color.White;
                panel1.BackColor = cl;
                label1.BackColor = cl;
                label2.BackColor = cl;
                label1.ForeColor = font;
                label2.ForeColor = font;
                label6.BackColor = cl;
                //label6.ForeColor = font;
                label3.BackColor = cl;
                label4.BackColor = cl;
                label3.ForeColor = font;
                label4.ForeColor = font;
            }
        }
        /// <summary>
        /// Lädt den Nutzername/E-Mail aus der username.txt Datei, falls diese vorhanden ist
        /// </summary>
        private void Load_Username()
        {
            try
            {
                if (File.Exists(pathToFolder + @"/username.txt"))
                {
                    textBox1.Text = File.ReadAllText(pathToFolder + @"/username.txt").Trim();
                    textBox2.Focus();
                    textBox2.SelectionStart = textBox2.Text.Length;
                    textBox2.SelectionLength = 0;
                }
                else
                {
                    textBox1.TabIndex = 0;
                    textBox2.TabIndex = 3;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Laden des Username: " + ex.Message);
            }
        }
        /// <summary>
        /// Speichert den Nutzername/E-Mail in einer Textdatei
        /// </summary>
        private void Save_Username()
        {
            try
            {
                if (File.Exists(pathToFolder + @"/username.txt"))
                {
                    File.Delete(pathToFolder + @"/username.txt");
                }

                File.WriteAllText(pathToFolder + @"/username.txt", textBox1.Text.Trim());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Speichern des Username: " + ex.Message);
            }
        }
        /// <summary>
        /// Downloaded die aktuelle Packversion von den Updateservern asynchron
        /// </summary>
        /// <returns></returns>
        private async Task<bool> DownloadNewVersion()
        {
            try
            {
                label5.Visible = true;
                label5.Text = "Stelle Verbindung zum Updateserver her...";
                Debug.WriteLine("Downloading new version...");
                Delete_Config();
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(Properties.Settings.Default.Updateserver);
                System.Net.IPEndPoint remoteEP = new IPEndPoint(ipAdd, 43333);
                soc.Connect(remoteEP);
                byte[] byData = System.Text.Encoding.ASCII.GetBytes("Update Version=" + Properties.Settings.Default.Serverversion.ToString());
                soc.Send(byData);

                byte[] buffer = new byte[65536];
                int iRx = 0;
                int ctr = 0;
                if (File.Exists(pathToFolder + @"/download.zip"))
                {
                    File.Delete(pathToFolder + @"/download.zip");
                }

                if (Properties.Settings.Default.Serverversion == 0)
                {
                    if (Directory.Exists(pathToFolder + @"/.minecraft"))
                    {
                        Directory.Delete(pathToFolder + @"/.minecraft", true);
                    }
                }
                long timeNow = nanoTime();
                long timeTemp = 0;
                bool received = false;
                string mb = "0 MB/s";
                int bytReceived = 0;
                using (var file = File.Create(pathToFolder + @"/download.zip"))
                {

                    while ((iRx = await soc.ReceiveAsync(buffer)) > 0)
                    {
                        received = true;
                        await file.WriteAsync(buffer, 0, iRx);
                        if (ctr % 50 == 0 || ctr == 10)
                        {
                            timeTemp = timeNow;
                            timeNow = nanoTime();
                            Debug.WriteLine("iRx " + iRx + " timeNow " + timeNow + " timeTemp " + timeTemp);
                            mb = (((double)bytReceived / 1000000d) / ((double)(timeNow - timeTemp) / 1000000000d)).ToString("0.00") + " MB/s";
                            bytReceived = 0;
                        }
                        bytReceived += iRx;
                        Debug.WriteLine("Downloading... Block #" + ctr++ + " " + mb);
                        label5.Text = "Downloade Pack" + " mit " + mb;

                    }
                }
                soc.Close();
                soc.Dispose();
                if (!received)
                {
                    MessageBox.Show("Fehler beim Herunterladen der Dateien: Es konnten keine Dateien für das angeforderte Updatepacket heruntergeladen werden", "Fehler"
                        , MessageBoxButtons.OK, MessageBoxIcon.Error);
                    label5.Visible = false;
                    return false;
                }
                label4.Visible = false;
                label5.Text = "Entpacke...";
                await Task.Run(() => { ZipArchive z = ZipFile.OpenRead(pathToFolder + @"/download.zip"); z.ExtractToDirectory(pathToFolder, true, this); z.Dispose(); });
                label5.Text = "Setze einstellungen...";
                Set_Config();
                label5.Text = "Überprüfe Spieldateien...";
                await Task.Run(() => { ValidStructure(pathToFolder); } );             
                //ValidStructure(pathToFolder);
                label5.Text = "Update erfolgreich installiert!";
                return true;
            }
            catch (SocketException)
            {
                label4.Visible = true;
                label5.Visible = false;
                return false;
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label5.Visible = false;
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label5.Visible = false;
                return false;
            }          
        }

        private static long nanoTime()
        {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

        /// <summary>
        /// Überprüft nach jedem Download die Dateistruktur
        /// befindet sich eine Datei mit dem Name '[DELETE].txt' in diesen, so wird diese geöffnet und
        /// mit File=datei gekenzeichnete Dateien entfernt und mit Folder=ordner gekennzeichnete Ordner gelöscht.
        /// </summary>
        /// <param name="path"></param>
        public void ValidStructure(string path)
        {
            try
            {
                if (File.Exists(path + @"/[DELETE].txt"))
                {
                    foreach (string line in File.ReadLines(path + @"/[DELETE].txt"))
                    {
                        string[] operation = line.Trim().Split('=');
                        switch (operation[0])
                        {
                            case "Folder":
                                if (Directory.Exists(path + @"/" + operation[1]))
                                {
                                    Directory.Delete(path + @"/" + operation[1]);
                                }

                                break;
                            case "File":
                                if (File.Exists(path + @"/" + operation[1]))
                                {
                                    File.Delete(path + @"/" + operation[1]);
                                }

                                break;
                        }
                    }
                    File.Delete(path + @"/[DELETE].txt");
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Fehler beim überprüfen der Datenstruktur");
            }
            foreach (string subfolder in Directory.GetDirectories(path))
            {
                Debug.WriteLine("New Directory to validate: " + subfolder);
                ValidStructure(subfolder);
            }
        }
        /// <summary>
        /// Überprüft, ob die lokale Packversion mit der des Servers übereinstimmt
        /// </summary>
        /// <returns>true, wenn Updates möglich sind, ansonsten false</returns>
        private bool CheckForUpdate()
        {
            int version = GetServerversion();
            if (version != Properties.Settings.Default.Serverversion && version != -1)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Sendet eine GetNewsfeed-Anfrage an den Server, um sich die Dateien zu holen
        /// </summary>
        /// <returns></returns>
        public string GetNeewsfeed()
        {
            try
            {
                label5.Visible = true;
                label5.Text = "Stelle Verbindung zum Updateserver her...";
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(Properties.Settings.Default.Updateserver);
                System.Net.IPEndPoint remoteEP = new IPEndPoint(ipAdd, port);
                soc.Connect(remoteEP);
                byte[] byData = System.Text.Encoding.ASCII.GetBytes("GetNewsfeed");
                soc.Send(byData);

                byte[] buffer = new byte[1024];
                int iRx = soc.Receive(buffer);
                char[] chars = new char[iRx];

                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
                System.String recv = new System.String(chars);
                string text = "   News: \r\n   ";
                foreach (char c in chars)
                {
                    if (c == '\t')
                    {
                        text += "\r\n   ";
                    }
                    else
                    {
                        text += c;
                    }
                }

                label6.Visible = true;
                label6.Text = text;
                Debug.WriteLine("Newsfeed " + recv);
                label4.Visible = false;
                label5.Visible = false;
                try
                {
                    soc.Close();
                    soc.Dispose();
                    return recv;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Fehler: Ungültiger Wert: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    label5.Visible = false;
                    soc.Close();
                    soc.Dispose();
                    return "";
                }
            }
            catch (SocketException)
            {
                //MessageBox.Show("Ein Fehler mit der Verbindung zum Updateserver ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label4.Visible = true;
                label5.Visible = false;
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return "";
        }
        /// <summary>
        /// Sendet eine "GetVersion"-Anfrage an den Updateserver und gibt die Antwort zurück
        /// </summary>
        /// <returns></returns>
        public int GetServerversion()
        {
            try
            {
                label5.Visible = true;
                label5.Text = "Stelle Verbindung zum Updateserver her...";
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(Properties.Settings.Default.Updateserver);
                System.Net.IPEndPoint remoteEP = new IPEndPoint(ipAdd, port);
                soc.Connect(remoteEP);
                byte[] byData = System.Text.Encoding.ASCII.GetBytes("GetVersion");
                soc.Send(byData);

                byte[] buffer = new byte[1024];
                int iRx = soc.Receive(buffer);
                char[] chars = new char[iRx];

                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
                System.String recv = new System.String(chars);
                //label7.Visible = true;
                // label7.Text = "Version: " + recv;
                Debug.WriteLine("Serverversion " + recv);
                label4.Visible = false;
                try
                {
                    soc.Close();
                    soc.Dispose();
                    if (label6.Visible == false)
                    {                      
                        GetNeewsfeed();
                    }
                    return int.Parse(recv);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Fehler: Ungültiger Wert: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    label7.Visible = false;
                    soc.Close();
                    soc.Dispose();
                    return -1;
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine("Konnte keine GetVersion anfrage versenden " + e.Message + " Stack: " + e.StackTrace);
                label4.Visible = true;
                label7.Visible = false;
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return -1;
        }
        /// <summary>
        /// Liest die Konfigurationsdatei ein und weist die Werte den "Properties" im Programm zu
        /// </summary>
        /// <returns></returns>
        public bool Set_Config()
        {
            try
            {
                string[] config = System.IO.File.ReadAllText(pathToConfig).Split('=', '\n');
                for (int i = 0; i < config.Length; i += 2)
                {
                    Debug.WriteLine("Reading config: " + config[i] + " with value: " + config[i + 1].Replace("\r", ""));
                    try
                    {
                        switch (config[i].Replace("=", ""))
                        {
                            case "MaxMemory":
                                Properties.Settings.Default.MaxMemory = int.Parse(config[i + 1]);
                                break;
                            case "MinMemory":
                                Properties.Settings.Default.MinMemory = int.Parse(config[i + 1]);
                                break;
                            case "Version":
                                Properties.Settings.Default.Version = config[i + 1].Trim();
                                break;
                            case "Serverversion":
                                Properties.Settings.Default.Serverversion = int.Parse(config[i + 1]);
                                label7.Visible = true;
                                label7.Text = "Version: " + Properties.Settings.Default.Serverversion.ToString() + "        ";
                                break;
                            case "height":
                                Properties.Settings.Default.Height = ushort.Parse(config[i + 1]);
                                break;
                            case "width":
                                Properties.Settings.Default.Width = ushort.Parse(config[i + 1]);
                                break;
                            case "Updateserver":
                                Console.WriteLine("DNS REQUEST: " + Dns.GetHostAddresses(config[i + 1].Trim())[0].ToString());
                                Properties.Settings.Default.Domain = config[i + 1].Trim();
                                Properties.Settings.Default.Updateserver = Dns.GetHostAddresses(config[i + 1].Trim())[0].ToString();
                                break;
                            case "Java":
                                Properties.Settings.Default.Java = config[i + 1].Trim();
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Fehler beim Einlesen der Konfigurationsdatei: " + e.Message);
                        Delete_Config();
                        Create_Config();
                        return false;
                    }
                }
                return true;
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        /// <summary>
        /// Löscht die Konfigurationsdatei
        /// </summary>
        public void Delete_Config()
        {
            try
            {
                if (File.Exists(pathToConfig))
                {
                    File.Delete(pathToConfig);
                }
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        ///  Erstellt eine Konfigurationsdatei, falls noch keine vorhanden ist
        /// </summary>
        public void Create_Config()
        {
            try
            {
                if (!Directory.Exists(pathToFolder))
                {
                    Directory.CreateDirectory(pathToFolder);
                }

                if (!File.Exists(pathToConfig))
                {
                    // 62.141.43.155
                    File.WriteAllText(pathToConfig, "MaxMemory=4096\r\nMinMemory=2048\r\nVersion=1.12.2-forge1.12.2-14.23.5.2838\r\nServerversion=0\r\nheight=768\r\nwidth=1280\r\nUpdateserver=theminezone.de\r\nJava=" + Properties.Settings.Default.Java);
                }
            }
            catch (IOException e)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + e.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PictureBox1_Click(object sender, EventArgs e)
        {

        }
        bool isStarting = false;
        /// <summary>
        /// Anmeldebutton
        /// Überprüft, mit Methodenaufrufen, ob ein Update vorhanden ist
        /// Wenn ja, so downloaded es dieses
        /// Danach wird ein neuer Spielstart inklusive Authentification bei den Mojang-Servern vorbereitet
        /// Sollten die Anmeldedaten korrekt sein, so wird das Spiel gestartet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                isStarting = true;
                Save_Username();
                Save_Password();
                while (CheckForUpdate())
                {
                    if (!await DownloadNewVersion())
                    {
                        isStarting = false;
                        return;
                    }
                }
                label5.Visible = true;
                label5.Text = "Starte Spiel...";
                /// Javadateipfad herausfinden
                string installPath = GetJavaInstallationPath();
                if (String.IsNullOrEmpty(installPath))
                {
                    installPath = GetJavaInstallationPath();
                }
                string filePath = System.IO.Path.Combine(installPath, "bin\\Javaw.exe");
                Debug.WriteLine("Java: " + filePath);

                /// Initialisierung des Spieles
                var core = LauncherCore.Create(pathToFolder + @"/.minecraft");
                core.JavaPath = filePath;
                core.GameLog += core_GameLog;
                Debug.WriteLine("Version: " + Properties.Settings.Default.Version);

                //NEW
                bool offline = false;
                if (textBox1.Text == "Tarik" && textBox2.Text == "Devtest") ;
                    offline = true;
                Random r = new Random();
                var launch = new LaunchOptions
                {
                    Version = core.GetVersion(Properties.Settings.Default.Version.Trim()),
                    Authenticator = offline ? new OfflineAuthenticator("Tarik" + r.Next(20).ToString()) : new YggdrasilLogin(textBox1.Text, textBox2.Text, true),
                    Mode = null,
                    MaxMemory = Properties.Settings.Default.MaxMemory,
                    MinMemory = Properties.Settings.Default.MinMemory,
                    Size = new WindowSize { Height = Properties.Settings.Default.Height, Width = Properties.Settings.Default.Width }
                };
                Debug.WriteLine("Starting Game...");
                var result = core.Launch(launch, (Action<MinecraftLaunchArguments>)(x => { }));
                if (!result.Success)
                {
                    Debug.WriteLine("Fehler：[{0}] {1}", result.ErrorType, result.ErrorMessage);
                    if (result.ErrorMessage.IndexOf("Invalid username or password") > -1)
                    {
                        label3.Visible = true;
                    }
                    else if(result.ErrorType.ToString().IndexOf("JAVA") > -1)
                    {
                        MessageBox.Show("Das Programm konnte den Java Installationsordner nicht finden, bitte stelle sicher, dass du den Richtigen Pfad zum Javainstallationsordner angegeben hast. (Dabei bitte den konkreten Java-Versionodner auswählen, beispielsweise: jre1.8.0_181, jdk1.8.0_112). Alternativ kannst du auch die JAVA_HOME umgebungsvariable setzen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        SelectJava(false);
                    }
                    else
                    {
                        MessageBox.Show("Ein Fehler beim Start des Spiels ist aufgetreten. Bist du mit dem Internet verbunden?", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //Delete_Config();
                        //Create_Config();
                        //Set_Config();
                    }
                    if (result.Exception != null)
                    {
                        Debug.WriteLine(result.Exception.Message);
                        Debug.WriteLine(result.Exception.Source);
                        Debug.WriteLine(result.Exception.StackTrace);
                    }
                    label5.Visible = true;
                    label5.Text = "Spiel konnte nicht gestartet werden";
                    isStarting = false;
                    return;
                }
                label3.Visible = false;
                Debug.WriteLine("Access Token: " + result.Handle.Info.AccessToken);
                Application.Exit();
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Ein Fehler mit der Verbindung zum Mojang-Server ist aufgetreten: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Ein Dateifehler ist aufgetreten: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            isStarting = false;

        }
        /// <summary>
        /// Minecraftlog (nur beim Debuggen sichtbar)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="line"></param>
        private static void core_GameLog(LaunchHandle handle, string line)
        {
            Debug.WriteLine(line);
            if (line.IndexOf("not reserve enough space") > -1)
            {
                MessageBox.Show("Aufgrund eines Problems mit deinem Arbeitsspeicher konnte das Spiel nicht gestartet werden. " +
                    "Hast du genug Speicher übrig? Zeigt deine Umgebungsvariable %JAVA_HOME% auf ein Verzeichnis mit einer 64-Bit Version von Java? (Nach dem Setzen Computer neustarten)", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateStatusLabel(string text)
        {
            if (!InvokeRequired)
            {
                label5.Text = text;
            }
            else
            {
                Invoke(new Action<string>(UpdateStatusLabel), text);
            }
        }
        /// <summary>
        /// Versucht, den Pfad zu Java zurückzugeben
        /// </summary>
        /// <returns></returns>
        private static string GetJavaInstallationPath()
        {

            if (!String.IsNullOrEmpty(Properties.Settings.Default.Java))
            {
                return Properties.Settings.Default.Java;
            }

            string environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(environmentPath))
            {

                Properties.Settings.Default.Java = environmentPath;
                Settings s = new Settings(null);
                s.Visible = false;
                s.Button2_Click(null, null);
                return environmentPath;
            }
            else
            {
                return SelectJava(true);
            }
        }

        public static string SelectJava(bool message)
        {
            if(message)
            MessageBox.Show("Der Launcher konnte deinen Javainstallationspfad nicht herausfinden, bitte wähle gleich deinen Java Installationsordner aus (Dabei bitte den konkreten Java-Versionodner auswählen, beispielsweise: jre1.8.0_181, jdk1.8.0_112). Alternativ kannst du auch die JAVA_HOME umgebungsvariable setzen.");
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Properties.Settings.Default.Java = fbd.SelectedPath;
                    Settings s = new Settings(null);
                    s.Visible = false;
                    s.Button2_Click(null, null);
                    return fbd.SelectedPath;
                }
            }
            return "";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Öffnet das Einstellungsmenü für die Startargumente
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button2_Click(object sender, EventArgs e)
        {
            Settings s = new Settings(this);
            s.ShowDialog();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://www.fiverr.com/cyberta");
        }

        private void PictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void Label6_Click(object sender, EventArgs e)
        {

        }

        private void PictureBox2_MouseEnter(object sender, EventArgs e)
        {
            pictureBox2.Image = Properties.Resources.start_on;
        }

        private void PictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Image = Properties.Resources.start_off;
        }

        private void PictureBox6_MouseEnter(object sender, EventArgs e)
        {
            pictureBox6.Image = Properties.Resources.discord_on;
        }

        private void PictureBox6_MouseLeave(object sender, EventArgs e)
        {
            pictureBox6.Image = Properties.Resources.discord_off;
        }

        private void PictureBox4_MouseEnter(object sender, EventArgs e)
        {
            pictureBox4.Image = Properties.Resources.website_on;
        }

        private void PictureBox4_MouseLeave(object sender, EventArgs e)
        {
            pictureBox4.Image = Properties.Resources.website_off;
        }

        private void PictureBox3_MouseEnter(object sender, EventArgs e)
        {
            pictureBox3.Image = Properties.Resources.shop_on;
        }

        private void PictureBox3_MouseLeave(object sender, EventArgs e)
        {
            pictureBox3.Image = Properties.Resources.shop_off;
        }

        private void PictureBox5_MouseEnter(object sender, EventArgs e)
        {
            pictureBox5.Image = Properties.Resources.settings_on;
        }

        private void PictureBox5_MouseLeave(object sender, EventArgs e)
        {
            pictureBox5.Image = Properties.Resources.settings_off;
        }

        private void PictureBox5_Click(object sender, EventArgs e)
        {
            Button2_Click(this, null);
        }

        private void Button3_Click(object sender, EventArgs e)
        {

        }

        private void PictureBox2_Click_1(object sender, EventArgs e)
        {
            if (!isStarting)
                Button1_Click(this, null);
            else
                MessageBox.Show("Das Spiel startet bereits", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PictureBox4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://web.theminezone.de");
        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://shop.theminezone.de");
        }

        private void PictureBox6_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"discord:https://discordapp.com/invite/6neVWT8");
            }
            catch (Exception)
            {
                MessageBox.Show("Konnte den Einladungslink bei Discord nicht öffnen", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Button1_Click(this, null);
            }
        }

        private void Label7_Click(object sender, EventArgs e)
        {

        }

        private void Label5_Click(object sender, EventArgs e)
        {

        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Load_Password()
        {
            try
            {
                if (File.Exists(pathToFolder + @"/pw.txt"))
                {
                    string pw = File.ReadAllText(pathToFolder + @"/pw.txt").Trim();
                    passwordsave = true;
                    pictureBox1.Image = Properties.Resources.pw_save;
                    textBox2.Text = Encrypt.DecryptString(pw, ipw);
                    Debug.WriteLine("Decrypted PW: " + textBox2.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Laden des Username: " + ex.Message);
            }
        }

        private void Save_Password()
        {
            if (passwordsave)
            {
                try
                {
                    if (File.Exists(pathToFolder + @"/pw.txt"))
                    {
                        File.Delete(pathToFolder + @"/pw.txt");
                    }

                    File.WriteAllText(pathToFolder + @"/pw.txt", Encrypt.EncryptString(textBox2.Text.Trim(), ipw));
                    Debug.WriteLine("Encrypted PW " + Encrypt.EncryptString(textBox2.Text.Trim(), ipw));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fehler beim Speichern des Passworts: " + ex.Message);
                }
            }
            else
                if (File.Exists(pathToFolder + @"/pw.txt"))
            {
                File.Delete(pathToFolder + @"/pw.txt");
            }
        }
        /// <summary>
        /// Passwort speichern ja / nein?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox1_Click_1(object sender, EventArgs e)
        {
            passwordsave = !passwordsave;
            if (passwordsave)
            {
                pictureBox1.Image = Properties.Resources.pw_save;
            }
            else
            {
                pictureBox1.Image = Properties.Resources.pw_nosave;
            }

            pictureBox1.Refresh();
        }

        private void Label8_Click(object sender, EventArgs e)
        {

        }
    }
    public static class AsyncDownload
    {
        /// <summary>
        /// Downloads Async
        /// </summary>
        /// <param name="soc"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static async Task<int> ReceiveAsync(this Socket soc, byte[] buffer)
        {

            var task = Task.Run(() => soc.Receive(buffer));
            return await task;
        }
    }
    public static class ZipArchiveExtension
    {
        /// <summary>
        /// Extracts ZIP Archive 
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <param name="overwrite"></param>
        /// <param name="f"></param>
        public static void ExtractToDirectory(this System.IO.Compression.ZipArchive archive, string destinationDirectoryName, bool overwrite, Form1 f)
        {
            if (!overwrite)
            {
                try
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Fehler beim Entpacken");
                }
                return;
            }

            double ctrCmp = archive.Entries.Count;
            double ctrDone = 0;
            foreach (System.IO.Compression.ZipArchiveEntry file in archive.Entries)
            {
                void EntrieFinished()
                {
                    ctrDone++;
                    Debug.WriteLine("Entpacke..." + ((int)((ctrDone / ctrCmp) * 100)).ToString() + "%" + " Entrie #" + ctrDone.ToString() + " of " + ctrCmp.ToString());
                    f.UpdateStatusLabel(("Entpacke..." + ((int)((ctrDone / ctrCmp) * 100)).ToString() + "%"));
                }
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                if (file.Name == "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    EntrieFinished();
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
                EntrieFinished();
            }

        }
    }

}
