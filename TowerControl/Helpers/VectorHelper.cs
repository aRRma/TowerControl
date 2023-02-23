using TowerControl.ApiModels.Base;

namespace TowerControl.Helpers
{
    public static class VectorHelper
    {
        public static double CalculateDistansce(Coords plane)
        {
            // метрах
            double airportX = 0;
            double airportY = 0;

            return Math.Round(Math.Sqrt(Math.Pow((plane.CoordX - airportX), 2) + Math.Pow((plane.CoordY - airportY), 2)), 2);
        }

        public static double CalculateHeight(Coords plane)
        {
            // метрах
            double airportZ = 0;

            return Math.Round(plane.CoordZ - airportZ, 2);
        }
    }
}
