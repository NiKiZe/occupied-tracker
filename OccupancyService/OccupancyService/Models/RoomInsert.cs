namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class RoomInsert
    {
        /// <summary>
        /// Id of room
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Generates a new room from these insert-parameters
        /// </summary>
        /// <returns></returns>
        public Room ToRoom()
        {
            return new Room
            {
                Id = Id,
                Description = Description
            };
        }
    }
}