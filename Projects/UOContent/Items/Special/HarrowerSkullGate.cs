using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class HarrowerSkullGate : Item
    {
        private int _insertedSkulls;
        private Point3D _destLocation;
        private Map _destMap;
        private Point3D _harrowerSpawnLocation;
        private int _requiredSkulls;
        private TimeSpan _gateDuration;

        private Moongate _spawnedGate;
        private TimerExecutionToken _gateTimerToken;

        [CommandProperty(AccessLevel.GameMaster)]
        public int InsertedSkulls
        {
            get => _insertedSkulls;
            set => _insertedSkulls = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D DestLocation
        {
            get => _destLocation;
            set => _destLocation = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map DestMap
        {
            get => _destMap;
            set => _destMap = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D HarrowerSpawnLocation
        {
            get => _harrowerSpawnLocation;
            set => _harrowerSpawnLocation = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkulls
        {
            get => _requiredSkulls;
            set => _requiredSkulls = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan GateDuration
        {
            get => _gateDuration;
            set => _gateDuration = value;
        }

        [Constructible]
        public HarrowerSkullGate() : base(0x0DDA)
        {
            Movable = false;
            Name = "Altar de Calaveras de Campeón";

            _destLocation = new Point3D(7078, 727, 0);
            _destMap = Map.Felucca;
            _harrowerSpawnLocation = new Point3D(6995, 733, 0);
            _requiredSkulls = 1;
            _gateDuration = TimeSpan.FromMinutes(5.0);
        }

        public HarrowerSkullGate(Serial serial) : base(serial)
        {
        }

        public void ActivateFromPuzzleChest(
            Mobile summoner,
            Point3D newDestLocation,
            Map newDestMap,
            Point3D newHarrowerSpawnLocation,
            TimeSpan? duration = null
        )
        {
            if (Deleted)
            {
                return;
            }

            _destLocation = newDestLocation;
            _destMap = newDestMap ?? Map;
            _harrowerSpawnLocation = newHarrowerSpawnLocation;
            _insertedSkulls = 0;

            if (duration.HasValue)
            {
                _gateDuration = duration.Value;
            }

            ActivateGateAndSummonHarrower(summoner);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Deleted || !from.Alive)
            {
                return;
            }

            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045);
                return;
            }

            from.SendMessage("Selecciona una calavera de campeón para colocar en el altar.");
            from.Target = new SkullTarget(this);
        }

        public void TryInsertSkull(Mobile from, Item skull)
        {
            if (skull == null || skull.Deleted || !skull.IsChildOf(from.Backpack))
            {
                from.SendMessage("Debes tener la calavera de campeón en tu mochila.");
                return;
            }

            var typeName = skull.GetType().Name;
            if (!typeName.Contains("Skull", StringComparison.OrdinalIgnoreCase))
            {
                from.SendMessage("Eso no es una calavera de campeón válida.");
                return;
            }

            skull.Consume();
            _insertedSkulls++;

            Effects.PlaySound(Location, Map, 0x243);
            from.SendMessage($"Calavera colocada ({_insertedSkulls}/{Math.Max(1, _requiredSkulls)}).");

            if (_insertedSkulls >= Math.Max(1, _requiredSkulls))
            {
                _insertedSkulls = 0;
                ActivateGateAndSummonHarrower(from);
            }
        }

        private static bool HasActiveHarrower(Map map, Point3D location, int range = 64)
        {
            if (map == null || map == Map.Internal)
            {
                return false;
            }

            foreach (var mob in map.GetMobilesInRange(location, range))
            {
                if (mob is Harrower h && !h.Deleted && h.Alive)
                {
                    return true;
                }
            }

            return false;
        }

        private void ActivateGateAndSummonHarrower(Mobile summoner)
        {
            var map = _destMap ?? Map;
            if (map == null || map == Map.Internal || Map == null || Map == Map.Internal)
            {
                return;
            }

            _gateTimerToken.Cancel();
            CloseGate();

            var gate = new Moongate(_destLocation, map)
            {
                Movable = false,
                Hue = 0x482
            };
            gate.MoveToWorld(Location, Map);
            _spawnedGate = gate;

            if (_gateDuration > TimeSpan.Zero)
            {
                Timer.StartTimer(_gateDuration, ExpireGateAndAltar, out _gateTimerToken);
            }

            Effects.SendLocationEffect(Location, Map, 0x3728, 10, 10);
            Effects.PlaySound(Location, Map, 0x1FE);

            var spawnPos = _harrowerSpawnLocation == Point3D.Zero ? _destLocation : _harrowerSpawnLocation;
            if (!HasActiveHarrower(map, spawnPos))
            {
                var harrower = new Harrower();
                harrower.MoveToWorld(spawnPos, map);
                summoner?.SendMessage(
                    $"¡Se ha abierto el portal hacia ({_destLocation.X}, {_destLocation.Y}) y Harrower ha sido invocado!"
                );
            }
            else
            {
                summoner?.SendMessage(
                    $"¡Se ha abierto el portal hacia ({_destLocation.X}, {_destLocation.Y})! Harrower ya está presente."
                );
            }
        }

        private void ExpireGateAndAltar()
        {
            CloseGate();

            if (!Deleted)
            {
                Delete();
            }
        }

        private void CloseGate()
        {
            if (_spawnedGate != null && !_spawnedGate.Deleted)
            {
                _spawnedGate.Delete();
            }

            _spawnedGate = null;
        }

        public override void OnDelete()
        {
            _gateTimerToken.Cancel();
            CloseGate();
            base.OnDelete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(_insertedSkulls);
            writer.Write(_destLocation);
            writer.Write(_destMap);
            writer.Write(_harrowerSpawnLocation);
            writer.Write(_requiredSkulls);
            writer.Write(_gateDuration);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            _insertedSkulls = reader.ReadInt();
            _destLocation = reader.ReadPoint3D();
            _destMap = reader.ReadMap();
            _harrowerSpawnLocation = reader.ReadPoint3D();
            _requiredSkulls = reader.ReadInt();
            _gateDuration = reader.ReadTimeSpan();
        }

        private class SkullTarget : Target
        {
            private readonly HarrowerSkullGate _gate;

            public SkullTarget(HarrowerSkullGate gate) : base(2, false, TargetFlags.None)
            {
                _gate = gate;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_gate == null || _gate.Deleted)
                {
                    return;
                }

                if (targeted is Item item)
                {
                    _gate.TryInsertSkull(from, item);
                }
                else
                {
                    from.SendMessage("Selecciona una calavera de campeón.");
                }
            }
        }
    }
}
