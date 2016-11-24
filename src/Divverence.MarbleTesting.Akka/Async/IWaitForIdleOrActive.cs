using System.Threading.Tasks;

namespace Divverence.MarbleTesting.Akka.Async
{
    public interface IWaitForIdleOrActive
    {
        Task Active();
        Task Idle();
    }
}