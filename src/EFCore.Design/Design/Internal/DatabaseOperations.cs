// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Design.Internal
{
    public class DatabaseOperations
    {
        private readonly IOperationReporter _reporter;
        private readonly string _projectDir;
        private readonly string _rootNamespace;
        private readonly DesignTimeServicesBuilder _servicesBuilder;

        public DatabaseOperations(
            [NotNull] IOperationReporter reporter,
            [NotNull] Assembly startupAssembly,
            [NotNull] string projectDir,
            [NotNull] string rootNamespace)
        {
            Check.NotNull(reporter, nameof(reporter));
            Check.NotNull(startupAssembly, nameof(startupAssembly));
            Check.NotNull(projectDir, nameof(projectDir));
            Check.NotNull(rootNamespace, nameof(rootNamespace));

            _reporter = reporter;
            _projectDir = projectDir;
            _rootNamespace = rootNamespace;

            var startup = new StartupInvoker(reporter, startupAssembly);
            _servicesBuilder = new DesignTimeServicesBuilder(startup);
        }

        public virtual Task<ReverseEngineerFiles> ScaffoldContextAsync(
            [NotNull] string provider,
            [NotNull] string connectionString,
            [CanBeNull] string outputDir,
            [CanBeNull] string dbContextClassName,
            [NotNull] IEnumerable<string> schemas,
            [NotNull] IEnumerable<string> tables,
            bool useDataAnnotations,
            bool overwriteFiles,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotEmpty(provider, nameof(provider));
            Check.NotEmpty(connectionString, nameof(connectionString));
            Check.NotNull(schemas, nameof(schemas));
            Check.NotNull(tables, nameof(tables));

            var services = _servicesBuilder.Build(provider);

            var loggerFactory = services.GetService<ILoggerFactory>();
            loggerFactory.AddProvider(new LoggerProvider(categoryName => new OperationLogger(categoryName, _reporter)));

            var generator = services.GetRequiredService<ReverseEngineeringGenerator>();
            var tableSelectionSet = new TableSelectionSet(tables, schemas);
            var configuration = new ReverseEngineeringConfiguration
            {
                ConnectionString = connectionString,
                ContextClassName = dbContextClassName,
                ProjectPath = _projectDir,
                ProjectRootNamespace = _rootNamespace,
                OutputPath = outputDir,
                TableSelectionSet = tableSelectionSet,
                UseFluentApiOnly = !useDataAnnotations,
                OverwriteFiles = overwriteFiles
            };

            return generator.GenerateAsync(configuration, cancellationToken);
        }
    }
}
