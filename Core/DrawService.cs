using GameEngine.Util;
using GameEngine.Util.Resources;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace GameEngine.Core;

public static class DrawService
{
    private static readonly Dictionary<uint, ResourceDrawData> ResourceData = new();

    public enum BufferUsage { Static, Dynamic, Stream };

    #region Gl Binded data
    public static int GlBinded_ShaderProgram = -1;
    #endregion

    public static void CreateCanvasItem(uint NID)
        => ResourceData.Add(NID, new ResourceDrawData());

    public static void DeleteCanvasItem(uint NID)
        => ResourceData.Remove(NID);

    public static uint CreateBuffer(uint NID, string bufferName)
        => ResourceData[NID].CreateBuffer(bufferName);

    #region SetBufferData Methods

    public static unsafe void SetBufferData<T>(uint NID, string buffer, T[] data, int size, BufferUsage usage = BufferUsage.Static) where T : unmanaged
    {
        var gl = Engine.gl;

        VertexData vertexData = ResourceData[NID].VertexBuffers[buffer];

        gl.BindVertexArray(ResourceData[NID].VertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexData.bufferId);

        BufferUsageARB currentUsage =
            usage == BufferUsage.Static 
                ? BufferUsageARB.StaticDraw 
                : usage == BufferUsage.Dynamic 
                    ? BufferUsageARB.DynamicDraw 
                    : BufferUsageARB.StreamDraw;

        fixed (T* buf = data)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * sizeof(T)), buf, currentUsage);

        vertexData.size = size;
        vertexData.type = typeof(T);
        ResourceData[NID].VertexBuffers[buffer] = vertexData;
    }

    public static unsafe void SetBufferData<T>(uint NID, uint id, T[] data, int size, BufferUsage usage = BufferUsage.Static) where T : unmanaged
    {
        var gl = Engine.gl;

        var a = ResourceData[NID].VertexBuffers.ToArray()[id];
        VertexData vertexData = a.Value;

        gl.BindVertexArray(ResourceData[NID].VertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexData.bufferId);

        BufferUsageARB currentUsage =
            usage == BufferUsage.Static
                ? BufferUsageARB.StaticDraw
                : usage == BufferUsage.Dynamic
                    ? BufferUsageARB.DynamicDraw
                    : BufferUsageARB.StreamDraw;

        fixed (T* buf = data)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * sizeof(T)), buf, currentUsage);

        vertexData.size = size;
        vertexData.type = typeof(T);
        ResourceData[NID].VertexBuffers[a.Key] = vertexData;
    }

    #endregion

    #region Operations with instances
    public static void SetBufferAtribDivisor(uint NID, string buffer, uint divisor)
    {
        var gl = Engine.gl;

        VertexData vertexData = ResourceData[NID].VertexBuffers[buffer];
        vertexData.divisions = divisor;
        ResourceData[NID].VertexBuffers[buffer] = vertexData;
    }
    public static void SetBufferAtribDivisor(uint NID, uint id, uint divisor)
    {
        var gl = Engine.gl;

        var a = ResourceData[NID].VertexBuffers.ToArray()[id];
        VertexData vertexData = a.Value;
        vertexData.divisions = divisor;
        ResourceData[NID].VertexBuffers[a.Key] = vertexData;
    }

    public static void EnableInstancing(uint NID, uint instanceCount)
    {
        var gl = Engine.gl;

        var res = ResourceData[NID];

        if (instanceCount > 0)
        {
            res.useInstancing = true;
            res.instanceCount = instanceCount;
        }
        else res.useInstancing = false;

        ResourceData[NID] = res;
    }
    #endregion

    public static unsafe void SetElementBufferData(uint NID, uint[] data, BufferUsage usage = BufferUsage.Static)
    {
        var gl = Engine.gl;

        uint bufferId = ResourceData[NID].ElementBuffer;

        gl.BindVertexArray(ResourceData[NID].VertexArray);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, bufferId);

        BufferUsageARB currentUsage =
            usage == BufferUsage.Static 
                ? BufferUsageARB.StaticDraw 
                : usage == BufferUsage.Dynamic 
                    ? BufferUsageARB.DynamicDraw 
                    : BufferUsageARB.StreamDraw;

        fixed (uint* buf = data)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(data.Length * sizeof(uint)), buf, currentUsage);

        var a = ResourceData[NID];
        a.elementsLength = (uint)data.Length;
        ResourceData[NID] = a;
    }

    public static unsafe void EnableAttributes(uint NID, Material material)
    {
        var gl = Engine.gl;
        var res = ResourceData[NID];

        foreach (var i in res.VertexBuffers)
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, i.Value.bufferId);
            int iloc = material.GetALocation(i.Key);
            if (iloc < 0) continue;

            uint loc = (uint)iloc;
            var ts = Marshal.SizeOf(i.Value.type);

            #region get correct type
            VertexAttribPointerType type = VertexAttribPointerType.Float;
            if (i.Value.type == typeof(int))
                type = VertexAttribPointerType.Int;
            else if (i.Value.type == typeof(byte))
                type = VertexAttribPointerType.Byte;
            else if (i.Value.type == typeof(double))
                type = VertexAttribPointerType.Double;
            #endregion

            if (i.Value.size < 16)
            {
                gl.EnableVertexAttribArray(loc);
                gl.VertexAttribPointer(loc, i.Value.size, type, false, (uint)(i.Value.size * ts), (void*)0);
                gl.VertexAttribDivisor(loc, i.Value.divisions);
            }
            else
                for (uint j = 0; j < 4; j++)
                {
                    var nloc = loc + j;
                    gl.EnableVertexAttribArray(nloc);
                    gl.VertexAttribPointer(nloc, 4, type, false, (uint)(16 * ts), (void*)(j * 4 * ts));
                    gl.VertexAttribDivisor(nloc, i.Value.divisions);
                }
        }
    }

    public static unsafe void Draw(uint NID)
    {
        var res = ResourceData[NID];
        Engine.gl.BindVertexArray(res.VertexArray);

        if (!res.useInstancing)
            Engine.gl.DrawElements(PrimitiveType.Triangles, res.elementsLength, DrawElementsType.UnsignedInt, (void*)0);
        else
            Engine.gl.DrawElementsInstanced(PrimitiveType.Triangles, res.elementsLength, DrawElementsType.UnsignedInt, (void*)0, res.instanceCount);
    }
}

struct ResourceDrawData
{
    // opengl info
    public uint VertexArray = 0;
    public Dictionary<string, VertexData> VertexBuffers = new();
    public uint ElementBuffer = 0;
    public uint elementsLength = 0;
    public uint instanceCount = 0;

    // configs
    public bool useInstancing = false;

    public ResourceDrawData()
    {
        VertexArray = Engine.gl.GenVertexArray();
        ElementBuffer = Engine.gl.GenBuffer();
    }

    public uint CreateBuffer(string bufferName)
    {
        if (VertexBuffers.ContainsKey(bufferName))
            throw new ApplicationException(string.Format("Buffer {0} already exists inside this resource!", bufferName));

        var vertData = new VertexData();
        var id = Engine.gl.GenBuffer();
        vertData.bufferId = id;
        VertexBuffers.Add(bufferName, vertData);

        return (uint)(VertexBuffers.Count - 1);
    }
}

struct VertexData
{
    public uint bufferId = 0;
    public int size = 0;
    public Type type = typeof(float);
    public uint divisions = 0;

    public VertexData() { }
}