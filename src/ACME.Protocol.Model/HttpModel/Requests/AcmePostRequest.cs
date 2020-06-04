﻿using System;
using System.Text.Json.Serialization;
using TGIT.ACME.Protocol.HttpModel.Converters;
using TGIT.ACME.Protocol.Infrastructure;

namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    [JsonConverter(typeof(AcmeJsonConverterFactory))]
    public class AcmePostRequest
    {
        public AcmePostRequest(DecodedHeader header, string signature)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Request body signature is missing", nameof(signature));

            Header = header;
            Signature = signature;
        }

        public DecodedHeader Header { get; private set; }
        public DecodedPayload? Payload { get; protected set; }

        public string Signature { get; private set; }
    }

    [JsonConverter(typeof(AcmeJsonConverterFactory))]
    public class AcmePostRequest<TPayload> : AcmePostRequest
        where TPayload : class?
    {
        public AcmePostRequest(DecodedHeader header, DecodedPayload<TPayload> payload, string signature)
            : base(header, signature)
        {
            Payload = payload;
        }

        public new DecodedPayload<TPayload>? Payload { 
            get => base.Payload as DecodedPayload<TPayload>; 
            private set => base.Payload = value; 
        }
    }
}
