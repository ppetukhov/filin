using System.Threading.Tasks.Dataflow;

namespace Filin.Storage.DataFlow;

/// <summary>
/// Defines a dataflow target block responsible for persisting or updating read model projections.
/// </summary>
/// <remarks>
/// This block acts exclusively as a consumer (sink) in the dataflow pipeline, 
/// processing incoming <see cref="Projection"/> data packets without producing an output.
/// </remarks>
public interface ISaveProjectionBlock : ITargetBlock<Projection>
{
}
