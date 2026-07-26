using Cysharp.Threading.Tasks;
using Fusion;

public interface INetworkSessionService
{
    NetworkRunner Runner { get; }

    UniTask StartSharedSession(string sessionName, int sceneBuildIndex);
    UniTask StartSharedSession(string sessionName, int sceneBuildIndex, int maxPlayers, bool enableClientSessionCreation = true);
    UniTask StartSharedSession(NetworkSessionProfile profile);
    UniTask JoinSharedSession(string sessionName);
    UniTask JoinOpenWorldAsync(string openWorldSceneName);
    UniTask JoinOpenWorldAsync(NetworkSessionProfile profile);
    UniTask Disconnect();
}
