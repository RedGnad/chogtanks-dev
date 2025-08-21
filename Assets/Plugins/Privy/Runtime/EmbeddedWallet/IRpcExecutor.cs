using System.Threading.Tasks;
using static Privy.RpcRequestData;
using static Privy.RpcResponseData;

namespace Privy
{
    internal interface IRpcExecutor
    {
        internal Task<IRpcResponseDetails> Evaluate(IRpcRequestDetails request);
    }
}