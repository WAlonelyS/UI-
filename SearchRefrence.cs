using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Lylibs;
using System.Reflection;

public enum OPTIONS
{
    /// <summary>
    /// UI资源
    /// </summary>
    UI = 0,
    /// <summary>
    ///丢失资源
    /// </summary>
    LOSEUI
}


public class SearchRefrenceEditorWindow : EditorWindow
{
    public OPTIONS op;
    /// <summary>
    /// 查找引用
    /// </summary>
    [MenuItem("Assets/SearchRefrence")]
    static void SearchRefrence()
    {
        SearchRefrenceEditorWindow window = (SearchRefrenceEditorWindow)EditorWindow.GetWindow(typeof(SearchRefrenceEditorWindow), false, "Searching", true);
        window.Show();
    }

    private static Object searchObject;
    private List<Object> result = new List<Object>();
    private List<Transform> nodeList = new List<Transform>();
    private bool isCencel = false;
    private GameObject findObject;
    private string assetPath;
    private string assetGuid;

    Vector2 v2 = Vector2.zero;

    GameObject curObj;
    private void OnGUI()
    {
        op = (OPTIONS)EditorGUILayout.EnumPopup("选择类型:", op);
        if (op == OPTIONS.UI)
        {
            GUILayout.Label("UI资源:");
            EditorGUILayout.BeginHorizontal();
            searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Texture), true, GUILayout.Width(200));
            if (GUILayout.Button("UI引用查询", GUILayout.Width(100)))
            {
                GetResult();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("UI节点:");
        }
        else if (op == OPTIONS.LOSEUI)
        {
            GUILayout.Label("全局搜索丢失UI节点:");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("丢失UI节点查询", GUILayout.Width(100)))
            {
                GetResult();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("丢失UI节点:");
        }

        if (nodeList.Count > 0)
        {
            v2 = EditorGUILayout.BeginScrollView(v2, GUILayout.Width(450), GUILayout.Height(800));
            for (int i = 0; i < nodeList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(nodeList[i], typeof(Transform), true, GUILayout.Width(300), GUILayout.Height(20));
                if (GUILayout.Button("定位", GUILayout.Width(100), GUILayout.Height(20)))
                {
                    if (Selection.activeGameObject)
                    {
                        findObject = Selection.activeGameObject;
                    }
                    else
                    {
                        Selection.activeGameObject = findObject;
                    }
                    if (!findObject)
                    {
                        Debug.LogError("错误提示，未进入预制体，请双击进入后点击搜索");
                        return;
                    }
                    curObj = nodeList[i].gameObject;
                    FindInAsset();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        Debug.Log("显示滑动框位置" + v2.y);
    }
    //路径
    string path = string.Empty;
    //遍历路径
    void GetPath(GameObject obj)
    {
        if (obj.transform.root == obj.transform.parent)
        {
            return;
        }
        else
        {
            path = obj.transform.parent.name + "/" + path;
            GetPath(obj.transform.parent.gameObject);
        }
    }
    //遍历预制体内的节点
    private void FindInAsset()
    {
        GameObject prefab = Selection.activeGameObject.gameObject;
        path = curObj.name;
        GetPath(curObj);
        Transform tr = prefab.transform.Find(path);

        if (tr)
        {
            Selection.activeGameObject = tr.gameObject;
        }
        Selection.activeObject = tr;
        EditorGUIUtility.PingObject(Selection.activeGameObject);
    }
    //开始检查
    private void GetResult()
    {
        result.Clear();
        nodeList.Clear();

        if (op == OPTIONS.UI)
        {
            if (searchObject == null)
                return;
            assetPath = AssetDatabase.GetAssetPath(searchObject);
            assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        }

        //只检查prefab
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/arts/prefabs/ui" });

        int length = guids.Length;
        List<string> fs = new List<string>();//创建fs列表，储存guids
        for (int i = 0; i < length; i++)
        {
            fs.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        string[] filePath = fs.ToArray();
        int startIndex = 0;

        EditorApplication.update = delegate ()
        {
            if (isCencel || EditorUtility.DisplayCancelableProgressBar("Checking", filePath[startIndex], (float)startIndex / (float)filePath.Length))//点击取消按钮直接打断进程并返回已遍历的结果
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                isCencel = false;
                //显示结果
                outputResult();
                return;
            }
            //检查是否包含guid
            if (op == OPTIONS.UI)
            {
                string content = File.ReadAllText(filePath[startIndex]);
                if (content.Contains(assetGuid))
                {
                    Object fileObject = AssetDatabase.LoadAssetAtPath(filePath[startIndex], typeof(Object));
                    result.Add(fileObject);
                }
            }
            else if (op == OPTIONS.LOSEUI)
            {
                Object fileObject = AssetDatabase.LoadAssetAtPath(filePath[startIndex], typeof(Object));
                result.Add(fileObject);
            }

            if (!isCencel)
            {
                startIndex++;
                if (startIndex >= filePath.Length)
                {
                    startIndex = filePath.Length - 1;
                    isCencel = true;
                }
            }
            EditorUtility.DisplayCancelableProgressBar("Checking", filePath[startIndex], (float)startIndex / (float)filePath.Length);
        };
    }
    //输出结果
    void outputResult()
    {

        //GUILayout.Label("资源引用预制体:");
        for (int i = 0; i < result.Count; i++)
        {
            this.searchNode(result[i]);
        }

    }
    //搜索节点
    void searchNode(Object father)
    {
        GameObject Father = (GameObject)father;
        var images = Father.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            //检查是否包含guid
            if (op == OPTIONS.UI)
            {
                if (image.mainTexture == searchObject)
                {
                    nodeList.Add(image.transform);
                }
            }
            else if (op == OPTIONS.LOSEUI)
            {
                var instaceID = image.sprite.GetInstanceID();
                if (instaceID > 0)
                {
                    var instace = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(instaceID), typeof(Sprite));
                    if (instace == null && image.sprite == null)
                    {
                        nodeList.Add(image.transform);
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        isCencel = false;
        searchObject = null;
    }
}
