using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PokemonMarbleRace
{
    public enum RaceFormat { SingleRace, MiniGP, BestOfThree, Knockout }

    [Serializable]
    public sealed class RaceConfig
    {
        public string competitionId = "pokemon_marble_race";
        public string seasonId = "temporada_01";
        public string title = "Pokémon Marble Race";
        public RaceFormat format = RaceFormat.MiniGP;
        public int raceCount = 3;
        public int configuredSeed;
        public int trackVersion = 1;
        public List<string> participantIds = new List<string>();
        public float targetSeconds = 45f;
        public float maxSeconds = 60f;
        public bool typePowers = true;
        public float marbleSize = 0.55f;
    }

    [Serializable]
    public sealed class RacePlan
    {
        public string id;
        public int number;
        public int stage;
        public int heat;
        public string label;
        public int seed;
        public List<string> participantIds = new List<string>();
    }

    [Serializable]
    public sealed class RaceFinish
    {
        public string id;
        public int place;
        public float time;
        public bool finished;
        public float progress;
        public int rescues;
    }

    [Serializable]
    public sealed class RaceResult
    {
        public string raceId;
        public List<RaceFinish> finishOrder = new List<RaceFinish>();
    }

    [Serializable]
    public sealed class RaceStanding
    {
        public string id;
        public int position;
        public int points;
        public int wins;
        public int races;
        public float totalTime;
    }

    [Serializable]
    public sealed class RaceSeason
    {
        public int version = 1;
        public RaceConfig config;
        public int effectiveSeed;
        public List<RacePlan> plans = new List<RacePlan>();
        public List<RaceResult> results = new List<RaceResult>();
    }

    /// <summary>A configuration mismatch must never silently recover an older season.</summary>
    public sealed class RaceConfigurationException : InvalidOperationException
    {
        public RaceConfigurationException(string message) : base(message) { }
    }

    /// <summary>Unity-independent schedule, scoring and saved-season validation.</summary>
    public sealed class RaceCompetition
    {
        private RaceSeason data;
        private static readonly int[] GrandPrixPoints = { 10, 8, 6, 5, 4, 3, 2, 1 };

        private RaceCompetition(RaceSeason season) { data = season; }

        public int EffectiveSeed { get { return data.effectiveSeed; } }
        public int TotalRaces { get { return data.plans.Count; } }
        public int CompletedRaces { get { return data.results.Count; } }
        public RaceConfig Config { get { return CopyConfig(data.config); } }
        public bool IsComplete
        {
            get
            {
                if (data.config.format == RaceFormat.BestOfThree && HasDuelWinner()) return true;
                return data.results.Count == data.plans.Count;
            }
        }

        public RacePlan CurrentRace
        {
            get
            {
                if (IsComplete) return null;
                RacePlan plan = data.plans[data.results.Count];
                Require(plan.participantIds != null && plan.participantIds.Count >= 2,
                    "The next race is missing its qualified participants.");
                return CopyPlan(plan);
            }
        }

        public static void ValidateConfig(RaceConfig config)
        {
            Require(config != null, "The championship configuration is missing.");
            Require(ValidId(config.competitionId) && ValidId(config.seasonId),
                "Championship and season IDs accept 1–80 letters, numbers, underscores and hyphens.");
            Require(!string.IsNullOrWhiteSpace(config.title), "Enter the championship name.");
            Require(Enum.IsDefined(typeof(RaceFormat), config.format), "Choose a valid race format.");
            Require(config.trackVersion == 1, "This saved track algorithm version is not supported.");
            Require(config.participantIds != null && config.participantIds.Count >= 2,
                "Choose at least two distinct participants.");
            int limit = config.format == RaceFormat.Knockout ? 32 : 16;
            Require(config.participantIds.Count <= limit,
                "This format supports at most " + limit + " participants.");
            Require(config.participantIds.All(ValidId) &&
                config.participantIds.Distinct(StringComparer.Ordinal).Count() == config.participantIds.Count,
                "Participant IDs must be valid and unique.");
            Require(config.format != RaceFormat.BestOfThree || config.participantIds.Count == 2,
                "Best of Three requires exactly two participants.");
            Require(config.raceCount >= 1 && config.raceCount <= 30,
                "Choose from 1 to 30 races.");
            Require(Finite(config.targetSeconds) && config.targetSeconds >= 5f &&
                Finite(config.maxSeconds) && config.maxSeconds >= config.targetSeconds && config.maxSeconds <= 600f,
                "Race durations must be finite, with a target of at least 5 seconds and a maximum up to 600 seconds.");
            Require(Finite(config.marbleSize) && config.marbleSize >= 0.15f && config.marbleSize <= 1.2f,
                "Choose a marble diameter between 0.15 and 1.2 world units.");
        }

        public static RaceCompetition Create(RaceConfig config)
        {
            ValidateConfig(config);
            int seed = config.configuredSeed != 0 ? config.configuredSeed : Guid.NewGuid().GetHashCode() & int.MaxValue;
            return CreateWithSeed(config, seed);
        }

        public static RaceCompetition Restore(RaceSeason saved, RaceConfig expected)
        {
            ValidateConfig(expected);
            Require(saved != null && saved.version == 1 && saved.config != null,
                "The saved championship format is invalid or unsupported.");
            ValidateConfig(saved.config);
            if (!SameConfig(saved.config, expected))
                throw new RaceConfigurationException("The configuration differs from the saved championship. Restore the settings or create a new season.");
            Require(saved.config.configuredSeed == 0 || saved.effectiveSeed == saved.config.configuredSeed,
                "The saved track seed does not match the configured seed.");
            Require(saved.plans != null && saved.results != null,
                "The saved championship is missing its schedule or results.");

            RaceCompetition rebuilt = CreateWithSeed(saved.config, saved.effectiveSeed);
            foreach (RaceResult result in saved.results)
                Require(rebuilt.CompleteRace(result), "The saved championship contains a repeated race result.");
            Require(SamePlans(rebuilt.data.plans, saved.plans),
                "The saved schedule or its qualified participants were changed.");
            // The name is cosmetic; it can change without resetting the season.
            rebuilt.data.config.title = expected.title;
            return rebuilt;
        }

        public RaceSeason Snapshot() { return CopySeason(data); }

        /// <summary>Pure in-memory commit. Runtime code should use RaceStorage.SaveResult.</summary>
        public bool CompleteRace(RaceResult result)
        {
            Require(result != null && !string.IsNullOrEmpty(result.raceId), "The race result is missing.");
            RaceResult existing = data.results.Find(r => r.raceId == result.raceId);
            if (existing != null)
            {
                Require(SameResult(existing, result), "This race already has a different saved result.");
                return false;
            }
            Require(!IsComplete, "This championship is already complete.");
            RacePlan current = data.plans[data.results.Count];
            Require(result.raceId == current.id, "Finish the pending race before saving another race.");
            RaceResult canonical = ValidateResult(current, result);
            data.results.Add(canonical);
            if (data.config.format == RaceFormat.Knockout) ResolveNextKnockoutStage(current.stage);
            return true;
        }

        public List<RaceStanding> Standings()
        {
            var lines = new Dictionary<string, RaceStanding>(StringComparer.Ordinal);
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            var qualifiedStage = new Dictionary<string, int>(StringComparer.Ordinal);
            var lastPlace = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string id in data.config.participantIds)
            {
                lines[id] = new RaceStanding { id = id };
                positions[id] = 0;
                qualifiedStage[id] = 1;
                lastPlace[id] = int.MaxValue;
            }
            foreach (RacePlan plan in data.plans)
                foreach (string id in plan.participantIds) qualifiedStage[id] = Math.Max(qualifiedStage[id], plan.stage);
            foreach (RaceResult result in data.results)
            {
                RacePlan plan = data.plans.Find(p => p.id == result.raceId);
                foreach (RaceFinish finish in result.finishOrder)
                {
                    RaceStanding row = lines[finish.id];
                    row.points += data.config.format == RaceFormat.BestOfThree
                        ? (finish.place == 1 ? 1 : 0) : PointsForPlace(finish.place);
                    row.wins += finish.place == 1 ? 1 : 0;
                    row.races++;
                    row.totalTime += finish.time;
                    positions[finish.id] += finish.place;
                    if (plan.stage == qualifiedStage[finish.id]) lastPlace[finish.id] = finish.place;
                }
            }
            IEnumerable<RaceStanding> ordered;
            if (data.config.format == RaceFormat.Knockout)
            {
                ordered = lines.Values.OrderByDescending(r => qualifiedStage[r.id])
                    .ThenBy(r => lastPlace[r.id]).ThenByDescending(r => r.points)
                    .ThenByDescending(r => r.wins).ThenBy(r => r.totalTime)
                    .ThenBy(r => data.config.participantIds.IndexOf(r.id));
            }
            else
            {
                ordered = lines.Values.OrderByDescending(r => r.points).ThenByDescending(r => r.wins)
                    .ThenBy(r => positions[r.id]).ThenBy(r => r.totalTime)
                    .ThenBy(r => data.config.participantIds.IndexOf(r.id));
            }
            List<RaceStanding> table = ordered.ToList();
            for (int i = 0; i < table.Count; i++) table[i].position = i + 1;
            return table;
        }

        public string ToStandingsCsv()
        {
            var csv = new StringBuilder("POS,PARTICIPANT,PTS,WINS,RACES,TIME\n");
            foreach (RaceStanding row in Standings())
                csv.Append(row.position).Append(',').Append(row.id).Append(',').Append(row.points).Append(',')
                    .Append(row.wins).Append(',').Append(row.races).Append(',')
                    .Append(row.totalTime.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
            return csv.ToString();
        }

        public static int PointsForPlace(int place)
        {
            return place >= 1 && place <= GrandPrixPoints.Length ? GrandPrixPoints[place - 1] : 0;
        }

        internal void Accept(RaceSeason season) { data = CopySeason(season); }
        internal static bool SameState(RaceSeason a, RaceSeason b)
        {
            return a != null && b != null && a.version == b.version && SameConfig(a.config, b.config) &&
                a.effectiveSeed == b.effectiveSeed && SamePlans(a.plans, b.plans) &&
                a.results.Count == b.results.Count && a.results.Zip(b.results, SameResult).All(equal => equal);
        }

        private bool HasDuelWinner()
        {
            return data.results.SelectMany(r => r.finishOrder.Where(f => f.place == 1))
                .GroupBy(f => f.id).Any(group => group.Count() >= 2);
        }

        private static RaceCompetition CreateWithSeed(RaceConfig config, int seed)
        {
            var season = new RaceSeason { config = CopyConfig(config), effectiveSeed = seed };
            int raceTotal = config.format == RaceFormat.SingleRace ? 1 :
                config.format == RaceFormat.BestOfThree ? 3 : config.raceCount;
            if (config.format != RaceFormat.Knockout)
            {
                for (int i = 0; i < raceTotal; i++)
                {
                    RacePlan plan = MakePlan(seed, i + 1, 1, i + 1,
                        config.format == RaceFormat.BestOfThree ? "DUEL " + (i + 1) : "RACE " + (i + 1));
                    plan.participantIds = Shuffled(config.participantIds, DeriveSeed(seed, 5000 + i));
                    season.plans.Add(plan);
                }
            }
            else
            {
                int remaining = config.participantIds.Count;
                int stage = 1;
                while (true)
                {
                    int heats = (remaining + 7) / 8;
                    int next = 0;
                    for (int heat = 0; heat < heats; heat++)
                    {
                        int count = GroupSize(remaining, heats, heat);
                        string label = heats == 1 ? "FINAL" : "STAGE " + stage + " · HEAT " + (heat + 1) + "/" + heats;
                        season.plans.Add(MakePlan(seed, season.plans.Count + 1, stage, heat + 1, label));
                        next += Math.Max(1, count / 2);
                    }
                    if (heats == 1) break;
                    remaining = next;
                    stage++;
                }
                AssignStage(season, 1, config.participantIds);
            }
            var courseSeeds = new HashSet<int>();
            foreach (RacePlan plan in season.plans)
            {
                while (!courseSeeds.Add(plan.seed)) plan.seed = (plan.seed + 1) & int.MaxValue;
            }
            return new RaceCompetition(season);
        }

        private void ResolveNextKnockoutStage(int stage)
        {
            List<RacePlan> completedStage = data.plans.Where(p => p.stage == stage).ToList();
            if (completedStage.Count <= 1 || completedStage.Any(p => !data.results.Any(r => r.raceId == p.id))) return;
            var qualifiers = new List<string>();
            foreach (RacePlan plan in completedStage)
            {
                RaceResult result = data.results.Find(r => r.raceId == plan.id);
                qualifiers.AddRange(result.finishOrder.Take(Math.Max(1, plan.participantIds.Count / 2)).Select(f => f.id));
            }
            AssignStage(data, stage + 1, qualifiers);
        }

        private static void AssignStage(RaceSeason season, int stage, IEnumerable<string> participants)
        {
            List<RacePlan> plans = season.plans.Where(p => p.stage == stage).ToList();
            Require(plans.Count > 0, "The knockout schedule is missing its next stage.");
            List<string> shuffled = Shuffled(participants, DeriveSeed(season.effectiveSeed, 10000 + stage));
            int offset = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                int count = GroupSize(shuffled.Count, plans.Count, i);
                Require(count >= 2 && count <= 8, "A knockout heat has an invalid participant count.");
                plans[i].participantIds = shuffled.GetRange(offset, count);
                offset += count;
            }
        }

        private static int GroupSize(int participants, int groups, int index)
        {
            return participants / groups + (index < participants % groups ? 1 : 0);
        }

        private static RacePlan MakePlan(int seed, int number, int stage, int heat, string label)
        {
            return new RacePlan { id = "race_" + number.ToString("D4", CultureInfo.InvariantCulture), number = number,
                stage = stage, heat = heat, label = label, seed = DeriveSeed(seed, number) };
        }

        private static int DeriveSeed(int seed, int salt)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)salt * 0x9e3779b9u) ^ 0xa341316cu;
                value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
                return (int)(value & 0x7fffffffu);
            }
        }

        private static List<string> Shuffled(IEnumerable<string> input, int seed)
        {
            var result = new List<string>(input);
            uint state = unchecked((uint)seed) | 1u;
            for (int i = result.Count - 1; i > 0; i--)
            {
                unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
                int j = (int)(state % (uint)(i + 1));
                string item = result[i]; result[i] = result[j]; result[j] = item;
            }
            return result;
        }

        private static RaceResult ValidateResult(RacePlan plan, RaceResult result)
        {
            Require(result.finishOrder != null && result.finishOrder.Count == plan.participantIds.Count,
                "Every participant needs exactly one finish record.");
            Require(result.finishOrder.All(f => f != null), "A finish record is missing.");
            Require(new HashSet<string>(plan.participantIds, StringComparer.Ordinal).SetEquals(result.finishOrder.Select(f => f.id)) &&
                result.finishOrder.Select(f => f.id).Distinct(StringComparer.Ordinal).Count() == plan.participantIds.Count,
                "The result participants do not match the pending race.");
            Require(result.finishOrder.Select(f => f.place).OrderBy(p => p)
                .SequenceEqual(Enumerable.Range(1, plan.participantIds.Count)), "Finish places must be unique and consecutive.");
            foreach (RaceFinish finish in result.finishOrder)
            {
                Require(Finite(finish.time) && finish.time >= 0f && finish.time <= 86400f,
                    "A recorded race time is invalid.");
                Require(Finite(finish.progress) && finish.progress >= 0f && finish.progress <= 1f &&
                    (!finish.finished || finish.progress >= 0.999f), "A recorded race progress is invalid.");
                Require(finish.rescues >= 0 && finish.rescues <= 100000, "A recorded rescue count is invalid.");
            }
            return new RaceResult { raceId = result.raceId,
                finishOrder = result.finishOrder.OrderBy(f => f.place).Select(CopyFinish).ToList() };
        }

        private static bool SameConfig(RaceConfig a, RaceConfig b)
        {
            return a != null && b != null && a.competitionId == b.competitionId && a.seasonId == b.seasonId &&
                a.format == b.format && a.raceCount == b.raceCount && a.configuredSeed == b.configuredSeed &&
                a.trackVersion == b.trackVersion && a.targetSeconds == b.targetSeconds && a.maxSeconds == b.maxSeconds &&
                a.typePowers == b.typePowers && a.marbleSize == b.marbleSize &&
                a.participantIds != null && b.participantIds != null && a.participantIds.SequenceEqual(b.participantIds);
        }

        private static bool SamePlans(List<RacePlan> a, List<RacePlan> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                RacePlan x = a[i], y = b[i];
                if (x == null || y == null || x.id != y.id || x.number != y.number || x.stage != y.stage ||
                    x.heat != y.heat || x.label != y.label || x.seed != y.seed || x.participantIds == null ||
                    y.participantIds == null || !x.participantIds.SequenceEqual(y.participantIds)) return false;
            }
            return true;
        }

        private static bool SameResult(RaceResult a, RaceResult b)
        {
            if (a == null || b == null || a.raceId != b.raceId || a.finishOrder == null || b.finishOrder == null ||
                a.finishOrder.Count != b.finishOrder.Count || a.finishOrder.Any(f => f == null) || b.finishOrder.Any(f => f == null)) return false;
            var left = a.finishOrder.OrderBy(f => f.place).ToList();
            var right = b.finishOrder.OrderBy(f => f.place).ToList();
            for (int i = 0; i < left.Count; i++)
            {
                RaceFinish x = left[i], y = right[i];
                if (x.id != y.id || x.place != y.place || x.time != y.time || x.finished != y.finished ||
                    x.progress != y.progress || x.rescues != y.rescues) return false;
            }
            return true;
        }

        private static RaceSeason CopySeason(RaceSeason input)
        {
            return new RaceSeason { version = input.version, config = CopyConfig(input.config), effectiveSeed = input.effectiveSeed,
                plans = input.plans.Select(CopyPlan).ToList(), results = input.results.Select(r =>
                    new RaceResult { raceId = r.raceId, finishOrder = r.finishOrder.Select(CopyFinish).ToList() }).ToList() };
        }

        private static RaceConfig CopyConfig(RaceConfig input)
        {
            return new RaceConfig { competitionId = input.competitionId, seasonId = input.seasonId, title = input.title,
                format = input.format, raceCount = input.raceCount, configuredSeed = input.configuredSeed,
                trackVersion = input.trackVersion, targetSeconds = input.targetSeconds, maxSeconds = input.maxSeconds,
                typePowers = input.typePowers, marbleSize = input.marbleSize, participantIds = new List<string>(input.participantIds) };
        }

        private static RacePlan CopyPlan(RacePlan input)
        {
            return new RacePlan { id = input.id, number = input.number, stage = input.stage, heat = input.heat,
                label = input.label, seed = input.seed, participantIds = new List<string>(input.participantIds) };
        }

        private static RaceFinish CopyFinish(RaceFinish input)
        {
            return new RaceFinish { id = input.id, place = input.place, time = input.time, finished = input.finished,
                progress = input.progress, rescues = input.rescues };
        }

        private static bool ValidId(string value)
        {
            return !string.IsNullOrEmpty(value) && Regex.IsMatch(value, @"\A[A-Za-z0-9_-]{1,80}\z");
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
