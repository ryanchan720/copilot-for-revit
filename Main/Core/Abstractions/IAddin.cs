using Main.Core.Models;
using System;
using System.Collections.Generic;

namespace Main.Core.Abstractions
{
    public interface IAddin
    {
        Guid Id { get; }
        string Name { get; set; }
        List<RevitVersions.RevitVersion> RevitVersions { get; set; }
        string Description { get; set; }
        bool IsValidated { get; set; }
        bool IsActive { get; set; }
        bool IsCompatible { get; set; }
        List<IAddinNode> ItemList { get; set; }
    }
}