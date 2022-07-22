using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace LQTools
{
    public enum OPTIONS
    {
        /// <summary>
        /// UI资源
        /// </summary>
        UI = 0,
        /// <summary>
        ///丢失资源
        /// </summary>
        LOSEUI,
        /// <summary>
        ///一键替换资源
        /// </summary>
        REPLACEUI
    }


    public class SearchRefrenceEditorWindow : EditorWindow
    {
        public OPTIONS op;
        /// <summary>
        /// 查找引用
        /// </summary>
        [MenuItem("存放路径/UI资源检索工具")]
        static void SearchRefrence()
        {
            SearchRefrenceEditorWindow window = (SearchRefrenceEditorWindow)EditorWindow.GetWindow(typeof(SearchRefrenceEditorWindow), false, "Searching", true);
            window.Show();
        }
        private static Object searchObject;
        private static Object searchPrefab;
        private List<Object> result = new List<Object>();
        private List<string> noQuoteList = new List<string>();
        private List<string> quoteList = new List<string>();
        private List<Transform> nodeList = new List<Transform>();
        private bool isCencel = false;
        private GameObject findObject;
        private string assetPath;
        private string assetGuid;
        private bool isSingle;
        private bool noQuote;
        private bool Quote;
        private bool isChildWindow;
        private bool isChangeSprite;
        private int tipShowTime;
        private Sprite changeSprite;
        private Object lostUI;
        string selectPath = "";
        string Path;
        string[] textureGuids;
        bool isHaveSearched = false;
        Vector2 v1 = Vector2.zero;
        Vector2 v2 = Vector2.zero;
        Vector2 v3 = Vector2.zero;
        Rect windowRect = new Rect(600, 20, 400, 500);
        GameObject curObj;
        GameObject curImgNode;
        private void OnGUI()
        {
            op = (OPTIONS)EditorGUILayout.EnumPopup("选择类型:", op);
            if (op == OPTIONS.UI)
            {
                GUILayout.Label("UI资源:");
                isSingle = EditorGUILayout.Toggle("是否选择具体的目录", isSingle, GUILayout.Width(400));
                isChildWindow = EditorGUILayout.Toggle("是否启用助理贴贴", isChildWindow, GUILayout.Width(400));
                isChangeSprite = EditorGUILayout.Toggle("极致模式", isChangeSprite, GUILayout.Width(400));
                if (isChangeSprite)
                {
                    changeSprite = (Sprite)EditorGUILayout.ObjectField(changeSprite, typeof(Sprite), true, GUILayout.Width(100), GUILayout.Height(100));
                    if (GUILayout.Button("一键东来", GUILayout.Width(100)))
                    {
                        if (nodeList.Count > 0 && isChangeSprite)
                        {
                            for (int i = 0; i < nodeList.Count; i++)
                            {
                                curImgNode = nodeList[i].gameObject;
                                curImgNode.GetComponent<Image>().sprite = changeSprite;
                                EditorUtility.SetDirty(curImgNode);
                                AssetDatabase.SaveAssets();
                            }
                            AssetDatabase.Refresh();
                        }
                    }
                }
                if (isSingle)
                {
                    GUILayout.Label("搜索UI引用节点:");
                    EditorGUILayout.BeginHorizontal();
                    selectPath = GUILayout.TextField(Path, GUILayout.Width(350));
                    if (GUILayout.Button("选择引用节点", GUILayout.Width(100)))
                    {
                        GUILayout.Label("path");
                        selectPath = EditorUtility.OpenFolderPanel("选择配置文件目录", Application.dataPath, " ");
                        if (selectPath.Length > 0)
                        {
                            Path = selectPath.Substring(selectPath.IndexOf("Assets"));
                            Path = Path.Replace('\\', '/');
                        }
                    }
                    if (GUILayout.Button("查找", GUILayout.Width(100)))
                    {
                        noQuoteList.Clear();
                        quoteList.Clear();
                        searchUI(Path);
                        GetResult();
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label("搜索引用节点结果:");
                    if (isChildWindow == true)
                    {
                        string emo = (quoteList.Count > 0) ? "ヾ(o◕∀◕)ﾉヾ" : "> _ <";
                        BeginWindows();
                        windowRect = GUILayout.Window(0, windowRect, ChildWindow, "贴贴" + emo);
                        EndWindows();
                        if (tipShowTime <= 6)
                        {
                            ShowNotification(new GUIContent("贴贴为你服务ヾ(o◕∀◕)ﾉヾ"));
                            tipShowTime += 1;
                        }
                        else
                        {
                            RemoveNotification();
                        }
                    }
                    else
                    {
                        tipShowTime = 0;
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Texture), true, GUILayout.Width(200));
                    if (GUILayout.Button("UI引用查询", GUILayout.Width(100)))
                    {
                        if (isHaveSearched == true)
                        {
                            isHaveSearched = false;
                        }
                        GetResult();
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label("UI节点:");
                    if (isHaveSearched == true)
                    {
                        EditorGUILayout.HelpBox("暂无节点引用此图片", MessageType.None);
                    }
                }

            }
            else if (op == OPTIONS.LOSEUI)
            {
                GUILayout.Label("全局搜索丢失UI节点:");
                isSingle = EditorGUILayout.Toggle("是否启动局部预制体检索", isSingle, GUILayout.Width(200));
                EditorGUILayout.BeginHorizontal();
                if (isSingle == true)
                {
                    searchPrefab = EditorGUILayout.ObjectField(searchPrefab, typeof(GameObject), true, GUILayout.Width(200));
                }
                else
                {
                    nodeList.Clear();
                }
                if (GUILayout.Button("丢失UI节点查询", GUILayout.Width(100)))
                {
                    if (isHaveSearched == true)
                    {
                        isHaveSearched = false;
                    }
                    GetResult();
                    if (nodeList.Count <= 0)
                    {
                        isHaveSearched = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("丢失UI节点:");
                if (isHaveSearched == true)
                {
                    EditorGUILayout.HelpBox("预制体无Miss节点", MessageType.None);
                }
            }
            else if (op == OPTIONS.REPLACEUI)
            {
                GUILayout.Label("丢失资源节点:");
                EditorGUILayout.BeginHorizontal();
                searchObject = EditorGUILayout.ObjectField(searchObject, typeof(GameObject), true, GUILayout.Width(200));
                if (GUILayout.Button("丢失图片查找", GUILayout.Width(100)))
                {
                    locationUI();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("丢失图片:");
                if (lostUI != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(lostUI, typeof(Texture), true, GUILayout.Width(200));
                    EditorGUILayout.EndHorizontal();
                }

            }
            if (op != OPTIONS.REPLACEUI)
            {
                if (nodeList.Count > 0)
                {
                    v2 = EditorGUILayout.BeginScrollView(v2, GUILayout.Width(600), GUILayout.Height(800));
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
                        if (GUILayout.Button("替换", GUILayout.Width(100), GUILayout.Height(20)) && op == OPTIONS.UI)
                        {
                            if (isChangeSprite)
                            {
                                curImgNode = nodeList[i].gameObject;
                                curImgNode.GetComponent<Image>().sprite = changeSprite;
                                EditorUtility.SetDirty(curImgNode);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                            }
                            else
                            {
                                ShowNotification(new GUIContent("贴贴提示：Monster,您还没放置图片哦ヾ(o◕∀◕)ﾉヾ"));
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

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
        void FindInAsset()
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
        void GetResult()
        {
            result.Clear();
            nodeList.Clear();

            if (op == OPTIONS.UI && !isSingle)
            {
                if (searchObject == null)
                    return;
                assetPath = AssetDatabase.GetAssetPath(searchObject);
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            }
            //只检查prefab 预制体检测路径
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/arts/prefabs/ui" });
            int length = guids.Length;
            List<string> fs = new List<string>();//创建fs列表，储存guids
            for (int i = 0; i < length; i++)
            {
                fs.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            string[] filePath = fs.ToArray();

            int startIndex = 0;
            if (op == OPTIONS.LOSEUI && isSingle == true)
            {
                result.Add(searchPrefab);
                outputResult();
                return;
            }
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
                if (!isSingle)
                {
                    if (content.Contains(assetGuid))
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath[startIndex], typeof(Object));
                        if (!result.Contains(fileObject))
                        {
                            result.Add(fileObject);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < textureGuids.Length; i++)
                    {
                        if (content.Contains(textureGuids[i]))
                        {
                            Object fileObject = AssetDatabase.LoadAssetAtPath(filePath[startIndex], typeof(Object));
                            if (!result.Contains(fileObject))
                            {
                                result.Add(fileObject);
                            }
                        }
                    }
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
            if (isSingle != true)
            {
                EditorUtility.DisplayCancelableProgressBar("Checking", filePath[startIndex], (float)startIndex / (float)filePath.Length);
            }
        };
        }
        //输出结果
        void outputResult()
        {
            //GUILayout.Label("资源引用预制体:");
            for (int i = 0; i < result.Count; i++)
            {
                searchNode(result[i]);
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
                    if (isSingle == false)
                    {
                        if (image.mainTexture == searchObject)
                        {
                            nodeList.Add(image.transform);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < textureGuids.Length; i++)
                        {
                            Object fileObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(textureGuids[i]), typeof(Object));
                            if (image.mainTexture == fileObject)
                            {
                                if (!quoteList.Contains(fileObject.name))
                                {
                                    quoteList.Add(fileObject.name);
                                }
                                nodeList.Add(image.transform);
                            }
                            else if (image.mainTexture != fileObject)
                            {
                                if (!noQuoteList.Contains(fileObject.name))
                                {
                                    noQuoteList.Add(fileObject.name);
                                }
                            }
                        }
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
            if (nodeList.Count <= 0)
            {
                isHaveSearched = true;
            }
            else
            {
                isHaveSearched = false;
            }
        }
        //搜索目录下的所有图片GUID
        void searchUI(string path)
        {
            textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { path });
        }
        //检索定位图片
        void locationUI()
        {
            nodeList.Clear();
            if (searchObject == null)
                return;
            GameObject searchGameObject = (GameObject)searchObject;
            var image = searchGameObject.GetComponent<Image>();
            var instaceID = image.sprite.GetInstanceID();
            if (instaceID > 0)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instaceID));
                var instace = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(instaceID), typeof(Sprite));
                if (instace == null && image.sprite == null)
                {
                    if (guid == "")
                    {
                        return;
                    }
                }
                Object fileObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(instaceID), typeof(Object));
                lostUI = fileObject;
            }
        }
        void ChildWindow(int windowID)
        {
            noQuoteList.RemoveAll(it => quoteList.Contains(it));
            GUILayout.Label("本次搜索UI张数为" + (noQuoteList.Count + quoteList.Count));
            GUI.DragWindow(new Rect(50, 0, windowRect.width, 16));

            Quote = EditorGUILayout.Foldout(Quote, "引用图片数量" + quoteList.Count);
            if (Quote)
            {
                v1 = EditorGUILayout.BeginScrollView(v1, GUILayout.Width(300), GUILayout.Height(180));
                for (int k = 0; k < quoteList.Count; k++)
                {
                    EditorGUILayout.LabelField(quoteList[k]);
                }
                EditorGUILayout.EndScrollView();
            }

            noQuote = EditorGUILayout.Foldout(noQuote, "未引用图片数量" + noQuoteList.Count);
            if (noQuote)
            {
                v3 = EditorGUILayout.BeginScrollView(v3, GUILayout.Width(300), GUILayout.Height(320));
                for (int i = 0; i < noQuoteList.Count; i++)
                {
                    EditorGUILayout.LabelField(noQuoteList[i]);
                }
                EditorGUILayout.EndScrollView();
            }
        }
        void OnDestroy()
        {
            isCencel = false;
            searchObject = null;
        }
    }
}

