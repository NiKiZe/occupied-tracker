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
        
        public void Update(Room room)
        {
            Description = room.Description;
        }

        public Room ToRoom()
        {
            return new Room
            {
                Id = long.Parse(RowKey),
                Description = Description
            };
        }
    }
}