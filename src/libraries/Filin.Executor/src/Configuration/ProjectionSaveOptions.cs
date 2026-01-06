using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

using Filin.Storage;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Configuration;

sealed class ProjectionSaveOptions
{
    [Required]
    public required ProjectionSavePipeline<ProjectionTuple> Pipeline { get; set; }

    [Range(1, 1000)]
    public int ConcurentWorks { get; set; } = 10;

    [Required]
    public required IProjectionStore ProjectionStore { get; set; }
}

[OptionsValidator]
sealed partial class ProjectionSaveOptionsValidator : IValidateOptions<ProjectionSaveOptions>
{

}
