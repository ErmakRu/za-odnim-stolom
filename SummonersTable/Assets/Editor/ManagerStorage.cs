using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static class ManagerStorage
    {
        public static string DirectoryPath=>ConfigAuthoring.Folder;
        public static void Save(AuthoringManager manager,string directory=null)
        {
            SaveSection(manager.FileName,manager.Export(),directory);
        }
        public static void SaveSection(string file,string text,string directory=null)
        {
            directory??=DirectoryPath;if(!ConfigBundle.Files.Contains(file))throw new ArgumentException("Неизвестный раздел Config");
            ConfigBundle.Read(directory,file,text);
            string path=Path.Combine(directory,file),main=Path.Combine(directory,"main.json");
            string old=File.Exists(path)?File.ReadAllText(path):null;
            var sections=new Dictionary<string,object>();
            foreach(string name in ConfigBundle.Files)sections[Path.GetFileNameWithoutExtension(name)]=ConfigJson.Parse(name==file?text:File.ReadAllText(Path.Combine(directory,name)));
            string snapshot=ConfigAuthoring.Pretty(ConfigJson.Write(new Dictionary<string,object>{{"schemaVersion",1},{"managers",sections}}));
            try{Atomic(path,text);Atomic(main,snapshot);}
            catch{if(old!=null)Atomic(path,old);throw;}
        }
        public static string Copy(AuthoringManager manager,string directory=null,DateTime? at=null)
        {
            directory??=DirectoryPath;string section=Path.GetFileNameWithoutExtension(manager.FileName);
            string folder=Path.Combine(directory,"History",section);Directory.CreateDirectory(folder);
            string name=section+"_"+(at??DateTime.UtcNow).ToUniversalTime().ToString("yyyyMMdd-HHmmss-fffffff'Z'",CultureInfo.InvariantCulture)+".json";
            string path=Path.Combine(folder,name);Atomic(path,manager.Export());return path;
        }
        public static string Latest(AuthoringManager manager,string directory=null)
        {
            string id=Path.GetFileNameWithoutExtension(manager.FileName),folder=Path.Combine(directory??DirectoryPath,"History",id);
            if(!Directory.Exists(folder))throw new FileNotFoundException("У этого Manager ещё нет копий.");
            var copies=Directory.GetFiles(folder,id+"_*.json").Select(path=>
            {
                string stamp=Path.GetFileNameWithoutExtension(path).Substring(id.Length+1);
                bool valid=DateTime.TryParseExact(stamp,"yyyyMMdd-HHmmss-fffffff'Z'",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out var time);
                return new{path,time,valid};
            }).Where(x=>x.valid&&x.time<=DateTime.UtcNow).OrderByDescending(x=>x.time).ToArray();
            if(copies.Length==0)throw new FileNotFoundException("Нет копии с отметкой времени до текущего момента.");
            manager.Import(File.ReadAllText(copies[0].path));return copies[0].path;
        }
        public static void Atomic(string path,string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".saving";
            try{File.WriteAllText(temp,text,new System.Text.UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
