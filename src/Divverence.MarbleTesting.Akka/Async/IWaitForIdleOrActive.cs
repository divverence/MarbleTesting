using System.Threading.Tasks;

namespace Divverence.MarbleTesting.Akka.Async
{
    internal interface IWaitForIdleOrActive
    {
        Task Active();
        Task Idle();
    }
}