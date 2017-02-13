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

        public async Task<IEnumerable<OccupancyEntity>> DeleteAll()
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            TableQuery<OccupancyEntity> query = new TableQuery<OccupancyEntity>();
            var occupancyEntities = table.ExecuteQuery(query).ToList();
            var occupancyEntitiesGroupedByRoom = occupancyEntities.GroupBy(x => x.PartitionKey);
            foreach (var occupancyEntitiesInRoom in occupancyEntitiesGroupedByRoom)
            {
                var batchOperation = new TableBatchOperation();
                foreach (var occupancyEntity in occupancyEntitiesInRoom)
                {
                    batchOperation.Delete(occupancyEntity);
                }
                if (batchOperation.Count > 0)
                {
                    await table.ExecuteBatchAsync(batchOperation);
                }
            }
            return occupancyEntities;
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
                        roomId.Value.ToString("d19")));
            }

            var occupancyEntities = table.ExecuteQuery(query);
            return occupancyEntities;
        }

        public async Task<IEnumerable<OccupancyEntity>> DeleteAllInRoom(long roomId)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Get all occupancies in room
            TableQuery<OccupancyEntity> query =
                new TableQuery<OccupancyEntity>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey",
                        QueryComparisons.Equal,
                        roomId.ToString("d19")));
            var deleteEntities = table.ExecuteQuery(query);

            // Delete entities
            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach (var deleteEntity in deleteEntities)
            {
                batchOperation.Delete(deleteEntity);
            }
            if (batchOperation.Count > 0)
            {
                await table.ExecuteBatchAsync(batchOperation);
            }
            return deleteEntities;
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
                            roomId.ToString("d19")))
                    .Take(1);
            return table.ExecuteQuery(query).FirstOrDefault();
        }

        public async Task<OccupancyEntity> Insert(Occupancy occupancy)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            await table.CreateIfNotExistsAsync();

            // Insert new occupancy
            var occupancyEntity = new OccupancyEntity(occupancy.RoomId, occupancy.StartTime.UtcDateTime);
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