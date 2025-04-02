extern alias SC;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    public interface IGeometry
    {
        Vec3 Center { get; }
        bool Traversable { get; }
        double Radius { get; }
        double MaxBoundingRadius { get; }

        double MaxBoundingRadiusSquared { get; }

    }
}