using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public class DependentFinder : EditorWindow
{
    private VisualTreeAsset visualTreeAsset = default;

    private Button executeBtn;
    private TextField pathField;
    private TextField assetNameField;
    private TextField checkTypeNameField;
    private DropdownField typeDropdownField;
    private ScrollView targetScrollView;

    private string checkAssetsPath;
    private List<string> checkTypeList = new List<string>();
    private List<string> prefabPathList = new List<string>();
    private List<SDependenciesInfo> dependenciesList = new List<SDependenciesInfo>();

    private string checkTypeName;
    private string checkAssetName;
    [MenuItem("Tools/DependentFinder")]
    public static void ShowExample()
    {
        DependentFinder wnd = GetWindow<DependentFinder>();
        wnd.titleContent = new GUIContent("DependentFinder");
        wnd.maxSize = new Vector2(500, 260);
        wnd.minSize = new Vector2(500, 260);
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        visualTreeAsset = Resources.Load<VisualTreeAsset>("UIBuilder/DependentFinder");
        visualTreeAsset.CloneTree(root);

        InitCheckTypeList();

        pathField = root.Q<TextField>("pathField");
        pathField.RegisterValueChangedCallback(OnPathValueChange);

        targetScrollView = root.Q<ScrollView>("targetScrollView");

        assetNameField = root.Q<TextField>("assetNameField");
        assetNameField.RegisterValueChangedCallback(OnCheckAssetNameChange);
        assetNameField.SetValueWithoutNotify(checkAssetName);

        typeDropdownField = root.Q<DropdownField>("typeDropdownField");
        typeDropdownField.RegisterValueChangedCallback(OnCheckAssetTypeChange);
        typeDropdownField.choices = checkTypeList;
        typeDropdownField.SetValueWithoutNotify(checkTypeList[0]);

        checkTypeNameField = root.Q<TextField>("typeNameField");
        checkTypeNameField.RegisterValueChangedCallback(OnCheckAssetTypeChange);
        checkTypeNameField.SetValueWithoutNotify(checkTypeList[0]);
        checkTypeNameField.SetEnabled(true);

        executeBtn = root.Q<Button>("executeBtn");
        executeBtn.clicked += OnClickExecuteBtn;
        executeBtn.SetEnabled(true);

    }

    private void OnCheckAssetTypeChange(ChangeEvent<string> evt)
    {
        checkTypeName = evt.newValue;
        checkTypeNameField.SetEnabled(typeDropdownField.value == "Custom");
    }

    private void OnCheckAssetNameChange(ChangeEvent<string> evt)
    {
        checkAssetName = evt.newValue;
    }

    private void InitCheckTypeList()
    {
        checkTypeList.Clear();
        checkTypeList.Add("Custom");
        checkTypeList.Add(typeof(Shader).FullName);
        checkTypeList.Add(typeof(Texture).FullName);
        checkTypeList.Add(typeof(Texture2D).FullName);
        checkTypeList.Add(typeof(Sprite).FullName);
    }
    private void OnClickExecuteBtn() 
    {
        prefabPathList.Clear();
        dependenciesList.Clear();
        targetScrollView.contentContainer.Clear();

        CheckRes(checkAssetsPath, ".unity", (_path) => { prefabPathList.Add(_path); });
        CheckRes(checkAssetsPath, ".mat", (_path) => { prefabPathList.Add(_path); });
        CheckRes(checkAssetsPath, ".fbx", (_path) => { prefabPathList.Add(_path); });
        CheckRes(checkAssetsPath, ".prefab", (_path) => { prefabPathList.Add(_path); });

        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            UnityEngine.Object checkObj = AssetDatabase.LoadAssetAtPath(shortPath, typeof(UnityEngine.Object));

            GetDependenciesInfo(checkObj, shortPath);
        }
        foreach (SDependenciesInfo info in dependenciesList) 
        {
            TextField targetPathField = new TextField("target");
            targetPathField.label = "   ¡ú";
            targetPathField.multiline = true;
            targetPathField.isReadOnly = true;
            targetPathField.value = info.targetPath;
            targetPathField.RegisterCallback<MouseDownEvent>((evt) => 
            {
                EditorGUIUtility.PingObject(info.targetObj);
            });
            targetScrollView.Add(targetPathField);
        }
    }
    private void GetDependenciesInfo(UnityEngine.Object targetObject,string shortPath) 
    {
        if (targetObject != null)
        {
            UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { targetObject });

            foreach (UnityEngine.Object dependency in dependencies)
            {
                Debug.Log(targetObject + "  Dependency: " + dependency.name + ", Type: " + dependency.GetType());
                if (dependency.GetType().FullName != checkTypeName) continue;
                if (dependency.name != checkAssetName) continue;

                SDependenciesInfo info = new SDependenciesInfo();
                info.targetObj = targetObject;
                info.dependency = dependency.name;
                info.dependencyType = dependency.GetType().FullName;
                info.targetPath = shortPath;
                dependenciesList.Add(info);
            }
        }
        else
        {
            Debug.LogWarning("No game object selected.");
        }
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
    private void OnPathValueChange(ChangeEvent<string> evt)
    {
        checkAssetsPath = evt.newValue;
    }
    private void OnGUI()
    {
        checkAssetsPath = OnDrawElementAcceptDrop(pathField.contentRect, checkAssetsPath);
        pathField.SetValueWithoutNotify(checkAssetsPath);
        executeBtn.SetEnabled(!string.IsNullOrEmpty(checkAssetsPath) 
            && !string.IsNullOrEmpty(checkTypeName)
            && !string.IsNullOrEmpty(checkAssetName));
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
        pathField.UnregisterValueChangedCallback(OnPathValueChange);
        assetNameField.UnregisterValueChangedCallback(OnCheckAssetNameChange);
        typeDropdownField.UnregisterValueChangedCallback(OnCheckAssetTypeChange);
        checkTypeNameField.UnregisterValueChangedCallback(OnCheckAssetTypeChange);
    }
}
public struct SDependenciesInfo
{
    public UnityEngine.Object targetObj;
    public string targetPath;
    public string dependency;
    public string dependencyType;
}