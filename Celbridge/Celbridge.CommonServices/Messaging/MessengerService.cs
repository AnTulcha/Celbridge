﻿using CommunityToolkit.Mvvm.Messaging;

namespace Celbridge.CommonServices.Messaging;

/// <summary>
/// A wrapper for CommunityToolkit.Mvvm.Messaging.
/// </summary>
public class MessengerService : IMessengerService
{
    private IMessenger _messenger = WeakReferenceMessenger.Default;

    public void Register<TMessage>(object recipient, BaseLibrary.Messaging.MessageHandler<object, TMessage> handler) 
        where TMessage : class
    {
        _messenger.Register<TMessage>(recipient, handler.Invoke);
    }

    public void UnregisterAll(object recipient)
    {
        _messenger.UnregisterAll(recipient);
    }

    public TMessage Send<TMessage>()
        where TMessage : class, new()
    {
        return _messenger.Send<TMessage>();
    }

    public TMessage Send<TMessage>(TMessage message)
        where TMessage : class
    {
        return _messenger.Send<TMessage>(message);
    }
}