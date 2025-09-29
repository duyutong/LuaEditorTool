using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using XLua;
using System.Text;
using UnityEditor.UIElements;
using System.Linq;

public class LuaEditorTool : EditorWindow
{
    private VisualTreeAsset visualTreeAsset = default;
    internal static LuaEnv luaEnv; //all lua behaviour shared one luaenv only!

    private TextField luaScriptTop;
    private TextField luaScriptBottom;
    private TextField luaScript;
    private ObjectField objectField;
    private Toggle objToggle;

    private string assetTableName = "asset";
    private string objectTableName = "object";
    private string dirtyTableName = "checkDirty";

    private TextField pathField;
    private TextField extensionField;
    private Button executeBtn;

    private string objectPath;
    private string extensionStr;
    private HashSet<string> extension;
    private List<string> objectPathList = new List<string>();
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
        visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LuaEditorTool/Editor/UIBuilder/LuaEditorTool.uxml");
        visualTreeAsset.CloneTree(root);

        objToggle = root.Q<Toggle>("objToggle");
        objToggle.SetValueWithoutNotify(false);
        objToggle.RegisterValueChangedCallback(OnObjToggleChanged);

        objectField = root.Q<ObjectField>("objectField");
        objectField.SetEnabled(objToggle.value);

        pathField = root.Q<TextField>("pathField");
        pathField.RegisterValueChangedCallback(OnPathValueChange);

        extensionField = root.Q<TextField>("extensionField");
        extensionField.RegisterValueChangedCallback(OnExtensionValueChange);

        executeBtn = root.Q<Button>("executeBtn");
        executeBtn.clicked += OnClickExecuteBtn;
        executeBtn.SetEnabled(false);

        luaScriptTop = root.Q<TextField>("luaScriptTop");
        string luaTop = luaScriptTop.text;
        luaTop = luaTop.Replace("#objectTableName#", objectTableName);
        luaTop = luaTop.Replace("#assetName#", assetTableName);
        luaTop = luaTop.Replace("#dirtyTableName#", dirtyTableName);
        luaScriptTop.SetValueWithoutNotify(luaTop);
        luaScriptTop.SetEnabled(false);

        luaScriptBottom = root.Q<TextField>("luaScriptButtom");
        luaScriptBottom.SetEnabled(false);

        luaScript = root.Q<TextField>("luaScript");
        string lua = luaScript.text;
        lua = lua.Replace("#objectTableName#", objectTableName);
        luaScript.SetValueWithoutNotify(lua);
    }

    private void OnExtensionValueChange(ChangeEvent<string> evt)
    {
        extensionStr = evt.newValue;
    }

    private void OnObjToggleChanged(ChangeEvent<bool> evt)
    {
        bool curr = evt.newValue;
        objectField.SetEnabled(curr);
    }

    private void OnPathValueChange(ChangeEvent<string> evt)
    {
        objectPath = evt.newValue;
    }

    private StringBuilder luaStr = new StringBuilder();
    private void OnClickExecuteBtn()
    {
        luaStr.Clear();
        luaStr.AppendLine(luaScriptTop.text);
        luaStr.AppendLine(luaScript.text);
        luaStr.AppendLine(luaScriptBottom.text);

        objectPathList.Clear();
        extension = extensionStr.Split("|").ToHashSet();
        foreach (string ext in extension) EditorUtilityExtensions.CheckRes(objectPath, ext, (_path) => { objectPathList.Add(_path); });
        //Debug.Log(luaStr.ToString());
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        if (objToggle.value) scriptEnv.Set(assetTableName, objectField.value);
        for (int i = 0; i < objectPathList.Count; i++)
        {
            int index = i;
            string _path = objectPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            UnityEngine.Object targetObj = AssetDatabase.LoadAssetAtPath(shortPath, typeof(UnityEngine.Object));
            scriptEnv.Set(objectTableName, targetObj);
            luaEnv.DoString(luaStr.ToString(), null, scriptEnv);
            scriptEnv.Get(dirtyTableName, out checkDirty);
            if (checkDirty)
            {
                EditorUtility.SetDirty(targetObj);
                ReplaceObject(shortPath, targetObj);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("处理完毕！");
    }
    private void ReplaceObject(string savePath, UnityEngine.Object saveObj)
    {
        string _savePath = savePath.Replace(@"\", "/");
        GUID guid = AssetDatabase.GUIDFromAssetPath(_savePath);
        EditorUtility.SetDirty(saveObj);
        AssetDatabase.SaveAssetIfDirty(guid);
    }
    private void OnGUI()
    {
        if (pathField == null) return;
        if (executeBtn == null) return;

        objectPath = OnDrawElementAcceptDrop(pathField.contentRect, objectPath);
        
        pathField.SetValueWithoutNotify(objectPath);
        
        executeBtn.SetEnabled(!string.IsNullOrEmpty(objectPath));

        if (string.IsNullOrEmpty(extensionStr)) extensionStr = ".prefab";
        extensionField.SetValueWithoutNotify(extensionStr);
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
    private void OnDisable()
    {
        luaEnv.Tick();
        scriptEnv.Dispose();
        executeBtn.clicked -= OnClickExecuteBtn;
        pathField.UnregisterValueChangedCallback(OnPathValueChange);
    }
}