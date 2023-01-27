using System;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace FileSystem
{
    public enum FileLocation
    {
        SingleHouse,
        Autosave,
        Save
    }
    
    public class FileSystem
    {
        public static void SetupNecessaryFolders()
        {
            try
            {
                var singleHouse = GetDataPath(FileLocation.SingleHouse);
                if (!Directory.Exists(singleHouse)) Directory.CreateDirectory(singleHouse);
                
                var autosave = GetDataPath(FileLocation.Autosave);
                if (!Directory.Exists(autosave)) Directory.CreateDirectory(autosave);
                
                var save = GetDataPath(FileLocation.Save);
                if (!Directory.Exists(save)) Directory.CreateDirectory(save);
 
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void DeleteAllFiles(FileLocation location)
        {
            foreach (var fileInfo in GetFileInformation(location))
            {
                fileInfo.Delete();
            }
        }

        public static void WriteFile(string content, string fileName, FileLocation location)
        {
            var path = GetDataPath(location, fileName);
            using var fs = new FileStream(path, FileMode.Create);
            fs.Write(Encoding.UTF8.GetBytes(content));
        }

        public static void Rename(FileInfo file, string newFileName)
        {
            if (file.Directory == null) return;
            
            var directoryFullName = file.Directory.FullName;
            var newPath = Path.Combine(directoryFullName, newFileName);
            file.MoveTo(newPath);
        }
        
        public static string ReadFile(string fileName, FileLocation location)
        {
            var path = GetDataPath(location, fileName);
            using var reader = new StreamReader(path);
            return reader.ReadToEnd();
        }
        
        public static string ReadFile(FileInfo file)
        {
            using var reader = file.OpenText();
            return reader.ReadToEnd();
        }

        public static void DeleteFile(string fileName, FileLocation location)
        {
            File.Delete(GetDataPath(location, fileName));
        }

        public static FileInfo[] GetFileInformation(FileLocation location)
        {
            var info = new DirectoryInfo(GetDataPath(location));
            return info.GetFiles();
        }
        
        private static string GetDataPath(FileLocation location, string fileName = null)
        {
            var subPath = location switch
            {
                FileLocation.SingleHouse => "single",
                FileLocation.Autosave => "autosave",
                FileLocation.Save => "savex",
                _ => ""
            };

            if (fileName != null) subPath = Path.Combine(subPath, fileName);
            
            return Path.Combine(Application.persistentDataPath,subPath);
        }
    }
}