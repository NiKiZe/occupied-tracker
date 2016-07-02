using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using OccupancyService.Models;
using OccupancyService.TableEntities;

namespace OccupancyService.Repositories
{
    /// <summary>
    /// Mockup repository
    /// </summary>
    public class OccupancyRepository
    {
        private CloudTableClient _tableClient;

        public OccupancyRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        public async Task DeleteAll()
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            TableQuery<TableEntity> query = new TableQuery<TableEntity>();
            var batchOperation = new TableBatchOperation();
            foreach (var occupancyEntity in table.ExecuteQuery(query))
            {
                batchOperation.Delete(occupancyEntity);
            }
            if (batchOperation.Count > 0)
            {
                await table.ExecuteBatchAsync(batchOperation);
            }
        }

        public async Task<IEnumerable<OccupancyEntity>> GetAll(long? roomId = null)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Get all occupancies
            TableQuery<OccupancyEntity> query = new TableQuery<OccupancyEntity>();

            // Filter on room if requested
            if (roomId.HasValue)
            {
                query = query.Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey",
                        QueryComparisons.Equal,
                        roomId.ToString()));
            }

            return table.ExecuteQuery(query);
        }

        public async Task DeleteAllInRoom(long roomId)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Get all occupancies in room
            TableQuery<OccupancyEntity> query =
                new TableQuery<OccupancyEntity>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey",
                        QueryComparisons.Equal,
                        roomId.ToString()));
            var occupancyEntities = table.ExecuteQuery(query);

            // Delete entities
            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var occupancyEntity in occupancyEntities)
            {
                batchOperation.Delete(occupancyEntity);
            }
            await table.ExecuteBatchAsync(batchOperation);
        }

        public async Task<OccupancyEntity> GetLatestOccupancy(long roomId)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Get latest occupancy (it is the first record, since they are ordered by RowKey, which is a reversed timestamp)
            TableQuery<OccupancyEntity> query =
                new TableQuery<OccupancyEntity>()
                    .Where(
                        TableQuery.GenerateFilterCondition(
                            "PartitionKey",
                            QueryComparisons.Equal,
                            roomId.ToString()))
                    .Take(1);
            return table.ExecuteQuery(query).FirstOrDefault();
        }

        public async Task<OccupancyEntity> Insert(Occupancy occupancy)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Insert new occupancy
            var occupancyEntity = new OccupancyEntity(occupancy.RoomId, DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks)
            {
                StartTime = occupancy.StartTime,
                EndTime = occupancy.EndTime
            };
            TableOperation insertOperation = TableOperation.Insert(occupancyEntity);
            await table.ExecuteAsync(insertOperation);
            return occupancyEntity;
        }

        public async Task<OccupancyEntity> Update(OccupancyEntity occupancyEntity)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Replace
            TableOperation updateOperation = TableOperation.Replace(occupancyEntity);
            await table.ExecuteAsync(updateOperation);

            return occupancyEntity;
        }
    }
}