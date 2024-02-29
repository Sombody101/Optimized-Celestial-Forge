using GameEngine.Core;
using GameEngine.Util.Core;
using GameEngine.Util.Nodes;
using GameEngine.Util.Resources;
using GameEngine.Util.Values;
using Silk.NET.Windowing;
using Window = GameEngine.Util.Nodes.Window;

namespace GameEngine.Editor;

public class EditorMain
{
    private readonly ProjectSettings projectSettings;
    private readonly Window mainWindow;

    /* IMPORTANT NODES */
    private Node? editorRoot;
    private TreeGraph? filesList;
    private TreeGraph? nodesList;

    public EditorMain(ProjectSettings settings, Window mainWin)
    {
        projectSettings = settings;
        mainWindow = mainWin;

        CreateEditor();
    }

    private void CreateEditor()
    {
        /* CONFIGURATE WINDOW */
        mainWindow.State = WindowState.Maximized;
        mainWindow.Title = "Game Engine";

        /* INSTANTIATE EDITOR */
        var scene = PackagedScene.Load("Data/Screens/editor.json")!.Instantiate();
        mainWindow.AddAsChild(scene);
        editorRoot = scene;

        /* INSTANTIATE AND CONFIGURATE FILE MANANGER */
        var filesSection = scene.GetChild("Main/LeftPannel/FileMananger");

        filesList = new TreeGraph() { ClipChildren = true };
        filesSection!.AddAsChild(filesList);

        var svgtexture = new SvgTexture() { Filter = false };

        var txtFile = svgtexture;
        var cFolder = svgtexture;
        var eFolder = svgtexture;
        var unkFile = svgtexture;
        var anvilWk = svgtexture;
        var sceFile = svgtexture;
        
        txtFile.LoadFromFile("Assets/Icons/Files/textFile.svg", 20, 20);
        cFolder.LoadFromFile("Assets/Icons/Files/closedFolder.svg", 20, 20);
        eFolder.LoadFromFile("Assets/Icons/Files/emptyFolder.svg", 20, 20);
        unkFile.LoadFromFile("Assets/Icons/Files/unknowFile.svg", 20, 20);
        anvilWk.LoadFromFile("Assets/Icons/Files/AnvilKey.svg", 20, 20);
        sceFile.LoadFromFile("Assets/Icons/Files/scene.svg", 20, 20);

        filesList.Root.Icon = cFolder;
        filesList.Root.Name = "res://";

        List<FileSystemInfo> items = new();
        items.AddRange(FileService.GetDirectory("res://"));

        items.Sort((a, b) =>
        {
            if (a.Extension == string.Empty && b.Extension != string.Empty)
                return -1;
            else if (a.Extension != string.Empty && b.Extension == string.Empty)
                return 1;
            else return 0;
        });

        while (items.Count > 0)
        {
            var i = items[0];
            items.RemoveAt(0);
            var type = i.Extension != string.Empty
                ? i.Extension
                : "folder";

            SvgTexture iconImage = unkFile;

            switch (i.Extension)
            {
                case "":
                    var filesInDir = FileService.GetDirectory(i.FullName);
                    iconImage = filesInDir.Length == 0
                        ? eFolder
                        : cFolder;

                    items.AddRange(filesInDir);
                    items.Sort((a, b) =>
                    {
                        if (a.Extension == string.Empty && b.Extension != string.Empty)
                            return -1;
                        else if (a.Extension != string.Empty && b.Extension == string.Empty)
                            return 1;
                        else
                            return 0;
                    });

                    type = "folder";
                    break;

                case ".txt":
                    iconImage = txtFile;
                    break;

                case ".sce":
                    iconImage = sceFile;
                    break;

                case ".forgec":
                    iconImage = anvilWk;
                    break;
            }

            var path = FileService.GetProjRelativePath(i.FullName);
            path = path[6..][..^i.Name.Length];

            var item = filesList.AddItem(path, i.Name, iconImage);
            item!.Collapsed = type == "folder";
            item!.data.Add("type", type);
            item!.OnClick.Connect(OnFileClicked);
        }

        /* INSTANTIATE AND CONFIGURATE NODE MANANGER */
        var nodesSection = scene.GetChild("Main/RightPannel/NodeMananger");
        nodesList = new TreeGraph()
        {
            ClipChildren = true
        };

        nodesSection!.AddAsChild(nodesList);

        /* CONFIGURATE BUTTONS */
        var runButton = scene.GetChild("TopBar/RunButton") as Button;
        runButton?.OnPressed.Connect(RunButtonPressed);

        //LoadSceneInEditor("res://testScene.sce");
    }

    private void RunButtonPressed(object? from, dynamic[]? args)
        => RunGame();

    private void RunGame()
    {
        var gameWindow = new Window()
        {
            Size = (Vector2<uint>)projectSettings.DefaultCanvasSize
        };

        mainWindow.AddAsChild(gameWindow);

        var gameScene = PackagedScene.Load(projectSettings.EntryScene)!.Instantiate();
        gameWindow.AddAsChild(gameScene);
    }


    private void OnFileClicked(object? from, dynamic[]? args)
    {
        var item = from as TreeGraph.TreeGraphItem;

        var path = "res://" + item!.Path[7..];

        if (item!.data["type"] == "folder")
            item.Collapsed = !item.Collapsed;
        else if (item!.data["type"] == ".sce")
            LoadSceneInEditor(path);
    }

    private void LoadSceneInEditor(string scenePath)
    {
        var viewport = editorRoot!.GetChild("Main/Center/Viewport/ViewportContainer") as NodeUI;

        viewport!.sizePixels = projectSettings.DefaultCanvasSize;

        nodesList!.ClearGraph();
        viewport!.children.Clear();

        var scene = PackagedScene.Load(scenePath)!.Instantiate();
        viewport!.AddAsChild(scene);


        /* LOAD NODES LIST */
        List<KeyValuePair<string, Node>> ToList = new();
        foreach (var i in scene.children) ToList.Add(new(string.Empty, i));

        Dictionary<string, Texture> IconsBuffer = new();

        while (ToList.Count != 0)
        {
            var keyValue = ToList.Unqueue();
            var path = keyValue.Key;
            var node = keyValue.Value;

            Texture nodeIcon;
            if (IconsBuffer.TryGetValue(node.GetType().Name, out Texture? icon))
                nodeIcon = icon;
            else
            {
                var nTexture = new SvgTexture() { Filter = false };
                nTexture.LoadFromFile($"Assets/icons/Nodes/{node.GetType().Name}.svg", 20, 20);
                IconsBuffer.Add(node.GetType().Name, nTexture);
                nodeIcon = nTexture;
            }

            /* ?? */
            var item = nodesList!.AddItem(path, node.name, nodeIcon);

            for (int i = node.children.Count - 1; i >= 0; i--)
                ToList.Insert(0, new($"{path}/{node.name}", node.children[i]));
        }

        Texture rootIcon;
        if (IconsBuffer.TryGetValue(scene.GetType().Name, out Texture? texture))
            rootIcon = texture;
        else
        {
            var nTexture = new SvgTexture() { Filter = false };
            nTexture.LoadFromFile($"Assets/icons/Nodes/{scene.GetType().Name}.svg", 20, 20);
            IconsBuffer.Add(scene.GetType().Name, nTexture);
            rootIcon = nTexture;
        }

        nodesList!.Root.Name = scene.name;
        nodesList!.Root.Icon = rootIcon;
    }
}
