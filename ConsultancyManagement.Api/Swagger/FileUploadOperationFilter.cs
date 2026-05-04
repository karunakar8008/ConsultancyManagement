using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ConsultancyManagement.Api.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null)
            return;

        var formFileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source.Id == "Form")
            .ToList();

        if (!formFileParameters.Any())
            return;

        operation.RequestBody.Content.Clear();

        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var parameter in formFileParameters)
        {
            var name = parameter.Name ?? parameter.ModelMetadata?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                continue;

            var isFile = parameter.ModelMetadata?.ModelType == typeof(IFormFile) ||
                         parameter.ModelMetadata?.ModelType == typeof(IFormFile[]);

            schema.Properties[name] = isFile
                ? new OpenApiSchema { Type = "string", Format = "binary" }
                : new OpenApiSchema { Type = "string" };

            if (parameter.IsRequired)
                schema.Required.Add(name);
        }

        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };
    }
}
