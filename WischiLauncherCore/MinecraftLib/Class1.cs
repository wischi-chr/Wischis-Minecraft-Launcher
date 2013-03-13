using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.Net;
using Ionic.Zip;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using System.Web;
using WischisLauncherCore;

namespace WischisMinecraftCore
{
    public class MinecraftAuth
    {
        private static string ScrambleString(string pass)
        {
            return Crypter.Encrypt(pass, "\\/\\/15C|-|I L4UNCH3R", "Salzmich", "SHA1", 1, "1234567890abcdef", 128);
        }

        private static string DeScrambleString(string crypted)
        {
            try
            {
                return Crypter.Decrypt(crypted, "\\/\\/15C|-|I L4UNCH3R", "Salzmich", "SHA1", 1, "1234567890abcdef", 128);
            }
            catch
            {
                return "";
            }
        }

        public static bool ProfileAuth(LauncherProfile Profil,out string SessionID,out string MC_User,string optpass="")
        {
            string pass = DeScrambleString(Profil.Settings.MinecraftLoginPassword);
            if (pass == "")
            {
                pass = optpass;
            }
            string sid = "";
            try
            {
                string CorrectUser;
                sid = MinecraftAuth.TestAuth(Profil.Settings.MinecraftLoginUser, pass, out CorrectUser);
                MC_User = CorrectUser;
                //Profil.Settings.MinecraftLoginUser = CorrectUser;
                SessionID = sid;
                return true;
            }
            catch (Exception ex)
            {
                SessionID = ex.Message;
                MC_User = null;
                return false;
            }
        }


        public static string TestAuth(string user,string pass,out string CorrectUser)
        {
            //HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create("http://session.minecraft.net/game/getversion.jsp");
            HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create("https://login.minecraft.net/");
            ASCIIEncoding encoding = new ASCIIEncoding();
            string postData = "user=" + System.Web.HttpUtility.UrlEncode(user) + "&password=" + System.Web.HttpUtility.UrlEncode(pass) + "&version=13";
            byte[] data = encoding.GetBytes(postData);

            HttpWReq.Method = "POST";
            HttpWReq.ContentType = "application/x-www-form-urlencoded";
            HttpWReq.ContentLength = data.Length;

            Stream newStream = HttpWReq.GetRequestStream();
            newStream.Write(data,0,data.Length);
            newStream.Close();

            WebResponse resp = HttpWReq.GetResponse();
            string code = (new StreamReader(resp.GetResponseStream())).ReadToEnd();

            string[] teile = code.Split(':');

            if (!code.Contains(":")) throw new Exception(code);

            string currentVersion = teile[0].Trim();
            string downloadTicket = teile[1].Trim();
            CorrectUser = teile[2].Trim();
            string SessionID = teile[3].Trim();

            return SessionID;
        }
        public static CookieContainer WischiAuth(string sid, string user,out string Hinweis)
        {
            CookieContainer cc = new CookieContainer();
            
            //GET-Hash from Wischis-Server
            System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://dev.wischenbart.org/minecraft/requesthash.php?user=" + HttpUtility.UrlPathEncode(user));
            webreq.CookieContainer = cc;
            string hash = new StreamReader(webreq.GetResponse().GetResponseStream()).ReadToEnd();

            //Register-Hash on Mojangs-Server
            System.Net.HttpWebRequest webreq2 = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://session.minecraft.net/game/joinserver.jsp?user=" + HttpUtility.UrlPathEncode(user) + "&sessionId=" + HttpUtility.UrlPathEncode(sid) + "&serverId=" + HttpUtility.UrlPathEncode(hash));
            string antw = new StreamReader(webreq2.GetResponse().GetResponseStream()).ReadToEnd();
            Hinweis = hash + ":" + antw + ":" + cc.Count;
            if (antw.Trim() != "OK") return null;
            return cc;
        }

        public static string CheckWischiAuth(CookieContainer cc)
        {
            System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://dev.wischenbart.org/minecraft/check.php");
            webreq.CookieContainer = cc;
            return new StreamReader(webreq.GetResponse().GetResponseStream()).ReadToEnd();
        }
    }

