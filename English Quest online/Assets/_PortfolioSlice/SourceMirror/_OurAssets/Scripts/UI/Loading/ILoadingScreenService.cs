using System;
using Cysharp.Threading.Tasks;

namespace EnglishKingdom.UI.Loading
{
    public interface ILoadingScreenService : ISceneLoader
    {
        UniTask LoadSceneNetworkAsync(Func<UniTask> networkLoadAction);
    }
}
