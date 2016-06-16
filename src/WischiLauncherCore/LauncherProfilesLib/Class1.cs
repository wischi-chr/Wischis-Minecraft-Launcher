using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WischisLauncherCore
{
    public class LauncherProfile
    {
        public bool AutoSave
        {
            get; set;
        }
        public string ProfileName
        {
            get
            {
                return profile_name;
            }
        }
        public ProfileSettings Settings
        {
            get; set;
        }

        RegistryKey ProfileRegistryKey;

        private LauncherProfile(string ProfileName)
        {
            AutoSave = true;
            profile_name = System.Text.RegularExpressions.Regex.Replace(ProfileName, @"[\\/:*?""<>|]", string.Empty);
            ProfileRegistryKey = ProfilesRegistryLocation.CreateSubKey(profile_name);

            Settings = new ProfileSettings();
            Settings.PropertyChanged += new PropertyChangedEventHandler(Settings_PropertyChanged);
        }

        public static List<LauncherProfile> GetLauncherProfileList()
        {
            List<LauncherProfile> ProfileList = new List<LauncherProfile>();
            foreach (string profile in ProfilesRegistryLocation.GetSubKeyNames())
            {
                ProfileList.Add(LauncherProfile.Load(profile));
            }
            return ProfileList;
        }

        public static LauncherProfile Load(string ProfileName)
        {
            LauncherProfile prof = new LauncherProfile(ProfileName);
            prof.Load();
            return prof;
        }

        public void Save()
        {
            PropertyInfo[] PInfos = this.Settings.GetType().GetProperties();

            foreach (PropertyInfo inf in PInfos)
            {
                Save(inf.Name);
            }

        }
        public void Load()
        {
            PropertyInfo[] PInfos = this.Settings.GetType().GetProperties();

            foreach (PropertyInfo inf in PInfos)
            {
                internLoad(inf.Name);
            }
        }
        public void Delete()
        {
            try
            {
                Directory.Delete(WischisLauncherCore.LauncherFolderStructure.GetSpecificProfilePath(this), true);
            }
            catch
            {
                
            }
            ProfilesRegistryLocation.DeleteSubKeyTree(ProfileName);
            
        }

        public void Rename(string NewName)
        {
            string oldname = ProfileName;
            ProfilesRegistryLocation.DeleteSubKeyTree(ProfileName);
            profile_name = System.Text.RegularExpressions.Regex.Replace(NewName, @"[\\/:*?""<>|]", string.Empty);
            if (profile_name.Trim() == "") return;
            ProfileRegistryKey = ProfilesRegistryLocation.CreateSubKey(profile_name);

            try
            {
                Directory.Move(WischisLauncherCore.LauncherFolderStructure.GetSpecificProfilePath(oldname), WischisLauncherCore.LauncherFolderStructure.GetSpecificProfilePath(ProfileName));
            }
            catch
            {

            }

            Save();
        }
        public LauncherProfile Clone(string NewProfileName)
        {
            throw new NotImplementedException();
        }

        private string profile_name;

        private void internLoad(string PropertyName)
        {
            PropertyInfo PropInfo = null;
            PropInfo = Settings.GetType().GetProperty(PropertyName);

            object[] atts = PropInfo.GetCustomAttributes(false);
            ProfileSettingStoreAttribute attrb = null;
            foreach (object Attribut in atts)
            {
                if (!(Attribut is ProfileSettingStoreAttribute)) continue;
                attrb = Attribut as ProfileSettingStoreAttribute;
                break;
            }
            if (attrb == null)return;

            object val = ProfileRegistryKey.GetValue(attrb.SaveName, attrb.DefaultValue);
            try
            {
                PropInfo.SetValue(Settings, Convert.ChangeType(ProfileRegistryKey.GetValue(attrb.SaveName, attrb.DefaultValue), PropInfo.PropertyType), null);
            }
            catch
            {
                try
                {
                    PropInfo.SetValue(Settings, attrb.DefaultValue, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Fehler");
                }
            }
        }

        public void InvalidateJar()
        {
            if (File.Exists(Settings.MinecraftBinJarCache))
            {
                try
                {
                    File.Delete(Settings.MinecraftBinJarCache);
                }
                catch
                {

                }
                Settings.MinecraftBinJarCache = "";
            }
        }

        private void Save(string PropertyName)
        {
            PropertyInfo PropInfo = null;

            PropInfo = Settings.GetType().GetProperty(PropertyName);
            if (PropInfo == null) throw new Exception(PropertyName + " konnte nicht geladen werden.");

            object[] atts = PropInfo.GetCustomAttributes(false);
            ProfileSettingStoreAttribute attrb = null;
            foreach (object Attribut in atts)
            {
                if (!(Attribut is ProfileSettingStoreAttribute)) continue;
                attrb = Attribut as ProfileSettingStoreAttribute;
                break;
            }
            if (attrb == null) return;

            //speichere Attribut

            //ermittle RegistryKey für Speicherung ermitteln
            
            object val = PropInfo.GetValue(Settings,null);
            ProfileRegistryKey.SetValue(attrb.SaveName,val);
            Settings.GetType();
            PropInfo.SetValue(Settings, Convert.ChangeType(ProfileRegistryKey.GetValue(attrb.SaveName, attrb.DefaultValue),PropInfo.PropertyType) , null);
            
        }
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AutoSave) Save(e.PropertyName);
        }


        private T LoadRegValue<T>(string RegName, T Default)
        {
            object val = ProfileRegistryKey.GetValue(RegName, Default);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private void SaveRegValue(string RegName, RegistryValueKind Typ, object Value)
        {
            ProfileRegistryKey.SetValue(RegName, Value);
        }


        readonly static RegistryKey LauncherRegistryLocation = Registry.CurrentUser.CreateSubKey("Software\\WischisMinecraftLauncher");
        readonly static RegistryKey ProfilesRegistryLocation = Registry.CurrentUser.CreateSubKey("Software\\WischisMinecraftLauncher\\Profile");


        public int Setting_LauncherModAnalyzeStatus
        {
            get
            {
                return LoadRegValue<int>("Launcher.ModAnalyzerStatus", 0);
            }
            set
            {
                SaveRegValue("Launcher.ModAnalyzerStatus", RegistryValueKind.DWord, value);
            }
        }


    }

    /// <summary>
    /// Beinhaltet alle Einstellungen des Profils (Java,Minecraft
    /// </summary>
    public class ProfileSettings : INotifyPropertyChanged
    {

        [ProfileSettingStore("Logging.Enabled", RegistryValueKind.String,false)]
        public bool EnableLogging
        {
            get
            {
                return _EnableLogging;
            }
            set
            {
                bool istsneu = _EnableLogging != value;
                _EnableLogging = value;
                if(istsneu)PropertyChange("EnableLogging");
            }
        }
        private bool _EnableLogging;



        [ProfileSettingStore("Minecraft.JarCache",RegistryValueKind.String,"")]
        public string MinecraftBinJarCache
        {
            get
            {
                return _MinecraftBinJarCache;
            }
            set
            {
                bool istsneu = _MinecraftBinJarCache != value;
                _MinecraftBinJarCache = value;
                if (istsneu) PropertyChange("MinecraftBinJarCache");
            }
        }
        private string _MinecraftBinJarCache;

        [ProfileSettingStore("Logging.File", RegistryValueKind.String, "")]
        public string LoggingFile
        {
            get
            {
                return _LoggingFile;
            }
            set
            {
                bool istsneu = _LoggingFile != value;
                _LoggingFile = value;
                if (istsneu) PropertyChange("LoggingFile");
            }
        }
        private string _LoggingFile;

        [ProfileSettingStore("Minecraft.BinjarHash", RegistryValueKind.String, "")]
        public string MinecraftBinJarHash
        {
            get
            {
                return _MinecraftBinJarHash;
            }
            set
            {
                bool istsneu = _MinecraftBinJarHash != value;
                _MinecraftBinJarHash = value;
                if (istsneu) PropertyChange("MinecraftBinJarHash");
            }
        }
        private string _MinecraftBinJarHash;

        [ProfileSettingStore("Minecraft.OptionOverwrite", RegistryValueKind.String, false)]
        public bool MinecraftOptionOverwrite
        {
            get
            {
                return _MinecraftOptionOverwrite;
            }
            set
            {
                bool istsneu = _MinecraftOptionOverwrite != value;
                _MinecraftOptionOverwrite = value;
                if (istsneu) PropertyChange("MinecraftOptionOverwrite");
            }
        }
        private bool _MinecraftOptionOverwrite;





        [ProfileSettingStore("Minecraft.WaitNonResponding", RegistryValueKind.String, false)]
        public bool LauncherWaitNonResponding
        {
            get
            {
                return _LauncherWaitNonResponding;
            }
            set
            {
                bool istsneu = _LauncherWaitNonResponding != value;
                _LauncherWaitNonResponding = value;
                if (istsneu) PropertyChange("LauncherWaitNonResponding");
            }
        }
        private bool _LauncherWaitNonResponding;





        [ProfileSettingStore("Minecraft.StateInfo", RegistryValueKind.String, "")]
        public string MinecraftStateInfo
        {
            get
            {
                return _MinecraftStateInfo;
            }
            set
            {
                bool istsneu = _MinecraftStateInfo != value;
                _MinecraftStateInfo = value;
                if (istsneu) PropertyChange("MinecraftStateInfo");
            }
        }
        private string _MinecraftStateInfo;



        [ProfileSettingStore("Minecraft.StartState", RegistryValueKind.String, 0)]
        public int MinecraftStartState
        {
            get
            {
                return _MinecraftStartState;
            }
            set
            {
                bool istsneu = _MinecraftStartState != value;
                _MinecraftStartState = value;
                if (istsneu) PropertyChange("MinecraftStartState");
            }
        }
        private int _MinecraftStartState;

        [ProfileSettingStore("Java.Executable", RegistryValueKind.String, "")]
        public string JavaExecutable
        {
            get
            {
                return _JavaExecutable;
            }
            set
            {
                bool istsneu = _JavaExecutable != value;
                _JavaExecutable = value;
                if (istsneu) PropertyChange("JavaExecutable");
            }
        }
        private string _JavaExecutable;

        [ProfileSettingStore("Java.Memory.Use", RegistryValueKind.String, false)]
        public bool JavaMemoryUse
        {
            get
            {
                return _JavaMemoryUse;
            }
            set
            {
                bool istsneu = _JavaMemoryUse != value;
                _JavaMemoryUse = value;
                if (istsneu) PropertyChange("JavaMemoryUse");
            }
        }
        private bool _JavaMemoryUse;

        [ProfileSettingStore("Java.Memory.Init", RegistryValueKind.String, 512)]
        public int JavaMemoryInit
        {
            get
            {
                return _JavaMemoryInit;
            }
            set
            {
                bool istsneu = _JavaMemoryInit != value;
                _JavaMemoryInit = value;
                if (istsneu) PropertyChange("JavaMemoryInit");
            }
        }
        private int _JavaMemoryInit;


        [ProfileSettingStore("Minecraft.Priority", RegistryValueKind.String, "Normal")]
        public string MinecraftPrio
        {
            get
            {
                return _MinecraftPrio;
            }
            set
            {
                bool istsneu = _MinecraftPrio != value;
                _MinecraftPrio = value;
                if (istsneu) PropertyChange("MinecraftPrio");
            }
        }
        private string _MinecraftPrio;


        [ProfileSettingStore("Java.Memory.Max", RegistryValueKind.String, 1024)]
        public int JavaMemoryMax
        {
            get
            {
                return _JavaMemoryMax;
            }
            set
            {
                bool istsneu = _JavaMemoryMax != value;
                _JavaMemoryMax = value;
                if (istsneu) PropertyChange("JavaMemoryMax");
            }
        }
        private int _JavaMemoryMax;

        [ProfileSettingStore("Java.Proxy.Use", RegistryValueKind.String, false)]
        public bool JavaProxyUse
        {
            get
            {
                return _JavaProxyUse;
            }
            set
            {
                bool istsneu = _JavaProxyUse != value;
                _JavaProxyUse = value;
                if (istsneu) PropertyChange("JavaProxyUse");
            }
        }
        private bool _JavaProxyUse;

        [ProfileSettingStore("Java.Proxy.User", RegistryValueKind.String, "")]
        public string JavaProxyUser
        {
            get
            {
                return _JavaProxyUser;
            }
            set
            {
                bool istsneu = _JavaProxyUser != value;
                _JavaProxyUser = value;
                if (istsneu) PropertyChange("JavaProxyUser");
            }
        }
        private string _JavaProxyUser;

        [ProfileSettingStore("Java.Proxy.Password", RegistryValueKind.String, "")]
        public string JavaProxyPassword
        {
            get
            {
                return _JavaProxyPassword;
            }
            set
            {
                bool istsneu = _JavaProxyPassword != value;
                _JavaProxyPassword = value;
                if (istsneu) PropertyChange("JavaProxyPassword");
            }
        }
        private string _JavaProxyPassword;

        [ProfileSettingStore("Java.Proxy.Host", RegistryValueKind.String, "")]
        public string JavaProxyHost
        {
            get
            {
                return _JavaProxyHost;
            }
            set
            {
                bool istsneu = _JavaProxyHost != value;
                _JavaProxyHost = value;
                if (istsneu) PropertyChange("JavaProxyHost");
            }
        }
        private string _JavaProxyHost;

        [ProfileSettingStore("Java.Proxy.Post", RegistryValueKind.String, 0)]
        public int JavaProxyPort
        {
            get
            {
                return _JavaProxyPort;
            }
            set
            {
                bool istsneu = _JavaProxyPort != value;
                _JavaProxyPort = value;
                if (istsneu) PropertyChange("JavaProxyPort");
            }
        }
        private int _JavaProxyPort;

        [ProfileSettingStore("Java.Proxy.UseAuth", RegistryValueKind.String, false)]
        public bool UseJavaProxyAuth
        {
            get
            {
                return _UseJavaProxyAuth;
            }
            set
            {
                bool istsneu = _UseJavaProxyAuth != value;
                _UseJavaProxyAuth = value;
                if (istsneu) PropertyChange("UseJavaProxyAuth");
            }
        }
        private bool _UseJavaProxyAuth;

        [ProfileSettingStore("Minecraft.SoundVolume", RegistryValueKind.String, 50)]
        public int MinecraftSoundVolume
        {
            get
            {
                return _MinecraftSoundVolume;
            }
            set
            {
                bool istsneu = _MinecraftSoundVolume != value;
                _MinecraftSoundVolume = value;
                if (istsneu) PropertyChange("MinecraftSoundVolume");
            }
        }
        private int _MinecraftSoundVolume;

        [ProfileSettingStore("Minecraft.MusicVolume", RegistryValueKind.String, 50)]
        public int MinecraftMusicVolume
        {
            get
            {
                return _MinecraftMusicVolume;
            }
            set
            {
                bool istsneu = _MinecraftMusicVolume != value;
                _MinecraftMusicVolume = value;
                if (istsneu) PropertyChange("MinecraftMusicVolume");
            }
        }
        private int _MinecraftMusicVolume;

        [ProfileSettingStore("Minecraft.FancyGraphics", RegistryValueKind.String, false)]
        public bool FancyGraphics
        {
            get
            {
                return _FancyGraphics;
            }
            set
            {
                bool istsneu = _FancyGraphics != value;
                _FancyGraphics = value;
                if (istsneu) PropertyChange("FancyGraphics");
            }
        }
        private bool _FancyGraphics;

        [ProfileSettingStore("Minecraft.ViewBobbing", RegistryValueKind.String, true)]
        public bool ViewBobbing
        {
            get
            {
                return _ViewBobbing;
            }
            set
            {
                bool istsneu = _ViewBobbing != value;
                _ViewBobbing = value;
                if (istsneu) PropertyChange("EnableLogging");
            }
        }
        private bool _ViewBobbing;

        [ProfileSettingStore("Minecraft.FOV", RegistryValueKind.String, 0.5)]
        public double MinecraftFOV
        {
            get
            {
                return _MinecraftFOV;
            }
            set
            {
                bool istsneu = _MinecraftFOV != value;
                _MinecraftFOV = value;
                if (istsneu) PropertyChange("MinecraftFOV");
            }
        }
        private double _MinecraftFOV;

        [ProfileSettingStore("Minecraft.MouseSense", RegistryValueKind.String, 0.5)]
        public double MouseSense
        {
            get
            {
                return _MouseSense;
            }
            set
            {
                bool istsneu = _MouseSense != value;
                _MouseSense = value;
                if (istsneu) PropertyChange("MouseSense");
            }
        }
        private double _MouseSense;

        [ProfileSettingStore("Minecraft.Hardness", RegistryValueKind.String, 3)]
        public int Hardness
        {
            get
            {
                return _Hardness;
            }
            set
            {
                bool istsneu = _Hardness != value;
                _Hardness = value;
                if (istsneu) PropertyChange("Hardness");
            }
        }
        private int _Hardness;

        [ProfileSettingStore("Minecraft.MouseInvert", RegistryValueKind.String, false)]
        public bool InvertMouse
        {
            get
            {
                return _InvertMouse;
            }
            set
            {
                bool istsneu = _InvertMouse != value;
                _InvertMouse = value;
                if (istsneu) PropertyChange("InvertMouse");
            }
        }
        private bool _InvertMouse;

        [ProfileSettingStore("Minecraft.Graphics.3DAnaglyph", RegistryValueKind.String, false)]
        public bool Minecraft3DAnaglyph
        {
            get
            {
                return _Minecraft3DAnaglyph;
            }
            set
            {
                bool istsneu = _Minecraft3DAnaglyph != value;
                _Minecraft3DAnaglyph = value;
                if (istsneu) PropertyChange("Minecraft3DAnaglyph");
            }
        }
        private bool _Minecraft3DAnaglyph;

        [ProfileSettingStore("Minecraft.Options.SmoothLightning", RegistryValueKind.String, true)]
        public bool SmoothLightning
        {
            get
            {
                return _SmoothLightning;
            }
            set
            {
                bool istsneu = _SmoothLightning != value;
                _SmoothLightning = value;
                if (istsneu) PropertyChange("SmoothLightning");
            }
        }
        private bool _SmoothLightning;

        [ProfileSettingStore("Minecraft.Mods", RegistryValueKind.String, "")]
        public string MinecraftModListe
        {
            get
            {
                return _MinecraftModListe;
            }
            set
            {
                bool istsneu = _MinecraftModListe != value;
                _MinecraftModListe = value;
                if (istsneu) PropertyChange("MinecraftModListe");
            }
        }
        private string _MinecraftModListe;

        [ProfileSettingStore("Minecraft.Graphics.AdvancedOpenGL", RegistryValueKind.String, true)]
        public bool AdvancedOpenGL
        {
            get
            {
                return _AdvancedOpenGL;
            }
            set
            {
                bool istsneu = _AdvancedOpenGL != value;
                _AdvancedOpenGL = value;
                if (istsneu) PropertyChange("AdvancedOpenGL");
            }
        }
        private bool _AdvancedOpenGL;

        [ProfileSettingStore("Minecraft.Graphics.Texturepack", RegistryValueKind.String, "")]
        public string TexturePackName
        {
            get
            {
                return _TexturePackName;
            }
            set
            {
                bool istsneu = _TexturePackName != value;
                _TexturePackName = value;
                if (istsneu) PropertyChange("TexturePackName");
            }
        }
        private string _TexturePackName;

        [ProfileSettingStore("Minecraft.Login.UseAutoLogin",RegistryValueKind.String,"False")]
        public bool MinecraftAutoLogin
        {
            get
            {
                return _MinecraftAutoLogin;
            }
            set
            {
                bool istsneu = _MinecraftAutoLogin != value;
                _MinecraftAutoLogin = value;
                if (istsneu) PropertyChange("MinecraftAutoLogin");
            }
        }
        private bool _MinecraftAutoLogin;

        [ProfileSettingStore("Minecraft.Login.RememberPassword",RegistryValueKind.String,"False")]
        public bool MinecraftRememberPassword
        {
            get
            {
                return _MinecraftRememberPassword;
            }
            set
            {
                bool istsneu = _MinecraftRememberPassword != value;
                _MinecraftRememberPassword = value;
                if (istsneu) PropertyChange("MinecraftRememberPassword");
            }
        }
        private bool _MinecraftRememberPassword;


        [ProfileSettingStore("Minecraft.Login.User",RegistryValueKind.String,"")]
        public string MinecraftLoginUser
        {
            get
            {
                return _MinecraftLoginUser;
            }
            set
            {
                bool istsneu = _MinecraftLoginUser != value;
                _MinecraftLoginUser = value;
                if (istsneu) PropertyChange("MinecraftLoginUser");
            }
        }
        private string _MinecraftLoginUser;

        [ProfileSettingStore("Minecraft.Login.Password",RegistryValueKind.String,"")]
        public string MinecraftLoginPassword
        {
            get
            {
                return _MinecraftLoginPassword;
            }
            set
            {
                bool istsneu = _MinecraftLoginPassword != value;
                _MinecraftLoginPassword = value;
                if (istsneu) PropertyChange("MinecraftLoginPassword");
            }
        }
        private string _MinecraftLoginPassword;

        [ProfileSettingStore("Minecraft.Autoconnect",RegistryValueKind.String,"False")]
        public bool MinecraftServerAutoConnect
        {
            get
            {
                return _MinecraftServerAutoConnect;
            }
            set
            {
                bool istsneu = _MinecraftServerAutoConnect != value;
                _MinecraftServerAutoConnect = value;
                if (istsneu) PropertyChange("MinecraftServerAutoConnect");
            }
        }
        private bool _MinecraftServerAutoConnect;

        [ProfileSettingStore("Minecraft.Autoconnect.Host",RegistryValueKind.String,"")]
        public string MinecraftServerAutoconnectHost
        {
            get
            {
                return _MinecraftServerAutoconnectHost;
            }
            set
            {
                bool istsneu = _MinecraftServerAutoconnectHost != value;
                _MinecraftServerAutoconnectHost = value;
                if (istsneu) PropertyChange("MinecraftServerAutoconnectHost");
            }
        }
        private string _MinecraftServerAutoconnectHost;



        //Default Einstellungen treffen.
        public ProfileSettings()
        {
            _EnableLogging = false;
            _LoggingFile = "";
        }


        private void PropertyChange(string ProptertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(ProptertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ProfileSettingStoreAttribute : Attribute
    {
        public ProfileSettingStoreAttribute(string SaveName,RegistryValueKind Type,object DefaultValue)
        {
            _savename = SaveName;
            _kind = Type;
            _default = DefaultValue;
        }

        string _savename;
        RegistryValueKind _kind;
        object _default;

        public string SaveName
        {
            get
            {
                return _savename;
            }
        }
        public RegistryValueKind RegType
        {
            get
            {
                return _kind;
            }
        }
        public object DefaultValue
        {
            get
            {
                return _default;
            }
        }
    }

    public static class LauncherFolderStructure
    {
        public static string WischiLauncherFolder
        {
            get
            {
                return create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + ".wischilauncher");
            }
        }


        public static string GetSpecificProfilePath(LauncherProfile Profil)
        {
            return GetSpecificProfilePath(Profil.ProfileName);
        }

        public static string GetSpecificProfilePath(string Profil)
        {
            return LauncherFolderStructure.ProfilePath + Path.DirectorySeparatorChar + Profil;
        }

        public static string GetSpecificMinecraftFolder(LauncherProfile Profil)
        {
            return GetSpecificProfilePath(Profil) + Path.DirectorySeparatorChar + ".minecraft";
        }

        public static string GetSpecificBinFolder(LauncherProfile Profil)
        {
            return GetSpecificMinecraftFolder(Profil) + Path.DirectorySeparatorChar + "bin";
        }

        //wischilaucher/profiles
        public static string ProfilePath
        {
            get
            {
                return create(WischiLauncherFolder + Path.DirectorySeparatorChar + "profiles");
            }
        }

        //wischilaucher/mods
        public static string ModFolder
        {
            get
            {
                return create(WischiLauncherFolder + Path.DirectorySeparatorChar + "mods");
            }
        }

        //wischilaucher/binjars
        public static string BinjarPath
        {
            get
            {
                return create(WischiLauncherFolder + Path.DirectorySeparatorChar + "binjars");
            }
        }

        //wischilaunher/plugins
        public static string PluginFolder
        {
            get
            {
                return create(WischiLauncherFolder + Path.DirectorySeparatorChar + "plugins");
            }
        }

        private static string create(string folder)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }


    }

    
}
