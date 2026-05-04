using Folminder.Models;
using System.Windows;

namespace Folminder.Platform
{
    public interface IWindowMetricsService
    {
        Rect GetWorkingArea();
        double GetNonVariableWidth();

        ShortPathBuilder CreateShortPathBuilder();
    }
}
