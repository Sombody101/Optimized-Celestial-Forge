using GameEngine.Util;
using GameEngine.Util.Resources;
using Silk.NET.OpenGL;

namespace GameEngine.Core;

public static class ResourceHeap
{
    private static List<uint> texturesToDelete = new();

    public enum DeleteTarget { Texture }
    public static void Delete(uint id, DeleteTarget target)
    {
        switch (target)
        {
            case DeleteTarget.Texture:
                texturesToDelete.Add(id); break;

            default: return;
        }
    }

    private static List<GlShaderProgram> _GlShaderPrograms = new();

    public static GlShaderProgram? GetShaderProgramReference(FileReference vs, FileReference fs, FileReference? gs)
    {
        foreach (var i in _GlShaderPrograms)
            if (i.vertexShader == vs && i.fragmentShader == fs && i.geometryShader == gs)
                return i;

        return null;
    }

    public static void AddShaderProgramReference(GlShaderProgram program)
        => _GlShaderPrograms.Add(program);

    public static void RemoveShaderProgramReference(GlShaderProgram program)
    {
        _GlShaderPrograms.Remove(program);
        program.Dispose();
    }

    public static void Collect()
    {
        var gl = Engine.gl;

        if (texturesToDelete.Count > 0)
        {
            gl.DeleteTextures((uint)texturesToDelete.Count, texturesToDelete.ToArray());
            texturesToDelete.Clear();
        }
    }

    public static void CallProcess()
        => Collect();
}
