using System;
using System.Collections.Generic;
using System.Text;
using Ionic.Zip;
using System.IO;

namespace WischisMinecraftLauncher.ModMergeLib
{
    public class ModMerge
    {
        public ModMerge()
        {
            
        }
    }


    public class Mod
    {
        Stream ModStream;

        public Mod(Stream ModStream)
        {
            this.ModStream = ModStream;
        }
    }




}
