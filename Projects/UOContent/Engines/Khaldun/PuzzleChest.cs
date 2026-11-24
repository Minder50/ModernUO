using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Commands;
using Server.Targeting;

namespace Server.Items
{
    public enum PuzzleChestCylinder
    {
        None = 0xE73,
        LightBlue = 0x186F,
        Blue = 0x186A,
        Green = 0x186B,
        Orange = 0x186C,
        Purple = 0x186D,
        Red = 0x186E,
        DarkBlue = 0x1869,
        Yellow = 0x1870
    }

    [SerializationGenerator(1)]
    public partial class PuzzleChestSolution
    {
        [DirtyTrackingEntity]
        private PuzzleChest _chest;

        [SerializableField(0)]
        private PuzzleChestCylinder[] _cylinders;

        public const int Length = 5;

        public PuzzleChestSolution(PuzzleChest chest) : this() => _chest = chest;

        public PuzzleChestSolution() =>
            _cylinders = [RandomCylinder(), RandomCylinder(), RandomCylinder(), RandomCylinder(), RandomCylinder()];

        public PuzzleChestSolution(
            PuzzleChestCylinder first, PuzzleChestCylinder second, PuzzleChestCylinder third,
            PuzzleChestCylinder fourth, PuzzleChestCylinder fifth
        ) => _cylinders = [first, second, third, fourth, fifth];

        public PuzzleChestSolution(PuzzleChestSolution solution)
        {
            _cylinders = new PuzzleChestCylinder[Length];
            solution.Cylinders.AsSpan().CopyTo(Cylinders);
        }

        protected PuzzleChestSolution(PuzzleChest chest, PuzzleChestSolution solution) : this(solution) =>
            _chest = chest;

        private void Deserialize(IGenericReader reader, int version)
        {
            var length = reader.ReadEncodedInt();
            Cylinders = new PuzzleChestCylinder[length];
            for (var i = 0; i < length; i++)
            {
                Cylinders[i] = (PuzzleChestCylinder)reader.ReadInt();
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (Cylinders.Length == Length)
            {
                return;
            }

            var cylinders = Cylinders;
            Cylinders = new PuzzleChestCylinder[Length];

            if (cylinders.Length > Length)
            {
                Cylinders = cylinders[..Length];
                return;
            }

            cylinders.CopyTo(Cylinders);
            for (var i = cylinders.Length; i < Length; i++)
            {
                Cylinders[i] = RandomCylinder();
            }
        }

        public PuzzleChestCylinder First
        {
            get => Cylinders[0];
            set => Cylinders[0] = value;
        }

        public PuzzleChestCylinder Second
        {
            get => Cylinders[1];
            set => Cylinders[1] = value;
        }

        public PuzzleChestCylinder Third
        {
            get => Cylinders[2];
            set => Cylinders[2] = value;
        }

        public PuzzleChestCylinder Fourth
        {
            get => Cylinders[3];
            set => Cylinders[3] = value;
        }

        public PuzzleChestCylinder Fifth
        {
            get => Cylinders[4];
            set => Cylinders[4] = value;
        }

        public static PuzzleChestCylinder RandomCylinder()
        {
            return Utility.Random(8) switch
            {
                0 => PuzzleChestCylinder.LightBlue,
                1 => PuzzleChestCylinder.Blue,
                2 => PuzzleChestCylinder.Green,
                3 => PuzzleChestCylinder.Orange,
                4 => PuzzleChestCylinder.Purple,
                5 => PuzzleChestCylinder.Red,
                6 => PuzzleChestCylinder.DarkBlue,
                _ => PuzzleChestCylinder.Yellow
            };
        }

        public bool Matches(PuzzleChestSolution solution, out int cylinders, out int colors)
        {
            cylinders = 0;
            colors = 0;

            Span<bool> matchesSrc = stackalloc bool[solution.Cylinders.Length];
            Span<bool> matchesDst = stackalloc bool[solution.Cylinders.Length];

            for (var i = 0; i < Cylinders.Length; i++)
            {
                if (Cylinders[i] == solution.Cylinders[i])
                {
                    cylinders++;
                    matchesSrc[i] = true;
                    matchesDst[i] = true;
                }
            }

            for (var i = 0; i < Cylinders.Length; i++)
            {
                if (matchesSrc[i])
                {
                    continue;
                }

                for (var j = 0; j < solution.Cylinders.Length; j++)
                {
                    if (Cylinders[i] == solution.Cylinders[j] && !matchesDst[j])
                    {
                        colors++;
                        matchesDst[j] = true;
                    }
                }
            }

            return cylinders == Cylinders.Length;
        }
    }

