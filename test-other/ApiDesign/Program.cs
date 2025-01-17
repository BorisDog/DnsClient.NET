﻿using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiDesign
{
    public class Program
    {
        public static async Task Main()
        {
            var services = new ServiceCollection();
            services
                .AddLogging(c =>
                {
                    c.AddConsole();
                    c.SetMinimumLevel(LogLevel.Debug);
                })
                .AddOptions();

            services.AddSingleton(new LookupClientOptions()
            {
                AutoResolveNameServers = true,
                UseCache = false
            });

            services.AddSingleton<ILookupClient>(f =>
            {
                var o = f.GetRequiredService<LookupClientOptions>();
                return new LookupClient(o);
            });

            //services.AddSingleton<ILookupClient, LookupClient>();

            /* - -- - - - - - */
            var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            DnsClient.Logging.LoggerFactory = new LoggerFactoryWrapper(loggerFactory);

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var lookup = provider.GetRequiredService<ILookupClient>();

            logger.LogInformation($"Starting stuff...");

            while (true)
            {
                try
                {
                    var x = lookup.Query("google.com", QueryType.A);
                }
                catch { }

                // var r = lookup.QueryServer(new NameServer[] { NameServer.Cloudflare, NameServer.Cloudflare2 }, new DnsQuestion("google.com", QueryType.A));

                await Task.Delay(5000);
            }

            ////Console.ReadKey();
        }
    }

    ////var provider = services.BuildServiceProvider();
    ////var factory = provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
    ////DnsClient.Logging.LoggerFactory = new LoggerFactoryWrapper(factory);

    internal class LoggerFactoryWrapper : DnsClient.Internal.ILoggerFactory
    {
        private readonly ILoggerFactory _microsoftLoggerFactory;

        public LoggerFactoryWrapper(ILoggerFactory microsoftLoggerFactory)
        {
            _microsoftLoggerFactory = microsoftLoggerFactory ?? throw new ArgumentNullException(nameof(microsoftLoggerFactory));
        }

        public DnsClient.Internal.ILogger CreateLogger(string categoryName)
        {
            return new DnsLogger(_microsoftLoggerFactory.CreateLogger(categoryName));
        }

        private class DnsLogger : DnsClient.Internal.ILogger
        {
            private readonly ILogger _logger;

            public DnsLogger(ILogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public bool IsEnabled(DnsClient.Internal.LogLevel logLevel)
            {
                return _logger.IsEnabled((LogLevel)logLevel);
            }

            public void Log(DnsClient.Internal.LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
            {
                _logger.Log((LogLevel)logLevel, eventId, exception, message, args);
            }
        }
    }
}
