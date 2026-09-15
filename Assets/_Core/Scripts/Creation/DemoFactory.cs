using System;
using System.Threading;
using CH013.Commons;
using CH013.Gameplay;
using Cysharp.Threading.Tasks;

namespace CH013.Creation
{
    public sealed class DemoFactory : IFactory<DemoView>
    {
        public UniTask<DemoView> Create(ICreateParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DemoView view ; 
            return UniTask.FromResult(view);
        }
    }
}
