﻿using ElectronicParts.Services.Interfaces;
using ElectronicParts.Services.Implementations;
using ElectronicParts.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using GuiLabs.Undo;

namespace ElectronicParts.DI
{
    public static class Container
    {
        private static IServiceProvider provider;

        static Container()
        {
            IServiceCollection services = new ServiceCollection();

            /// ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<PreferencesViewModel>();

            // Services
            services.AddSingleton<IAssemblyService, AssemblyService>();
            services.AddSingleton<IPinConnectorService, PinConnectorService>();
            services.AddSingleton<IExecutionService, ExecutionService>();
            services.AddSingleton<INodeSerializerService, NodeSerializerService>();
            services.AddSingleton<AssemblyBinder>();
            services.AddSingleton<IGenericTypeComparerService, GenericTypeComparerService>();
            services.AddSingleton<IAssemblyNameExtractorService, AssemblyNameExtractorService>();
            services.AddTransient<INodeValidationService, NodeValidationService>();
            services.AddSingleton<INodeCopyService, NodeCopyService>();
            services.AddSingleton<ActionManager>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Logging
            services.AddLogging();

            provider = services.BuildServiceProvider();

            // Loggin configuration
            provider.GetService<ILoggerFactory>().AddFile(Path.Combine(Directory.GetCurrentDirectory(), "logs/electronicparts.log"), minimumLevel: LogLevel.Trace, fileSizeLimitBytes: 1024 * 1024 * 3);
        }


        public static TItem Resolve<TItem>()
        {
            return provider.GetService<TItem>();
        }
    }
}
