using System;
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
    public class RoomRepository
    {
        private CloudTableClient _tableClient;

        public RoomRepository()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        public void DeleteTable()
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.DeleteIfExists();
        }

        public IEnumerable<RoomEntity> GetAll()
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.CreateIfNotExists();

            // Get all rooms
            TableQuery<RoomEntity> query = new TableQuery<RoomEntity>();
            return table.ExecuteQuery(query);
        }

        public RoomEntity Get(long id)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.CreateIfNotExists();

            // Get a single room
            TableQuery<RoomEntity> query = new TableQuery<RoomEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id.ToString()));
            return table.ExecuteQuery(query).FirstOrDefault();
        }

        public RoomEntity Insert(Room room)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.CreateIfNotExists();

            // Insert new room
            var roomEntity = new RoomEntity(room.Id)
            {
                Description = room.Description
            };
            TableOperation insertOperation = TableOperation.Insert(roomEntity);
            table.Execute(insertOperation);
            return roomEntity;
        }

        public RoomEntity Update(RoomEntity roomEntity)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.CreateIfNotExists();

            // Replace
            TableOperation updateOperation = TableOperation.Replace(roomEntity);
            table.Execute(updateOperation);

            return roomEntity;
        }

        public void Delete(long id)
        {
            CloudTable table = _tableClient.GetTableReference("rooms");
            table.CreateIfNotExists();

            // Delete entry
            TableOperation retrieveOperation = TableOperation.Retrieve<RoomEntity>("Rooms", id.ToString());
            TableResult retrievedResult = table.Execute(retrieveOperation);
            RoomEntity deleteEntity = (RoomEntity)retrievedResult.Result;

            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);
            }
        }
    }
}