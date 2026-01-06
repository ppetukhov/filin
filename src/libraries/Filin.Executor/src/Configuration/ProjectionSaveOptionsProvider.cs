using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Filin.Storage;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Configuration;

sealed class ProjectionSaveOptionsProvider(IServiceProvider serviceProvider) : IPostConfigureOptions<ProjectionSaveOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, ProjectionSaveOptions options)
    {
        options.Pipeline = string.IsNullOrEmpty(name)
            ? serviceProvider.GetRequiredService<ProjectionSavePipeline<ProjectionTuple>>()
            : serviceProvider.GetRequiredKeyedService<ProjectionSavePipeline<ProjectionTuple>>(name);

        options.ProjectionStore = string.IsNullOrEmpty(name)
            ? serviceProvider.GetRequiredService<IProjectionStore>()
            : serviceProvider.GetRequiredKeyedService<IProjectionStore>(name);
    }
}
