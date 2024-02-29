using GameEngine.Util.Values;

namespace GameEngine.Util.Core;

public class ProjectSettings
{
    public Vector2<int> DefaultCanvasSize { get; set; } = new(800, 600);
    public bool ProjectLoaded { get; set; } = false;
    public string ProjectPath { get; set; } = string.Empty;
    public string EntryScene { get; set; } = string.Empty;
}