using System.Threading;
using Cysharp.Threading.Tasks;

namespace SE001.Commons
{
    public interface IFactory<T>
    {
        UniTask<T> Create(ICreateParameters parameters, CancellationToken cancellationToken);
    }
}
