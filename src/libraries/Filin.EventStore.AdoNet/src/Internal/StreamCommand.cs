namespace Filin.EventStore.AdoNet.Internal;

abstract record StreamCommand(WeakReference<IDcbEventStream> Subscriber);

sealed record SubscribeCommand(WeakReference<IDcbEventStream> Subscriber) : StreamCommand(Subscriber);

sealed record UnsubscribeCommand(WeakReference<IDcbEventStream> Subscriber) : StreamCommand(Subscriber);
