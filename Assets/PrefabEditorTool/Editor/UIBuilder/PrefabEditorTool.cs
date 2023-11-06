using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
public class PrefabEditorTool : EditorWindow
{
    private VisualTreeAsset visualTreeAsset = default;

    private ObjectField objectField;
    private Toggle objToggle;
    private TextField pathField;
    private DropdownField typeDropdownField;
    private Button executeBtn;
    private TextField resultTextField;

    private string prefabPath;

    private List<string> checkTypeList = new List<string>();
    private List<string> prefabPathList = new List<string>();

    private StringBuilder stringBuilder = new StringBuilder();
    private Func<Component,bool> replaceAssetFunc;

    [MenuItem("Tools/PrefabEditorTool")]
    public static void ShowExample()
    {
        PrefabEditorTool wnd = GetWindow<PrefabEditorTool>();
        wnd.titleContent = new GUIContent("PrefabEditorTool");
        wnd.maxSize = new Vector2(500, 260);
        wnd.minSize = new Vector2(500, 260);
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PrefabEditorTool/Editor/UIBuilder/PrefabEditorTool.uxml");
        visualTreeAsset.CloneTree(root);

        InitCheckTypeList();
        SetReplaceAssetAction();

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

        typeDropdownField = root.Q<DropdownField>("typeDropdownField");
        typeDropdownField.choices = checkTypeList;
        typeDropdownField.value = checkTypeList[0];

        resultTextField = root.Q<TextField>("resultTextField");
        resultTextField.isReadOnly = true;
    }

    private void InitCheckTypeList()
    {
        checkTypeList.Clear();
        checkTypeList.Add(typeof(RectTransform).FullName);
        checkTypeList.Add(typeof(UnityEngine.UI.Text).FullName);
        checkTypeList.Add(typeof(UnityEngine.UI.Image).FullName);
    }

    private void OnGUI()
    {
        prefabPath = OnDrawElementAcceptDrop(pathField.contentRect, prefabPath);
        pathField.SetValueWithoutNotify(prefabPath);
        executeBtn.SetEnabled(!string.IsNullOrEmpty(prefabPath));
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
    private void OnObjToggleChanged(ChangeEvent<bool> evt)
    {
        bool curr = evt.newValue;
        objectField.SetEnabled(curr);
    }
    private void OnPathValueChange(ChangeEvent<string> evt)
    {
        prefabPath = evt.newValue;
    }
    private void OnClickExecuteBtn()
    {
        prefabPathList.Clear();
        stringBuilder.Clear();
        CheckRes(prefabPath, ".prefab", (_path) => { prefabPathList.Add(_path); });
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        string checkTypeName = typeDropdownField.value;
        Type checkType = GetTypeByName(checkTypeName);
        bool isNeedChangeAsset = objToggle.value;
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            GameObject prefab = AssetDatabase.LoadAssetAtPath(shortPath, typeof(GameObject)) as GameObject;
            //做点啥
            Component[] components = prefab.GetComponentsInChildren(checkType);
            if(components.Length > 0) stringBuilder.AppendLine(prefab.name + ":");
            bool isDirty = false;
            foreach (Component com in components)
            {
                stringBuilder.AppendLine("          " + FindChildAddress(com.transform));
                if (isNeedChangeAsset) 
                {
                    bool isChange = replaceAssetFunc.Invoke(com);
                    if (isChange) isDirty = true;
                }
            }
            //end
            if (isDirty)
            {
                EditorUtility.SetDirty(prefab);
                ReplacePrefab(shortPath, prefab);
            }
        }

        resultTextField.SetValueWithoutNotify(stringBuilder.ToString());
        AssetDatabase.SaveAssets();
        Debug.Log("处理完毕！");
    }

    private void SetReplaceAssetAction()
    {
        replaceAssetFunc = (_com) => 
        {
            if (_com as UnityEngine.UI.Image != null) 
            {
                UnityEngine.UI.Image img = _com as UnityEngine.UI.Image;
                if (img.sprite!= null && img.sprite.name == objectField.value.name) return false;
                img.sprite = objectField.value as Sprite;
                return true;
            }
            if (_com as UnityEngine.UI.Text != null)
            {
                UnityEngine.UI.Text txt = _com as UnityEngine.UI.Text;
                if (txt.font != null && txt.font.name == objectField.value.name) return false;
                txt.font = objectField.value as Font;
                return true;
            }
            return false;
        };
    }

    private Type GetTypeByName(string typeName)
    {
        Type type = null;
        if (typeName.Contains("Text")) type = typeof(UnityEngine.UI.Text);
        if (typeName.Contains("Image")) type = typeof(UnityEngine.UI.Image);
        if (typeName.Contains("RectTransform")) type = typeof(RectTransform);
        return type;
    }
    public List<GameObject> FindGameObjectsWithComponent(GameObject self, string componentName)
    {
        Type componentType = Type.GetType(componentName);
        if (componentType == null)
        {
            Debug.LogError("Component type not found: " + componentName);
            return null;
        }

        Component[] components = self.GetComponentsInChildren(componentType, true);
        List<GameObject> gameObjectsWithComponent = new List<GameObject>();

        foreach (Component component in components)
        {
            gameObjectsWithComponent.Add(component.gameObject);
        }

        return gameObjectsWithComponent;
    }
    private void ReplacePrefab(string savePath, GameObject saveObj)
    {
        string _savePath = savePath.Replace(@"\", "/");
        GUID guid = AssetDatabase.GUIDFromAssetPath(_savePath);
        EditorUtility.SetDirty(saveObj);
        AssetDatabase.SaveAssetIfDirty(guid);
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
    public string FindChildAddress(Transform childTransform)
    {
        Transform root = childTransform.root;
        string address = GetTransformAddress(childTransform, root);
        return address;
    }

    private string GetTransformAddress(Transform child, Transform root)
    {
        string address = child.name;
        Transform parent = child.parent;

        while (parent != null && parent != root)
        {
            address = parent.name + "/" + address;
            parent = parent.parent;
        }

        return address;
    }
}