using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minecraft_Launcher
{
    public partial class Settings : Form
    {
        Form1 f;
        public Settings(Form1 f)
        {
            InitializeComponent();
            this.f = f;
            SetTextbox();
             
        }
        private void SetTextbox()
        {

            textBox1.Text = Properties.Settings.Default.MaxMemory.ToString();
            textBox2.Text = Properties.Settings.Default.MinMemory.ToString();
            textBox3.Text = Properties.Settings.Default.Version.ToString();
            textBox4.Text = Properties.Settings.Default.Serverversion.ToString();
            textBox5.Text = Properties.Settings.Default.Height.ToString();
            textBox6.Text = Properties.Settings.Default.Width.ToString();
            textBox7.Text = Properties.Settings.Default.Domain.ToString();
            textBox8.Text = Properties.Settings.Default.Java.ToString();
        }
        private void Settings_Load(object sender, EventArgs e)
        {

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Label6_Click(object sender, EventArgs e)
        {

        }

        private void Label5_Click(object sender, EventArgs e)
        {

        }

        private void Label4_Click(object sender, EventArgs e)
        {

        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void TextBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Label7_Click(object sender, EventArgs e)
        {

        }

        public void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                string pathToFolder = Environment.GetEnvironmentVariable("APPDATA") + @"/.TheMineZone";
                string pathToConfig = Environment.GetEnvironmentVariable("APPDATA") + @"/.TheMineZone/config.txt";

                Form1 f;
                    if (this.f != null)
                        f = this.f;
                    else
                        f = new Form1(true);
                f.Delete_Config();

                if (!File.Exists(pathToConfig))
                    File.WriteAllText(pathToConfig, String.Format("MaxMemory={0}\r\nMinMemory={1}\r\nVersion={2}\r\nServerversion={3}\r\nheight={4}\r\nwidth={5}\r\nUpdateserver={6}\r\nJava={7}", textBox1.Text,
                        textBox2.Text, textBox3.Text, textBox4.Text, textBox5.Text, textBox6.Text, textBox7.Text.Trim(), textBox8.Text.Trim()));
                if(sender != null || e != null)
                    MessageBox.Show("Erfolgreich gespeichert!", "Datei gespeichert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                f.Set_Config();
                SetTextbox();
                if (this.f == null)
                {
                    f.Dispose();
                }
            }
            catch(IOException ex)
            {
                MessageBox.Show("Dateifehler: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
              
            }
            catch(Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.Delete_Config();
            f.Create_Config();
            f.Set_Config();
            SetTextbox();
            f.Dispose();
        }

        private void TextBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void Label9_Click(object sender, EventArgs e)
        {

        }
    }
}
