using Amazon;
using Studium_Catalog.Services.DTOs;
using Wolverine;
using Wolverine.AmazonSqs;

namespace Studium_Catalog.Api.Extensions;

public static class MessagingExtensions
{
    public static void AddStudiumMessaging(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(RoomMaintenanceEvent).Assembly);

            var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-2";
            opts.UseAmazonSqsTransport(cfg =>
            {
                cfg.RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);
                var serviceUrl = builder.Configuration["AWS:ServiceURL"];
                if (!string.IsNullOrWhiteSpace(serviceUrl))
                    cfg.ServiceURL = serviceUrl;
            }).AutoProvision();

            var bookingQueueName = builder.Configuration["AWS:SQS:BookingQueueName"];
            if (!string.IsNullOrWhiteSpace(bookingQueueName))
            {
                opts.PublishMessage<RoomMaintenanceEvent>().ToSqsQueue(bookingQueueName);
                opts.PublishMessage<OperatingHoursChangedEvent>().ToSqsQueue(bookingQueueName);
                opts.PublishMessage<RoomStatusChangedEvent>().ToSqsQueue(bookingQueueName);
                opts.PublishMessage<ResourceUpdatedEvent>().ToSqsQueue(bookingQueueName);
            }

            var notificationsQueueName = builder.Configuration["AWS:SQS:NotificationsQueueName"];
            if (!string.IsNullOrWhiteSpace(notificationsQueueName))
            {
                opts.PublishMessage<RoomMaintenanceEvent>().ToSqsQueue(notificationsQueueName);
            }
        });
    }
}
