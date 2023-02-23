using TowerControl.ApiModels.Base;
using TowerControl.Data.Base;

namespace TowerControl.Data.DTO
{
    public class PlaneDTO : IBaseEntity
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string? FlightNumber { get; set; }
        public PlaneStatusType Status { get; set; }
        public long? WayNumber { get; set; }
        public long LastCoordX { get; set; }
        public long LastCoordY { get; set; }
        public long LastCoordZ { get; set; }
        public long? LastSpeed { get; set; }

        public void SetCoordinate(Coords coords)
        {
            LastCoordX = coords.CoordX;
            LastCoordY = coords.CoordY;
            LastCoordZ = coords.CoordZ;
        }

        public Coords GetCoordinate()
        {
            return new Coords()
            {
                CoordX = LastCoordX,
                CoordY = LastCoordY,
                CoordZ = LastCoordZ,
            };
        }
    }
}
