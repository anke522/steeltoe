﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Relational.MySql;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.MySql
{
    public static class MySqlProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Add MySql and its IHealthContributor to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logging factory</param>
        /// <param name="addSteeltoeHealthChecks">Add steeltoeHealth checks even if community health checks exist</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>MySqlConnection is retrievable as both MySqlConnection and IDbConnection</remarks>
        public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null, bool addSteeltoeHealthChecks = false)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            MySqlServiceInfo info = config.GetSingletonServiceInfo<MySqlServiceInfo>();

            DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);
            return services;
        }

        /// <summary>
        /// Add MySql and its IHealthContributor to a ServiceCollection.
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logging factory</param>
        /// <param name="addSteeltoeHealthChecks">Add steeltoeHealth checks even if community health checks exist</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>MySqlConnection is retrievable as both MySqlConnection and IDbConnection</remarks>
        public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null, bool addSteeltoeHealthChecks = false)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            MySqlServiceInfo info = config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

            DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);
            return services;
        }

        private static void DoAdd(IServiceCollection services, MySqlServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime, bool addSteeltoeHealthChecks)
        {
            Type mySqlConnection = ConnectorHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
            var mySqlConfig = new MySqlProviderConnectorOptions(config);
            var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, mySqlConnection);
            services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(mySqlConnection, factory.Create, contextLifetime));

            if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
            {
                services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalHealthContributor>>()), ServiceLifetime.Singleton));
            }
        }
    }
}
