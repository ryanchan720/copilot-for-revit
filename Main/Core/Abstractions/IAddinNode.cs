using Main.Core.Models;

namespace Main.Core.Abstractions
{
    public interface IAddinNode
    {
        bool Save { get; set; }

        bool Hidden { get; set; }
    }
}
