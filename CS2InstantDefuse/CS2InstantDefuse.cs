using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace CS2InstantDefuse
{
    [MinimumApiVersion(84)]
    public class CS2InstantDefuse : BasePlugin
    {
        public override string ModuleName => "CS2InstantDefuse";
        public override string ModuleVersion => "1.1.1";
        public override string ModuleAuthor => "LordFetznschaedl";
        public override string ModuleDescription => "Simple Plugin that allowes the bomb to be instantly defused when no enemy is alive and no utility is in use";

        private float _bombPlantedTime = float.NaN;
        private bool _bombTicking = false;
        private int _molotovThreat = 0;
        private int _heThreat = 0;

        private bool _debug = false;

        private List<int> _infernoThreat = new List<int>();

        public override void Load(bool hotReload)
        {
            this.Log(PluginInfo());
            this.Log(this.ModuleDescription);

            this.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            this.RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
            this.RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);

            this.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);

            this.RegisterEventHandler<EventInfernoStartburn>(OnInfernoStartBurn);
            this.RegisterEventHandler<EventInfernoExtinguish>(OnInfernoExtinguish);
            this.RegisterEventHandler<EventInfernoExpire>(OnInfernoExpire);

            this.RegisterEventHandler<EventHegrenadeDetonate>(OnHeGrenadeDetonate);
            this.RegisterEventHandler<EventMolotovDetonate>(OnMolotovDetonate);

            // Comment in if you need to debug the defuse stuff.
            if (this._debug)
            {
                this.RegisterEventHandler<EventBombBeep>(OnBombBeep);
            }
            
        }

        [GameEventHandler]
        private HookResult OnBombBeep(EventBombBeep @event, GameEventInfo info)
        {
            var plantedBomb = this.FindPlantedBomb();
            if (plantedBomb == null)
            {
                this.Log("Planted bomb is null!");
                return HookResult.Continue;
            }

            Server.PrintToChatAll($"{plantedBomb.TimerLength - (Server.CurrentTime - this._bombPlantedTime)}");
            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            this.Debug($"OnGrenadeThrown: {@event.Weapon} - isBot: {@event.Userid?.IsBot}"); 

            if (@event.Weapon == "smokegrenade" || @event.Weapon == "flashbang" || @event.Weapon == "decoy")
            {
                return HookResult.Continue;
            }

            if(@event.Weapon == "hegrenade")
            {
                this._heThreat++;
            }

            if(@event.Weapon == "incgrenade" || @event.Weapon == "molotov")
            {
                this._molotovThreat++;
            }

            this.PrintThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnInfernoStartBurn(EventInfernoStartburn @event, GameEventInfo info)
        {
            this.Debug($"OnInfernoStartBurn");
            
            var infernoPosVector = new Vector3(@event.X, @event.Y, @event.Z);

            var plantedBomb = this.FindPlantedBomb();
            if(plantedBomb == null)
            {
                return HookResult.Continue;
            }

            var plantedBombVector = plantedBomb.CBodyComponent?.SceneNode?.AbsOrigin ?? null;
            if(plantedBombVector == null)
            {
                return HookResult.Continue;
            }

            var plantedBombVector3 = new Vector3(plantedBombVector.X, plantedBombVector.Y, plantedBombVector.Z);

            var distance = Vector3.Distance(infernoPosVector, plantedBombVector3);

            this.Log($"Inferno Distance to bomb: {distance}");

            if(distance > 250) 
            {
                return HookResult.Continue;
            }

            this._infernoThreat.Add(@event.Entityid);

            this.PrintThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnInfernoExtinguish(EventInfernoExtinguish @event, GameEventInfo info)
        {
            this.Debug($"OnInfernoExtinguish");
            
            this._infernoThreat.Remove(@event.Entityid);

            this.PrintThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnInfernoExpire(EventInfernoExpire @event, GameEventInfo info)
        {
            this.Debug($"OnInfernoExpire");
            
            this._infernoThreat.Remove(@event.Entityid);

            this.PrintThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnHeGrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
        {
            this.Debug($"OnHeGrenadeDetonate");
            
            if (this._heThreat > 0)
            {
                this._heThreat--;
            }

            this.PrintThreatLevel();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
        {
            this.Debug($"OnMolotovDetonate");
            
            if (this._molotovThreat > 0)
            {
                this._molotovThreat--;
            }

            this.PrintThreatLevel();

            return HookResult.Continue;
        }


        [GameEventHandler]
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            this.Debug($"OnRoundStart");
            
            this._bombPlantedTime = float.NaN;
            this._bombTicking = false;

            this._heThreat = 0;
            this._molotovThreat = 0;
            this._infernoThreat = new List<int>();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            this.Debug($"OnBombPlanted");
            
            this._bombPlantedTime = Server.CurrentTime;
            this._bombTicking = true;

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            this.Debug($"OnBombBeginDefuse");

            if (@event.Userid == null)
            {
                return HookResult.Continue;
            }

            if(!@event.Userid.IsValid)
            {
                return HookResult.Continue;
            }

            
            this.TryInstantDefuse(@event.Userid);

            return HookResult.Continue;
        }

        private bool TryInstantDefuse(CCSPlayerController player)
        {
            this.Log("Attempting instant defuse...");

            if (!this._bombTicking)
            {
                this.Log("Bomb is not planted!");
                return false;
            }

            this.PrintThreatLevel();

            if(this._heThreat > 0 || this._molotovThreat > 0 || this._infernoThreat.Any())
            {
                Server.PrintToChatAll($"Instant Defuse not possible because a grenade threat is active!");
                this.Log($"Instant Defuse not possible because a grenade threat is active!");
                return false;
            }

            var plantedBomb = this.FindPlantedBomb();
            if(plantedBomb == null)
            {
                this.Log("Planted bomb is null!");
                return false;
            }

            if(plantedBomb.CannotBeDefused)
            {
                this.Log("Planted bomb can not be defused!");
                return false;
            }

            if(this.TeamHasAlivePlayers(CsTeam.Terrorist))
            {
                this.Log($"Terrorists are still alive");
                return false;
            }

            var bombTimeUntilDetonation = plantedBomb.TimerLength - (Server.CurrentTime - this._bombPlantedTime);

            var defuseLength = plantedBomb.DefuseLength;
            if(defuseLength != 5 && defuseLength != 10)
            {
                defuseLength = player.PawnHasDefuser ? 5 : 10;
            }
            this.Log($"DefuseLength: {defuseLength}");

            bool bombCanBeDefusedInTime = (bombTimeUntilDetonation - defuseLength) >= 0.0f;

            if(!bombCanBeDefusedInTime)
            {
                Server.PrintToChatAll($"Defuse started with {bombTimeUntilDetonation.ToString("n3")} seconds left on the bomb. Not enough time left!");
                this.Log($"Defuse started with {bombTimeUntilDetonation.ToString("n3")} seconds left on the bomb. Not enough time left!");
                return false;
            }

            Server.NextFrame(() =>
            {
                IntPtr defuseCountdownPtr = Schema.GetSchemaValue<IntPtr>(plantedBomb.Handle, "CPlantedC4", "m_flDefuseCountDown");
                IntPtr defuseValuePtr = Schema.GetSchemaValue<IntPtr>(defuseCountdownPtr, "GameTime_t", "m_Value");
                byte[] floatBytes = BitConverter.GetBytes(0.0f);
                for (int i = 0; i < floatBytes.Length; i++) Marshal.WriteByte(defuseValuePtr, i, floatBytes[i]);

                Server.PrintToChatAll($"Instant Defuse was successful! Defuse started with {bombTimeUntilDetonation.ToString("n3")} seconds left on the bomb.");
                this.Log($"Instant Defuse was successful! [{bombTimeUntilDetonation.ToString("n3")}s left]");
            });
            
            return true;
        }

        private bool TeamHasAlivePlayers(CsTeam team)
        {
            var playerList = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            if (!playerList.Any())
            {
                this.Log("No player entities have been found!");
                throw new Exception("NNo player entities have been found!");
            }

            return playerList.Where(player => player.IsValid && player.TeamNum == (byte)team && player.PawnIsAlive).Any();
        }

        private CPlantedC4? FindPlantedBomb()
        {
            var plantedBombList = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");

            if (!plantedBombList.Any())
            {
                this.Log("No planted bomb entities have been found!");
                return null;
            }

            return plantedBombList.FirstOrDefault();
        }

        private void PrintThreatLevel()
        {
            this.Log($"Threat-Levels: HE [{this._heThreat}], Molotov [{this._molotovThreat}], Inferno [{this._infernoThreat.Count}]");
        }

        private string PluginInfo()
        {
            return $"Plugin: {this.ModuleName} - Version: {this.ModuleVersion} by {this.ModuleAuthor}";
        }

        private void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[{this.ModuleName}] {message}");
            Console.ResetColor();
        }

        private void Debug(string message)
        {
            if(!this._debug) 
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[{this.ModuleName}][DEBUG] {message}");
            Console.ResetColor();
        }
    }
}