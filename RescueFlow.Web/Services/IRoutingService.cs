using System.Threading.Tasks;

namespace RescueFlow.Web.Services;

public record Location(double Latitude, double Longitude);
public record RouteEstimate(double DistanceKm, double EtaMinutes);

public interface IRoutingService
{
    Task<RouteEstimate> EstimateAsync(Location from, Location to);
}
