using Studium_Catalog.Services.DTOs;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Studium_Catalog.Api.Extensions;

public static class MessagingExtensions
{
    public static void AddStudiumMessaging(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(RoomMaintenanceEvent).Assembly);

            var rabbitConnectionString = builder.Configuration["RabbitMQ:ConnectionString"]
                ?? builder.Configuration["RABBITMQ_URL"]
                ?? "amqp://studium:StudiumRabbitMQ2026!@studium-rabbitmq-service.studium.svc.cluster.local:5672";

            opts.UseRabbitMq(new Uri(rabbitConnectionString)).AutoProvision();

            var bookingQueueName = builder.Configuration["RabbitMQ:BookingQueueName"] ?? "studium-booking-events-queue";
            opts.PublishMessage<RoomMaintenanceEvent>().ToRabbitQueue(bookingQueueName);
            opts.PublishMessage<OperatingHoursChangedEvent>().ToRabbitQueue(bookingQueueName);
            opts.PublishMessage<RoomStatusChangedEvent>().ToRabbitQueue(bookingQueueName);
            opts.PublishMessage<ResourceUpdatedEvent>().ToRabbitQueue(bookingQueueName);
        });
    }
}
