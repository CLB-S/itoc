using ITOC.Core.BlockModels;

namespace ITOC.Core.Items.Models;

public class CubeBlockItemModel : IItemModel
{
    public CubeModelBase BlockModel { get; private set; }

    public CubeBlockItemModel(CubeBlock block)
    {
        ArgumentNullException.ThrowIfNull(block, nameof(block));
        BlockModel = block.BlockModel ?? throw new ArgumentNullException(nameof(block.BlockModel), "Block model cannot be null");
    }
}