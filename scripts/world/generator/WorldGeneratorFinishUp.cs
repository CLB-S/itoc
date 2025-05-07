using Godot;
using System.Threading.Tasks;

namespace WorldGenerator;

public partial class WorldGenerator
{
    protected void AdjustTemperatureAccordingToHeight()
    {
        ReportProgress("Adjusting temperature");

        Parallel.ForEach(_cellDatas.Values, cell =>
        {
            if (cell.Height > 0)
                cell.Temperature -= cell.Height * Settings.TemperatureGradientWithAltitude;
        });
    }

    protected void SetBiomes()
    {
        ReportProgress("Setting biomes");

        Parallel.ForEach(_cellDatas.Values, cell =>
        {
            cell.Biome = BiomeLibrary.Instance.GetBiomeForConditions(cell.Temperature, cell.Precipitation,
                cell.Height);
        });
    }
}