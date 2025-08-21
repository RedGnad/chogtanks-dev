using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Privy
{
    public class IframeRequest<T>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class ReadyRequestData
    {
    }

    public class CreateEthereumWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [CanBeNull]
        [JsonProperty("solanaAddress")]
        public string SolanaAddress;
    }

    public class CreateSolanaWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [CanBeNull]
        [JsonProperty("ethereumAddress")]
        public string EthereumAddress;
    }

    public class CreateAdditionalWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;

        [JsonProperty("hdWalletIndex")]
        public int WalletIndex;
    }

    public class ConnectWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;
    }

    public class RecoverWalletRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;
    }

    public class RpcRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        [JsonProperty("chainType")]
        public ChainType ChainType;

        [JsonProperty("entropyId")]
        public string EntropyId;

        [JsonProperty("entropyIdVerifier")]
        public EntropyIdVerifierName EntropyIdVerifier;

        [JsonProperty("hdWalletIndex")]
        public int WalletIndex;

        /// <summary>
        /// The details of the RPC request.
        /// </summary>
        /// <seealso cref="EthereumRpcRequestDetails"/>
        /// <seealso cref="SolanaRpcRequestDetails"/>
        [JsonProperty("request")]
        public IRpcRequestDetails Request;

        public interface IRpcRequestDetails
        {
        }

        public class EthereumRpcRequestDetails : IRpcRequestDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("params")]
            public string[] Params;
        }

        public class SolanaRpcRequestDetails : IRpcRequestDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("params")]
            public SolanaSignMessageRpcRequestParams Params;
        }

        public class SolanaSignMessageRpcRequestParams
        {
            [JsonProperty("message")]
            public string Message;
        }
    }

    internal class UserSignerSignRequestData
    {
        [JsonProperty("accessToken")]
        public string AccessToken;

        /// <summary>
        /// A base64 encoding of the bytes to sign over
        /// </summary>
        [JsonProperty("message")]
        public string Message;
    }

    //Responses

    //Base Class, used to parse event and id
    public class IframeResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }
    }

    public class IframeResponseSuccess<T> : IframeResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class ErrorDetails
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }


    public class IframeResponseError : IframeResponse
    {
        [JsonProperty("error")]
        public ErrorDetails Error { get; set; }
    }

    public class ReadyResponseData
    {
        // Add specific properties for iframe ready data
    }


    public class CreateEthereumWalletResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }

    public class CreateSolanaWalletResponseData
    {
        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }
    }

    public class CreateAdditionalWalletResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("hdWalletIndex")]
        public string WalletIndex { get; set; }
    }

    public class ConnectWalletResponseData
    {
        [JsonProperty("entropyId")]
        public string EntropyId { get; set; }
    }

    public class RecoverWalletResponseData
    {
        [JsonProperty("entropyId")]
        public string EntropyId { get; set; }
    }

    public class RpcResponseData
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// The details of the RPC response.
        /// </summary>
        /// <seealso cref="EthereumRpcResponseDetails"/>
        /// <seealso cref="SolanaRpcResponseDetails"/>
        [JsonProperty("response")]
        [JsonConverter(typeof(RpcResponseDetailsConverter))]
        public IRpcResponseDetails Response;

        public interface IRpcResponseDetails
        {
        }

        private class RpcResponseDetailsConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // Not necessary for a "response" type.
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                var jo = JObject.Load(reader);

                var method = jo.GetValue("method");

                // FIXME: Need a much better way to deal with polymorphic parsing here,
                // possibly higher up to take "chainType" into account.
                if (method != null && method.Type == JTokenType.String && method.Value<string>() == "signMessage")
                {
                    // Treat it as a solana RPC
                    return jo.ToObject<SolanaRpcResponseDetails>(serializer);
                }

                return jo.ToObject<EthereumRpcResponseDetails>(serializer);
            }

            public override bool CanConvert(Type objectType) => objectType == typeof(IRpcResponseDetails);
        }

        public class EthereumRpcResponseDetails : IRpcResponseDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("data")]
            public string Data;
        }

        public class SolanaRpcResponseDetails : IRpcResponseDetails
        {
            [JsonProperty("method")]
            public string Method;

            [JsonProperty("data")]
            public SolanaSignMessageRpcResponseData Data;
        }

        public class SolanaSignMessageRpcResponseData
        {
            [JsonProperty("signature")]
            public string Signature;
        }
    }

    internal class UserSignerSignResponseData
    {
        /// <summary>
        /// A base64 encoding of the resulting signature
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChainType
    {
        [EnumMember(Value = "ethereum")]
        Ethereum,

        [EnumMember(Value = "solana")]
        Solana
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EntropyIdVerifierName
    {
        // In contrast with EntropyIdVerifier, this enum is public, for compatibility with the public classes here.
        [EnumMember(Value = "ethereum-address-verifier")]
        EthereumAddress,

        [EnumMember(Value = "solana-address-verifier")]
        SolanaAddress
    }

    internal static class EntropyIdVerifierNameExtensions
    {
        internal static EntropyIdVerifierName ToVerifierName(this EntropyIdVerifier verifier)
        {
            return verifier switch
            {
                EntropyIdVerifier.EthereumAddress => EntropyIdVerifierName.EthereumAddress,
                EntropyIdVerifier.SolanaAddress => EntropyIdVerifierName.SolanaAddress
            };
        }
    }
}