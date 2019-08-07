using Automatonymous;
using System;
using LightMessagingCore.Boilerplate.Messaging;
using LightMessagingCore.Boilerplate.Saga.Messages;

namespace LightMessagingCore.Boilerplate.Saga
{
    public class OrderSaga : MassTransitStateMachine<OrderSagaState>
    {
        public State Received { get; set; }
        public State Processed { get; set; }
        public State Cancelled { get; set; }

        public Event<IOrderCommand> OrderCommand { get; set; }
        public Event<IOrderProcessedEvent> OrderProcessed { get; set; }
        public Event<IOrderCanceledEvent> OrderCanceled { get; set; }

        public OrderSaga()
        {
            InstanceState(s => s.CurrentState);

            Event(() => OrderCommand,
                cec =>
                        cec.CorrelateBy(state => state.OrderCode, context => context.Message.OrderCode)
                        .SelectId(selector => Guid.NewGuid()));

            Event(() => OrderProcessed, cec => cec.CorrelateById(selector =>
                        selector.Message.CorrelationId));
            Event(() => OrderCanceled, cec => cec.CorrelateById(selector =>
                       selector.Message.CorrelationId));

            Initially(
                When(OrderCommand)
                    .Then(context =>
                    {
                        context.Instance.OrderCode = context.Data.OrderCode;
                        context.Instance.OrderId = context.Data.OrderId;
                    })
                    .ThenAsync(
                        context => Console.Out.WriteLineAsync($"{context.Data.OrderId} order id is received..")
                    )
                    .TransitionTo(Received)
                    .Publish(context => new OrderReceivedEvent(context.Instance))
                );


            During(Received,
                When(OrderCanceled)
                .Then(context =>
                {
                    Console.Out.WriteLineAsync($"{context.Data.OrderId} order id is cancelled..");
                })
                .Finalize());

            During(Received,
             When(OrderProcessed)
                .ThenAsync(
                    context => Console.Out.WriteLineAsync($"{context.Data.OrderId} order id is processed.."))
                .Finalize()
                );


            SetCompletedWhenFinalized();
        }
    }
}