using GameEngine.Util.Values;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameEngine.Core;

public static class WindowService
{
    public static IWindow? mainWindow { get; set; } = null;
    public static List<IWindow> windows { get; set; } = new();

    private static List<IWindow> _windowsToClose = new();

    public static IWindow CreateNewWindow(Action? onload = null)
        => CreateNewWindow(new Vector2<int>(800, 600), "New Window " + windows.Count, onload);

    public static IWindow CreateNewWindow(Vector2<int> size, string title, Action? onload=null)
    {
        WindowOptions options = WindowOptions.Default with
        {
            Title = title,
            Size = size.GetAsSilkInt(),
            WindowState = WindowState.Normal,
            ShouldSwapAutomatically = false,
            Samples = 4,
            VSync = false
        };

        if (mainWindow != null)
            options.SharedContext = mainWindow.GLContext;
        else
            options.VSync = true;

        var nWin = Window.Create(options);

        windows.Add(nWin);

        if (onload != null)
            nWin.Load += onload;

        nWin.Initialize();

        if (mainWindow == null)
        {
            mainWindow = nWin;
            Engine.gl = nWin.CreateOpenGL();
        }

        nWin.ConfigWindow();

        return nWin;
    }

    public static void CloseWindow(IWindow win)
    {
        _windowsToClose.Add(win);
        windows.Remove(win);
        if (win == mainWindow) 
            mainWindow = null;
    }

    private static void ConfigWindow(this IWindow win)
    {
        var gl = Engine.gl;

        // GL configurations //
        gl.ClearColor(1f, 1f, 1f, 1f);
        gl.Enable(EnableCap.Multisample);
        gl.Enable(EnableCap.ScissorTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    public static void CallProcess()
    {
        foreach (var win in _windowsToClose)
            win.Dispose();
    }
}