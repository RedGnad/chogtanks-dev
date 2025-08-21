namespace Privy
{
    public class RpcRequest
    {
        public string Method { get; set; }
        public string[] Params { get; set; }
    }

    public class RpcResponse
    {
        public string Method { get; set; }
        public string Data { get; set; }
    }
}