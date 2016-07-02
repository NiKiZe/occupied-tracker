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
            RowKey = roomId.ToString("d19");
        }

        public RoomEntity() { }

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates if the room is currently occupied
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// Indicates when this room was last updated
        /// </summary>
        public DateTime LastUpdate { get; set; }

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
                IsOccupied = IsOccupied,
                LastUpdate = LastUpdate
            };
        }
    }
}