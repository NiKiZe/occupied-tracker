using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public void DeleteTable()
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.DeleteIfExists();
        }

        public IEnumerable<OccupancyEntity> GetAll(long? roomId = null)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.CreateIfNotExists();

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

        public void DeleteAllInRoom(long roomId)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.CreateIfNotExists();
            
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
            table.ExecuteBatch(batchOperation);
        }

        public OccupancyEntity GetLatestOccupancy(long roomId)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.CreateIfNotExists();

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

        public OccupancyEntity Insert(Occupancy occupancy)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.CreateIfNotExists();

            // Insert new occupancy
            var occupancyEntity = new OccupancyEntity(occupancy.RoomId, DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks)
            {
                StartTime = occupancy.StartTime,
                EndTime = occupancy.EndTime
            };
            TableOperation insertOperation = TableOperation.Insert(occupancyEntity);
            table.Execute(insertOperation);
            return occupancyEntity;
        }

        public OccupancyEntity Update(OccupancyEntity occupancyEntity)
        {
            CloudTable table = _tableClient.GetTableReference("occupancies");
            table.CreateIfNotExists();

            // Replace
            TableOperation updateOperation = TableOperation.Replace(occupancyEntity);
            table.Execute(updateOperation);

            return occupancyEntity;
        }
    }
}