using System;
using System.Threading.Tasks;

namespace RescueFlow.Web.Services;

public class RoutingService : IRoutingService
{
    public Task<RouteEstimate> EstimateAsync(Location from, Location to)
    {
        // Straight-line Haversine MVP routing approximation as planned.
        // Can be replaced in the future by traffic-aware A* or an external routing engine (OSRM/Google Maps).
        double distanceKm = HaversineDistanceKm(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        
        // Simple ETA estimation assuming average speed of 50 km/h and a traffic multiplier of 1.2
        const double averageSpeedKmh = 50.0;
        const double trafficMultiplier = 1.2;
        double hours = distanceKm / averageSpeedKmh;
        double etaMinutes = hours * 60.0 * trafficMultiplier;

        return Task.FromResult(new RouteEstimate(distanceKm, etaMinutes));
    }

    private double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
            Math.Cos(ToRadians(lat1)) *
            Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2.0) *
            Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return earthRadiusKm * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
