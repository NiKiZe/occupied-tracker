using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using OccupancyService.Models;

namespace OccupancyService.TableEntities
{
    public class RoomEntity : TableEntity
    {
        public RoomEntity(long roomId)
        {
            PartitionKey = "Rooms";
            RowKey = roomId.ToString();
        }

        public RoomEntity() { }

        public string Description { get; set; }

        public bool IsOccupied { get; set; }

        public void Update(RoomUpdate roomUpdate)
        {
            if (roomUpdate.Description != null)
            {
                Description = roomUpdate.Description;
            }

            if (roomUpdate.IsOccupied.HasValue)
            {
                IsOccupied = roomUpdate.IsOccupied.Value;
            }
        }

        public Room ToRoom()
        {
            return new Room
            {
                Id = long.Parse(RowKey),
                Description = Description,
                IsOccupied = IsOccupied
            };
        }
    }
}