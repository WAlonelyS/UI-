using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using Lylibs;
using System.Reflection;
using LQTools;
using System.Xml.Linq;
using NPOI.SS.Util;
using NPOI.HSSF.Util;

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
        REPLACEUI,
        /// <summary>
        ///一键查询Z轴
        /// </summary>
        SEARCHNODE_Z,
        /// <summary>
        ///组件操作 
        /// </summary>
        COMPONENT
    }

    public enum COMPONENT
    {
        /// <summary>
        /// UI资源
        /// </summary>
        BUTTON = 0,
        /// <summary>
        ///丢失资源
        /// </summary>
        TXTFONT,
    }


    public class SearchRefrenceEditorWindow : EditorWindow
    {
        public OPTIONS op;
        public COMPONENT component;
        /// <summary>
        /// 查找引用
        /// </summary>
        [MenuItem("leiyan/UI工具/UI检索", false, 1)]
        static void SearchRefrence()
        {
            SearchRefrenceEditorWindow window = (SearchRefrenceEditorWindow)EditorWindow.GetWindow(typeof(SearchRefrenceEditorWindow), false, "UI检索", true);
            window.Show();
        }
        private static Object searchObject;
        private static Object addObject;
        private static int SearchNum;
        private static Object searchPrefab;
        public List<Object> result = new List<Object>();
        private List<string> noQuoteList = new List<string>();
        private List<string> quoteList = new List<string>();
        public List<Transform> nodeList = new List<Transform>();
        public List<Object> searchList = new List<Object>();
        public List<string> fs = new List<string>();
        public bool isCencel = false;
        private GameObject findObject;
        private string assetPath;
        private string assetGuid;
        public bool isSingle;
        private bool isFindFont;
        private Color labelColor;
        private Color changeColor;
        public bool noQuote;
        public bool Quote;
        private bool isChildWindow;
        private bool isChangeSprite;
        private int tipShowTime;
        private Sprite changeSprite;
        private Object lostUI;
        string selectPath = "";
        string Path;
        string[] textureGuids;
        bool isHaveSearched = false;
        public Vector2 v1 = Vector2.zero;
        public Vector2 v2 = Vector2.zero;
        public Vector2 v3 = Vector2.zero;
        Rect windowRect = new Rect(600, 20, 400, 500);
        GameObject curObj;
        GameObject curObject;
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
            else if (op == OPTIONS.SEARCHNODE_Z)
            {
                EditorGUILayout.LabelField("搜索数量:" + SearchNum);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("开始搜索", GUILayout.Width(100), GUILayout.Height(20)))
                {

                }
                if (GUILayout.Button("加一", GUILayout.Width(100), GUILayout.Height(20)))
                {
                    SearchNum += 1;
                }
                EditorGUILayout.EndHorizontal();
                for (int i = 0; i < SearchNum; i++)
                {
                    searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Transform), true, GUILayout.Width(200));
                    if (searchObject != null)
                    {
                        searchList.Add(searchObject);
                    }
                }
            }
            else if (op == OPTIONS.COMPONENT)
            {
                component = (COMPONENT)EditorGUILayout.EnumPopup("选择组件操作类型:", component);
                EditorGUILayout.BeginHorizontal();
                if (component == COMPONENT.BUTTON)
                {
                    addObject = EditorGUILayout.ObjectField(addObject, typeof(Object), true, GUILayout.Width(200));
                    if (addObject && nodeList.Count > 0)
                    {
                        if (GUILayout.Button("一键挂载", GUILayout.Width(100)))
                        {
                            var type = Assembly.Load("Assembly-CSharp").GetType("Lylibs." + addObject.name);
                            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/arts/prefabs/ui" });
                            int index = 0;
                            EditorApplication.update = delegate ()
                            {

                                if (index == guids.Length || EditorUtility.DisplayCancelableProgressBar("Checking", guids[index], (float)index / (float)guids.Length))
                                {
                                    AssetDatabase.Refresh();
                                    EditorUtility.ClearProgressBar();
                                    EditorApplication.update = null;
                                    return;
                                };

                                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                                var buttons = pfb.GetComponentsInChildren<Button>(true);
                                foreach (var item in buttons)
                                {
                                    item.gameObject.AddComponent(type);
                                    EditorUtility.SetDirty(item.gameObject);
                                    AssetDatabase.SaveAssets();
                                }
                                index++;
                                EditorUtility.DisplayCancelableProgressBar("Checking", guids[index], (float)index / (float)guids.Length);
                            };
                        }
                        if (GUILayout.Button("一键移除", GUILayout.Width(100)))
                        {

                            var type = Assembly.Load("Assembly-CSharp").GetType("Lylibs." + addObject.name);
                            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/arts/prefabs/ui" });
                            int index = 0;
                            EditorApplication.update = delegate ()
                            {

                                if (index >= guids.Length || EditorUtility.DisplayCancelableProgressBar("Checking", guids[index], (float)index / (float)guids.Length))
                                {
                                    AssetDatabase.Refresh();
                                    EditorUtility.ClearProgressBar();
                                    EditorApplication.update = null;
                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();
                                    return;
                                };

                                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                                var componets = pfb.GetComponentsInChildren<Button>(true);
                                foreach (var componet in componets)
                                {
                                    if (componet.GetComponent(type))
                                    {
                                        DestroyImmediate(componet.GetComponent(type), true);
                                    }
                                }
                                EditorUtility.DisplayCancelableProgressBar("Checking", guids[index], (float)index / (float)guids.Length);
                                index++;
                            };
                        }
                    }
                    else if (addObject == null)
                    {
                        nodeList.Clear();
                        result.Clear();
                    }
                }
                else if (component == COMPONENT.TXTFONT)
                {

                    isFindFont = EditorGUILayout.Toggle("是否搜索字体组件", isFindFont, GUILayout.Width(200));
                    if (isFindFont)
                    {
                        addObject = EditorGUILayout.ObjectField(addObject, typeof(Text), true, GUILayout.Width(200));
                    }
                    else
                    {
                        changeColor = EditorGUILayout.ColorField("替换颜色", changeColor, GUILayout.Width(200));
                        labelColor = EditorGUILayout.ColorField("搜索的颜色", labelColor, GUILayout.Width(200));
                    }
                    if (((labelColor != null) || addObject) && nodeList.Count > 0)
                    {
                        if (GUILayout.Button("一键", GUILayout.Width(100)))
                        {
                            int index = 0;
                            EditorApplication.update = delegate ()
                            {
                                if (index >= nodeList.Count)
                                {
                                    AssetDatabase.Refresh();
                                    EditorUtility.ClearProgressBar();
                                    EditorApplication.update = null;
                                    return;
                                };

                                curImgNode = nodeList[index].gameObject;
                                curImgNode.GetComponent<Text>().color = changeColor;
                                EditorUtility.SetDirty(curImgNode);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                EditorUtility.DisplayCancelableProgressBar("Checking", nodeList[index].gameObject.name, (float)index / ((float)nodeList.Count - 1));
                                index++;
                            };
                        }
                    }
                    else if (labelColor == null)
                    {
                        nodeList.Clear();
                        result.Clear();
                    }
                }
                if (GUILayout.Button("开始查找", GUILayout.Width(100)))
                {
                    AssetDatabase.Refresh();
                    isSingle = false;
                    GetResult();
                }
                EditorGUILayout.EndHorizontal();
            }
            if (op != OPTIONS.REPLACEUI)
            {
                if (nodeList.Count > 0)
                {
                    v2 = EditorGUILayout.BeginScrollView(v2, GUILayout.Width(1000), GUILayout.Height(800));
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
                        if (op == OPTIONS.UI)
                        {
                            if (GUILayout.Button("替换", GUILayout.Width(100), GUILayout.Height(20)))
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
                        }
                        else if (op == OPTIONS.COMPONENT)
                        {
                            if (GUILayout.Button("挂载", GUILayout.Width(100), GUILayout.Height(20)))
                            {
                                if (addObject)
                                {
                                    curObject = nodeList[i].gameObject;
                                    string name = addObject.name;
                                    var type = Assembly.Load("Assembly-CSharp").GetType("Lylibs." + name);
                                    GameObject[] objectArray = Selection.gameObjects;
                                    objectArray[0].AddComponent(type);
                                    EditorUtility.SetDirty(objectArray[0]);
                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();
                                }
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
        private bool isSuccess;

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
            //只检查prefab
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/arts/prefabs/ui" });
            int length = guids.Length;
            fs = new List<string>();//创建fs列表，储存guids
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
                else if (op == OPTIONS.LOSEUI || op == OPTIONS.COMPONENT)
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
        public void outputResult()
        {
            //GUILayout.Label("资源引用预制体:");
            for (int i = 0; i < result.Count; i++)
            {
                searchNode(result[i]);
            }
        }
        //搜索节点
        public void searchNode(Object father)
        {
            GameObject Father = (GameObject)father;
            if (op != OPTIONS.COMPONENT)
            {
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
                                checkIsInMaterial(image, textureGuids[i]);
                                if (image.mainTexture == fileObject)
                                {
                                    if (!quoteList.Contains(fileObject.name))
                                    {
                                        quoteList.Add(fileObject.name);
                                    }
                                    if (!nodeList.Contains(image.transform))
                                    {
                                        nodeList.Add(image.transform);
                                    }
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
            }
            else
            {
                if (component == COMPONENT.BUTTON)
                {
                    var buttons = Father.GetComponentsInChildren<Button>(true);
                    foreach (var button in buttons)
                    {
                        if (addObject && button.gameObject.GetComponent(addObject.name) == null)
                        {
                            nodeList.Add(button.transform);
                        }

                    }
                }
                else if (component == COMPONENT.TXTFONT)
                {
                    var fonts = Father.GetComponentsInChildren<Text>(true);
                    foreach (var font in fonts)
                    {
                        if (font.color == labelColor)
                        {
                            nodeList.Add(font.transform);
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
        public void searchUI(string path)
        {
            textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { path });
        }
        //检索定位字体组件
        void locationFont()
        {
            nodeList.Clear();
            if (searchObject == null)
                return;
            GameObject searchGameObject = (GameObject)searchObject;
            var font = searchGameObject.GetComponent<Text>();
            var instaceID = font.GetInstanceID();
            if (instaceID > 0)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instaceID));
                var instace = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(instaceID), typeof(Text));
                if (instace == null && font.color == null)
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
            if (windowID == 0)
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
        }

        public void checkIsInMaterial(Image gamObj, string contrastGuid)
        {
            if (gamObj.material.shader && gamObj.material.shader.GetInstanceID() > 0)
            {
                Material Material = gamObj.material;
                Shader shader = Material.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = Material.GetTexture(propertyName);
                        if (tex && tex.GetInstanceID() > 0)
                        {
                            Object Object = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(tex.GetInstanceID()), typeof(Object));
                            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Object));
                            if (contrastGuid == guid)
                            {
                                Object fileObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(contrastGuid), typeof(Object));
                                if (!quoteList.Contains(fileObject.name))
                                {
                                    quoteList.Add(fileObject.name);
                                }
                                if (!nodeList.Contains(gamObj.transform))
                                {
                                    nodeList.Add(gamObj.transform);
                                }
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            isCencel = false;
            searchObject = null;
        }
    }
}
public class UIEditorWindow : SearchRefrenceEditorWindow
{
    //各类型的引用列表
    public Dictionary<string, List<string>> citeDic = new Dictionary<string, List<string>>();
    //各类型的未引用列表
    public Dictionary<string, List<string>> noCiteDic = new Dictionary<string, List<string>>();
    //各类型的guid
    public Dictionary<string, List<string>> guidDic = new Dictionary<string, List<string>>();
    //引用的材质
    public List<string> materialList = new List<string>();
    //引用的Shader
    public List<string> texture2DList = new List<string>();
    //搜索的声音声音
    public List<string> audioList = new List<string>();
    //搜索的声音声音
    public List<string> meshList = new List<string>();
    //路径
    public FileInfo[] luaScriptFiles;
    //脚本
    public List<string> luaScriptDicList = new List<string>();
    //丢失节点
    public List<Transform> loseNodeList = new List<Transform>();
    //
    bool isAudio = false;
    //
    bool isMesh = false;
    IRow row;
    ICell cell;
    FileStream file;
    string selectPrefabPath = "";
    string selectTexture2DPath = "";
    string selectMaterialPath = "";
    string selectAudioClipPath = "";
    string selectMeshPath = "";
    string PrefabPath = "";
    string Texture2DPath = "";
    string MaterialPath = "";
    string AudioClipPath = "";
    string MeshPath = "";
    [MenuItem("leiyan/UI工具/美术引用检索报告", false, 0)]

    static void SearchRefrence()
    {
        UIEditorWindow window = (UIEditorWindow)EditorWindow.GetWindow(typeof(UIEditorWindow), false, "美术检测报告", true);
        window.Show();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("检测Lua引用", GUILayout.Height(20), GUILayout.Width(200)))
        {
            outputLuaResult(noCiteDic);
        }
        EditorGUILayout.BeginHorizontal();
        selectPrefabPath = GUILayout.TextField(PrefabPath, GUILayout.Width(350));
        if (GUILayout.Button("场景目录", GUILayout.Width(100)))
        {
            selectPrefabPath = EditorUtility.OpenFolderPanel("选择场景目录", Application.dataPath, "场景路径");
            PrefabPath = selectPrefabPath.Substring(selectPrefabPath.IndexOf("Assets")).Replace('\\', '/');
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        selectTexture2DPath = GUILayout.TextField(Texture2DPath, GUILayout.Width(350));
        if (GUILayout.Button("纹理目录", GUILayout.Width(100)))
        {
            selectTexture2DPath = EditorUtility.OpenFolderPanel("选择纹理目录", Application.dataPath, "纹理路径");
            if (selectTexture2DPath.Length > 0)
            {
                Texture2DPath = selectTexture2DPath.Substring(selectTexture2DPath.IndexOf("Assets")).Replace('\\', '/');
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        selectMaterialPath = GUILayout.TextField(MaterialPath, GUILayout.Width(350));
        if (GUILayout.Button("材质目录", GUILayout.Width(100)))
        {
            selectMaterialPath = EditorUtility.OpenFolderPanel("选择材质目录", Application.dataPath, "材质路径");
            if (selectMaterialPath.Length > 0)
            {
                MaterialPath = selectMaterialPath.Substring(selectMaterialPath.IndexOf("Assets")).Replace('\\', '/');
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        selectAudioClipPath = GUILayout.TextField(AudioClipPath, GUILayout.Width(350));


        if (GUILayout.Button("音频目录", GUILayout.Width(100)))
        {
            selectAudioClipPath = EditorUtility.OpenFolderPanel("选择音频目录", Application.dataPath, "音频路径");
            if (selectAudioClipPath.Length > 0)
            {
                AudioClipPath = selectAudioClipPath.Substring(selectAudioClipPath.IndexOf("Assets")).Replace('\\', '/');
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        selectMeshPath = GUILayout.TextField(MeshPath, GUILayout.Width(350));
        if (GUILayout.Button("Mesh目录", GUILayout.Width(100)))
        {
            selectMeshPath = EditorUtility.OpenFolderPanel("选择Mesh目录", Application.dataPath, "Mesh路径");
            if (selectAudioClipPath.Length > 0)
            {
                MeshPath = selectMeshPath.Substring(selectMeshPath.IndexOf("Assets")).Replace('\\', '/');
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("检查", GUILayout.Height(20), GUILayout.Width(200)))
        {
            actionCheck();
        }
        if (GUILayout.Button("导出Excel", GUILayout.Height(20), GUILayout.Width(200)))
        {
            int count = 0;
            int index = 0;
            foreach (KeyValuePair<string, List<string>> noCites in noCiteDic)
            {
                index += 1;
                refExcel2(noCites.Value, noCites.Key, index, count);
                count += noCites.Value.Count;
            }
        }
        if (GUILayout.Button("创建文件夹", GUILayout.Width(100)))
        {
            createFolder();
        }
        //只检查prefab
    }

    public void ExternalInterface()
    {
        actionCheck();
    }
    public void actionCheck()
    {
        guidDic.Clear();
        texture2DList.Clear();
        materialList.Clear();
        audioList.Clear();
        meshList.Clear();
        //材质
        List<string> materialGuidList = new List<string>();
        //纹理
        List<string> texture2dGuidList = new List<string>();
        //声音
        List<string> audioGuidList = new List<string>();
        //Mesh网格纹理
        List<string> meshGuidList = new List<string>();
        AssetDatabase.Refresh();

        string[] preGuids = AssetDatabase.FindAssets("t:Prefab", new[] { selectPrefabPath.Substring(selectPrefabPath.IndexOf("Assets")).Replace('\\', '/') });
        string[] searGuids = AssetDatabase.FindAssets("t:Texture2D ", new[] { selectTexture2DPath.Substring(selectTexture2DPath.IndexOf("Assets")).Replace('\\', '/') });
        string[] searMatGuids = AssetDatabase.FindAssets("t:Material", new[] { selectMaterialPath.Substring(selectMaterialPath.IndexOf("Assets")).Replace('\\', '/') });
        string[] searAudioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { selectAudioClipPath.Substring(selectAudioClipPath.IndexOf("Assets")).Replace('\\', '/') });
        string[] searMeshGuids = AssetDatabase.FindAssets("t:Mesh", new[] { selectMeshPath.Substring(selectMeshPath.IndexOf("Assets")).Replace('\\', '/') });
        if (Directory.Exists("LuaScripts"))
        {
            DirectoryInfo direction = new DirectoryInfo("LuaScripts");
            luaScriptFiles = direction.GetFiles("*", SearchOption.AllDirectories);
        }
        fs = new List<string>();
        for (int i = 1; i < preGuids.Length; i++)
        {
            fs.Add(AssetDatabase.GUIDToAssetPath(preGuids[i]));
        }
        for (int i = 1; i < searAudioGuids.Length; i++)
        {
            audioGuidList.Add(searAudioGuids[i]);
        }
        for (int i = 1; i < searMeshGuids.Length; i++)
        {
            meshGuidList.Add(searMeshGuids[i]);
        }
        for (int i = 1; i < searGuids.Length; i++)
        {
            texture2dGuidList.Add(searGuids[i]);
        }
        for (int i = 1; i < searMatGuids.Length; i++)
        {
            materialGuidList.Add(searMatGuids[i]);
        }
        guidDic = new Dictionary<string, List<string>> { { "音频", audioGuidList }, { "材质", materialGuidList }, { "mesh网格", meshGuidList }, { "纹理", texture2dGuidList } };
        for (int i = 1; i < luaScriptFiles.Length; i++)
        {
            string path = luaScriptFiles[i].FullName;
            if (luaScriptFiles[i].Name.EndsWith(".lua"))
            {
                path = path.Replace(@"\", "/");
                path = path.Substring(path.IndexOf("LuaScripts"));
                string luaFile = File.ReadAllText(path);
                luaScriptDicList.Add(luaFile);
            }
        }

        string[] filePath = fs.ToArray();
        int startIndex = 0;
        EditorApplication.update = delegate ()
        {
            if (isCencel || EditorUtility.DisplayCancelableProgressBar("检索中：" + startIndex + "/" + (filePath.Length - 1), filePath[startIndex], (float)startIndex / (float)filePath.Length) == true)//点击取消按钮直接打断进程并返回已遍历的结果
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                isCencel = false;
                outputResult();
                return;
            }

            Object fileObject = AssetDatabase.LoadAssetAtPath(filePath[startIndex], typeof(Object));
            if (!result.Contains(fileObject))
            {
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
                EditorUtility.DisplayCancelableProgressBar("检索中：" + startIndex + "/" + (filePath.Length - 1), filePath[startIndex], (float)startIndex / (float)filePath.Length);
            }
        };
    }
    private void searchNode(Object father)
    {
        GameObject Father = (GameObject)father;
        if (Father.GetComponentsInChildren<AutoRefImage>(true) != null)
        {
            AutoRefImageWay(Father);
        }
        else if (Father.GetComponentsInChildren<Image>(true) != null)
        {
            ImageWay(Father);
        }
        if (Father.GetComponentsInChildren<ModelMaterials>(true) != null)
        {
            ModelMaterialsWay(Father);
        }
        if (Father.GetComponentsInChildren<SkinnedMeshRenderer>(true) != null)
        {
            SkinnedMeshRendererWay(Father);
        }
        if (Father.GetComponentsInChildren<AudioSource>(true) != null)
        {
            AudioSourceWay(Father);
        }
        if (Father.GetComponentsInChildren<MeshFilter>(true) != null)
        {
            MeshFilterWay(Father);
        }
    }
    //未引用节点
    void instaceFindNode()
    {
        foreach (KeyValuePair<string, List<string>> noCites in noCiteDic)
        {
            bool noQuote = false;
            Vector2 v1 = Vector2.zero;
            noQuote = EditorGUILayout.Foldout(noQuote, "未引用" + noCites.Value.Count);
            if (noQuote)
            {
                EditorGUILayout.BeginScrollView(v1, GUILayout.Width(600), GUILayout.Height(180));
                for (int k = 0; k < noCites.Value.Count; k++)
                {
                    Object obj = EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(noCites.Value[k]), typeof(Object)), typeof(Material), true, GUILayout.Width(300), GUILayout.Height(20));
                    if (GUILayout.Button("删除", GUILayout.Width(100)))
                    {
                        DestroyImmediate(obj, true);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
    //检测AutoRefImage组件中的材质与贴图
    public void AutoRefImageWay(GameObject gameObject)
    {
        AutoRefImage[] refImages = gameObject.GetComponentsInChildren<AutoRefImage>(true);
        foreach (var refImage in refImages)
        {
            if (refImage.material && refImage.material.GetInstanceID() > 0)
            {
                var guid = getGuid(refImage.material.GetInstanceID().ToString());
                if (!materialList.Contains(guid))
                {
                    materialList.Add(refImage.material.GetInstanceID().ToString());
                }
                if (refImage.material.shader && refImage.material.shader.GetInstanceID() > 0)
                {
                    searchTexture2D(refImage.material);
                }
            }
        }
    }
    //检测Image组件中的材质与贴图
    public void ImageWay(GameObject gameObject)
    {
        Image[] Images = gameObject.GetComponentsInChildren<Image>(true);
        foreach (var Image in Images)
        {
            var guid = getGuid(Image.material.GetInstanceID().ToString());
            if (Image.material && Image.material.GetInstanceID() > 0)
            {
                if (!materialList.Contains(guid))
                {
                    materialList.Add(guid);
                }
                if (Image.material.shader && Image.material.shader.GetInstanceID() > 0)
                {
                    searchTexture2D(Image.material);
                }
            }
        }
    }
    //检测AudioSource组件中的音频
    public void AudioSourceWay(GameObject gameObject)
    {
        AudioSource[] audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
        foreach (var audioSource in audioSources)
        {
            if (audioSource.clip != null && audioSource.clip.GetInstanceID() > 0)//  GetMaterials().Count > 0
            {
                var guid = getGuid(audioSource.clip.GetInstanceID().ToString());
                if (!audioList.Contains(guid))
                {
                    audioList.Add(guid);
                }
            }
        }
    }
    //检测MeshFilterWay组件中的mesh
    public void MeshFilterWay(GameObject gameObject)
    {
        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.GetInstanceID() > 0)//  GetMaterials().Count > 0
            {
                var guid = getGuid(meshFilter.sharedMesh.GetInstanceID().ToString());
                if (!meshList.Contains(guid))
                {
                    meshList.Add(guid);
                }
            }
        }
    }
    //检测ModelMaterials组件中的材质、贴图
    public void ModelMaterialsWay(GameObject gameObject)
    {
        ModelMaterials[] modelMaterials = gameObject.GetComponentsInChildren<ModelMaterials>(true);
        foreach (var modelMaterial in modelMaterials)
        {
            for (int i = 0; i < modelMaterial.getMatList().Count; i++)
            {
                var material = modelMaterial.getMatList()[i];
                var guid = getGuid(material.GetInstanceID().ToString());
                if (material && material.GetInstanceID() > 0)
                {
                    if (!materialList.Contains(guid))
                    {
                        materialList.Add(guid);
                    }
                    if (material.shader && material.shader.GetInstanceID() > 0)
                    {
                        searchTexture2D(material);
                    }
                }
            }
        }
    }
    //检测SkinnedMeshRenderer组件中的材质、贴图、mesh
    public void SkinnedMeshRendererWay(GameObject gameObject)
    {
        SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (skinnedMeshRenderer.sharedMaterial != null && skinnedMeshRenderer.sharedMaterials.Length > 0)//  GetMaterials().Count > 0
            {
                foreach (var material in skinnedMeshRenderer.sharedMaterials)
                {
                    if (material && material.GetInstanceID() > 0)
                    {
                        var guid = getGuid(material.GetInstanceID().ToString());
                        if (!materialList.Contains(guid))
                        {
                            materialList.Add(guid);
                        }
                        if (material.shader && material.shader.GetInstanceID() > 0)
                        {
                            searchTexture2D(material);
                        }
                    }
                }
            }
            if (skinnedMeshRenderer.sharedMesh && skinnedMeshRenderer.sharedMesh.GetInstanceID() > 0)
            {
                var guid = getGuid(skinnedMeshRenderer.sharedMesh.GetInstanceID().ToString());
                if (!meshList.Contains(guid))
                {
                    meshList.Add(guid);
                }
            }
        }
    }
    //获取Guid
    string getGuid(string instanceID)
    {
        Object Obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(int.Parse(instanceID)), typeof(Object));
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Obj));
        return guid;
    }
    //输出lua文本检测结果
    void outputLuaResult(Dictionary<string, List<string>> searchAwary)
    {
        int startIndex = 0;
        int searchIndex = 0;
        List<string> tempList = new List<string>();
        foreach (var Key in searchAwary.Keys)
        {
            tempList.Add(Key);
        }
        string searchState = tempList[searchIndex];
        EditorApplication.update = delegate ()
        {
            if (startIndex < tempList.Count)
            {
                if (searchIndex == searchAwary[searchState].Count || searchIndex > searchAwary[searchState].Count)
                {
                    startIndex = 0;
                    isCencel = true;
                }
                if (isCencel || EditorUtility.DisplayCancelableProgressBar("lua脚本检索中：" + searchState + searchIndex + "/" + searchAwary[searchState].Count, searchAwary[searchState][searchIndex], (float)searchIndex / (float)(searchAwary[searchState].Count)) == true)//点击取消按钮直接打断进程并返回已遍历的结果
                {
                    startIndex += 1;
                    if (tempList[startIndex] != null)
                    {
                        searchIndex = 0;
                        searchState = tempList[startIndex];
                    }
                    else if (startIndex >= tempList.Count)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        instaceFindNode();
                        startIndex = 0;
                        searchIndex = 0;
                        return;
                    }
                    isCencel = false;
                }
                EditorUtility.DisplayCancelableProgressBar("lua脚本检索中：" + searchState + searchIndex + "/" + searchAwary[searchState].Count, searchAwary[searchState][searchIndex], (float)searchIndex / (float)(searchAwary[searchState].Count));
                searchTargetText(searchAwary[searchState][searchIndex]);
                searchIndex += 1;
            }
            else
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                searchIndex = 0;
                return;
            }
        };
        instaceFindNode();
    }
    //输出结果
    void outputResult()
    {
        List<string> noReftexture2DList = new List<string>();
        List<string> noRefaudioList = new List<string>();
        List<string> noRefmaterialList = new List<string>();
        List<string> noRefmeshList = new List<string>();
        for (int i = 0; i < result.Count; i++)
        {
            searchNode(result[i]);
        }
        citeDic.Add("音频", audioList);
        citeDic.Add("材质", materialList);
        citeDic.Add("mesh网格", meshList);
        citeDic.Add("纹理", texture2DList);
        noCiteDic.Add("音频", noRefaudioList);
        noCiteDic.Add("材质", noRefmaterialList);
        noCiteDic.Add("mesh网格", noRefmeshList);
        noCiteDic.Add("纹理", noReftexture2DList);
        foreach (KeyValuePair<string, List<string>> guids in guidDic)
        {
            foreach (var guid in guids.Value)
            {
                if (!citeDic[guids.Key].Contains(guid))
                {
                    noCiteDic[guids.Key].Add(guid);
                }
            }
        }
    }
    //检查lua引用文本
    void searchTargetText(string guid)
    {
        Object obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
        foreach (var text in luaScriptDicList)
        {
            if (text.Contains(obj.name))
            {
                foreach (KeyValuePair<string, List<string>> noCites in noCiteDic)
                {
                    if (noCites.Value.Count > 0 && noCites.Value.Contains(guid))
                    {
                        noCites.Value.Remove(guid);
                    }
                }
            }
        }
    }
    void refTxt(List<string> list, string title)
    {
        string path = Application.dataPath + "/美术检测报告.txt";
        StreamWriter sw;
        FileInfo fi = new FileInfo(path);

        if (!File.Exists(path))
        {
            sw = fi.CreateText();
        }
        else
        {
            sw = fi.AppendText();   //在原文件后面追加内容      
        }
        foreach (var guid in list)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
            sw.WriteLine(obj.name);
        }
        sw.Close();
        sw.Dispose();
    }
    //Excel输出
    void refExcel2(List<string> list, string title, int colorIndex, int addrow)
    {
        if (list.Count > 0)
        {
            string path = Application.dataPath + "/" + title + "检测报告.xlsx";
            HSSFWorkbook Excel = new HSSFWorkbook();
            ISheet sheet = Excel.CreateSheet("mysheet");
            ICellStyle style = Excel.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            if (colorIndex > 1)
            {
                style.FillForegroundColor = HSSFColor.Red.Index;
            }
            style.FillPattern = FillPattern.SolidForeground;
            for (var i = 0; i < list.Count; i++)
            {
                Object obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(list[i]), typeof(Object));
                row = sheet.CreateRow(i);
                row.CreateCell(2).SetCellValue(obj.name);
                cell = row.CreateCell(5);
                cell.SetCellValue(AssetDatabase.GUIDToAssetPath(list[i]));
                row.CreateCell(0).SetCellValue(title);
            }
            file = File.Create(path);
            Excel.Write(file);
            file.Close();
            file.Dispose();
        }
    }
    //材质球检测贴图
    public void searchTexture2D(Material Material)
    {
        Shader shader = Material.shader;
        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = Material.GetTexture(propertyName);
                if (tex && tex.GetInstanceID() > 0)
                {
                    Object Object = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(tex.GetInstanceID()), typeof(Object));
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Object));
                    if (!texture2DList.Contains(guid))
                    {
                        texture2DList.Add(guid);
                    }
                }
            }
        }
    }
    //资源判断优化
    void test()
    {

        // AssetDatabase.GenerateUniqueAssetPath()
    }
    //创建文件夹
    void createFolder()
    {
        //成功
        // string path = Application.dataPath + "/";
        //Directory.CreateDirectory(path+"测试");

        StreamWriter sw;//流信息
        FileInfo t = new FileInfo("" + "//" + name);
        if (!t.Exists)
        {//判断文件是否存在
            sw = t.CreateText();//不存在，创建
        }

        // AssetDatabase.GenerateUniqueAssetPath()
    }
}
