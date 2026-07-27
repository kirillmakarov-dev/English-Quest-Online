using System;
using Cysharp.Threading.Tasks;

namespace EnglishQuest.UI.Loading
{
    public interface ILoadingScreenService : ISceneLoader
    {
        UniTask LoadSceneNetworkAsync(Func<UniTask> networkLoadAction);
    }
}

