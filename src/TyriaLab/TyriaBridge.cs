using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

sealed record BridgeTelemetry(long EnhancedFrames, long DeclinedFrames, double GpuMs, double NetworkMs, double RouteMs);
sealed record BridgeState(
    string GameExe, bool GameRunning, bool NeuralInstalled, bool ManagedInstall,
    string Profile, bool NeuralEnabledAtStartup, double? ResolutionScale,
    bool Int4Mixed, int? AcoMode, BridgeTelemetry? Telemetry,
    string ScreenshotFolder, string[] Conflicts, string[] Actions, DateTimeOffset CheckedAt);

static class TyriaBridge {
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    static readonly Regex TelemetryPattern = new(
        @"frames:\s*(?<enhanced>\d+)\s+enhanced,\s*(?<declined>\d+)\s+declined;\s*(?<gpu>[0-9.]+)\s*ms on the GPU \(network\s*(?<network>[0-9.]+),\s*route\s*(?<route>[0-9.]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static readonly string[] Profiles = { "Efficient", "Balanced", "Cinematic", "Quality", "RSR1080p", "Off" };

    static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> Values = new(StringComparer.OrdinalIgnoreCase) {
        ["Efficient"] = Profile("1", "1", "1", "0.70", "1", "0.85", "0.85", "-0.50", "1", "0.60", "0", "1", "0.30", "2", "1", "1.00", "0.95", "1.10", "0.15", "0.05"),
        ["Quality"] = Profile("0", "0", "1", "0.85", "0", "0.85", "0.70", "-0.50", "0", "1.00", "0", "1", "0.20", "1", "0", "1.00", "1.00", "1.00", "0.00", "0.00"),
        ["Balanced"] = Profile("1", "1", "1", "0.75", "0", "0.75", "0.60", "-0.40", "0", "0.75", "0", "1", "0.25", "1", "0", "1.00", "1.00", "1.00", "0.00", "0.00"),
        ["Cinematic"] = Profile("1", "1", "1", "1.10", "2", "1.05", "1.10", "-0.20", "1", "0.75", "0", "1", "0.35", "2", "1", "1.20", "1.05", "1.35", "0.35", "0.20"),
        ["RSR1080p"] = Profile("1", "1", "1", "1.10", "2", "1.05", "1.10", "-0.20", "1", "1.00", "1", "1", "0.35", "2", "1", "1.20", "1.05", "1.35", "0.35", "0.20"),
        ["Off"] = new(StringComparer.OrdinalIgnoreCase) {
            ["RenoDX"] = new(StringComparer.OrdinalIgnoreCase) { ["Enabled"] = "0" }
        }
    };

    static Dictionary<string, Dictionary<string, string>> Profile(
        string int4, string aco, string enabled, string intensity, string style, string tone,
        string structure, string skin, string mask, string scale, string followInput, string passes,
        string highlightGuard, string stability, string lookMode, string lookStrength,
        string lookTone, string lookDetail, string halo, string haloBright) => new(StringComparer.OrdinalIgnoreCase) {
        ["Int4Mixed"] = new(StringComparer.OrdinalIgnoreCase) { ["Enabled"] = int4 },
        ["Network"] = new(StringComparer.OrdinalIgnoreCase) { ["ACO Mode"] = aco },
        ["RenoDX"] = new(StringComparer.OrdinalIgnoreCase) {
            ["Enabled"] = enabled, ["NeuralUplift"] = "1", ["NRIntensity"] = intensity,
            ["NRStyle"] = style, ["NRLocalTone"] = tone, ["NRLocalStructure"] = structure,
            ["NRSkinStructure"] = skin, ["NRAutoMask"] = mask, ["NRResolutionScale"] = scale,
            ["NRFollowInputRes"] = followInput, ["NRPasses"] = passes,
            ["NRStyleHighlightGuard"] = highlightGuard, ["NRDetailStability"] = stability,
            ["NRLookMode"] = lookMode, ["NRLookStrength"] = lookStrength,
            ["NRLookTone"] = lookTone, ["NRLookDetail"] = lookDetail,
            ["NRLookHalo"] = halo, ["NRLookHaloBrightSide"] = haloBright
        }
    };

    public static BridgeTelemetry? ParseTelemetry(string text) {
        Match? last = null;
        foreach (Match match in TelemetryPattern.Matches(text)) last = match;
        if (last == null) return null;
        var c = System.Globalization.CultureInfo.InvariantCulture;
        return new(
            long.Parse(last.Groups["enhanced"].Value, c), long.Parse(last.Groups["declined"].Value, c),
            double.Parse(last.Groups["gpu"].Value, c), double.Parse(last.Groups["network"].Value, c),
            double.Parse(last.Groups["route"].Value, c));
    }

    public static BridgeState Inspect(string gameExe) {
        gameExe = ValidateGameExe(gameExe);
        string dir = Path.GetDirectoryName(gameExe)!;
        string ini = Path.Combine(dir, "dlssnr-amd.ini");
        bool installed = File.Exists(ini) && File.Exists(Path.Combine(dir, "dlssnr_amd.addon64")) && File.Exists(Path.Combine(dir, "dxgi.dll"));
        bool managed = File.Exists(Path.Combine(dir, "dlssnr-amd-install.txt"));
        var parsed = File.Exists(ini) ? ReadIni(File.ReadAllLines(ini)) : new(StringComparer.OrdinalIgnoreCase);
        string Get(string section, string key, string fallback = "") => parsed.TryGetValue(section, out var s) && s.TryGetValue(key, out var value) ? value : fallback;
        bool enabled = Get("RenoDX", "Enabled") == "1";
        bool int4 = Get("Int4Mixed", "Enabled") == "1";
        int? aco = int.TryParse(Get("Network", "ACO Mode"), out int a) ? a : null;
        double? scale = double.TryParse(Get("RenoDX", "NRResolutionScale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double sc) ? sc : null;
        string profile = DetectProfile(parsed);
        string log = Path.Combine(dir, "dlssnr-amd.log");
        BridgeTelemetry? telemetry = File.Exists(log) ? ParseTelemetry(ReadTail(log, 512 * 1024)) : null;
        var conflicts = new List<string>();
        if (File.Exists(Path.Combine(dir, "d3d9.dll"))) conflicts.Add("d3d9.dll gefunden: alter DX9-Proxy/Add-on; heutiges GW2 nutzt ihn nicht.");
        if (File.Exists(Path.Combine(dir, "d3d11.dll"))) conflicts.Add("d3d11.dll im Spielordner gefunden: möglichen fremden DX11-Proxy prüfen.");
        if (File.Exists(Path.Combine(dir, "dxgi.dll")) && !managed) conflicts.Add("dxgi.dll ohne DLSSNR-AMD-Manifest: Herkunft vor Änderungen prüfen.");
        var actions = new List<string>();
        if (!installed) actions.Add("Neural-Pfad ist nicht vollständig installiert.");
        else if (!enabled) actions.Add("Profil Off ist gesetzt; zum Aktivieren Efficient, Balanced, Cinematic oder Quality anwenden und GW2 neu starten.");
        else actions.Add("Bereit: GW2 starten. F6 schaltet NR im laufenden Spiel um, F5 speichert ein A/B-Paar.");
        if (profile == "RSR1080p") actions.Add("AMD Software: RSR und AFMF aktivieren; GW2 exklusiv auf 1920x1080 stellen.");
        return new(gameExe, Process.GetProcessesByName("Gw2-64").Length > 0, installed, managed, profile, enabled, scale, int4, aco,
            telemetry, Path.Combine(dir, "NR Screenshots"), conflicts.ToArray(), actions.ToArray(), DateTimeOffset.Now);
    }

    public static string WriteStatus(string gameExe, string? reportPath = null) {
        var state = Inspect(gameExe);
        reportPath ??= Path.Combine(LocalSetup.Root, "reports", "bridge-status.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(state, Json));
        return JsonSerializer.Serialize(state, Json);
    }

    public static string Apply(string gameExe, string profile) {
        gameExe = ValidateGameExe(gameExe);
        if (!Values.TryGetValue(profile, out var wanted)) throw new ArgumentException("Profil: Efficient, Balanced, Cinematic, Quality, RSR1080p oder Off.");
        if (Process.GetProcessesByName("Gw2-64").Length > 0) throw new IOException("GW2 läuft. Bitte normal beenden; die Konfiguration wurde nicht verändert.");
        string dir = Path.GetDirectoryName(gameExe)!;
        string ini = Path.Combine(dir, "dlssnr-amd.ini");
        if (!File.Exists(ini) || !File.Exists(Path.Combine(dir, "dlssnr-amd-install.txt"))) throw new IOException("Keine verwaltete DLSSNR-AMD-Installation gefunden.");
        string[] original = File.ReadAllLines(ini);
        string[] changed = ApplyToLines(original, wanted);
        string backups = Path.Combine(dir, ".tyria-realism-backups"); Directory.CreateDirectory(backups);
        string backup = Path.Combine(backups, "bridge-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-dlssnr-amd.ini");
        File.Copy(ini, backup, false);
        string temp = ini + ".tyria-new";
        try {
            File.WriteAllLines(temp, changed, new System.Text.UTF8Encoding(false));
            ReadIni(File.ReadAllLines(temp));
            File.Move(temp, ini, true);
        } catch { if (File.Exists(temp)) File.Delete(temp); throw; }
        SaveLastGame(gameExe);
        return $"{profile} ist für den nächsten GW2-Start aktiv. Sicherung: {backup}";
    }

    internal static string[] ApplyToLines(string[] original, Dictionary<string, Dictionary<string, string>> wanted) {
        var changed = (string[])original.Clone(); string section = ""; var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < changed.Length; i++) {
            var sec = Regex.Match(changed[i], @"^\s*\[([^]]+)\]");
            if (sec.Success) { section = sec.Groups[1].Value; continue; }
            var pair = Regex.Match(changed[i], @"^\s*([^;#=]+?)\s*=");
            if (pair.Success && wanted.TryGetValue(section, out var values)) {
                string key = pair.Groups[1].Value.Trim();
                if (values.TryGetValue(key, out string? value)) { changed[i] = key + " = " + value; seen.Add(section + "/" + key); }
            }
        }
        foreach (var sectionPair in wanted) foreach (var keyPair in sectionPair.Value)
            if (!seen.Contains(sectionPair.Key + "/" + keyPair.Key)) throw new InvalidDataException($"Erwarteter INI-Eintrag fehlt: [{sectionPair.Key}] {keyPair.Key}");
        return changed;
    }

    public static void Launch(string gameExe) {
        gameExe = ValidateGameExe(gameExe); SaveLastGame(gameExe);
        if (Process.GetProcessesByName("Gw2-64").Length > 0) throw new IOException("GW2 läuft bereits.");
        Process.Start(new ProcessStartInfo(gameExe) { WorkingDirectory = Path.GetDirectoryName(gameExe), UseShellExecute = true });
    }

    public static void OpenAmdSoftware() {
        string[] candidates = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AMD", "CNext", "CNext", "RadeonSoftware.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AMD", "CNext", "CNext", "AMDRSServ.exe")
        };
        string? exe = candidates.FirstOrDefault(File.Exists);
        if (exe == null) throw new FileNotFoundException("AMD Software wurde am Standardpfad nicht gefunden. Bitte über das Startmenü öffnen.");
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
    }

    public static string ApplyMaterials(string gameExe, string preset) {
        gameExe = ValidateGameExe(gameExe);
        if (!new[] { "Natural", "Detail", "Off" }.Contains(preset)) throw new ArgumentException("Materialprofil: Natural, Detail oder Off.");
        string script = Path.Combine(LocalSetup.Root, "scripts", "Set-MaterialShader.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException("Material-Installer fehlt.", script);
        var start = new ProcessStartInfo("powershell.exe") {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-Preset", preset, "-GameExe", gameExe }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Material-Installer konnte nicht gestartet werden.");
        string output = process.StandardOutput.ReadToEnd(); string error = process.StandardError.ReadToEnd(); process.WaitForExit();
        if (process.ExitCode != 0) throw new IOException((output + Environment.NewLine + error).Trim());
        return output.Trim();
    }

    public static string? LoadLastGame() {
        string path = SettingsPath(); if (!File.Exists(path)) return null;
        try { using var doc = JsonDocument.Parse(File.ReadAllText(path)); string? value = doc.RootElement.GetProperty("gameExe").GetString(); return value != null && File.Exists(value) ? value : null; }
        catch { return null; }
    }
    public static void SaveLastGame(string gameExe) {
        string path = SettingsPath(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { gameExe = Path.GetFullPath(gameExe) }, Json));
    }
    static string SettingsPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TyriaRealism", "bridge.json");
    static string ValidateGameExe(string path) {
        path = Path.GetFullPath(path);
        if (!File.Exists(path) || !string.Equals(Path.GetFileName(path), "Gw2-64.exe", StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Bitte die echte Gw2-64.exe auswählen.", path);
        return path;
    }
    static Dictionary<string, Dictionary<string, string>> ReadIni(string[] lines) {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase); string section = "";
        foreach (string line in lines) {
            var sec = Regex.Match(line, @"^\s*\[([^]]+)\]");
            if (sec.Success) { section = sec.Groups[1].Value; if (!result.ContainsKey(section)) result[section] = new(StringComparer.OrdinalIgnoreCase); continue; }
            var pair = Regex.Match(line, @"^\s*([^;#=]+?)\s*=\s*(.*?)\s*$");
            if (pair.Success && section.Length > 0) result[section][pair.Groups[1].Value.Trim()] = pair.Groups[2].Value.Trim();
        }
        return result;
    }
    static string DetectProfile(Dictionary<string, Dictionary<string, string>> ini) {
        string Get(string s, string k) => ini.TryGetValue(s, out var section) && section.TryGetValue(k, out var value) ? value : "";
        if (Get("RenoDX", "Enabled") != "1") return "Off";
        string scale = Get("RenoDX", "NRResolutionScale"), follow = Get("RenoDX", "NRFollowInputRes"), style = Get("RenoDX", "NRStyle");
        if (follow == "1" && scale == "1.00") return "RSR1080p";
        if (style == "2") return "Cinematic";
        if (scale == "0.60" && Get("Int4Mixed", "Enabled") == "1") return "Efficient";
        if (scale == "0.75" && Get("Int4Mixed", "Enabled") == "1") return "Balanced";
        if (scale == "1.00") return "Quality";
        return "Benutzerdefiniert";
    }
    static string ReadTail(string path, int maxBytes) {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        stream.Seek(Math.Max(0, stream.Length - maxBytes), SeekOrigin.Begin);
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
}

sealed class BridgePanel : Form {
    readonly TextBox game = new() { Width = 560, ReadOnly = true };
    readonly ComboBox profile = new() { Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Label status = new() { AutoSize = false, Width = 760, Height = 205 };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 2500 };

    public BridgePanel() {
        Text = "Tyria Bridge 0.8 · Neural + Materials"; ClientSize = new Size(820, 565); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10.5f); BackColor = Color.FromArgb(22, 30, 39); ForeColor = Color.White;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "TYRIA BRIDGE", Font = new Font("Segoe UI", 23, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(new Label { Text = "Steuert den vorhandenen DX11-Neural-Pfad, zeigt echte Logwerte und startet GW2.\nFSR4/DLSS-SR werden nur angeboten, wenn ein Spiel die nötigen temporalen Eingaben besitzt.", AutoSize = true, Margin = new Padding(0, 5, 0, 12) });
        var gameRow = new FlowLayoutPanel { Width = 760, Height = 38 }; gameRow.Controls.Add(game);
        var browse = new Button { Text = "GW2 wählen", AutoSize = true }; Style(browse); gameRow.Controls.Add(browse); layout.Controls.Add(gameRow);
        profile.Items.AddRange(TyriaBridge.Profiles.Cast<object>().ToArray()); profile.SelectedItem = "Balanced";
        var row = new FlowLayoutPanel { Width = 760, Height = 78 }; row.Controls.Add(profile);
        var apply = new Button { Text = "Profil anwenden", AutoSize = true }; var launch = new Button { Text = "GW2 starten", AutoSize = true };
        var amd = new Button { Text = "AMD Software", AutoSize = true }; var shots = new Button { Text = "A/B-Bilder", AutoSize = true }; var refresh = new Button { Text = "Aktualisieren", AutoSize = true };
        foreach (var b in new[] { apply, launch, amd, shots, refresh }) { Style(b); row.Controls.Add(b); } layout.Controls.Add(row);
        var materialRow = new FlowLayoutPanel { Width = 760, Height = 45 };
        materialRow.Controls.Add(new Label { Text = "Materialdetails:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
        var materials = new ComboBox { Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
        materials.Items.AddRange(new object[] { "Natural", "Detail", "Off" }); materials.SelectedItem = "Natural"; materialRow.Controls.Add(materials);
        var applyMaterials = new Button { Text = "Materialprofil anwenden", AutoSize = true }; Style(applyMaterials); materialRow.Controls.Add(applyMaterials); layout.Controls.Add(materialRow);
        layout.Controls.Add(status);
        layout.Controls.Add(new Label { Text = "Im Spiel: F6 = Neural Rendering an/aus · F5 = identisches A/B-Paar · Home = ReShade\nMaterialdetails arbeiten räumlich und benötigen kein neuronales Netz. Änderung bei beendetem GW2.\nRSR1080p: zusätzlich in AMD Software RSR + AFMF aktivieren und GW2 auf 1920×1080 Vollbild stellen.", AutoSize = true });
        game.Text = TyriaBridge.LoadLastGame() ?? "";
        browse.Click += (_, _) => { using var d = new OpenFileDialog { Title = "Gw2-64.exe auswählen", Filter = "Guild Wars 2|Gw2-64.exe" }; if (d.ShowDialog() == DialogResult.OK) { game.Text = d.FileName; TyriaBridge.SaveLastGame(d.FileName); RefreshState(); } };
        apply.Click += (_, _) => Run(() => TyriaBridge.Apply(game.Text, (string)profile.SelectedItem!));
        applyMaterials.Click += (_, _) => Run(() => TyriaBridge.ApplyMaterials(game.Text, (string)materials.SelectedItem!));
        launch.Click += (_, _) => Run(() => { TyriaBridge.Launch(game.Text); return "GW2 wurde gestartet. Der Status aktualisiert sich automatisch."; });
        amd.Click += (_, _) => Run(() => { TyriaBridge.OpenAmdSoftware(); return "AMD Software geöffnet. RSR/AFMF werden bewusst nicht automatisch verändert."; });
        shots.Click += (_, _) => Run(() => { string folder = Path.Combine(Path.GetDirectoryName(game.Text)!, "NR Screenshots"); Directory.CreateDirectory(folder); Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true }); return "A/B-Ordner geöffnet."; });
        refresh.Click += (_, _) => RefreshState(); timer.Tick += (_, _) => RefreshState(false); timer.Start(); RefreshState();
    }
    static void Style(Button b) { b.ForeColor = Color.Black; b.BackColor = Color.FromArgb(225, 233, 237); b.UseVisualStyleBackColor = false; }
    void Run(Func<string> action) { try { status.Text = action(); RefreshState(false); } catch (Exception ex) { status.Text = ex.Message; } }
    void RefreshState(bool showErrors = true) {
        if (string.IsNullOrWhiteSpace(game.Text) || !File.Exists(game.Text)) { status.Text = "Bitte Gw2-64.exe auswählen."; return; }
        try {
            var s = TyriaBridge.Inspect(game.Text); profile.SelectedItem = TyriaBridge.Profiles.Contains(s.Profile) ? s.Profile : profile.SelectedItem;
            string telemetry = s.Telemetry == null ? "Noch keine auswertbare Telemetrie im Log." : $"Letzter Logwert: {s.Telemetry.EnhancedFrames:N0} NR-Frames · {s.Telemetry.GpuMs:0.00} ms GPU ({s.Telemetry.NetworkMs:0.00} Netz + {s.Telemetry.RouteMs:0.00} Route)";
            string conflicts = s.Conflicts.Length == 0 ? "Proxycheck: kein zusätzlicher d3d9-/d3d11-Konflikt erkannt." : "Hinweise: " + string.Join(" ", s.Conflicts);
            status.Text = $"{(s.GameRunning ? "GW2 läuft" : "GW2 beendet")} · Installation {(s.NeuralInstalled && s.ManagedInstall ? "bereit" : "unvollständig")}\nProfil: {s.Profile} · Startzustand NR {(s.NeuralEnabledAtStartup ? "AN" : "AUS")} · Modellskalierung {(s.ResolutionScale?.ToString("0.00") ?? "?")} · INT4 {(s.Int4Mixed ? "AN" : "AUS")} · ACO {s.AcoMode?.ToString() ?? "?"}\n{telemetry}\n{conflicts}\n{string.Join(" ", s.Actions)}";
        } catch (Exception ex) { if (showErrors) status.Text = ex.Message; }
    }
}

