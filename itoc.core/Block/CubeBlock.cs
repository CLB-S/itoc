using ITOC.Core.BlockModels;

namespace ITOC.Core;

public class CubeBlock : Block
{
    public CubeModelBase BlockModel { get; }

    public CubeBlock(Identifier id, string name, CubeModelBase blockModel, BlockProperties properties = null)
        : base(id, name, properties)
    {
        BlockModel = blockModel ?? throw new ArgumentNullException(nameof(blockModel));
    }
}