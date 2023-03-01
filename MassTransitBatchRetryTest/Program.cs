// See https://aka.ms/new-console-template for more information

using MassTransit;
using MassTransit.Middleware;
using MassTransitBatchRetryTest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;

File.Delete("batch.log");

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices(
    (hostBuilderContext, serviceCollection) =>
    {
        serviceCollection.AddLogging();

        serviceCollection.AddMassTransit(c =>
        {
            c.AddConfigureEndpointsCallback(
                (name, cfg) =>
                {
                    if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                    {
                        rmq.SingleActiveConsumer = true;
                    }
                });

            c.AddConsumer<TestConsumer>().Endpoint(erc =>
            {
                string? typeName = typeof(TestConsumer).FullName;

                erc.Name = typeName.ToLower().Replace('.', '-');
            });

            c.UsingRabbitMq(
            (ctx, cfg) =>
            {
                cfg.Host(
                    "localhost",
                    5675,
                    "/",
                    h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                cfg.UseRetry(
                    r => r.Incremental(
                        2,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(1)));

                cfg.UseConcurrencyLimit(1);

                cfg.UseTimeout(c => c.Timeout = TimeSpan.FromMinutes(10));

                cfg.SingleActiveConsumer = true;

                cfg.PrefetchCount = 5;

                // Change the a.b.c:D format to a-b-c-d
                cfg.MessageTopology.SetEntityNameFormatter(new HyphenatedEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter));

                cfg.ReceiveEndpoint(
                    "BatchConsumer01",
                    e =>
                    {
                        e.Batch<TestMessage>(
                            b =>
                            {
                                b.ConcurrencyLimit = 1;
                                b.MessageLimit = 5;
                                b.TimeLimit = TimeSpan.FromSeconds(10);

                                b.Consumer<TestConsumer, TestMessage>(ctx);
                            });

                        e.SingleActiveConsumer = true;
                        e.ConfigureError(
                            ec =>
                            {
                                ec.UseFilter(new GenerateFaultFilter());
                                ec.UseFilter(new ErrorTransportFilter());
                            });

                        e.ConfigureConsumeTopology = false;
                        e.Bind<TestMessage>(
                            b =>
                            {
                                b.ExchangeType = ExchangeType.Direct;
                                b.RoutingKey = "batch01";
                            });

                        e.PublishFaults = true;
                    });

                cfg.Publish<TestMessage>(x => x.ExchangeType = "direct");
            });
        });


        serviceCollection.AddHostedService<MassTransitConsoleHostedService>();
    });

hostBuilder.ConfigureLogging(
    (hostBuilderContext, loggingBuilder) =>
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {messageId} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("batch.log",
                outputTemplate:
                "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {messageId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        loggingBuilder.AddSerilog(Log.Logger);
    });

// To publish 10 messages
var myHost = hostBuilder.Build();
var provider = myHost.Services;

var bus = provider.GetRequiredService<IBusControl>();
for (int i = 0; i < 10; i++)
{
    bus.Publish(new TestMessage
                {
                    Id = Guid.NewGuid()
                },
        c =>
        {
            c.SetRoutingKey("batch01");
        });
}

await myHost.RunAsync();

