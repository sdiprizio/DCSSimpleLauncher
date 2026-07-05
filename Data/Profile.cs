using System.Collections.Generic;
using System.Linq;

namespace DCSSimpleLauncher.Data
{
    internal class Profile
    {
        public string Name { get; set; } = "Default";
        public bool UseVR { get; set; } = false;
        public bool UseLauncher { get; set; } = true;
        public IEnumerable<CompanionApp> PrelaunchApps { get; set; } = Enumerable.Empty<CompanionApp>();
    }
}
