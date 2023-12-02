using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS2InstantDefuse.Configs
{
    public class CS2InstantDefuseConfig : BasePluginConfig
    {
        public override int Version { get; set; } = 1;
        public bool DetonateBombIfNotEnoughTimeForDefuse { get; set; } = true;


        public CS2InstantDefuseConfig() {}
    }
}
