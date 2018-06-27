﻿using System.Collections.Generic;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;

namespace FunctionalTestUtils
{
    using System;
    using System.IO;
    using System.Net;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit.Abstractions;

    public class EndpointAddress
    {
        private const string httpListenerConnectionString = "http://localhost:{0}/v2/track/";
        private static Random random = new Random();

        public EndpointAddress()
        {
            this.ConnectionString = string.Format(httpListenerConnectionString, random.Next(2000, 5000).ToString());
        }

        public string ConnectionString { get; }
    }

    // a variant of aspnet/Hosting/test/Microsoft.AspNetCore.Hosting.Tests/HostingEngineTests.cs
    public class InProcessServer : IDisposable
    {
        public const string IKey = "Foo";
        public const string AppId = "AppId";
        private readonly string httpListenerConnectionString;
        private readonly ITestOutputHelper output;

        private static Random random = new Random();
        private static object noParallelism = new object();

        private IWebHost hostingEngine;
        private string url;

        private TelemetryHttpListenerObservable listener;
        private readonly Func<IWebHostBuilder, IWebHostBuilder> configureHost;

        public InProcessServer(string assemblyName, ITestOutputHelper output, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            this.output = output;

            // localhost instead of machine name, as its not possible to get machine name when running non windows.
            var machineName = "localhost";
            this.url = "http://" + machineName + ":" + random.Next(5000, 14000).ToString();
            this.configureHost = configureHost;
            this.httpListenerConnectionString = LauchApplicationAndStartListener(assemblyName);
        }

        private string LauchApplicationAndStartListener(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            string listenerConnectionString = "";
            bool listenerStarted = false;
            int retryCount = 1;
            while (retryCount <= 3)
            {
                output.WriteLine(string.Format("{0}: Attempt {1} to StartApplication", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), retryCount));
                listenerConnectionString = StartApplication(assemblyName, configureHost);
                listenerStarted = StartListener(listenerConnectionString);
                if(listenerStarted)
                {
                    break;
                }
                else
                {
                    StopApplication();
                }
                retryCount++;
            }

            if(!listenerStarted)
            {
                throw new Exception("Unable to start listener after 3 attempts. Failing. Read logs above for details about the exceptions.");
            }
            
            return listenerConnectionString;
        }

        private bool StartListener(string listenerConnectionString)
        {
            output.WriteLine(string.Format("{0}: Starting listener at: {1}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), this.httpListenerConnectionString));

            this.listener = new TelemetryHttpListenerObservable(listenerConnectionString, this.output);
            try
            {
                this.listener.Start();
                output.WriteLine(string.Format("{0}: Started listener", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt")));
            }
            catch (HttpListenerException ex)
            {
                output.WriteLine(string.Format("{0}: Error starting listener.ErrorCode {1} Native Code {2}", DateTime.Now.ToString("G"), ex.ErrorCode, ex.NativeErrorCode));
                return false;
            }

            return true;
        }

        private string StartApplication(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            output.WriteLine(string.Format("{0}: Launching application at: {1}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), this.url));
            return this.Start(assemblyName, configureHost);
        }

        private void StopApplication()
        {
            if (this.hostingEngine != null)
            {
                this.output.WriteLine(string.Format("{0}:Disposing WebHost starting.....", DateTime.Now.ToString("G")));
                this.hostingEngine.Dispose();
                this.output.WriteLine(string.Format("{0}:Disposing WebHost completed.", DateTime.Now.ToString("G")));
            }
        }

        public TelemetryHttpListenerObservable Listener
        {
            get
            {
                return this.listener;
            }
        }

        public string BaseHost
        {
            get
            {
                return this.url;
            }
        }

        public IServiceProvider ApplicationServices { get; private set; }

        private string Start(string assemblyName, Func<IWebHostBuilder, IWebHostBuilder> configureHost = null)
        {
            var builder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(this.BaseHost)
                .UseKestrel()
                .UseStartup(assemblyName)
                .UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IApplicationIdProvider>(provider =>
                    new DictionaryApplicationIdProvider()
                    {
                        Defined = new Dictionary<string, string> {[IKey] = AppId}
                    });
            });
            
            if (configureHost != null)
            {
                builder = configureHost(builder);
            }
            
            this.hostingEngine = builder.Build();
            this.hostingEngine.Start();

            this.ApplicationServices = this.hostingEngine.Services;

            return ((EndpointAddress)this.hostingEngine.Services.GetService<EndpointAddress>()).ConnectionString;
        }

        public void Dispose()
        {
            if (this.hostingEngine != null)
            {
                this.output.WriteLine(string.Format("{0}:Disposing WebHost starting.....", DateTime.Now.ToString("G")));
                this.hostingEngine.Dispose();                
                this.output.WriteLine(string.Format("{0}:Disposing WebHost completed.", DateTime.Now.ToString("G")));
            }
            else
            {
                this.output.WriteLine(string.Format("{0}: Hosting engine is null.", DateTime.Now.ToString("G")));
            }

            if (this.listener != null)
            {
                output.WriteLine(string.Format("{0}: Stopping listener at: {1}", DateTime.Now.ToString("G"), this.httpListenerConnectionString));
                this.listener.Stop();                
            }
        }
    }
}
