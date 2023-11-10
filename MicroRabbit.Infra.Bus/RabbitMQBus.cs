using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;

        private readonly Dictionary<string, List<Type>> _handlers;

        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator)
        {
            this._mediator = mediator;
            this._handlers = new Dictionary<string, List<Type>>();
            this._eventTypes = new List<Type>();
        }
        
        public Task SendCommand<T>(T command) where T : Command
        {
            return this._mediator.Send(command);
        }

        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory() { HostName = "localhost"};

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateChannel())
            {
                // Declaring the queue
                var eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                // Realize the sending the message
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                // Publish the message
                channel.BasicPublish("", eventName, body); 
            }
        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            if (!this._eventTypes.Contains(typeof(T)))
            {
                this._eventTypes.Add(typeof(T));
            }

            // this._handlers.TryAdd(eventName, new List<Type> { handlerType});
            if (!this._handlers.TryAdd(eventName, new List<Type> { handlerType, handlerType}))
            {
                throw new ArgumentException($"Handler Type {handlerType} already is registered for '{eventName}'", nameof(eventName));
            }

            this.StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            var factory = new ConnectionFactory()
            { 
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateChannel())
            {
                var eventName = typeof(T).Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += this.Consumer_Received;

                channel.BasicConsume(eventName, true, consumer);



            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.ToArray());

            try
            {
                await this.ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            var registeredHandlerTypes = this._handlers
                .FirstOrDefault(handler => handler.Key == eventName)
                .Value;

            // Look whether there is any type registered for this eventName.
            var validType = registeredHandlerTypes
                .FirstOrDefault(registeredHandlerType =>
                {
                    if (registeredHandlerType.Name == eventName)
                    {
                        return Activator.CreateInstance(registeredHandlerType) != null;
                    }

                    return false;
                });

            if (validType != null)
            {
                var validObject = Activator.CreateInstance(validType);
                var validEvent = JsonConvert.DeserializeObject(message, validType);
                var concreteType = typeof(IEventHandler<>).MakeGenericType(validType); // ?? Why this?

                await (Task)concreteType.GetMethod("Handle").Invoke(validObject, new object[] { validEvent });
            }
        }
    }
}