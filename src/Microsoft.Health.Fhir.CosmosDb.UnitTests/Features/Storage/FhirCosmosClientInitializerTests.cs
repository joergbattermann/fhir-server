﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.CosmosDb.Configs;
using Microsoft.Health.Fhir.CosmosDb.Features.Queries;
using Microsoft.Health.Fhir.CosmosDb.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Fhir.CosmosDb.UnitTests.Features.Storage
{
    public class FhirCosmosClientInitializerTests
    {
        private readonly FhirCosmosClientInitializer _initializer;

        private readonly Container _container = Substitute.ForPartsOf<Container>();
        private readonly ICollectionInitializer _collectionInitializer1 = Substitute.For<ICollectionInitializer>();
        private readonly ICollectionInitializer _collectionInitializer2 = Substitute.For<ICollectionInitializer>();
        private readonly List<ICollectionInitializer> _collectionInitializers;
        private CosmosDataStoreConfiguration _cosmosDataStoreConfiguration;

        public FhirCosmosClientInitializerTests()
        {
            var clientTestProvider = Substitute.For<ICosmosClientTestProvider>();
            var fhirRequestContextAccessor = Substitute.For<IFhirRequestContextAccessor>();
            var cosmosResponseProcessor = Substitute.For<ICosmosResponseProcessor>();
            _cosmosDataStoreConfiguration = new CosmosDataStoreConfiguration();

            _initializer = new FhirCosmosClientInitializer(
                clientTestProvider,
                fhirRequestContextAccessor,
                cosmosResponseProcessor,
                Enumerable.Empty<RequestHandler>(),
                NullLogger<FhirCosmosClientInitializer>.Instance);

            _collectionInitializers = new List<ICollectionInitializer> { _collectionInitializer1, _collectionInitializer2 };
        }

        [Fact]
        public void CreateClient_NullPreferredLocations_DoesNotSetPreferredLocations()
        {
            var client = _initializer.CreateCosmosClient(_cosmosDataStoreConfiguration);

            Assert.Null(client.ClientOptions.ApplicationPreferredRegions);
        }

        [Fact]
        public void CreateClient_EmptyPreferredLocations_DoesNotSetPreferredLocations()
        {
            _cosmosDataStoreConfiguration.PreferredLocations = new string[] { };
            var client = _initializer.CreateCosmosClient(_cosmosDataStoreConfiguration);

            Assert.Null(client.ClientOptions.ApplicationPreferredRegions);
        }

        [Fact]
        public void CreateClient_SetsPreferredLocations()
        {
            _cosmosDataStoreConfiguration.PreferredLocations = new[] { "southcentralus", "northcentralus" };
            var client = _initializer.CreateCosmosClient(_cosmosDataStoreConfiguration);

            Assert.NotEmpty(client.ClientOptions.ApplicationPreferredRegions);
            Assert.Equal(2, client.ClientOptions.ApplicationPreferredRegions.Count);

            for (int i = 0; i < _cosmosDataStoreConfiguration.PreferredLocations.Count; i++)
            {
                Assert.Equal(_cosmosDataStoreConfiguration.PreferredLocations[i], client.ClientOptions.ApplicationPreferredRegions[i]);
            }
        }

        [Fact]
        public void CreateClient_SetsMaxRetryAttemptsOnThrottledRequests()
        {
            _cosmosDataStoreConfiguration.RetryOptions.MaxNumberOfRetries = 10;
            var client = _initializer.CreateCosmosClient(_cosmosDataStoreConfiguration);

            Assert.Equal(10, client.ClientOptions.MaxRetryAttemptsOnRateLimitedRequests);
        }

        [Fact]
        public void CreateClient_SetsMaxRetryWaitTimeInSeconds()
        {
            _cosmosDataStoreConfiguration.RetryOptions.MaxWaitTimeInSeconds = 99;
            var client = _initializer.CreateCosmosClient(_cosmosDataStoreConfiguration);

            Assert.Equal(TimeSpan.FromSeconds(99), client.ClientOptions.MaxRetryWaitTimeOnRateLimitedRequests);
        }
    }
}
