using System.Threading.Tasks.Dataflow;

using Filin.Storage;

namespace Filin.DataFlow;

public interface ISaveProjectionBlock : ITargetBlock<Projection>
{
}
