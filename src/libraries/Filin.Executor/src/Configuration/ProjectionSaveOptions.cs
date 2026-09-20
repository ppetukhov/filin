using System.ComponentModel.DataAnnotations;

using Filin.Storage;

using Microsoft.Extensions.Options;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Configuration;

sealed class ProjectionSaveOptions
{
    [Required]
    public required ProjectionSavePipeline<ProjectionTuple> Pipeline { get; set; }

    [Range(1, 20)]
    public int ConcurentWorks { get; set; } = 10;

    [Required]
    public required IProjectionStore ProjectionStore { get; set; }
}

[OptionsValidator]
sealed partial class ProjectionSaveOptionsValidator : IValidateOptions<ProjectionSaveOptions>
{

}
