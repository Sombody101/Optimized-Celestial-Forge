namespace GameEngine.Core;

public static class FileService
{
    public static string GetFile(string path)
    {
        var gPath = GetGlobalPath(path);
        try
        {
            return File.ReadAllText(gPath);
        }
        catch (Exception e)
        {
            throw new ApplicationException("File can't be loaded!", e);
        }
    }

    public static FileSystemInfo[] GetDirectory(string path)
    {
        var gPath = GetGlobalPath(path);
        DirectoryInfo info = new(gPath);
        
        return info.GetFileSystemInfos();
    }

    public static string GetProjRelativePath(string path)
    {
        string p = path.Replace("\\", "/");

        if (p.StartsWith(Engine.projectSettings.ProjectPath))
            return string.Concat("res://", p.AsSpan(Engine.projectSettings.ProjectPath.Length));

        return p;
    }

    public static string GetGlobalPath(string path)
    {
        string p = path.Replace("\\", "/");

        if (p.StartsWith("res://"))
            p = Engine.projectSettings.ProjectPath + p[6..];
        else if (p.ToLower().StartsWith("c:/"))
            return p;
        else
            p = "../../../" + p;

        return p;
    }
}
