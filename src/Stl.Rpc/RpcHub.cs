using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private ILogger? _log;
    private RpcServiceRegistry? _serviceRegistry;
    private RpcPeerFactory? _peerFactory;
    private RpcInboundContextFactory? _inboundContextFactory;
    private RpcClientChannelProvider? _clientChannelProvider;
    private RpcPeerResolver? _peerResolver;
    private RpcSystemCallSender? _systemCallSender;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcPeerFactory PeerFactory => _peerFactory ??= Services.GetRequiredService<RpcPeerFactory>();
    internal RpcPeerResolver PeerResolver => _peerResolver ??= Services.GetRequiredService<RpcPeerResolver>();
    internal RpcInboundContextFactory InboundContextFactory => _inboundContextFactory ??= Services.GetRequiredService<RpcInboundContextFactory>();
    internal RpcClientChannelProvider ClientChannelProvider => _clientChannelProvider ??= Services.GetRequiredService<RpcClientChannelProvider>();
    internal RpcSystemCallSender SystemCallSender => _systemCallSender ??= Services.GetRequiredService<RpcSystemCallSender>();

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();
    public RpcHubInternals Internals => new(this);

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        Configuration.Freeze();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, peer) in Peers)
            disposeTasks.Add(peer.DisposeAsync().AsTask());
        return Task.WhenAll(disposeTasks);
    }

    public TClient CreateClient<TService, TClient>()
        where TClient : TService
        => (TClient)CreateClient(typeof(TService), typeof(TClient));

    public TService CreateClient<TService>(Type? clientType = null)
        => (TService)CreateClient(typeof(TService), clientType);

    public object CreateClient(Type serviceType, Type? clientType = null)
    {
        if (clientType == null)
            clientType = serviceType;
        else if (!serviceType.IsAssignableFrom(clientType))
            throw Errors.MustBeAssignableTo(clientType, serviceType, nameof(clientType));

        var interceptor = Services.GetRequiredService<RpcClientInterceptor>();
        interceptor.Setup(ServiceRegistry[serviceType]);
        var proxy = Proxies.New(clientType, interceptor);
        return proxy;
    }

    public RpcPeer GetPeer(Symbol name)
        => Peers.GetOrAdd(name, CreatePeer);

    // Private methods

    private RpcPeer CreatePeer(Symbol name)
    {
        if (WhenDisposed != null)
            throw Errors.AlreadyDisposed();

        var peer = PeerFactory.Invoke(name);
        _ = peer.Run().ContinueWith(
            _ => Peers.TryRemove(name, peer),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return peer;
    }
}
