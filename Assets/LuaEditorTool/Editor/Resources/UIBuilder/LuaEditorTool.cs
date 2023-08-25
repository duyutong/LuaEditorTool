using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Collections.Generic;
using XLua;
using System.Text;
using UnityEditor.UIElements;
using Unity.VisualScripting;

public class LuaEditorTool : EditorWindow
{
    private VisualTreeAsset visualTreeAsset = default;
    internal static LuaEnv luaEnv; //all lua behaviour shared one luaenv only!

    private TextField luaScriptTop;
    private TextField luaScriptBottom;
    private TextField luaScript;
    private ObjectField objectField;
    private Toggle objToggle;

    private string objTableName = "asset";
    private string prefabTableName = "prefab";
    private string dirtyTableName = "checkDirty";

    private TextField pathField;
    private string prePath;
    private Button executeBtn;

    private List<string> prefabPathList = new List<string>();
    private LuaTable scriptEnv;

    private bool checkDirty;

    [MenuItem("Tools/LuaEditorTool")]
    public static void ShowExample()
    {
        LuaEditorTool wnd = GetWindow<LuaEditorTool>();
        wnd.titleContent = new GUIContent("LuaEditorTool");
        wnd.minSize = new Vector2(670, 320);
        wnd.maxSize = new Vector2(1000, 600);
    }

    public void CreateGUI()
    {
        luaEnv = new LuaEnv();
        scriptEnv = luaEnv.NewTable();

        // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", this);
        scriptEnv.Set(dirtyTableName, checkDirty);

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        visualTreeAsset = Resources.Load<VisualTreeAsset>("UIBuilder/LuaEditorTool");
        visualTreeAsset.CloneTree(root);

        objToggle = root.Q<Toggle>("objToggle");
        objToggle.SetValueWithoutNotify(false);
        objToggle.RegisterValueChangedCallback(OnObjToggleChanged);

        objectField = root.Q<ObjectField>("objectField");
        objectField.SetEnabled(objToggle.value);

        pathField = root.Q<TextField>("pathField");
        pathField.RegisterValueChangedCallback(OnPathValueChange);

        executeBtn = root.Q<Button>("executeBtn");
        executeBtn.clicked += OnClickExecuteBtn;
        executeBtn.SetEnabled(false);

        luaScriptTop = root.Q<TextField>("luaScriptTop");
        string luaTop = luaScriptTop.text;
        luaTop = luaTop.Replace("#prefabTableName#", prefabTableName);
        luaTop = luaTop.Replace("#assetName#", objTableName);
        luaTop = luaTop.Replace("#dirtyTableName#", dirtyTableName);
        luaScriptTop.SetValueWithoutNotify(luaTop);
        luaScriptTop.SetEnabled(false);

        luaScriptBottom = root.Q<TextField>("luaScriptButtom");
        luaScriptBottom.SetEnabled(false);

        luaScript = root.Q<TextField>("luaScript");
        string lua = luaScript.text;
        lua = lua.Replace("#prefabTableName#", prefabTableName);
        luaScript.SetValueWithoutNotify(lua);
    }

    private void OnObjToggleChanged(ChangeEvent<bool> evt)
    {
        bool curr = evt.newValue;
        objectField.SetEnabled(curr);
    }

    private void OnPathValueChange(ChangeEvent<string> evt)
    {
        prePath = evt.newValue;
    }

    private StringBuilder luaStr = new StringBuilder();
    private void OnClickExecuteBtn()
    {
        luaStr.Clear();
        luaStr.AppendLine(luaScriptTop.text);
        luaStr.AppendLine(luaScript.text);
        luaStr.AppendLine(luaScriptBottom.text);

        prefabPathList.Clear();
        CheckRes(prePath, ".prefab", (_path) => { prefabPathList.Add(_path); });
        //Debug.Log(luaStr.ToString());
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        if (objToggle.value) scriptEnv.Set(objTableName, objectField.value);
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            GameObject prefab = AssetDatabase.LoadAssetAtPath(shortPath, typeof(GameObject)) as GameObject;
            scriptEnv.Set(prefabTableName, prefab);
            luaEnv.DoString(luaStr.ToString(), null, scriptEnv);
            scriptEnv.Get(dirtyTableName, out checkDirty);
            //Debug.Log(prefab.name + " checkDirty：" + checkDirty);
            if (checkDirty) 
            {
                EditorUtility.SetDirty(prefab);
                ReplacePrefab(shortPath, prefab);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("处理完毕！");
    }
    private void ReplacePrefab(string savePath,GameObject saveObj)
    {
        string _savePath = savePath.Replace(@"\", "/");
        GUID guid = AssetDatabase.GUIDFromAssetPath(_savePath);
        EditorUtility.SetDirty(saveObj);
        AssetDatabase.SaveAssetIfDirty(guid);
    }
    private void OnGUI()
    {
        prePath = OnDrawElementAcceptDrop(pathField.contentRect, prePath);
        pathField.SetValueWithoutNotify(prePath);
        executeBtn.SetEnabled(!string.IsNullOrEmpty(prePath));
    }
    private string OnDrawElementAcceptDrop(Rect rect, string label)
    {
        if (!rect.Contains(Event.current.mousePosition)) return label;
        if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) return label;
        if (!string.IsNullOrEmpty(DragAndDrop.paths[0]))
        {
            DragAndDrop.AcceptDrag();
            GUI.changed = true;
            label = DragAndDrop.paths[0];
        }
        return label;
    }
    private void CheckRes(string path, string extension, Action<string> action = null)
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
    private void OnDisable()
    {
        luaEnv.Tick();
        scriptEnv.Dispose();
        executeBtn.clicked -= OnClickExecuteBtn;
        pathField.UnregisterValueChangedCallback(OnPathValueChange);
    }
}