    [SerializationGenerator(1)]
    public partial class PuzzleChestSolutionAndTime : PuzzleChestSolution
    {
        private void MigrateFrom(V0Content content)
        {
            _when = content.When;
        }

        [AnchoredDateTime]
        [SerializableField(0)]
        private DateTime _when;

        public PuzzleChestSolutionAndTime(PuzzleChest chest, DateTime when, PuzzleChestSolution solution)
            : base(chest, solution) => _when = when;

        public PuzzleChestSolutionAndTime(PuzzleChest chest) : base(chest)
        {
        }
    }

    [SerializationGenerator(1)]
    public abstract partial class PuzzleChest : BaseTreasureChest
    {
        [Usage("SetPuzzleCount <cantidad>")]
        [Description("Establece el contador actual de cofres resueltos para pruebas.")]
        private static void SetPuzzleCount_OnCommand(CommandEventArgs e)
        {
            if (e.Length < 1 || !int.TryParse(e.GetString(0), out var count) || count < 0)
            {
                e.Mobile.SendMessage($"Contador actual: {_solvedChestCount}/{HarrowerAndGateAtSixChests}. Uso: [SetPuzzleCount <numero>");
                return;
            }

            _solvedChestCount = count;
            e.Mobile.SendMessage($"Contador de cofres resueltos ajustado a {_solvedChestCount}/{HarrowerAndGateAtSixChests}.");
        }

        private static void OnWorldLoad()
        {
            _solvedChestCount = 0;
            CloseFifthChestGates();

            foreach (var item in World.Items.Values)
            {
                if (item is Moongate gate &&
                    !gate.Deleted &&
                    !gate.Movable &&
                    gate.Hue == 0x482 &&
                    gate.Target == FifthChestGateDest &&
                    gate.TargetMap == Map.Felucca)
                {
                    gate.Delete();
                }
            }

            ResetAllPuzzleChests();
        }

        public static void Configure()
        {
            EventSink.WorldLoad += OnWorldLoad;
            CommandSystem.Register("ResetPuzzle", AccessLevel.GameMaster, ResetPuzzle_OnCommand);
            CommandSystem.Register("SetPuzzleCount", AccessLevel.GameMaster, SetPuzzleCount_OnCommand);
        }

        [Usage("ResetPuzzle")]
        [Description("Resetea el puzle de un PuzzleChest seleccionado.")]
        private static void ResetPuzzle_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Selecciona el cofre de puzle que quieres resetear.");
            e.Mobile.Target = new ResetPuzzleTarget();
        }

