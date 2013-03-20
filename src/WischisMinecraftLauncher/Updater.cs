using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using System.Reflection;
using System.Net;
using System.Threading;

namespace WischisMinecraftLauncher
{
    public partial class Updater : Form
    {
        public Updater()
        {
            InitializeComponent();
            LoadMinecraftFont();
            //verticalLabel1.Font = new Font(MinecraftFont, 12);
        }

        public string LoadingText
        {
            get
            {
                return pictureBox2.Tag.ToString(); //verticalLabel1.Text;
            }
            set
            {
                /*verticalLabel1.Text = value;
                verticalLabel1.Invalidate();*/
                pictureBox2.Tag = value;
                pictureBox2.Invalidate();
                Thread.Sleep(300);
                Application.DoEvents();
            }
        }

        PrivateFontCollection PrvFontCollection;
        FontFamily MinecraftFont;


        //Load font family from stream
        public FontFamily LoadFontFamily(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            IntPtr data = Marshal.AllocCoTaskMem((int)stream.Length);

            Marshal.Copy(buffer, 0, data, (int)stream.Length);
            PrvFontCollection = new PrivateFontCollection();
            PrvFontCollection.AddMemoryFont(data, (int)stream.Length);

            Marshal.FreeCoTaskMem(data);
            return PrvFontCollection.Families[0];
        }

        void LoadMinecraftFont()
        {
            //Font
            string EmbeddedFont = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "minecraft.ttf";
            Stream FontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedFont);
            MinecraftFont = LoadFontFamily(FontStream);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        bool actived = false;
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (actived) return;
            actived = true;

            Assembly CoreAsm = null;

            string asmFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + ".wischilauncher" + Path.DirectorySeparatorChar + "internal.dat";
            #if DEBUG
            asmFile = @"C:\Users\Christian\Documents\Visual Studio 2010\Projects\WischiLauncherSVN\WischiLauncherCore\bin\Debug\WischisMinecraftLauncherCoreDLL.exe";
            #endif
            Version LocalVersion = new Version(0, 0, 0);
            Version OnlineVersion = new Version(0, 0, 0);


            LoadingText = "Ermittle Lokale Version...";
            if (File.Exists(asmFile))   //falls datei existiert dann Verison 
            {
                try
                {
                    byte[] AsmMemory = File.ReadAllBytes(asmFile);
                    Assembly OldAsm = Assembly.Load(AsmMemory);
                    LocalVersion = OldAsm.GetName().Version;
                }
                catch
                {
                    LocalVersion = new Version(0, 0, 0);
                }
            }

            LoadingText = "Ermittle Online Version...";
            //Get onlineVersion
            try
            {
                HttpWebRequest Versionrequest = (HttpWebRequest)WebRequest.Create("http://dev.wischenbart.org/minecraft/version.php");
                Versionrequest.Proxy = null;
                Stream Response = Versionrequest.GetResponse().GetResponseStream();
                string Vstring = new StreamReader(Response).ReadToEnd();
                OnlineVersion = new Version(Vstring);
            }
            catch
            {
                OnlineVersion = new Version(0, 0, 0);
            }

            if (LocalVersion < OnlineVersion)
            {
                LoadingText = "Update WMCL...";
                FileStream corefile = null;
                try
                {
                    HttpWebRequest NewCoreRequest = (HttpWebRequest)WebRequest.Create("http://dev.wischenbart.org/minecraft/internal.dat");
                    NewCoreRequest.Proxy = null;
                    Stream CoreFileStream = NewCoreRequest.GetResponse().GetResponseStream();
                    string dir = Path.GetDirectoryName(asmFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    corefile = File.Open(asmFile, FileMode.OpenOrCreate, FileAccess.Write);
                    byte[] buffer = new byte[1024 * 1024];
                    int read;
                    while ((read = CoreFileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        corefile.Write(buffer, 0, read);
                    }
                }
                catch
                {

                }
                finally
                {
                    if (corefile != null) corefile.Close();
                }
            }

            LoadingText = "Load WMCL Core...";
            CoreAsm = null;
            try
            {
                CoreAsm = Assembly.LoadFile(asmFile);
            }
            catch
            {
                MessageBox.Show("CoreDaten konnten nicht geladen werden.");
                Environment.Exit(-4);
            }
            LoadingText = "Start...";
            MethodInfo Entry = CoreAsm.EntryPoint;

            if (Entry == null)
            {
                MessageBox.Show("Einstiegspunkt wurde nicht gefunden.");
                Environment.Exit(-3);
            }

            this.Hide();

            //MethodInfo start 
            try
            {
                Entry.Invoke(null, null);
            }
            catch(Exception ex)
            {
                ExceptionForm asf = new ExceptionForm(ex);
                asf.Show();
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            using (Font myFont = new Font(MinecraftFont, 12))
            {
                if(pictureBox2.Tag is string)
                    e.Graphics.DrawString((string)pictureBox2.Tag, myFont, Brushes.White, new Point(2, 2));
            }
        }

    }




}
