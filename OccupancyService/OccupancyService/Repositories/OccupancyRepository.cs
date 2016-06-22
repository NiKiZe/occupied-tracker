using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OccupancyService.Models;

namespace OccupancyService.Repositories
{
    /// <summary>
    /// Mockup repository
    /// </summary>
    public class OccupancyRepository
    {
        private static List<Occupancy> _occupancies = new List<Occupancy>();

        public IEnumerable<Occupancy> GetAll(IEnumerable<int> roomIds = null)
        {
            IEnumerable<Occupancy> occupancies = _occupancies;

            if (roomIds != null)
            {
                // Filter on given rooms
                occupancies = occupancies.Where(x => roomIds.Contains(x.RoomId));
            }

            // Sort on id, because why not
            return occupancies.OrderBy(x => x.Id);
        }
        public Occupancy GetLastOccupancy(int roomId)
        {
            return _occupancies.Last(x => x.RoomId == roomId);
        }

        public Occupancy Insert(Occupancy occupancy)
        {
            occupancy.Id = _occupancies.Count > 0 ? _occupancies.Max(x => x.Id) + 1 : 1;
            _occupancies.Add(occupancy);
            return occupancy;
        }
        
        public Occupancy Update(Occupancy occupancy)
        {
            _occupancies.RemoveAll(x => x.Id == occupancy.Id);
            _occupancies.Add(occupancy);
            return occupancy;
        }
    }
}