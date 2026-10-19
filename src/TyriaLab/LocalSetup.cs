using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

static class LocalSetup {
    public static readonly string Root=Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName;
    public static string Runtime=>Path.Combine(Root,"runtime","Magpie");
    const string ArchiveHash="a3f774dce9d2e050fb0934c362d51d8db459a2e79e6f937bdb86852312899a9e";
    const string ExeHash="6f65fcc851d1178eaddefa2b00335dc004b526028e1da3ac55df4209e9b843bc";
    const string Url="https://github.com/Blinue/Magpie/releases/download/v0.13.0/Magpie-v0.13.0-x64.zip";
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    public static string Hash(string path){using var f=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant();}
    public static async Task Install() {
        string exe=Path.Combine(Runtime,"Magpie.exe");
        if(File.Exists(exe)){if(Hash(exe)!=ExeHash)throw new IOException("Magpie.exe wurde verändert; keine Ausführung. Bitte neue isolierte Kopie verwenden.");return;}
        // Never merge an installation into an unknown/partial directory.
        if(Directory.Exists(Runtime)&&Directory.EnumerateFileSystemEntries(Runtime).Any())throw new IOException("Unvollständiger runtime/Magpie-Ordner. Bitte neuen Tyria-Ordner verwenden.");
        string temp=Path.Combine(Root,"runtime","download");Directory.CreateDirectory(temp);
        string zip=Path.Combine(temp,"Magpie-v0.13.0-x64.zip");
        using(var http=new HttpClient{Timeout=TimeSpan.FromMinutes(3)}) {
            using var response=await http.GetAsync(Url,HttpCompletionOption.ResponseHeadersRead);response.EnsureSuccessStatusCode();
            using var output=File.Create(zip);await response.Content.CopyToAsync(output);
        }
        if(Hash(zip)!=ArchiveHash)throw new IOException("Download-Prüfsumme falsch. Nichts ausgeführt.");
        ZipFile.ExtractToDirectory(zip,Runtime);
        File.WriteAllText(Path.Combine(Root,"runtime","install-receipt.json"),JsonSerializer.Serialize(new{url=Url,sha256=ArchiveHash,exeSha256=Hash(exe),installed=DateTimeOffset.Now},Json));
        Select("Quality",false);
    }
    public static void Select(string preset,bool launch) {
        int idx=Array.IndexOf(new[]{"Quality","Balanced","Baseline"},preset);
        if(idx<0)throw new ArgumentException("Quality, Balanced oder Baseline wählen.");
        string exe=Path.Combine(Runtime,"Magpie.exe");
        if(!File.Exists(exe)||Hash(exe)!=ExeHash)throw new IOException("Magpie fehlt oder Datei verändert. Zuerst einrichten.");
        if(Process.GetProcessesByName("Magpie").Length>0)throw new IOException("Magpie zuerst im Tray-Menü beenden. Laufende Instanzen werden nicht verändert.");
        using var modes=JsonDocument.Parse(File.ReadAllText(Path.Combine(Root,"presets","scaling-modes.json")));
        string configDir=Path.Combine(Runtime,"config"),captures=Path.Combine(Root,"captures");Directory.CreateDirectory(configDir);Directory.CreateDirectory(captures);
        string path=Path.Combine(configDir,"config.json");
        if(File.Exists(path)){string backups=Path.Combine(Root,"runtime","backups");Directory.CreateDirectory(backups);File.Copy(path,Path.Combine(backups,"config-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".json"));}
        var profile=new Dictionary<string,object>{["scalingMode"]=idx,["captureMethod"]=0,["3DGameMode"]=true,["captureTitleBar"]=false,
          ["frameRateLimiterEnabled"]=false,["maxFrameRate"]=144,["fullscreenInitialToolbarState"]=0,
          ["screenshotsDir"]=captures,["screenshotFilenameTemplate"]="Tyria-%Y%-%m%-%D%_%H%-%M%-%S%-%MS%",
          ["graphicsCardId"]=new{idx=-1,vendorId=4098,deviceId=0}};
        var config=new{language="en-US",theme=2,scalingModes=modes.RootElement.GetProperty("scalingModes").Clone(),profiles=new[]{profile},
          shortcuts=new{scale=3137,windowedModeScale=3153,toolbar=3140,takeScreenshot=3155},showNotifyIcon=true,
          autoCheckForUpdates=false,alwaysRunAsAdmin=false,allowScalingMaximized=false,developerMode=false,debugMode=false,
          benchmarkMode=false,saveEffectSources=false,duplicateFrameDetectionMode=1,countdownSeconds=3};
        File.WriteAllText(path+".new",JsonSerializer.Serialize(config,Json));File.Move(path+".new",path,true);
        File.Copy(Path.Combine(Root,"effects","TyriaTone.hlsl"),Path.Combine(Runtime,"effects","TyriaTone.hlsl"),true);
        if(launch)Process.Start(new ProcessStartInfo(exe){WorkingDirectory=Runtime,UseShellExecute=true});
    }
    public static string Check(string? modelPath) {
        object? model=null;
        if(modelPath!=null){var f=new FileInfo(modelPath);var version=FileVersionInfo.GetVersionInfo(f.FullName);uint status=Signature.Check(f.FullName);
          model=new{path=f.FullName,bytes=f.Length,version=version.FileVersion,sha256=Hash(f.FullName),
            signature=status==0?"Valid":status==0x80096010?"HashMismatch":"Unverified",winTrustCode="0x"+status.ToString("X8"),
            note="Offline WinVerifyTrust. No module loaded, no writes, no online revocation check or license determination."};}
        string exe=Path.Combine(Runtime,"Magpie.exe");
        string json=JsonSerializer.Serialize(new{checkedAt=DateTimeOffset.Now,baselineReady=File.Exists(exe)&&Hash(exe)==ExeHash,
          gw2Running=Process.GetProcessesByName("Gw2-64").Length>0,hip7Available=File.Exists(Path.Combine(Environment.SystemDirectory,"amdhip64_7.dll")),
          neuralReady=false,model,neuralBlockers=new[]{"Kein geprüfter neuronaler Host eingerichtet.","Modellprovenienz, Signatur und Gewichtsrechte ungeklärt."}},Json);
        Directory.CreateDirectory(Path.Combine(Root,"reports"));File.WriteAllText(Path.Combine(Root,"reports","preflight-native.json"),json);return json;
    }
}

static class Signature {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct FileInfo {
        public uint size;[MarshalAs(UnmanagedType.LPWStr)]public string path;public IntPtr file;public IntPtr knownSubject;
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct TrustData {
        public uint size; public IntPtr policy,sip;public uint ui,revocation,choice;public IntPtr file;
        public uint action;public IntPtr state,url;public uint flags,context; public IntPtr signatureSettings;
    }
    [DllImport("wintrust.dll",ExactSpelling=true,CharSet=CharSet.Unicode)]static extern uint WinVerifyTrust(IntPtr hwnd,ref Guid action,ref TrustData data);
    public static uint Check(string path){
        var f=new FileInfo{size=(uint)Marshal.SizeOf<FileInfo>(),path=path};IntPtr ptr=Marshal.AllocHGlobal(Marshal.SizeOf<FileInfo>());
        try{Marshal.StructureToPtr(f,ptr,false);var d=new TrustData{size=(uint)Marshal.SizeOf<TrustData>(),ui=2,choice=1,file=ptr,flags=0x1000};
          var action=new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");return WinVerifyTrust(new IntPtr(-1),ref action,ref d);
        }finally{Marshal.DestroyStructure<FileInfo>(ptr);Marshal.FreeHGlobal(ptr);}
    }
}

sealed class ControlPanel:Form {
    readonly Label status=new(){AutoSize=false,Height=112,Dock=DockStyle.Top};
    readonly ComboBox preset=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=240};
    public ControlPanel(){
        Text="Tyria Realism 0.7";ClientSize=new Size(720,470);StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",11);BackColor=Color.FromArgb(24,32,41);ForeColor=Color.White;
        var layout=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),FlowDirection=FlowDirection.TopDown,WrapContents=false};Controls.Add(layout);
        layout.Controls.Add(new Label{Text="TYRIA REALISM",Font=new Font("Segoe UI",23,FontStyle.Bold),AutoSize=true});
        layout.Controls.Add(new Label{Text="Externe SDR-Bildaufwertung · FSR1 + TyriaTone\nDieser Prototyp enthält kein DLSS 5 und keine neuronale Neubeleuchtung.",AutoSize=true,Margin=new Padding(0,8,0,14)});
        preset.Items.AddRange(new object[]{"Quality","Balanced","Baseline"});preset.SelectedIndex=0;layout.Controls.Add(preset);
        var buttons=new FlowLayoutPanel{Width=565,Height=50};layout.Controls.Add(buttons);
        var bridge=new Button{Text="Tyria Bridge öffnen",AutoSize=true};var install=new Button{Text="Magpie einrichten",AutoSize=true};var start=new Button{Text="Preset starten",AutoSize=true};var chart=new Button{Text="Testfenster",AutoSize=true};var neural=new Button{Text="Neuralstatus prüfen",AutoSize=true};var compat=new Button{Text="Spiel-Kompatibilität",AutoSize=true};
        foreach(var b in new[]{bridge,install,start,chart,neural,compat}){b.ForeColor=Color.Black;b.BackColor=Color.FromArgb(225,233,237);b.UseVisualStyleBackColor=false;buttons.Controls.Add(b);}
        layout.Controls.Add(status);status.Width=560;
        status.Text="Quality: GW2-Fenster 1920×1080. Balanced: 1600×900.\nZiel: 2560×1440. In GW2 native Render-Abtastung wählen.\nAlt+Shift+A startet/stoppt Magpie; Alt+Shift+S speichert ein Bild.\nZum Wechseln Magpie zuerst über das Tray-Menü beenden.";
        bridge.Click+=(_,_)=>new BridgePanel().Show();
        install.Click+=async(_,_)=>{install.Enabled=false;try{status.Text="Lade offizielle Magpie-Version und prüfe SHA-256 …";await LocalSetup.Install();status.Text="Bereit. GW2 im Fenstermodus öffnen und Preset starten.";}catch(Exception ex){status.Text=ex.Message;}finally{install.Enabled=true;}};
        start.Click+=(_,_)=>{try{LocalSetup.Select((string)preset.SelectedItem!,true);status.Text="Magpie gestartet. GW2-Fenster aktivieren; Alt+Shift+A. Beenden: erneut Alt+Shift+A, dann Magpie im Tray schließen.";}catch(Exception ex){status.Text=ex.Message;}};
        chart.Click+=(_,_)=>new Chart(preset.SelectedIndex==1?1600:1920,preset.SelectedIndex==1?900:1080,false).Show();
        neural.Click+=(_,_)=>{using var dialog=new OpenFileDialog{Title="nvngx_dlssnr.dll auswählen (wird nur gelesen)",Filter="NVIDIA Neural Rendering DLL (nvngx_dlssnr.dll)|nvngx_dlssnr.dll|DLL (*.dll)|*.dll"};
          if(dialog.ShowDialog()!=DialogResult.OK)return;try{using var doc=JsonDocument.Parse(NeuralAudit.Check(dialog.FileName,null));var m=doc.RootElement.GetProperty("model");
            status.Text=m.GetProperty("accepted").GetBoolean()?"Modelldatei exakt erkannt. Für GW2 fehlt weiterhin der externe Neural-Host.":"Modelldatei abgelehnt: Hash/Version entspricht keiner geprüften Referenz. Es wurde nichts geladen oder kopiert.";}catch(Exception ex){status.Text=ex.Message;}};
        compat.Click+=(_,_)=>{using var dialog=new OpenFileDialog{Title="Spiel-EXE statisch prüfen",Filter="Windows-Spiel (*.exe)|*.exe"};
          if(dialog.ShowDialog()!=DialogResult.OK)return;try{var result=CompatibilityAudit.Inspect(dialog.FileName);string file=Path.Combine(LocalSetup.Root,"reports","compatibility-"+Path.GetFileNameWithoutExtension(dialog.FileName)+".json");File.WriteAllText(file,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
            status.Text=result.GraphicsApi+" · "+(result.AntiCheatMarkerFound?"Anti-Cheat-Hinweis erkannt":"kein Anti-Cheat-Dateiname erkannt")+"\n"+result.Recommendation+"\nBericht: "+file;}catch(Exception ex){status.Text=ex.Message;}};
    }
}

