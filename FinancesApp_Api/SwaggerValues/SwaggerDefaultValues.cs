using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace FinancesApp_Api.SwaggerValues
{
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.ActionDescriptor.EndpointMetadata.OfType<ObsoleteAttribute>().Any();

            foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
            {
                var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();

                var response = operation.Responses[responseKey];

                foreach (var contentType in response.Content.Keys)
                {
                    if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                    {
                        response.Content.Remove(contentType);
                    }
                }
            }

            if (operation.Parameters == null)
            {
                return;
            }

            foreach (var paramenter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions.First(p => p.Name == paramenter.Name);

                if (description == null) continue;

                if (description.ModelMetadata != null)
                {
                    paramenter.Description ??= description.ModelMetadata.Description;

                    if (paramenter.Schema.Default == null && description.DefaultValue != null)
                    {
                        var json = JsonSerializer.Serialize(description.DefaultValue, description.ModelMetadata!.ModelType);
                        paramenter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
                    }
                }
                paramenter.Required |= description.IsRequired;
            }


        }
    }
}
