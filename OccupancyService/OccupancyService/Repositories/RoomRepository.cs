using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OccupancyService.Models;

namespace OccupancyService.Repositories
{
    public class RoomRepository
    {
        private static List<Room> _rooms = new List<Room>();

        public IEnumerable<Room> GetAll(List<long> ids = null)
        {
            IEnumerable<Room> rooms = _rooms;

            if (ids != null && ids.Count > 0)
            {
                // Filter on id
                rooms = rooms.Where(x => ids.Contains(x.Id));
            }

            // Sort on id, because why not
            return rooms.OrderBy(x => x.Id);
        }

        public Room Get(long id)
        {
            return _rooms.FirstOrDefault(x => x.Id == id);
        }

        public Room Insert(NewRoom room)
        {
            var roomDb = room.ToRoom();
            roomDb.Id = _rooms.Count > 0 ? _rooms.Max(x => x.Id) + 1 : 1;
            _rooms.Add(roomDb);
            return roomDb;
        }

        public Room Update(Room room)
        {
            if (_rooms.All(x => x.Id != room.Id))
            {
                return null;
            }
            _rooms.RemoveAll(x => x.Id == room.Id);
            _rooms.Add(room);
            return room;
        }
    }
}