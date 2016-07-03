using System;
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
    public class RoomRepository
    {
        private CloudTableClient _tableClient;

        public RoomRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        public async Task<IEnumerable<RoomEntity>>  DeleteAll()
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            TableQuery<RoomEntity> query = new TableQuery<RoomEntity>();
            var batchOperation = new TableBatchOperation();
            var roomEntities = table.ExecuteQuery(query).ToList();
            foreach (var roomEntity in roomEntities)
            {
                batchOperation.Delete(roomEntity);
            }
            if (batchOperation.Count > 0)
            {
                await table.ExecuteBatchAsync(batchOperation);
            }
            return roomEntities;
        }

        public async Task<IEnumerable<RoomEntity>> GetAll()
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            await table.CreateIfNotExistsAsync();

            // Get all rooms
            TableQuery<RoomEntity> query = new TableQuery<RoomEntity>();
            return table.ExecuteQuery(query);
        }

        public async Task<RoomEntity> Get(long id)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            await table.CreateIfNotExistsAsync();

            // Get a single room
            TableQuery<RoomEntity> query = new TableQuery<RoomEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id.ToString("d19")));
            return table.ExecuteQuery(query).FirstOrDefault();
        }

        public async Task<RoomEntity> Insert(Room room)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            await table.CreateIfNotExistsAsync();

            // Insert new room
            var roomEntity = new RoomEntity(room.Id)
            {
                Description = room.Description,
                IsOccupied = room.IsOccupied,
                LastUpdate = room.LastUpdate
            };
            TableOperation insertOperation = TableOperation.Insert(roomEntity);
            await table.ExecuteAsync(insertOperation);
            return roomEntity;
        }

        public async Task<RoomEntity> Update(RoomEntity roomEntity)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            await table.CreateIfNotExistsAsync();

            // Replace
            TableOperation updateOperation = TableOperation.Replace(roomEntity);
            await table.ExecuteAsync(updateOperation);

            return roomEntity;
        }

        public async Task<RoomEntity> Delete(long id)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            await table.CreateIfNotExistsAsync();

            // Delete entry
            TableOperation retrieveOperation = TableOperation.Retrieve<RoomEntity>("Rooms", id.ToString("d19"));
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            RoomEntity deleteEntity = (RoomEntity)retrievedResult.Result;

            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
            }

            return deleteEntity;
        }
    }
}