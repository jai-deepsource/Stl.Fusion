using System.Net.WebSockets;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Internal;

public static class Errors
{
    public static Exception TypeMustBeOpenGenericType(Type type)
        => new InvalidOperationException($"'{type}' must be open generic type.");
    public static Exception MustHaveASingleGenericArgument(Type type)
        => new InvalidOperationException($"'{type}' must have a single generic argument.");

    public static Exception WrongComputedState(
        ConsistencyState expectedState, ConsistencyState state)
        => new InvalidOperationException(
            $"Wrong Computed.State: expected {expectedState}, was {state}.");
    public static Exception WrongComputedState(ConsistencyState state)
        => new InvalidOperationException(
            $"Wrong Computed.State: {state}.");

    public static Exception ComputedCurrentIsNull()
        => new NullReferenceException($"Computed.Current() == null.");
    public static Exception ComputedCurrentIsOfIncompatibleType(Type expectedType)
        => new InvalidCastException(
            $"Computed.Current() can't be converted to '{expectedType}'.");
    public static Exception CapturedComputedIsOfIncompatibleType(Type expectedType)
        => new InvalidCastException(
            $"Computed.Captured() can't be converted to '{expectedType}'.");
    public static Exception NoComputedCaptured()
        => new InvalidOperationException($"No {nameof(IComputed)} was captured.");

    public static Exception ComputedInputCategoryCannotBeSet()
        => new NotSupportedException(
            "Only IState and IAnonymousComputedInput allow to manually set Category property.");

    public static Exception AnonymousComputedSourceIsNotComputedYet()
        => new InvalidOperationException("This anonymous computed source isn't computed yet.");

    public static Exception WrongPublisher()
        => new PublisherException("Wrong publisher.");
    public static Exception WrongPublisher(IPublisher expected, Symbol providedPublisherId)
        => new PublisherException($"Wrong publisher: {expected.Id} (expected) != {providedPublisherId} (provided).");
    public static Exception UnknownChannel(Channel<BridgeMessage> channel)
        => new PublisherException("Unknown channel.");

    public static Exception PublicationAbsents()
        => new ReplicaException("The Publication absents on the server.");
    public static Exception NoPublicationStateInfo()
        => new ReplicaException(
            "No publication state info was found. " +
            "Typically this indicates you're hitting a wrong endpoint " +
            "(check your client definition interface) " +
            "or forgot to add [Publish] attribute to the controller's method.");

    public static Exception ReplicaHasNeverBeenUpdated()
        => new ReplicaException("The Replica has never been updated.");

    public static Exception WebSocketConnectTimeout()
        => new WebSocketException("Connection timeout.");

    public static Exception ComputeServiceMethodAttributeOnStaticMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to static method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnNonVirtualMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to non-virtual method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnNonAsyncMethod(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to non-async method '{method}'.");
    public static Exception ComputeServiceMethodAttributeOnAsyncMethodReturningNonGenericTask(MethodInfo method)
        => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to a method " +
            $"returning non-generic Task/ValueTask: '{method}'.");

    public static Exception UnsupportedReplicaType(Type replicaType)
        => new NotSupportedException(
            $"Replica<{replicaType.GetName()}> isn't supported by the current client, " +
            $"most likely because there is no good way to intercept the deserialization " +
            $"of results of this type.");

    public static Exception UnsupportedComputedOptions(Type unsupportedBy)
        => new NotSupportedException($"Specified {nameof(ComputedOptions)} aren't supported by '{unsupportedBy}'.");

    public static Exception InvalidContextCallOptions(CallOptions callOptions)
        => new InvalidOperationException(
            $"{nameof(ComputeContext)} with {nameof(CallOptions)} = {callOptions} cannot be used here.");
}
