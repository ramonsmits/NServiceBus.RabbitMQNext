﻿namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Routing;

    [TestFixture]
    class When_consuming_messages : RabbitMqContext
    {
        [Test]
        public async Task Should_block_until_a_message_is_available()
        {
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);
            var transportOperations = new TransportOperations(new TransportOperation(message, new UnicastAddressTag(ReceiverQueue)));

            await messageDispatcher.Dispatch(transportOperations, new TransportTransaction(), new ContextBag());

            var received = WaitForMessage();

            Assert.AreEqual(message.MessageId, received.MessageId);
        }

        [Test]
        public async Task Should_be_able_to_receive_messages_without_headers()
        {
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

            using (var connection = await connectionFactory.CreateAdministrationConnection())
            using (var channel = await connection.CreateChannelWithPublishConfirmation())
            {
                var properties = channel.RentBasicProperties();

                properties.MessageId = message.MessageId;

                await channel.BasicPublishWithConfirmation("testreceiver", ReceiverQueue, false, properties, new ArraySegment<byte>(message.Body));
            }

            var received = WaitForMessage();

            Assert.AreEqual(message.MessageId, received.MessageId);
        }

        [Test]
        public async Task Should_be_able_to_receive_a_blank_message()
        {
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

            using (var connection = await connectionFactory.CreateAdministrationConnection())
            using (var channel = await connection.CreateChannelWithPublishConfirmation())
            {
                var properties = channel.RentBasicProperties();

                properties.MessageId = message.MessageId;

                await channel.BasicPublishWithConfirmation(string.Empty, ReceiverQueue, false, properties, new ArraySegment<byte>(message.Body));
            }

            var received = WaitForMessage();

            Assert.NotNull(received.MessageId, "The message id should be defaulted to a new guid if not set");
        }

        [Test]
        public async Task Should_up_convert_the_native_type_to_the_enclosed_message_types_header_if_empty()
        {
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

            var typeName = typeof(MyMessage).FullName;

            using (var connection = await connectionFactory.CreateAdministrationConnection())
            using (var channel = await connection.CreateChannelWithPublishConfirmation())
            {
                var properties = channel.RentBasicProperties();

                properties.MessageId = message.MessageId;
                properties.Type = typeName;

                await channel.BasicPublishWithConfirmation(string.Empty, ReceiverQueue, false, properties, new ArraySegment<byte>(message.Body));
            }

            var received = WaitForMessage();

            Assert.AreEqual(typeName, received.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(typeof(MyMessage), Type.GetType(received.Headers[Headers.EnclosedMessageTypes]));
        }

        class MyMessage
        {

        }
    }
}