        private class ResetPuzzleTarget : Target
        {
            public ResetPuzzleTarget() : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is PuzzleChest chest && !chest.Deleted)
                {
                    chest.ResetPuzzle();
                    from.SendMessage("El puzle del cofre ha sido reseteado.");
                }
                else
                {
                    from.SendMessage("Debes seleccionar un cofre de puzle válido.");
                }
            }
        }
        public const int HintsCount = 3;
        public const int PortalAtFiveChests = 5;
        public const int HarrowerAndGateAtSixChests = 6;
        public static readonly TimeSpan CleanupTime = TimeSpan.FromHours(1.0);
        public static readonly TimeSpan GateDuration = TimeSpan.FromMinutes(5.0);

        private static readonly Point3D FifthChestGateDest = new(5451, 1374, 0);
        private static readonly Point3D SixthChestPlayerGateDest = new(7078, 727, 25);
        private static readonly Point3D HarrowerSpawnPos = new(6995, 733, 51);

        private static readonly List<PuzzleChest> _activeChests = [];
        private static readonly List<Moongate> _fifthChestGates = [];
        private static int _solvedChestCount;

        private TimerExecutionToken _cleanupTimerToken;

        [SerializableField(1, setter: "private")]
        private PuzzleChestCylinder[] _hints;

        [CanBeNull]
        [Tidy]
        [SerializableField(2)]
        private Dictionary<Mobile, PuzzleChestSolutionAndTime> _guesses;

        public PuzzleChest(int itemID) : base(itemID)
        {
            _hints = new PuzzleChestCylinder[HintsCount];
            EnsureSolution();
            TrackChest(this);
        }

        private static void TrackChest(PuzzleChest chest)
        {
            if (chest != null && !chest.Deleted && !_activeChests.Contains(chest))
            {
                _activeChests.Add(chest);
            }
        }

        public void EnsureSolution()
        {
            if (Solution == null || Solution.Cylinders.Length != PuzzleChestSolution.Length ||
                Array.Exists(Solution.Cylinders, c => c == 0 || c == PuzzleChestCylinder.None))
            {
                Solution = new PuzzleChestSolution(this);
                return;
            }

            if (_hints == null || _hints.Length != HintsCount ||
                Array.Exists(_hints, c => c == 0 || c == PuzzleChestCylinder.None))
            {
                InitHints();
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            TrackChest(this);
            EnsureSolution();
            StartCleanupTimer();
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _solution = new PuzzleChestSolution(this);
            _solution.Deserialize(reader);

            var length = reader.ReadEncodedInt();
            _hints = new PuzzleChestCylinder[HintsCount];
            for (var i = 0; i < length; i++)
            {
                var cylinder = (PuzzleChestCylinder)reader.ReadInt();

                if (i < HintsCount)
                {
                    _hints[i] = cylinder;
                }
            }

            var guessCount = reader.ReadEncodedInt();
            if (guessCount <= 0)
            {
                return;
            }

            _guesses = new Dictionary<Mobile, PuzzleChestSolutionAndTime>(guessCount);
            for (var i = 0; i < guessCount; i++)
            {
                var m = reader.ReadEntity<Mobile>();
                var guess = new PuzzleChestSolutionAndTime(this);
                guess.Deserialize(reader);
                if (m != null)
                {
                    _guesses[m] = guess;
                }
            }
        }

        [SerializableField(0, fieldChanged: nameof(OnSolutionChanged))]
        private PuzzleChestSolution _solution;

        private void OnSolutionChanged(PuzzleChestSolution oldValue, PuzzleChestSolution newValue)
        {
            if (newValue != null)
            {
                InitHints();
            }
        }

        public PuzzleChestCylinder FirstHint
        {
            get => _hints != null && _hints.Length > 0 ? _hints[0] : PuzzleChestCylinder.None;
            set
            {
                _hints ??= new PuzzleChestCylinder[HintsCount];
                _hints[0] = value;
            }
        }

        public PuzzleChestCylinder SecondHint
        {
            get => _hints != null && _hints.Length > 1 ? _hints[1] : PuzzleChestCylinder.None;
            set
            {
                _hints ??= new PuzzleChestCylinder[HintsCount];
                _hints[1] = value;
            }
        }

        public PuzzleChestCylinder ThirdHint
        {
            get => _hints != null && _hints.Length > 2 ? _hints[2] : PuzzleChestCylinder.None;
            set
            {
                _hints ??= new PuzzleChestCylinder[HintsCount];
                _hints[2] = value;
            }
        }

        public override string DefaultName => null;

        private void InitHints()
        {
            if (Solution == null || Solution.Cylinders.Length <= 1)
            {
                _hints = new PuzzleChestCylinder[HintsCount];
                return;
            }

            Span<PuzzleChestCylinder> cylinders = stackalloc PuzzleChestCylinder[Solution.Cylinders.Length - 1];
            Solution.Cylinders.AsSpan(1).CopyTo(cylinders);

            cylinders.Shuffle();
            _hints = cylinders[..HintsCount].ToArray();
        }

        public override void OnDelete()
        {
            StopCleanupTimer();
            _activeChests.Remove(this);
            base.OnDelete();
        }

        protected override void SetLockLevel()
        {
            LockLevel = ILockpickable.CannotPick;
        }

        public override bool CheckLocked(Mobile from)
        {
            if (from == null || from.Deleted)
            {
                return true;
            }

            if (Locked)
            {
                EnsureSolution();

                var last = GetLastGuess(from);
                var workingSolution = last != null
                    ? new PuzzleChestSolution(last)
                    : new PuzzleChestSolution(
                        PuzzleChestCylinder.None,
                        PuzzleChestCylinder.None,
                        PuzzleChestCylinder.None,
                        PuzzleChestCylinder.None,
                        PuzzleChestCylinder.None
                    );

                from.CloseGump<PuzzleGump>();
                from.SendGump(new PuzzleGump(from, this, workingSolution, 0));
                return true;
            }

            return false;
        }

        public PuzzleChestSolutionAndTime GetLastGuess(Mobile m) => m != null ? _guesses?.GetValueOrDefault(m) : null;

        public void SubmitSolution(Mobile m, PuzzleChestSolution solution)
        {
            if (m == null || m.Deleted || solution == null)
            {
                return;
            }

            EnsureSolution();

            if (solution.Matches(Solution, out var correctCylinders, out var correctColors))
            {
                LockPick(m);
                DisplayTo(m);
                RegisterSolvedChest(this, m);
            }
            else
            {
                ReplaceInGuesses(m, new PuzzleChestSolutionAndTime(this, Core.Now, solution));
                StartCleanupTimer();

                m.SendGump(new StatusGump(correctCylinders, correctColors));
                DoDamage(m);
            }
        }

        private static void CloseFifthChestGates()
        {
            for (var i = _fifthChestGates.Count - 1; i >= 0; i--)
            {
                var gate = _fifthChestGates[i];
                if (gate != null && !gate.Deleted)
                {
                    gate.Delete();
                }
            }

            _fifthChestGates.Clear();
        }

        public void ResetPuzzle()
        {
            if (Deleted)
            {
                return;
            }

            StopCleanupTimer();
            _guesses = null;
            Locked = true;
            SetLockLevel();
            Solution = new PuzzleChestSolution(this);
            InitHints();
        }

        private static void ResetAllPuzzleChests()
        {
            for (var i = _activeChests.Count - 1; i >= 0; i--)
            {
                var chest = _activeChests[i];
                if (chest == null || chest.Deleted)
                {
                    _activeChests.RemoveAt(i);
                    continue;
                }

                chest.ResetPuzzle();
            }
        }

        private static void RegisterSolvedChest(PuzzleChest chest, Mobile solver)
        {
            if (chest == null || solver == null || solver.Deleted)
            {
                return;
            }

            _solvedChestCount++;

            if (_solvedChestCount == PortalAtFiveChests)
            {
                SpawnFifthChestGateAtPlayer(
                    solver,
                    FifthChestGateDest,
                    Map.Felucca,
                    0x482,
                    "¡5.º cofre resuelto! Se ha abierto un portal a tus pies hacia (5451, 1374)."
                );
            }
            else if (_solvedChestCount >= HarrowerAndGateAtSixChests)
            {
                _solvedChestCount = 0;
                CloseFifthChestGates();
                ResetAllPuzzleChests();

                var playerMap = solver.Map;
                if (playerMap == null || playerMap == Map.Internal)
                {
                    return;
                }

                var destMap = Map.Felucca;
                var skullGate = new HarrowerSkullGate
                {
                    GateDuration = GateDuration
                };
                skullGate.MoveToWorld(solver.Location, playerMap);
                skullGate.ActivateFromPuzzleChest(
                    solver,
                    SixthChestPlayerGateDest,
                    destMap,
                    HarrowerSpawnPos,
                    GateDuration
                );

                solver.SendMessage(
                    "¡6.º cofre resuelto! Se ha generado el altar en tu posición (dura 5 minutos) hacia la Mazmorra y Harrower ha sido invocado en la Mazmorra."
                );
            }
            else
            {
                solver.SendMessage($"Cofres de puzle resueltos: {_solvedChestCount}/{HarrowerAndGateAtSixChests}.");
            }
        }

        private static void SpawnFifthChestGateAtPlayer(
            Mobile player, Point3D destLocation, Map destMap, int hue, string message
        )
        {
            var map = player.Map;
            if (map == null || map == Map.Internal || destMap == null || destMap == Map.Internal)
            {
                return;
            }

            var gate = new Moongate(destLocation, destMap)
            {
                Movable = false,
                Hue = hue
            };

            gate.MoveToWorld(player.Location, map);
            _fifthChestGates.Add(gate);

            Effects.SendLocationEffect(player.Location, map, 0x3728, 10, 10);
            Effects.PlaySound(player.Location, map, 0x1FE);
            player.SendMessage(message);
        }

        public static void DoDamage(Mobile to)
        {
            if (to == null || to.Deleted)
            {
                return;
            }

            switch (Utility.Random(4))
            {
                case 0:
                    {
                        Effects.SendLocationEffect(to, 0x113A, 20);
                        to.PlaySound(0x231);
                        to.LocalOverheadMessage(MessageType.Regular, 0x44, 1010523);
                        to.ApplyPoison(to, Poison.Regular);
                        break;
                    }
                case 1:
                    {
                        Effects.SendLocationEffect(to, 0x3709, 30);
                        to.PlaySound(0x54);
                        to.LocalOverheadMessage(MessageType.Regular, 0xEE, 1010524);
                        AOS.Damage(to, to, Utility.RandomMinMax(10, 40), 0, 100, 0, 0, 0);
                        break;
                    }
                case 2:
                    {
                        to.PlaySound(0x223);
                        to.LocalOverheadMessage(MessageType.Regular, 0x62, 1010525);
                        AOS.Damage(to, to, Utility.RandomMinMax(10, 40), 100, 0, 0, 0, 0);
                        break;
                    }
                default:
                    {
                        to.BoltEffect(0);
                        to.LocalOverheadMessage(MessageType.Regular, 0xDA, 1010526);
                        AOS.Damage(to, to, Utility.RandomMinMax(10, 40), 0, 0, 0, 0, 100);
                        break;
                    }
            }
        }

        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            StopCleanupTimer();
            _guesses = null;
        }

        private static void GetRandomAOSStats(out int attributeCount, out int min, out int max)
        {
            var rnd = Utility.Random(15);

            if (rnd < 1)
            {
                attributeCount = Utility.RandomMinMax(2, 6);
                min = 20;
                max = 70;
            }
            else if (rnd < 3)
            {
                attributeCount = Utility.RandomMinMax(2, 4);
                min = 20;
                max = 50;
            }
            else if (rnd < 6)
            {
                attributeCount = Utility.RandomMinMax(2, 3);
                min = 20;
                max = 40;
            }
            else if (rnd < 10)
            {
                attributeCount = Utility.RandomMinMax(1, 2);
                min = 10;
                max = 30;
            }
            else
            {
                attributeCount = 1;
                min = 10;
                max = 20;
            }
        }

        protected override void GenerateTreasure()
        {
            DropItem(new Gold(600, 900));

            using var gems = PooledRefList<Item>.Create();
            for (var i = 0; i < 9; i++)
            {
                var gem = Loot.RandomGem();
                var gemType = gem.GetType();

                for (var j = 0; j < gems.Count; j++)
                {
                    var listGem = gems[j];
                    if (listGem.GetType() == gemType)
                    {
                        listGem.Amount++;
                        gem.Delete();
                        break;
                    }
                }

                if (!gem.Deleted)
                {
                    gems.Add(gem);
                }
            }

            for (var i = 0; i < gems.Count; i++)
            {
                DropItem(gems[i]);
            }

            if (Utility.RandomDouble() < 0.2)
            {
                DropItem(new BagOfReagents());
            }

            for (var i = 0; i < 2; i++)
            {
                var item = Core.AOS ? Loot.RandomArmorOrShieldOrWeaponOrJewelry() : Loot.RandomArmorOrShieldOrWeapon();

                if (item is BaseWeapon weapon)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                    }
                    else
                    {
                        weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(6);
                        weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
                        weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);
                    }

                    DropItem(weapon);
                }
                else if (item is BaseArmor armor)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                    }
                    else
                    {
                        armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(6);
                        armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);
                    }

                    DropItem(armor);
                }
                else if (item is BaseHat hat)
                {
                    if (Core.AOS)
                    {
                        GetRandomAOSStats(out var attributeCount, out var min, out var max);
                        BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
                    }

                    DropItem(hat);
                }
                else if (item is BaseJewel jewel)
                {
                    GetRandomAOSStats(out var attributeCount, out var min, out var max);
                    BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);
                    DropItem(jewel);
                }
            }

            Solution = new PuzzleChestSolution(this);
        }

        private void StartCleanupTimer()
        {
            if (_cleanupTimerToken.Running || _guesses == null || _guesses.Count == 0)
            {
                return;
            }

            Timer.StartTimer(CleanupTime, CleanupTime, CleanupGuesses, out _cleanupTimerToken);
        }

        private void StopCleanupTimer()
        {
            _cleanupTimerToken.Cancel();
        }

        private void CleanupGuesses()
        {
            if (_guesses == null || _guesses.Count == 0)
            {
                StopCleanupTimer();
                return;
            }

            foreach (var (key, value) in _guesses)
            {
                if (Core.Now - value.When > CleanupTime)
                {
                    _guesses.Remove(key);
                }
            }

            if (_guesses.Count == 0)
            {
                _guesses = null;
                StopCleanupTimer();
            }
        }

        private class PuzzleGump : DynamicGump
        {
            private readonly int _check;
            private readonly PuzzleChest _chest;
            private readonly PuzzleChestSolution _solution;
            private readonly double _lockpicking;
            private readonly PuzzleChestSolution _lastGuess;

            public override bool Singleton => true;

            public PuzzleGump(Mobile from, PuzzleChest chest, PuzzleChestSolution solution, int check = 0) : base(50, 50)
            {
                _chest = chest;
                _solution = solution;
                _check = check;
                _lockpicking = from.Skills.Lockpicking.Base;
                _lastGuess = chest.GetLastGuess(from);
            }

            protected override void BuildLayout(ref DynamicGumpBuilder builder)
            {
                _chest.EnsureSolution();

                builder.SetNoDispose();
                builder.AddBackground(25, 0, 500, 410, 0x53);
                builder.AddImage(62, 20, 0x67);
                builder.AddLabel(80, 36, 0x481, "Puzzle Lock");
                builder.AddLabel(214, 26, 0x481, "Elige los cilindros para abrir el cierre.");
                builder.AddLabel(214, 46, 0x481, "Un mismo cilindro puede repetirse.");
                builder.AddLabel(214, 66, 0x481, "Cuidado: un fallo puede ser mortal.");

                AddLeftCylinderButton(ref builder, 62, 130, PuzzleChestCylinder.LightBlue, 10);
                AddLeftCylinderButton(ref builder, 62, 180, PuzzleChestCylinder.Blue, 11);
                AddLeftCylinderButton(ref builder, 62, 230, PuzzleChestCylinder.Green, 12);
                AddLeftCylinderButton(ref builder, 62, 280, PuzzleChestCylinder.Orange, 13);

                AddRightCylinderButton(ref builder, 451, 130, PuzzleChestCylinder.Purple, 14);
                AddRightCylinderButton(ref builder, 451, 180, PuzzleChestCylinder.Red, 15);
                AddRightCylinderButton(ref builder, 451, 230, PuzzleChestCylinder.DarkBlue, 16);
                AddRightCylinderButton(ref builder, 451, 280, PuzzleChestCylinder.Yellow, 17);

                if (_lockpicking >= 60.0)
                {
                    builder.AddLabel(160, 125, 0x481, "Pista de Lockpicking:");
                    builder.AddBackground(159, 150, 230, 95, 0x13EC);

                    if (_lockpicking >= 80.0)
                    {
                        builder.AddLabel(165, 157, 0x481, "En el 1er hueco:");
                        AddCylinder(ref builder, 330, 154, _chest.Solution.First);

                        builder.AddLabel(165, 185, 0x481, "En hueco desconocido:");
                        AddCylinder(ref builder, 330, 182, _chest.FirstHint);

                        if (_lockpicking >= 90.0)
                        {
                            AddCylinder(ref builder, 352, 182, _chest.SecondHint);
                        }

                        if (_lockpicking >= 100.0)
                        {
                            AddCylinder(ref builder, 330, 206, _chest.ThirdHint);
                        }
                    }
                    else
                    {
                        builder.AddLabel(165, 157, 0x481, "En hueco desconocido:");
                        AddCylinder(ref builder, 330, 160, _chest.FirstHint);

                        if (_lockpicking >= 70.0)
                        {
                            AddCylinder(ref builder, 352, 160, _chest.SecondHint);
                        }
                    }
                }

                if (_lastGuess != null)
                {
                    builder.AddLabel(127, 249, 0x481, "Intento anterior:");
                    builder.AddBackground(290, 247, 115, 25, 0x13EC);

                    AddCylinder(ref builder, 281, 254, _lastGuess.First);
                    AddCylinder(ref builder, 303, 254, _lastGuess.Second);
                    AddCylinder(ref builder, 325, 254, _lastGuess.Third);
                    AddCylinder(ref builder, 347, 254, _lastGuess.Fourth);
                    AddCylinder(ref builder, 369, 254, _lastGuess.Fifth);
                }

                AddPedestal(ref builder, 140, 270, _solution.First, 0, _check == 0);
                AddPedestal(ref builder, 195, 270, _solution.Second, 1, _check == 1);
                AddPedestal(ref builder, 250, 270, _solution.Third, 2, _check == 2);
                AddPedestal(ref builder, 305, 270, _solution.Fourth, 3, _check == 3);
                AddPedestal(ref builder, 360, 270, _solution.Fifth, 4, _check == 4);

                builder.AddButton(258, 370, 0xFA5, 0xFA7, 1);
            }

            private static void AddLeftCylinderButton(
                ref DynamicGumpBuilder builder, int x, int y, PuzzleChestCylinder cylinder, int buttonID
            )
            {
                builder.AddBackground(x, y, 30, 30, 0x13EC);
                AddCylinder(ref builder, x - 7, y + 10, cylinder);
                builder.AddButton(x + 38, y + 9, 0x13A8, 0x4B9, buttonID);
            }

            private static void AddRightCylinderButton(
                ref DynamicGumpBuilder builder, int x, int y, PuzzleChestCylinder cylinder, int buttonID
            )
            {
                builder.AddBackground(x, y, 30, 30, 0x13EC);
                AddCylinder(ref builder, x - 7, y + 10, cylinder);
                builder.AddButton(x - 26, y + 9, 0x13A8, 0x4B9, buttonID);
            }

            private static void AddPedestal(
                ref DynamicGumpBuilder builder, int x, int y, PuzzleChestCylinder cylinder, int switchID, bool initialState
            )
            {
                builder.AddItem(x, y, 0xB10);
                builder.AddItem(x - 23, y + 12, 0xB12);
                builder.AddItem(x + 23, y + 12, 0xB13);
                builder.AddItem(x, y + 23, 0xB11);

                if (cylinder != PuzzleChestCylinder.None && cylinder != 0)
                {
                    builder.AddItem(x, y + 2, 0x51A);
                    AddCylinder(ref builder, x - 1, y + 19, cylinder);
                }
                else
                {
                    builder.AddItem(x, y + 2, 0x521);
                }

                builder.AddRadio(x + 7, y + 65, 0x867, 0x86A, initialState, switchID);
            }

            private static void AddCylinder(ref DynamicGumpBuilder builder, int x, int y, PuzzleChestCylinder cylinder)
            {
                if (cylinder != PuzzleChestCylinder.None && cylinder != 0)
                {
                    builder.AddItem(x, y, (int)cylinder);
                }
            }

            public override void OnResponse(NetState sender, in RelayInfo info)
            {
                var from = sender?.Mobile;
                if (from == null || from.Deleted || _chest == null || _chest.Deleted || info.ButtonID == 0)
                {
                    return;
                }

                if (!from.CheckAlive())
                {
                    return;
                }

                if (from.AccessLevel == AccessLevel.Player &&
                    (from.Map != _chest.Map || !from.InRange(_chest.GetWorldLocation(), 2)))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500446);
                    return;
                }

                if (info.ButtonID == 1)
                {
                    _chest.SubmitSolution(from, _solution);
                    return;
                }

                if (info.Switches.IsEmpty)
                {
                    return;
                }

                var pedestal = info.Switches[0];
                if (pedestal < 0 || pedestal >= _solution.Cylinders.Length)
                {
                    return;
                }

                PuzzleChestCylinder cylinder = info.ButtonID switch
                {
                    10 => PuzzleChestCylinder.LightBlue,
                    11 => PuzzleChestCylinder.Blue,
                    12 => PuzzleChestCylinder.Green,
                    13 => PuzzleChestCylinder.Orange,
                    14 => PuzzleChestCylinder.Purple,
                    15 => PuzzleChestCylinder.Red,
                    16 => PuzzleChestCylinder.DarkBlue,
                    17 => PuzzleChestCylinder.Yellow,
                    _ => PuzzleChestCylinder.None
                };

                if (cylinder == PuzzleChestCylinder.None)
                {
                    return;
                }

                _solution.Cylinders[pedestal] = cylinder;
                from.SendGump(new PuzzleGump(from, _chest, _solution, pedestal));
            }
        }

        private class StatusGump : StaticGump<StatusGump>
        {
            private readonly int _correctCylinders;
            private readonly int _correctColors;

            public override bool Singleton => true;

            public StatusGump(int correctCylinders, int correctColors) : base(50, 50)
            {
                _correctCylinders = correctCylinders;
                _correctColors = correctColors;
            }

            protected override void BuildLayout(ref StaticGumpBuilder builder)
            {
                builder.AddBackground(15, 250, 305, 163, 0x53);
                builder.AddBackground(28, 265, 280, 133, 0xBB8);

                builder.AddLabel(35, 271, 0x481, "No has resuelto el puzle.");
                builder.AddLabel(35, 297, 0x481, "Colores bien colocados:");
                builder.AddLabelPlaceholder(285, 297, 0x44, "cylinders");

                builder.AddLabel(35, 323, 0x481, "Colores en otro hueco:");
                builder.AddLabelPlaceholder(285, 323, 0x44, "colors");

                builder.AddButton(152, 369, 0xFA5, 0xFA7, 0);
            }

            protected override void BuildStrings(ref GumpStringsBuilder builder)
            {
                builder.SetStringSlot("cylinders", $"{_correctCylinders}");
                builder.SetStringSlot("colors", $"{_correctColors}");
            }
        }
    }

    [Flippable(0xE41, 0xE40)]
    [SerializationGenerator(0)]
    public partial class MetalGoldenPuzzleChest : PuzzleChest
    {
        [Constructible]
        public MetalGoldenPuzzleChest() : base(0xE41)
        {
        }
    }

    [Flippable(0xE80, 0x9A8)]
    [SerializationGenerator(0)]
    public partial class StrongBoxPuzzle : PuzzleChest
    {
        [Constructible]
        public StrongBoxPuzzle() : base(0xE80)
        {
        }
    }
}
