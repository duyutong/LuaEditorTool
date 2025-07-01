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
    /// ȥ���б��е��ظ�����֧�ֵ�һ�ж�����
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
    // �ƶ��ļ���ָ���ļ���
    public static void MoveFileToFolder(string filePath, string folderPath)
    {
        // ����ļ��Ƿ����
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"�ļ�������: {filePath}");
            return;
        }

        // ����ļ����Ƿ���ڣ�����������򴴽���
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
            Debug.Log($"�����ļ���: {folderPath}");
        }

        // �ƶ��ļ���Ŀ���ļ���
        string destinationPath = Path.Combine(folderPath, Path.GetFileName(filePath));

        Debug.Log($"�ƶ��ļ�: {destinationPath} �����{AssetDatabase.MoveAsset(filePath.ToShortPath(), destinationPath)}");
    }
    // ɾ��ָ��·�����ļ�
    public static void DeleteFile(string filePath)
    {
        // ����ļ��Ƿ����
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"�ļ���ɾ��: {filePath}");
        }
        else
        {
            Debug.LogWarning($"�ļ�������: {filePath}");
        }
    }
    // ɾ��ָ��·���µĿ��ļ���
    public static void DeleteEmptyFolders(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            Debug.LogWarning($"ָ����Ŀ¼������: {rootDirectory}");
            return;
        }

        // ��ȡ�������ļ���
        string[] subdirectories = Directory.GetDirectories(rootDirectory);

        foreach (string directory in subdirectories)
        {
            // �ݹ�ɾ�����ļ���
            DeleteEmptyFolders(directory);
        }

        // ��鵱ǰ�ļ����Ƿ�Ϊ��
        if (Directory.GetFiles(rootDirectory).Length == 0 && Directory.GetDirectories(rootDirectory).Length == 0)
        {
            Directory.Delete(rootDirectory);
            File.Delete(rootDirectory + ".meta");
            Debug.Log($"��ɾ�����ļ���: {rootDirectory}");
        }
    }
    public static string GetFileName(string path, bool withExtension = false)
    {
        // ʹ��Path���GetFileName��������ȡ�ļ���
        string fileName = Path.GetFileName(path);

        // �������Ҫ�����ļ���չ������ȥ����չ������
        if (!withExtension)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        return fileName;
    }
    public static UnityEvent IntegrateEventInfo(PersistentData persistentData, int index = 0)
    {
        UnityEvent targetEvent = new UnityEvent();

        // ��ȡ UnityEventBase �ڲ��� m_PersistentCalls
        var baseType = typeof(UnityEventBase); // UnityEventBase �� UnityEvent �Ļ���
        var callsField = baseType.GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var persistentCalls = callsField.GetValue(targetEvent);

        // ��ȡ PersistentCallGroup �е� m_Calls �б�
        var callListType = persistentCalls.GetType();
        var callsFieldInGroup = callListType.GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);

        // ʹ�÷����ȡ List<PersistentCall>
        var calls = callsFieldInGroup.GetValue(persistentCalls) as IList;

        // ��ȡָ�������� PersistentCall
        Type persistentCallType = Type.GetType("UnityEngine.Events.PersistentCall,UnityEngine.CoreModule");
        var persistentCall = Activator.CreateInstance(persistentCallType);

        // ͨ�������ȡ PersistentCall ������ֶ�
        FieldInfo targetAssemblyTypeNameField = persistentCallType.GetField("m_TargetAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo target = persistentCallType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo methodName = persistentCallType.GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance);

        //��ֵ
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
        public UnityEngine.Object target;   // Ŀ�� GameObject
        public string assemblyTypeName;     // ����
        public string methodName;           // ������
    }
}

