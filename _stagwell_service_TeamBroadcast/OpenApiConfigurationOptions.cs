using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

namespace Breezy.Muticaster
{
    public class OpenApiConfigurationOptions : IOpenApiConfigurationOptions
    {
        public OpenApiInfo Info { get; set; } = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "Virtual Hearing - focus Connect",
            Description = "Virtual Hearing focus Connect API by For The Record",
            TermsOfService = new Uri("https://github.com/Azure/azure-functions-openapi-extension"),
            Contact = new OpenApiContact()
            {
                Name = "For The Record",
                Email = "info@fortherecord.com",
                Url = new Uri("http://www.fortherecord.com/"),
            }
        };

        public List<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>();

        public OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V2;
        public bool IncludeRequestingHostName { get; set; } = false;
        public bool ForceHttp { get; set; } = false;
        public bool ForceHttps { get; set; } = false;
    }
}