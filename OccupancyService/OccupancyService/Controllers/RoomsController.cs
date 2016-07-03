using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure;
using OccupancyService.Models;
using OccupancyService.Repositories;
using OccupancyService.TableEntities;

namespace OccupancyService.Controllers
{
    /// <summary>
    /// All rooms
    /// </summary>
    [RoutePrefix("Rooms")]
    public class RoomsController : ApiController
    {
        /// <summary>
        /// Gets list of all rooms
        /// </summary>
        /// <remarks>
        /// Gets list of all rooms
        /// </remarks>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<Room>> Get()
        {
            var repository = new RoomRepository();
            var roomEntities = await repository.GetAll();
            return roomEntities.Select(x => x.ToRoom());
        }

        /// <summary>
        /// Creates a new room
        /// </summary>
        /// <remarks>
        /// Creates a new room
        /// </remarks>
        /// <param name="room">The room to create</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("")]
        [HttpPost]
        public async Task<Room> Post(RoomInsert room, string passcode = null)
        {
            CheckPasscode(passcode);

            // Check if it is occupied (occupancies can has been created before the room)
            var occupancyRepository = new OccupancyRepository();
            var latestOccupancy = await occupancyRepository.GetLatestOccupancy(room.Id);
            var lastUpdate = latestOccupancy != null
                    ? (latestOccupancy.EndTime ?? latestOccupancy.StartTime) // Last time occupancy changed
                    : DateTime.Now; // It started being available now
            var isOccupied = latestOccupancy != null && !latestOccupancy.EndTime.HasValue;

            // Insert room
            var repository = new RoomRepository();
            var roomToInsert = room.ToRoom();
            roomToInsert.IsOccupied = isOccupied;
            roomToInsert.LastUpdate = lastUpdate;
            var insertedRoomEntity = await repository.Insert(roomToInsert);
            var insertedRoom = insertedRoomEntity?.ToRoom();
            PostSignalR(RoomChangeType.New, insertedRoom);
            return insertedRoom;
        }

        /// <summary>
        /// Delete all rooms
        /// </summary>
        /// <remarks>
        /// Delete all rooms
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("")]
        [HttpDelete]
        public async Task<IEnumerable<Room>> Delete(string passcode = null)
        {
            CheckPasscode(passcode);
            
            var repository = new RoomRepository();
            var deletedRoomEntities = await repository.DeleteAll();
            var deletedRooms = deletedRoomEntities?.Select(x => x.ToRoom());
            PostSignalR(RoomChangeType.Deleted, deletedRooms?.ToArray());
            return deletedRooms;
        }

        /// <summary>
        /// Gets a single room
        /// </summary>
        /// <remarks>
        /// Gets a single room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpGet]
        public async Task<Room> Get(long id)
        {
            var repository = new RoomRepository();
            var roomEntity = await repository.Get(id);
            return roomEntity?.ToRoom();
        }

        /// <summary>
        /// Deletes a single room
        /// </summary>
        /// <remarks>
        /// Deletes a single room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpDelete]
        public async Task<Room> Delete(long id, string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new RoomRepository();
            var deletedRoomEntity = await repository.Delete(id);
            var deletedRoom = deletedRoomEntity?.ToRoom();
            PostSignalR(RoomChangeType.Deleted, deletedRoom);
            return deletedRoom;
        }

