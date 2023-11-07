using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class EditorUtilityExtensions
{
    public static string GetMD5(string FolderPath)
    {
        using (FileStream Folder = File.OpenRead(FolderPath))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(Folder);
            Folder.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in bytes) stringBuilder.Append(b.ToString("x2"));

            return stringBuilder.ToString();
        }
    }
    public static void CheckRes(string path, string extension, Action<string> action = null)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path))
        {
            FileInfo fileInfo = new FileInfo(path);
            action?.Invoke(fileInfo.FullName);
        }
        else
        {
            string[] vs = Directory.GetDirectories(path);
            foreach (string v in vs) { CheckRes(v, extension, action); }
            DirectoryInfo directory = Directory.CreateDirectory(path);
            FileInfo[] fileInfos = directory.GetFiles();
            foreach (FileInfo info in fileInfos)
            {
                if (string.IsNullOrEmpty(info.FullName)) continue;
                if (info.Extension != extension) continue;
                action?.Invoke(info.FullName);
            }
        }
    }
    public static string ToShortPath(this string fullPath) 
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        string shortPath = "Assets" + @"\" + fullPath.Replace(dataPath, "");
        return shortPath;
    }
    /// <summary>
    /// 去除列表中的重复对象，支持单一判断条件
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="ts"></param>
    /// <param name="checkFunc"></param>
    public static void RemoveDuplicates<T1, T2>(this IList<T1> ts, Func<T1, T2> checkFunc)
    {
        IList<T2> temp = new List<T2>();
        List<int> indexList = new List<int>();

        for (int i = 0; i < ts.Count; i++)
        {
            T1 t1 = ts[i];
            T2 t2 = checkFunc(t1);
            if (temp.Contains(t2)) indexList.Add(i);
            else temp.Add(t2);
        }

        indexList.Sort((a, b) => b.CompareTo(a));

        for (int i = 0; i < indexList.Count; i++)
        {
            if (ts.Count <= indexList[i]) continue;
            ts.RemoveAt(indexList[i]);
        }
    }
}

