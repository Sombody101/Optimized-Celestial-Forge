using GameEngine.Core;
using GameEngine.Util.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 * Congratulations original creator!
 * You can write in raw obfuscation!
 */

namespace GameEngine.Util.Resources;

public class PackagedScene : Resource
{
    public string Path { get; private set; }

    private PackagedNode root;
    private readonly PackagedResource[] resources;

    private PackagedScene(string path, PackagedNode root, PackagedResource[] resources)
    {
        Path = path;
        this.root = root;
        this.resources = resources;
    }

    public static PackagedScene? Load(string path)
    {
        var data = FileService.GetFile(path);

        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new PackagedSceneFileConverter()
            }
        };

        return JsonConvert.DeserializeObject<PackagedScene>(data, settings);
    }

    public Node Instantiate()
    {
        List<Resource> resRepository = new();
        foreach (var i in resources)
            resRepository.Add(i.CreateResourceInstance());

        return root.CreateNodeInstance(resRepository.ToArray());
    }

    struct PackagedNode
    {
        public Type NodeType { get; set; } = typeof(Node);
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object?> data = new();
        public PackagedNode[] Children { get; set; } = Array.Empty<PackagedNode>();

        public PackagedNode() { }

        public readonly Node CreateNodeInstance(Resource[] resRepo)
        {
            var newNode = (Node)Activator.CreateInstance(NodeType)!;
            newNode.name = Name;

            foreach (var i in data)
            {
                var field = NodeType.GetField(i.Key);
                if (field != null)
                {
                    if (field.FieldType.IsAssignableTo(typeof(Node)))
                        newNode.AddToOnReady(i.Key, i.Value);
                    else if (field.FieldType.IsAssignableTo(typeof(Resource)))
                        field.SetValue(newNode, resRepo[int.Parse(i.Value!.ToString()!)]);
                    else
                        field.SetValue(newNode, Convert.ChangeType(i.Value, field.FieldType));
                    continue;
                }

                var prop = NodeType.GetProperty(i.Key);
                if (prop != null)
                {
                    if (prop.PropertyType.IsAssignableTo(typeof(Node)))
                        newNode.AddToOnReady(i.Key, i.Value);
                    else if (prop.PropertyType.IsAssignableTo(typeof(Resource)))
                        prop.SetValue(newNode, resRepo[int.Parse(i.Value!.ToString()!)]);
                    else
                        prop.SetValue(newNode, Convert.ChangeType(i.Value, prop.PropertyType));

                    continue;
                }

                throw new ApplicationException(string.Format("Field {0} don't exist in type {1}!", i.Key, NodeType.Name));
            }

            foreach (var i in Children)
                newNode.AddAsChild(i.CreateNodeInstance(resRepo));

            return newNode;

        }
    }
    struct PackagedResource
    {
        public Type ResType { get; set; } = typeof(Resource);
        public Dictionary<string, object?> data = new();

        public PackagedResource() { }

        public readonly Resource CreateResourceInstance()
        {
            var newRes = (Resource)Activator.CreateInstance(ResType)!;

            foreach (var i in data)
            {
                var field = ResType.GetField(i.Key);
                if (field != null)
                {
                    field.SetValue(newRes, Convert.ChangeType(i.Value, field.FieldType));
                    continue;
                }

                var prop = ResType.GetProperty(i.Key);
                if (prop != null)
                {
                    prop.SetValue(newRes, Convert.ChangeType(i.Value, prop.PropertyType));
                    continue;
                }

                throw new ApplicationException(string.Format("Field {0} don't exist in type {1}!", i.Key, ResType.Name));
            }

            return newRes;
        }

    }

    class PackagedSceneFileConverter : JsonConverter<PackagedScene>
    {
        public override PackagedScene? ReadJson(JsonReader reader, Type objectType, PackagedScene? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);

            PackagedNode root = new();
            List<PackagedResource> resources = new();

            // Load Tree
            if (json["NodeTree"] is JObject jObj)
            {
                var treeResult = LoadPackagedNodeFromJson(jObj);
                if (treeResult != null)
                    root = (PackagedNode)treeResult;
            }
            else
                throw new ApplicationException("PackagedScene File is corrupted!");

            // Load Resources
            if (json["Resources"] is JArray jArr)
            {
                foreach (var i in jArr)
                    if (i is JObject obj)
                    {
                        var res = LoadPackagedResourceFromJson(obj);
                        if (res != null)
                            resources.Add((PackagedResource)res);
                    }
            }

            return new PackagedScene(string.Empty, root, resources.ToArray());
        }

        public override void WriteJson(JsonWriter writer, PackagedScene? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private static PackagedNode? LoadPackagedNodeFromJson(JObject data)
        {
            Type? nodeType = Type.GetType("GameEngine.Util.Nodes." + data.GetValue("NodeType")!.Value<string>());

            if (nodeType != null)
            {
                PackagedNode node = new()
                {
                    NodeType = nodeType,
                    Name = data.GetValue("Name")!.ToString()
                };

                // LOAD DATA
                string[] ignore = new string[] { "NodeType", "Name", "Children" };
                foreach (var kvp in data)
                {
                    if (ignore.Contains(kvp.Key)) continue;

                    var field = nodeType.GetField(kvp.Key);
                    var prop = nodeType.GetProperty(kvp.Key);

                    // Check if data field exists
                    if (field == null && prop == null)
                        throw new ApplicationException(string.Format("Field {0} don't exist in base {1}!", kvp.Key, nodeType.Name));

                    if (kvp.Value is JArray) // Unpack custom values in arrays
                    {
                        var obj = (field != null)
                            ? Activator.CreateInstance(field.FieldType, kvp.Value.Values<float>().ToArray())
                            : Activator.CreateInstance(prop?.PropertyType!, kvp.Value.Values<float>().ToArray());

                        if (obj != null) 
                            node.data.Add(kvp.Key, obj);
                    }
                    else
                    {
                        if ((field?.FieldType.IsAssignableTo(typeof(Node)) ?? false) ||
                            (prop?.PropertyType.IsAssignableTo(typeof(Node)) ?? false))
                        {
                            node.data.Add(kvp.Key, kvp.Value?.ToString());
                            continue;
                        }

                        node.data.Add(kvp.Key, kvp.Value);
                    }
                }

                // LOAD CHILDREN
                var children = data.GetValue("Children");
                List<PackagedNode> childrenList = new();

                if (children is JArray)
                    foreach (var jToken in children)
                    {
                        var c = LoadPackagedNodeFromJson((JObject)jToken);
                        if (c != null) 
                            childrenList.Add((PackagedNode)c);
                    }

                node.Children = childrenList.ToArray();

                return node;
            }

            return null; // Error
        }
        private PackagedResource? LoadPackagedResourceFromJson(JObject data)
        {
            Type? resType = Type.GetType("GameEngine.Util.Resources." + data.GetValue("ResourceType")!.Value<string>());

            if (resType != null)
            {
                PackagedResource res = new()
                {
                    ResType = resType
                };

                // LOAD DATA
                string[] ignore = new string[] { "ResourceType" };
                foreach (var kvp in data)
                {
                    if (ignore.Contains(kvp.Key)) 
                        continue;

                    var field = resType.GetField(kvp.Key);
                    var prop = resType.GetProperty(kvp.Key);

                    // Check if data field exists
                    if (field == null && prop == null)
                        throw new ApplicationException(string.Format("Field {0} don't exist in base {1}!", kvp.Key, resType.Name));

                    if (kvp.Value is JArray) // Unpack custom values in arrays
                    {
                        var obj = Activator.CreateInstance(
                            field != null 
                                ? field.FieldType 
                                : prop!.PropertyType,
                            
                            kvp.Value.Values<float>().ToArray()
                        );

                        if (obj != null) 
                            res.data.Add(kvp.Key, obj);
                    }
                    else
                        res.data.Add(kvp.Key, kvp.Value);
                }

                return res;
            }

            return null; // Error
        }
    }
}