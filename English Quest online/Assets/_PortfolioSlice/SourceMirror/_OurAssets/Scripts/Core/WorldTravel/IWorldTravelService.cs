using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public interface IWorldTravelService
{
    bool IsTraveling { get; }
    bool IsTravelingFor(PlayerInteraction interactor);
    bool CanTravel(PlayerInteraction interactor);
    UniTask TravelAsync(WorldMapDefinitionSO map, string fromNodeId, string toNodeId, PlayerInteraction interactor);
    UniTask FollowPartyTravelAsync(
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction interactor,
        string sessionName,
        WorldTravelSessionMode sessionMode,
        int presentationEpoch = 0);
}
