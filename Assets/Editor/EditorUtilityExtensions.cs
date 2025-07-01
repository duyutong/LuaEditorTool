using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

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
#if UNITY_IOS
        string pattern = @"Assets/(.+)";
#else
        string pattern = @"Assets\\(.+)";
#endif
        Match match = Regex.Match(fullPath, pattern, RegexOptions.IgnoreCase);

        if (!match.Success) return fullPath;
        return match.Groups[0].Value;
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
    // 移动文件到指定文件夹
    public static void MoveFileToFolder(string filePath, string folderPath)
    {
        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"文件不存在: {filePath}");
            return;
        }

        // 检查文件夹是否存在，如果不存在则创建它
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
            Debug.Log($"创建文件夹: {folderPath}");
        }

        // 移动文件到目标文件夹
        string destinationPath = Path.Combine(folderPath, Path.GetFileName(filePath));

        Debug.Log($"移动文件: {destinationPath} 结果：{AssetDatabase.MoveAsset(filePath.ToShortPath(), destinationPath)}");
    }
    // 删除指定路径的文件
    public static void DeleteFile(string filePath)
    {
        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"文件已删除: {filePath}");
        }
        else
        {
            Debug.LogWarning($"文件不存在: {filePath}");
        }
    }
    // 删除指定路径下的空文件夹
    public static void DeleteEmptyFolders(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            Debug.LogWarning($"指定的目录不存在: {rootDirectory}");
            return;
        }

        // 获取所有子文件夹
        string[] subdirectories = Directory.GetDirectories(rootDirectory);

        foreach (string directory in subdirectories)
        {
            // 递归删除空文件夹
            DeleteEmptyFolders(directory);
        }

        // 检查当前文件夹是否为空
        if (Directory.GetFiles(rootDirectory).Length == 0 && Directory.GetDirectories(rootDirectory).Length == 0)
        {
            Directory.Delete(rootDirectory);
            File.Delete(rootDirectory + ".meta");
            Debug.Log($"已删除空文件夹: {rootDirectory}");
        }
    }
    public static string GetFileName(string path, bool withExtension = false)
    {
        // 使用Path类的GetFileName方法来获取文件名
        string fileName = Path.GetFileName(path);

        // 如果不需要包含文件扩展名，则去除扩展名部分
        if (!withExtension)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        return fileName;
    }
    public static UnityEvent IntegrateEventInfo(PersistentData persistentData, int index = 0)
    {
        UnityEvent targetEvent = new UnityEvent();

        // 获取 UnityEventBase 内部的 m_PersistentCalls
        var baseType = typeof(UnityEventBase); // UnityEventBase 是 UnityEvent 的基类
        var callsField = baseType.GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var persistentCalls = callsField.GetValue(targetEvent);

        // 获取 PersistentCallGroup 中的 m_Calls 列表
        var callListType = persistentCalls.GetType();
        var callsFieldInGroup = callListType.GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);

        // 使用反射获取 List<PersistentCall>
        var calls = callsFieldInGroup.GetValue(persistentCalls) as IList;

        // 获取指定索引的 PersistentCall
        Type persistentCallType = Type.GetType("UnityEngine.Events.PersistentCall,UnityEngine.CoreModule");
        var persistentCall = Activator.CreateInstance(persistentCallType);

        // 通过反射获取 PersistentCall 的相关字段
        FieldInfo targetAssemblyTypeNameField = persistentCallType.GetField("m_TargetAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo target = persistentCallType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo methodName = persistentCallType.GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance);

        //赋值
        targetAssemblyTypeNameField.SetValue(persistentCall, persistentData.assemblyTypeName);
        UnityEngine.Object targetComponent = (persistentData.target as GameObject).GetComponent(persistentData.assemblyTypeName);

        if (targetComponent == null) return null;
        target.SetValue(persistentCall, targetComponent);
        methodName.SetValue(persistentCall, persistentData.methodName);

        calls.Insert(index, persistentCall);

        callsFieldInGroup.SetValue(persistentCalls, calls);

        return targetEvent;
    }
    public class PersistentData
    {
        public UnityEngine.Object target;   // 目标 GameObject
        public string assemblyTypeName;     // 类名
        public string methodName;           // 方法名
    }
}