    public static class MinecraftRessourceDownloader
    {
        public static void DownloadBins(string binfolder,ExternalMinecraftLauncher.WischiLauncherMainForm form=null)
        {
            #region lwjgl_util.jar
            FileDownload("http://dev.wischenbart.org/minecraft/bin/lwjgl_util.jar", binfolder + Path.DirectorySeparatorChar + "lwjgl_util.jar",form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/jinput.jar", binfolder + Path.DirectorySeparatorChar + "jinput.jar", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/lwjgl.jar", binfolder + Path.DirectorySeparatorChar + "lwjgl.jar", form);

            //natives
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/jinput-raw.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "jinput-raw.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/jinput-dx8.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "jinput-dx8.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/jinput-raw_64.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "jinput-raw_64.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/jinput-dx8_64.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "jinput-dx8_64.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/OpenAL32.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "OpenAL32.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/lwjgl.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "lwjgl.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/OpenAL64.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "OpenAL64.dll", form);
            FileDownload("http://dev.wischenbart.org/minecraft/bin/natives/lwjgl64.dll", binfolder + Path.DirectorySeparatorChar + "natives" + Path.DirectorySeparatorChar + "lwjgl64.dll", form);

            #endregion
        }

        public static void FileDownload(string url, string file, ExternalMinecraftLauncher.WischiLauncherMainForm form = null)
        {
            if (File.Exists(file)) return;
            Debug.WriteLine(file);
            if (form != null) form.AddConsoleLine("download: " + Path.GetFileName(file));

            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            WebRequest req_inner = WebRequest.Create(url);
            Stream resourceread = req_inner.GetResponse().GetResponseStream();
            byte[] Buffer = new byte[1024 * 1024];
            int written;
            while ((written = resourceread.Read(Buffer, 0, Buffer.Length)) > 0)
            {
                fs.Write(Buffer, 0, written);
            }

            fs.Close();
            resourceread.Close();
        }

    }

    public class MinecraftBinJarLocation
    {
        string Location;
        bool encryption;

        public bool NeedAuth
        {
            get
            {
                return _needauth;
            }
        }

        public string Name
        {
            get
            {
                return titel;
            }
        }

        public string LocationName { get; set; }

        public string DownloadLocation
        {
            get
            {
                return Location;
            }
        }

        bool _needauth;
        //string locname;
        string titel;
        string hash;

        public MinecraftBinJar GetMinecraftBinJar(CookieContainer cc = null)
        {
            // the URL to download the file from
            string sUrlToReadFileFrom = Location;

            // first, we need to get the exact size (in bytes) of the file we are downloading
            Uri url = new Uri(sUrlToReadFileFrom);

            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            if (cc != null && req is HttpWebRequest)
            {
                HttpWebRequest r = req as HttpWebRequest;
                r.CookieContainer = cc;
            }
            System.Net.WebResponse response = req.GetResponse();
            
            // gets the size of the file in bytes
            Int64 iSize = response.ContentLength;
            
            // keeps track of the total bytes downloaded so we can update the progress bar
            //Int64 iRunningByteTotal = 0;

            MemoryStream streamLocal = new MemoryStream();
            Stream streamRemote = response.GetResponseStream();

            int iByteSize = 0;
            byte[] byteBuffer = new byte[iSize];
            while ((iByteSize = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                streamLocal.Write(byteBuffer, 0, iByteSize);
            }

            response.Close();

            streamLocal.Position = 0;
            ZipFile sd = ZipFile.Read(streamLocal);



            MinecraftBinJar jar = new MinecraftBinJar(sd);

            return jar;
        }

        public string Hash
        {
            get
            {
                return hash;
            }
        }

        public MinecraftBinJarLocation(string name,string LocName, string Location,string hash,bool needAuth = false,bool encrypted = false)
        {
            this.Location = Location;
            this.LocationName = LocName;
            encryption = encrypted;
            titel = name;
            _needauth = needAuth;
            this.hash = hash;
        }

        public override string ToString()
        {
            return titel + " [" + LocationName + "]";
        }
    }

    public class MinecraftBinJar
    {
        bool cleaned = false;
        private ZipFile MinecraftJarZip { get; set; }

        public MinecraftBinJar(ZipFile basefile)
        {
            MinecraftJarZip = basefile;
        }

        //löscht beim ersten Mod die META-INF
        private void CleanMetaInf()
        {
            List<ZipEntry> remove = new List<ZipEntry>();
            foreach (ZipEntry ent in MinecraftJarZip.Entries)
            {
                if (Regex.IsMatch(ent.FileName, "META-INF/.*")) remove.Add(ent);
            }

            MinecraftJarZip.RemoveEntries(remove);
            cleaned = true;
        }

        public Dictionary<string, string> AddModPack(ZipFile ModPack, WischisLauncherCore.LauncherProfile Profil)
        {
            Dictionary<string, string> ModListe = new Dictionary<string, string>();
            string ModName = Path.GetFileName(ModPack.Name);

            if (ModPack.EntryFileNames.Contains("modindex.txt"))
            {
                string index = new StreamReader(ModPack["modindex.txt"].OpenReader()).ReadToEnd();
                char[] splitchars = {'\r','\n'};
                string[] zeilen = index.Split(splitchars);

                foreach (string zzz in zeilen)
                {
                    string zeile = zzz.Trim();
                    if (zeile == "" || zeile.Substring(0, 1) == "#" || zeile.Substring(0, 1) == ";") continue;

                    string[] cmd = zeile.Split('\t');
                    if (cmd.Length <= 0) continue;

                    switch(cmd[0])
                    {
                        case "copy":
                            //falls nicht genau 1 befehl und 2 argumente, brich ab.
                            if (cmd.Length != 3) break;

                            foreach (ZipEntry ent in ModPack)
                            {
                                if (!Regex.IsMatch(ent.FileName, cmd[1]) || ent.FileName.Substring(ent.FileName.Length-1,1)=="/") continue;
                                string location = Regex.Replace(ent.FileName, cmd[1], cmd[2], RegexOptions.None);

                                location = LauncherFolderStructure.GetSpecificMinecraftFolder(Profil) + Path.DirectorySeparatorChar + location;

                                if (File.Exists(location)) continue;
                                string dir = Path.GetDirectoryName(location);

                                string fname = dir + Path.DirectorySeparatorChar + Path.GetFileName(location);
                                ModListe.Add(("file|" + fname).ToLower(), ModName.ToLower());
                                if(Directory.Exists(dir))Directory.CreateDirectory(dir);
                                FileStream fs = File.Open(location,FileMode.OpenOrCreate,FileAccess.Write);
                                ent.Extract(fs);
                                fs.Close();
                            }

                            break;

                        case "patch":
                            //falls nicht genau 1 befehl und 2 argumente, brich ab.
                            if (cmd.Length != 3) break;

                            foreach (ZipEntry ent in ModPack)
                            {
                                if (!Regex.IsMatch(ent.FileName, cmd[1]) || ent.FileName.Substring(ent.FileName.Length - 1, 1) == "/") continue;
                                string location = Regex.Replace(ent.FileName, cmd[1], cmd[2], RegexOptions.None);
                                ModListe.Add(("patch|" + ent.FileName).ToLower(), ModName.ToLower());
                                AddModFile(ent.OpenReader(),location);
                            }

                            break;

                        default:
                            //was ist das für ein befehl?
                            break;
                    }
                }
            }
            else
            {
                foreach (ZipEntry ent in ModPack)
                {
                    if(!ent.IsDirectory)ModListe.Add(("patch|" + ent.FileName).ToLower(), ModName.ToLower());
                    AddModFile(ent);
                }
            }

            return ModListe;
        }

        public void AddModFile(ZipEntry ModFile)
        {
            AddModFile(ModFile.OpenReader(), ModFile.FileName);
        }

        public void AddModFile(Stream ModStream,string locname)
        {
            if (!cleaned) CleanMetaInf();

            byte[] cont = new byte[ModStream.Length];
            ModStream.Read(cont, 0, cont.Length);

            if (MinecraftJarZip.EntryFileNames.Contains(locname)) MinecraftJarZip.RemoveEntry(locname);
            MinecraftJarZip.AddEntry(locname, cont);
        }

        public void Save(string Path)
        {
            string dir = System.IO.Path.GetDirectoryName(Path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            MinecraftJarZip.Save(Path);
        }
    }

}
