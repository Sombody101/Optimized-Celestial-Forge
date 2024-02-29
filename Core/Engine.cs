using GameEngine.Editor;
using GameEngine.Util.Core;
using GameEngine.Util.Nodes;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace GameEngine.Core;

public static class Engine
{
#pragma warning disable CS8618
    public static IWindow window { get; set; }
    public static GL gl { get; set; }
#pragma warning restore

    public static ProjectSettings projectSettings { get; set; } = new();
    public static NodeRoot root { get; set; } = new();
    public static int gl_MaxTextureUnits { get; private set; }

    public static void StartEngine()
    {
        /* CREATE MAIN WINDOW AND GL CONTEXT */
        var mainWin = new Util.Nodes.Window();
        window = mainWin.window;
        root.AddAsChild(mainWin);

        // get GL info //
        gl_MaxTextureUnits = gl.GetInteger(GLEnum.MaxTextureImageUnits);

        /* configurate project settings */
        projectSettings.ProjectLoaded = true;
        projectSettings.ProjectPath = @"put/here/the/project/folder/path";
        projectSettings.EntryScene = @"res://testScene.sce";

        projectSettings.DefaultCanvasSize = new(400, 300);

        /* START EDITOR */
        _ = new EditorMain(projectSettings, mainWin);

        /* START RUN */
        Run();

        /* END PROGRAM */
        root.Free();
        gl.Dispose();
    }

    private static void Run()
    {
        /* GAME LOOP PROCESS */

        Stopwatch frameTime = new();
        Stopwatch stopwatch = new();
        List<float> fpsHistory = new();

        frameTime.Start();
        stopwatch.Start();
        while (WindowService.mainWindow != null && !WindowService.mainWindow.IsClosing)
        {
            foreach (var win in WindowService.windows.ToArray())
                if (win.IsInitialized)
                {
                    DrawService.GlBinded_ShaderProgram = -1;
                    win.DoEvents();
                    win.DoUpdate();
                    win.DoRender();

                    if (win != WindowService.mainWindow)
                        win.SwapBuffers();
                }

            if (WindowService.mainWindow != null && !WindowService.mainWindow.IsClosing)
                WindowService.mainWindow.SwapBuffers();

            WindowService.CallProcess();
            ResourceHeap.CallProcess();

            /* FPS COUNTER */
            float elapsedSeconds = (float)frameTime.Elapsed.TotalSeconds;
            float fps = (float)(1.0 / elapsedSeconds);

            fpsHistory.Add(fps);
            frameTime.Restart();

            if (stopwatch.Elapsed.TotalSeconds > 2)
            {
                stopwatch.Restart();
                Console.Title = "fps: " + Math.Round(fpsHistory.Average());
                fpsHistory.Clear();
            }
        }
    }
}