        /// <summary>
        /// Updates the given room
        /// </summary>
        /// <remarks>
        /// Updates the given room. If IsOccupied is changed to true, a new Occupancy record will be created. If it is changed to false, the latest Occupancy record will get an EndTime.
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <param name="roomUpdate">The new properties for the room</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpPut]
        public async Task<Room> Put(long id, RoomUpdate roomUpdate, string passcode = null)
        {
            CheckPasscode(passcode);

            // Get old room version
            var repository = new RoomRepository();
            var roomEntity = await repository.Get(id);
            if (roomEntity == null)
            {
                throw new ArgumentException("Room not found");
            }

            // Update occupied if changed
            var occupiedChanged = roomUpdate.IsOccupied.HasValue && roomUpdate.IsOccupied != roomEntity.IsOccupied;
            if (occupiedChanged)
            {
                await ChangeOccupancy(id, roomUpdate.IsOccupied.Value);
            }

            // Save changes
            roomEntity.Update(roomUpdate);
            roomEntity.LastUpdate = DateTime.UtcNow;
            var updatedRoomEntity = await repository.Update(roomEntity);
            var updatedRoom = updatedRoomEntity?.ToRoom();
            PostSignalR(RoomChangeType.Updated, updatedRoom);
            return updatedRoom;
        }

        /// <summary>
        /// Gets occupancy history of all rooms
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of all rooms
        /// </remarks>
        /// <returns></returns>
        [Route("Occupancies")]
        [HttpGet]
        public async Task<IEnumerable<Occupancy>> GetOccupancies()
        {
            var repository = new OccupancyRepository();
            var occupancyEntities = await repository.GetAll();
            return occupancyEntities.Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Delete all occupancies
        /// </summary>
        /// <remarks>
        /// Delete all occupancies
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("Occupancies")]
        [HttpDelete]
        public async Task<IEnumerable<Occupancy>> DeleteOccupancies(string passcode = null)
        {
            CheckPasscode(passcode);

            // Delete all occupancies
            var occupancyRepository = new OccupancyRepository();
            var deletedEntities = await occupancyRepository.DeleteAll();

            // Set all rooms to unoccupied
            var roomRepository = new RoomRepository();
            var roomEntities = await roomRepository.GetAll();
            var updateRoomEntities = roomEntities.Where(x => x.IsOccupied).ToList();
            foreach (var updateRoomEntity in updateRoomEntities)
            {
                updateRoomEntity.IsOccupied = false;
                updateRoomEntity.LastUpdate = DateTime.UtcNow;
                await roomRepository.Update(updateRoomEntity);
            }
            PostSignalR(RoomChangeType.Updated, updateRoomEntities.Select(x => x.ToRoom()).ToArray());
            return deletedEntities?.Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Gets occupancy history of a single room
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpGet]
        public async Task<IEnumerable<Occupancy>> GetOccupanciesForRoom(long id)
        {
            var repository = new OccupancyRepository();
            var occupancyEntities = await repository.GetAll(id);
            return occupancyEntities.Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Updates the status of a given room
        /// </summary>
        /// <remarks>
        /// Updates the status of a given room. If isOccupied is true, a new Occupancy record will be created. If it is false, the latest Occupancy record will get an EndTime.
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="isOccupied">Indicates if the room is currently occupied or not</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpPost]
        public async Task<Occupancy> PostOccupancy(long id, bool isOccupied, string passcode = null)
        {
            CheckPasscode(passcode);

            // Get old room version if existing
            var repository = new RoomRepository();
            var roomEntity = await repository.Get(id);
            if (roomEntity != null)
            {
                // Update room status
                if (roomEntity.IsOccupied != isOccupied)
                {
                    roomEntity.IsOccupied = isOccupied;
                    PostSignalR(RoomChangeType.Updated, roomEntity.ToRoom());
                }
                roomEntity.LastUpdate = DateTime.UtcNow;
                await repository.Update(roomEntity);
            }

            // Create/update occupancy
            return await ChangeOccupancy(id, isOccupied);
        }

        /// <summary>
        /// Delete all occupancies in a single room
        /// </summary>
        /// <remarks>
        /// Delete all occupancies in a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpDelete]
        public async Task<IEnumerable<Occupancy>> DeleteOccupanciesInRoom(long id, string passcode = null)
        {
            CheckPasscode(passcode);

            var occupancyRepository = new OccupancyRepository();
            var deletedEntities = await occupancyRepository.DeleteAllInRoom(id);

            // Set rooms to unoccupied
            var roomRepository = new RoomRepository();
            var roomEntity = await roomRepository.Get(id);
            if (roomEntity != null && roomEntity.IsOccupied)
            {
                roomEntity.IsOccupied = false;
                roomEntity.LastUpdate = DateTime.UtcNow;
                await roomRepository.Update(roomEntity);
                PostSignalR(RoomChangeType.Updated, roomEntity.ToRoom());
            }

            return deletedEntities?.Select(x => x.ToOccupancy());
        }

        private async Task<Occupancy> ChangeOccupancy(long roomId, bool isOccupied)
        {
            // Update DB
            var repository = new OccupancyRepository();
            var occupancyEntity = await repository.GetLatestOccupancy(roomId);
            
            if (isOccupied && (occupancyEntity == null || occupancyEntity.EndTime.HasValue))
            {
                // Create a new occupancy
                var occupancy = new Occupancy
                {
                    RoomId = roomId,
                    StartTime = DateTime.UtcNow
                };
                var insertedRoomEntity = await repository.Insert(occupancy);
                return insertedRoomEntity?.ToOccupancy();
            }
            
            if (!isOccupied && occupancyEntity != null && !occupancyEntity.EndTime.HasValue)
            {
                // End the last occupancy
                occupancyEntity.EndTime = DateTime.UtcNow;
                var updatedOccupancyEntity = await repository.Update(occupancyEntity);
                return updatedOccupancyEntity?.ToOccupancy();
            }

            // Status has not changed (or could not be changed)
            return occupancyEntity?.ToOccupancy();
        }

        private void PostSignalR(RoomChangeType changeType, params Room[] rooms)
        {
            if (rooms == null)
            {
                return;
            }
            var roomsFiltered = rooms.Where(x => x != null).ToList();
            if (roomsFiltered.Count == 0)
            {
                return;
            }

            // Send event on SignalR
            string typeString;
            switch (changeType)
            {
                case RoomChangeType.New:
                    typeString = "new";
                    break;
                case RoomChangeType.Updated:
                    typeString = "updated";
                    break;
                case RoomChangeType.Deleted:
                    typeString = "deleted";
                    break;
                default:
                    throw new Exception($"Unknown RoomChangeType: {changeType}");
            }
            var context = GlobalHost.ConnectionManager.GetHubContext<RoomsHub>();
            context.Clients.All.roomsChanged(typeString, rooms);
        }

        private void CheckPasscode(string passcode)
        {
            // Check passcode if there is one
            var correctPasscode = CloudConfigurationManager.GetSetting("ApiPasscode");
            if (string.IsNullOrEmpty(correctPasscode) || passcode == correctPasscode)
            {
                // Passcode is disabled, or it is correct. Continue
            }
            else
            {
                throw new AuthenticationException("Wrong passcode");
            }
        }
    }
}