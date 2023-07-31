﻿using AutoMapper.EquivalencyExpression;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ContactsStore.Config;
using ContactsStore.Persistence;
using ContactsStore.Persistence.Interceptors;
using ContactsStore.Tests.Moq;

namespace ContactsStore.Tests.DependencyInjection;

public static class DependencyContainerFixtureExtensions
{
	public static DependencyContainerFixture SetDatabaseEntitiesConfig(this DependencyContainerFixture container,
																 EntitiesValidationOption option)
		=> container.ReplaceWithMock<IOptionsMonitor<DatabaseEntitiesConfig>>((sp, mock) => mock.Setup(x => x.CurrentValue)
			.Returns(new DatabaseEntitiesConfig
			{
				Validation = option
			}));

	public static DependencyContainerFixture WithTestScopeInMemoryDatabase(
		this DependencyContainerFixture fixture, IAppPartsCollection partsToScan, Action<DbContextOptionsBuilder>? configure = null)
	{
		var dependencies = new AppDependenciesCollection()
			.AddDatabase(new DummyEnvironment());
		return fixture
			.ReplaceWithMock<IOptionsMonitor<DatabaseEntitiesConfig>>((sp, mock) =>
				mock.Setup(x => x.CurrentValue)
					.Returns(new DatabaseEntitiesConfig
					{
						Validation = EntitiesValidationOption.Required
					}))
			.ConfigureServices(services => services
				.AddAutoMapper(am => am.AddCollectionMappers())
				.AddDbContext<ContactsStoreDbContext>((sp, builder) =>
				{
					builder.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
						.EnableDetailedErrors()
						.EnableSensitiveDataLogging()
						.AddInterceptors(sp.GetServices<IOrderedInterceptor>().OrderBy(x => x.Order));
					configure?.Invoke(builder);
				}, ServiceLifetime.Transient)
				.AddDependencyServices(dependencies, partsToScan));
	}

	private sealed class DummyEnvironment : IWebHostEnvironment
	{
		public string ApplicationName { get; set; } = null!;

		public IFileProvider ContentRootFileProvider { get; set; } = null!;

		public string ContentRootPath { get; set; } = null!;

		public string EnvironmentName { get; set; } = Environments.Development;

		public string WebRootPath { get; set; } = null!;

		public IFileProvider WebRootFileProvider { get; set; } = null!;
	}
}