﻿using System;
using CoreHelpers.WindowsAzure.Storage.Table.Delegates;
using CoreHelpers.WindowsAzure.Storage.Table.Tests.Contracts;
using CoreHelpers.WindowsAzure.Storage.Table.Tests.Extensions;
using CoreHelpers.WindowsAzure.Storage.Table.Tests.Models;
using Xunit.DependencyInjection;

namespace CoreHelpers.WindowsAzure.Storage.Table.Tests
{
	[Startup(typeof(Startup))]
	[Collection("Sequential")]
	public class ITS001StoreWithStaticEntityMapper
	{
		private readonly ITestEnvironment env;

		public ITS001StoreWithStaticEntityMapper(ITestEnvironment env)
		{
			this.env = env;
		}

		[Fact]
        public async Task VerifyStaticEntityMapperOperations()
        {
			using (var scp = new StorageContext(env.ConnectionString))
			{
				// set the tablename context
				scp.SetTableContext();

                // unification uuid
                var runId = Guid.NewGuid().ToString();

				// create a new user
				var user = new UserModel() { FirstName = "Egon", LastName = "Mueller", Contact = "em@acme.org" };
				user.Contact = $"{user.Contact}.{runId}";

				// configure the entity mapper
				scp.AddEntityMapper(typeof(UserModel), new DynamicTableEntityMapper() { TableName = "UserProfiles", PartitionKeyFormat = "Contact", RowKeyFormat = "Contact" });

				// execute the store operation
				using (var sc = new StorageContext(scp))
				{
					// ensure the table exists					
					await sc.CreateTableAsync<UserModel>();
									
					// inser the model
					await sc.MergeOrInsertAsync<UserModel>(user);					
				}

				// verify if the model was created				
				var result = await scp.QueryAsync<UserModel>();
				Assert.NotNull(result);
				Assert.Equal(1, result.Count());
				Assert.Equal("Egon", result.First().FirstName);
				Assert.Equal("Mueller", result.First().LastName);
				Assert.Equal($"em@acme.org.{runId}", result.First().Contact);

				// Clean up
				await scp.DropTableAsync<UserModel>();
				Assert.False(await scp.ExistsTableAsync<UserModel>());				
			}
		}

		[Fact]
		public async Task VerifyVirtualKeysPOCOModel()
		{
			using (var scp = new StorageContext(env.ConnectionString))
			{
                // set the tablename context
                scp.SetTableContext();

                // create model
                var vpmodel = new VirtualPartitionKeyDemoModelPOCO() { Value1 = "abc", Value2 = "def", Value3 = "ghi" };
				
				// configure the entity mapper				
				scp.AddEntityMapper(typeof(VirtualPartitionKeyDemoModelPOCO), new DynamicTableEntityMapper() { TableName = "VirtualPartitionKeyDemoModelPOCO", PartitionKeyFormat = "{{Value1}}-{{Value2}}", RowKeyFormat = "{{Value2}}-{{Value3}}" });

				// execute the store operation
				using (var sc = new StorageContext(scp))
				{
					// ensure the table exists					
					await sc.CreateTableAsync<VirtualPartitionKeyDemoModelPOCO>();

					// inser the model
					await sc.MergeOrInsertAsync<VirtualPartitionKeyDemoModelPOCO>(vpmodel);
				}

				// verify if the model was created				
				var result = await scp.QueryAsync<VirtualPartitionKeyDemoModelPOCO>();
				Assert.NotNull(result);
				Assert.Equal(1, result.Count());
				Assert.Equal("abc", result.First().Value1);
				Assert.Equal("def", result.First().Value2);
				Assert.Equal("ghi", result.First().Value3);

				// Clean up 
				await scp.DropTableAsync<VirtualPartitionKeyDemoModelPOCO>();
                Assert.False(await scp.ExistsTableAsync<VirtualPartitionKeyDemoModelPOCO>());                
			}
		}

		[Fact]
		public async Task VerifyStatsDelegate()
        {
			using (var scp = new StorageContext(env.ConnectionString))
			{
                // set the tablename context
                scp.SetTableContext();

                // set the delegate
                var stats = new StorageContextStatsDelegate();
				scp.SetDelegate(stats);

				// unification uuid
				var runId = Guid.NewGuid().ToString();

				// create a new user
				var user = new UserModel() { FirstName = "Egon", LastName = "Mueller", Contact = "em@acme.org" };
				user.Contact = $"{user.Contact}.{runId}";

				// configure the entity mapper
				scp.AddEntityMapper(typeof(UserModel), new DynamicTableEntityMapper() { TableName = "UserProfiles", PartitionKeyFormat = "Contact", RowKeyFormat = "Contact" });

				// execute the store operation
				using (var sc = new StorageContext(scp))
				{
					// ensure the table exists					
					await sc.CreateTableAsync<UserModel>();

					// inser the model
					await sc.MergeOrInsertAsync<UserModel>(user);
				}

				// verify if the model was created				
				var result = await scp.QueryAsync<UserModel>();
				
				// Clean up 
				await scp.DeleteAsync<UserModel>(result);				

				// verify stats
				Assert.Equal(1, stats.QueryOperations);
				Assert.Equal(2, stats.StoreOperations.Values.Sum());

				// Drop the table
                await scp.DropTableAsync<UserModel>();
                Assert.False(await scp.ExistsTableAsync<UserModel>());
            }
		}
	}
}



// PUT DElegates in another test

