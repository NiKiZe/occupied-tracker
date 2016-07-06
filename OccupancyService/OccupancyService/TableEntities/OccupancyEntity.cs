using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using OccupancyService.Models;

namespace OccupancyService.TableEntities
{
    public class OccupancyEntity : TableEntity
    {
        public OccupancyEntity(long roomId, long rowKey)
        {
            PartitionKey = roomId.ToString();
            RowKey = rowKey.ToString("d19");
        }

        public OccupancyEntity() { }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public void Update(Occupancy occupancy)
        {
            StartTime = StartTime;
            EndTime = EndTime;
        }

        public Occupancy ToOccupancy()
        {
            return new Occupancy
            {
                Id = long.Parse(RowKey),
                RoomId = long.Parse(PartitionKey),
                StartTime = StartTime,
                EndTime = EndTime
            };
        }
    }
}
