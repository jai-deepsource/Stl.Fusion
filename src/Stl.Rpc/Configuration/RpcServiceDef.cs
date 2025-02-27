using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public sealed class RpcServiceDef
{
    private readonly Dictionary<MethodInfo, RpcMethodDef> _methods;
    private readonly Dictionary<Symbol, RpcMethodDef> _methodByName;
    private object? _server;

    public RpcHub Hub { get; }
    public Type Type { get; }
    public CustomResolver? ServerResolver { get; }
    public Symbol Name { get; }
    public bool IsSystem { get; }
    public int MethodCount => _methods.Count;
    public IEnumerable<RpcMethodDef> Methods => _methods.Values;
    public bool HasServer => ServerResolver != null;
    public object Server => _server ??= ServerResolver.Resolve(Hub.Services);

    public RpcMethodDef this[MethodInfo method] => Get(method) ?? throw Errors.NoMethod(Type, method);
    public RpcMethodDef this[Symbol methodName] => Get(methodName) ?? throw Errors.NoMethod(Type, methodName);

    public RpcServiceDef(RpcHub hub, Symbol name, RpcServiceBuilder source, Func<RpcMethodDef, Symbol> methodNameBuilder)
    {
        Hub = hub;
        Name = name;
        Type = source.Type;
        ServerResolver = source.ServerResolver;
        IsSystem = typeof(IRpcSystemService).IsAssignableFrom(Type);

        _methods = new Dictionary<MethodInfo, RpcMethodDef>();
        _methodByName = new Dictionary<Symbol, RpcMethodDef>();
        foreach (var method in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
            if (method.IsGenericMethodDefinition)
                continue;

            var methodDef = new RpcMethodDef(this, method, methodNameBuilder);
            if (!methodDef.IsValid)
                continue;

            if (_methodByName.ContainsKey(methodDef.Name))
                throw Errors.MethodNameConflict(methodDef);

            _methods.Add(method, methodDef);
            _methodByName.Add(methodDef.Name, methodDef);
        }
    }

    public override string ToString()
    {
        var serverInfo = HasServer  ? "" : $", Serving: {ServerResolver}";
        return $"{GetType().Name}({Type.GetName()}, Name: '{Name}', {MethodCount} method(s){serverInfo})";
    }

    public RpcMethodDef? Get(MethodInfo method) => _methods.GetValueOrDefault(method);
    public RpcMethodDef? Get(Symbol methodName) => _methodByName.GetValueOrDefault(methodName);
}
