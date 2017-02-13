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
        static readonly TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById(CloudConfigurationManager.GetSetting("TimeZone"));

        /// <summary>
        /// Gets list of all rooms
        /// </summary>
        /// <remarks>
        /// Gets list of all rooms
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<Room>> Get(string passcode = null)
        {
            var repository = new RoomRepository();
            var rooms = (await repository.GetAll()).Select(x => x.ToRoom()).ToList();
            if (!IsAuthorized(passcode, PasscodeType.Read))
            {
                // Filtered list
                var minTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMin"));
                var maxTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMax"));
                foreach (var room in rooms)
                {
                    if (room.LastUpdate.TimeOfDay < minTime)
                    {
                        // Simulate exit at previous day office closing hours
                        room.IsOccupied = false;
                        var previousDay = room.LastUpdate.Date.AddDays(-1);
                        room.LastUpdate = new DateTimeOffset(previousDay.Year, previousDay.Month, previousDay.Day, maxTime.Hours, maxTime.Minutes, maxTime.Seconds, room.LastUpdate.Offset);
                    }
                    else if (room.LastUpdate.TimeOfDay > maxTime)
                    {
                        // Simulate exit at office closing hours
                        room.IsOccupied = false;
                        room.LastUpdate = new DateTimeOffset(room.LastUpdate.Year, room.LastUpdate.Month, room.LastUpdate.Day, maxTime.Hours, maxTime.Minutes, maxTime.Seconds, room.LastUpdate.Offset);
                    }
                }
            }
            return rooms;
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

            // Insert room
            var repository = new RoomRepository();
            var roomToInsert = room.ToRoom();
            roomToInsert.LastUpdate = DateTimeOffset.UtcNow;
            var insertedRoomEntity = await repository.Insert(roomToInsert);
            var insertedRoom = insertedRoomEntity?.ToRoom();
            PostSignalR(RoomChangeType.New, new[] { insertedRoom });
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

            var repository = new RoomRepository();
            var deletedRoomEntities = await repository.DeleteAll();
            var deletedRooms = deletedRoomEntities?.Select(x => x.ToRoom());
            PostSignalR(RoomChangeType.Delete, deletedRooms);
            return deletedRooms;
        }

        /// <summary>
        /// Gets a single room
        /// </summary>
        /// <remarks>
        /// Gets a single room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpGet]
        public async Task<Room> Get(long id, string passcode = null)
        {
            var repository = new RoomRepository();
            var room = (await repository.Get(id))?.ToRoom();
            if (room != null &&!IsAuthorized(passcode, PasscodeType.Read))
            {
                // Room status always unoccupied outside office hours
                var minTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMin"));
                var maxTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMax"));
                
                if (room.LastUpdate.TimeOfDay < minTime)
                {
                    // Simulate exit at previous day office closing hours
                    room.IsOccupied = false;
                    var previousDay = room.LastUpdate.Date.AddDays(-1);
                    room.LastUpdate = new DateTimeOffset(previousDay.Year, previousDay.Month, previousDay.Day, maxTime.Hours, maxTime.Minutes, maxTime.Seconds, room.LastUpdate.Offset);
                }
                else if (room.LastUpdate.TimeOfDay > maxTime)
                {
                    // Simulate exit at office closing hours
                    room.IsOccupied = false;
                    room.LastUpdate = new DateTimeOffset(room.LastUpdate.Year, room.LastUpdate.Month, room.LastUpdate.Day, maxTime.Hours, maxTime.Minutes, maxTime.Seconds, room.LastUpdate.Offset);
                }
            }
            return room;
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

            var repository = new RoomRepository();
            var deletedRoomEntity = await repository.Delete(id);
            var deletedRoom = deletedRoomEntity?.ToRoom();
            PostSignalR(RoomChangeType.Delete, new[] { deletedRoom });
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

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
            PostSignalR(occupiedChanged ? RoomChangeType.HiddenUpdate : RoomChangeType.Update, new[] { updatedRoom });
            return updatedRoom;
        }

        /// <summary>
        /// Gets occupancy history of all rooms
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of all rooms
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("Occupancies")]
        [HttpGet]
        public async Task<IEnumerable<Occupancy>> GetOccupancies(string passcode = null)
        {
            var repository = new OccupancyRepository();
            var occupancies = (await repository.GetAll())
                .Select(x => x.ToOccupancy());
            if (!IsAuthorized(passcode, PasscodeType.Read))
            {
                // Only show occupancies within the set time
                var minTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMin"));
                var maxTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMax"));
                occupancies = occupancies
                    .Where(x => x.StartTime.TimeOfDay >= minTime && x.StartTime.TimeOfDay <= maxTime);
            }
            return occupancies.OrderByDescending(x => x.StartTime);
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

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
            PostSignalR(RoomChangeType.Update, updateRoomEntities.Select(x => x.ToRoom()));
            return deletedEntities?.Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Gets occupancy history of a single room
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="passcode">Passcode</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpGet]
        public async Task<IEnumerable<Occupancy>> GetOccupanciesForRoom(long id, string passcode = null)
        {
            var repository = new OccupancyRepository();
            var occupancies = (await repository.GetAll(id))
                    .Select(x => x.ToOccupancy());

            if (!IsAuthorized(passcode, PasscodeType.Read))
            {
                // Only show occupancies within the set time
                var minTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMin"));
                var maxTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMax"));
                occupancies = occupancies
                    .Where(x => x.StartTime.TimeOfDay >= minTime && x.StartTime.TimeOfDay <= maxTime);
            }
            return occupancies.OrderByDescending(x => x.StartTime);
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

            // Get old room version if existing
            var repository = new RoomRepository();
            var roomEntity = await repository.Get(id);
            if (roomEntity != null)
            {
                // Update room status
                if (roomEntity.IsOccupied != isOccupied)
                {
                    roomEntity.IsOccupied = isOccupied;
                    PostSignalR(RoomChangeType.HiddenUpdate, new[] { roomEntity.ToRoom() });
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
            if (!IsAuthorized(passcode, PasscodeType.Write))
            {
                throw new AuthenticationException("Wrong passcode");
            }

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
                PostSignalR(RoomChangeType.Update, new[] { roomEntity.ToRoom() });
            }

            return deletedEntities?.Select(x => x.ToOccupancy());
        }

        private async Task<Occupancy> ChangeOccupancy(long roomId, bool isOccupied)
        {
            // Update DB
            var repository = new OccupancyRepository();

            if (isOccupied)
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
            else
            {
                // Show the old occupancy (do not store exit time for now)
                var occupancyEntity = await repository.GetLatestOccupancy(roomId);
                return occupancyEntity?.ToOccupancy();
            }
        }

        private void PostSignalR(RoomChangeType changeType, IEnumerable<Room> rooms)
        {
            if (rooms == null)
            {
                return;
            }
            rooms = rooms.Where(x => x != null);

            if (changeType == RoomChangeType.HiddenUpdate)
            {
                var minTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMin"));
                var maxTime = TimeSpan.Parse(CloudConfigurationManager.GetSetting("UnprotectedLocalTimeMax"));
                var currentLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalTimeZone);
                var currentTimeOfDay = currentLocalTime.TimeOfDay;
                if (currentTimeOfDay < minTime)
                {
                    // Do not show
                    return;
                }
                if (currentTimeOfDay > maxTime)
                {
                    // Since this is a hidden update to rooms outside normal office hours, we only show 'exits' within the first 30min after office closed
                    if ((currentTimeOfDay - maxTime).TotalMinutes <= 30)
                    {
                        rooms = rooms.Where(x => !x.IsOccupied);
                    }
                    else
                    {
                        // Do not show
                        return;
                    }
                }
            }

            var roomsFiltered = rooms.ToList();
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
                case RoomChangeType.Update:
                case RoomChangeType.HiddenUpdate:
                    typeString = "updated";
                    break;
                case RoomChangeType.Delete:
                    typeString = "deleted";
                    break;
                default:
                    throw new Exception($"Unknown RoomChangeType: {changeType}");
            }
            var context = GlobalHost.ConnectionManager.GetHubContext<RoomsHub>();
            context.Clients.All.roomsChanged(typeString, roomsFiltered);
        }

        private enum PasscodeType
        {
            Read,
            Write
        }

        private bool IsAuthorized(string passcode, PasscodeType type)
        {
            // Check passcode if there is one
            string correctPasscode;
            switch (type)
            {
                case PasscodeType.Read:
                    correctPasscode = CloudConfigurationManager.GetSetting("ApiReadPasscode");
                    break;
                case PasscodeType.Write:
                    correctPasscode = CloudConfigurationManager.GetSetting("ApiWritePasscode");
                    break;
                default:
                    throw new Exception("Unknown passcode type");
            }
            return string.IsNullOrEmpty(correctPasscode) || passcode == correctPasscode;
        }
    }
}