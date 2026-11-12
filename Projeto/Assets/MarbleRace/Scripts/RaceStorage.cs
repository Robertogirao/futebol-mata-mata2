using System;
using System.IO;
using System.Text;

namespace PokemonMarbleRace
{
    public interface IRaceSerializer
    {
        string Serialize(RaceSeason data);
        RaceSeason Deserialize(string json);
    }

    /// <summary>Atomic, recoverable storage. A result reaches the live model only after its file is durable.</summary>
    public sealed class RaceStorage
    {
        private readonly IRaceSerializer serializer;
        public string Folder { get; private set; }
        public string SavePath { get { return Path.Combine(Folder, "championship.json"); } }
        public string BackupPath { get { return Path.Combine(Folder, "championship.backup.json"); } }
        public bool RecoveredFromBackup { get; private set; }
        public string RecoveryMessage { get; private set; }

        public RaceStorage(string folder, IRaceSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Choose a championship save folder.");
            if (serializer == null) throw new ArgumentNullException("serializer");
            Folder = Path.GetFullPath(folder);
            this.serializer = serializer;
        }

        public RaceCompetition LoadOrCreate(RaceConfig config)
        {
            RaceCompetition.ValidateConfig(config);
            Directory.CreateDirectory(Folder);
            using (AcquireLock())
            {
                if (File.Exists(SavePath) || File.Exists(BackupPath)) return LoadExisting(config);
                RaceCompetition created = RaceCompetition.Create(config);
                WriteAtomic(created.Snapshot(), false);
                return created;
            }
        }

        public bool SaveResult(RaceCompetition competition, RaceResult result)
        {
            if (competition == null) throw new ArgumentNullException("competition");
            Directory.CreateDirectory(Folder);
            using (AcquireLock())
            {
                RaceCompetition stored = LoadExisting(competition.Config);
                if (!RaceCompetition.SameState(competition.Snapshot(), stored.Snapshot()))
                    throw new InvalidOperationException("The saved championship changed in another session. Reload before continuing.");
                RaceCompetition candidate = RaceCompetition.Restore(competition.Snapshot(), competition.Config);
                if (!candidate.CompleteRace(result)) return false;
                RaceSeason snapshot = candidate.Snapshot();
                WriteAtomic(snapshot, false);
                competition.Accept(snapshot);
                return true;
            }
        }

        public void ExportStandings(RaceCompetition competition)
        {
            if (competition == null) throw new ArgumentNullException("competition");
            Directory.CreateDirectory(Folder);
            using (AcquireLock()) WriteTextAtomic(Path.Combine(Folder, "standings.csv"), competition.ToStandingsCsv(), null);
        }

        private FileStream AcquireLock()
        {
            return new FileStream(Path.Combine(Folder, "championship.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
        }

        private RaceCompetition LoadExisting(RaceConfig config)
        {
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                throw new InvalidOperationException("The saved championship file is missing. Preserve the folder and choose a new season if it cannot be restored.");
            Exception mainError = null;
            if (File.Exists(SavePath))
            {
                try { return Read(SavePath, config); }
                catch (RaceConfigurationException) { throw; }
                catch (Exception error) { mainError = error; }
            }
            if (!File.Exists(BackupPath))
                throw new InvalidDataException("The saved championship is unreadable and no backup is available. The files were preserved.", mainError);
            RaceCompetition recovered;
            try { recovered = Read(BackupPath, config); }
            catch (RaceConfigurationException) { throw; }
            catch (Exception backupError)
            {
                throw new InvalidDataException("Both championship files are unreadable. The original files were preserved.", backupError);
            }
            if (File.Exists(SavePath))
            {
                string corrupt = Path.Combine(Folder, "championship.corrupt." + Guid.NewGuid().ToString("N") + ".json");
                File.Copy(SavePath, corrupt, false);
            }
            WriteAtomic(recovered.Snapshot(), true);
            RecoveredFromBackup = true;
            RecoveryMessage = "The last valid backup was restored. A race saved after that backup may need to be repeated.";
            return recovered;
        }

        private RaceCompetition Read(string path, RaceConfig expected)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("The championship file is empty.");
            return RaceCompetition.Restore(serializer.Deserialize(json), expected);
        }

        private void WriteAtomic(RaceSeason season, bool preserveBackup)
        {
            string json = serializer.Serialize(season);
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("The championship serializer returned no data.");
            // Verify serialization before touching the current save.
            RaceCompetition roundTrip = RaceCompetition.Restore(serializer.Deserialize(json), season.config);
            if (!RaceCompetition.SameState(season, roundTrip.Snapshot()))
                throw new InvalidDataException("The championship serializer changed the saved state.");
            WriteTextAtomic(SavePath, json, preserveBackup ? null : BackupPath);
        }

        private static void WriteTextAtomic(string destination, string text, string backup)
        {
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                    { writer.Write(text); writer.Flush(); }
                    stream.Flush(true);
                }
                if (File.Exists(destination)) File.Replace(temporary, destination, backup);
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
