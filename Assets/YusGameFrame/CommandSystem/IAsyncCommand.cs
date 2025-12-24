using System.Threading;
using System.Threading.Tasks;

namespace YusGameFrame
{
    public interface IAsyncCommand
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}

