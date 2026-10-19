using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

static class NeuralAudit {
    public const string ReleaseTag="v0.0.6-win-preview-alpha";
    public const string HotfixName="DLSSNR-AMD-Vulkan-Windows-0.0.6-win-preview-alpha-hotfix-x86_64.zip";
    public const string HotfixSha256="a5687dc3875be4bdb8149ca68bb5e3f3023402c720bf4a80a03ac2a19c436e2b";
    public const long HotfixBytes=196497671;
    public const string Model3108Sha256="e16bcf15e16e13f527491cdf7845b2fe6521a738d8f7c9c721866a8496e1fc8e";
    public const string Model310916Sha256="2bc19bc09a7b89dd44f73e2ac0108fcc586e2154bdd0830f242f4f040e28789f";
    public const string OldExtractedModelSha256="2b41c888cf4155b8958c665ba64018ab0bd25c85fc71a2b6db86d0d04d1f7fbd";
    public const string SmallExtractedModelSha256="7e966e2639dbec6596aa0bac3b8761137d2c40ca8cb4aca30df7ca10043c6e33";

    public static string? ClassifyModelHash(string hash) => hash.ToLowerInvariant() switch {
        Model3108Sha256 => "310.8.0",
        Model310916Sha256 => "310.9.16",
        _ => null
    };

    public static string Check(string? modelPath,string? packagePath) {
        object? model=null,package=null;
        bool modelAccepted=false,packageAccepted=false;
        if(!string.IsNullOrWhiteSpace(modelPath)) {
            string path=Path.GetFullPath(modelPath);var f=new FileInfo(path);
            if(!f.Exists)throw new FileNotFoundException("Modelldatei nicht gefunden.",path);
            string hash=LocalSetup.Hash(path);string? acceptedVersion=ClassifyModelHash(hash);uint trust=Signature.Check(path);
            modelAccepted=acceptedVersion!=null;
            model=new{path=f.FullName,bytes=f.Length,fileVersion=FileVersionInfo.GetVersionInfo(path).FileVersion,sha256=hash,
                acceptedVersion,accepted=modelAccepted,authenticode=trust==0?"Valid":trust==0x80096010?"HashMismatch":"Unverified",
                winTrustCode="0x"+trust.ToString("X8"),loaded=false,copied=false,
                note=modelAccepted?"Exakter Hash einer vom geprüften Extraktor unterstützten DLL.":"Hash wird vom geprüften Extraktor abgelehnt; nicht verwenden."};
        }
        if(!string.IsNullOrWhiteSpace(packagePath)) {
            string path=Path.GetFullPath(packagePath);var f=new FileInfo(path);
            if(!f.Exists)throw new FileNotFoundException("Neural-Paket nicht gefunden.",path);
            string hash=LocalSetup.Hash(path);int entries=0;bool safeNames=true,hasInstaller=false,hasExtractor=false,containsModel=false;
            using(var zip=ZipFile.OpenRead(path))foreach(var e in zip.Entries){entries++;string n=e.FullName.Replace('\\','/');
                if(n.StartsWith('/')||n.Split('/').Any(p=>p==".."))safeNames=false;
                if(n.Equals("install.ps1",StringComparison.OrdinalIgnoreCase))hasInstaller=true;
                if(n.Equals("model-tools/dlssnr_extract_model.exe",StringComparison.OrdinalIgnoreCase))hasExtractor=true;
                if(n.EndsWith("/dlssnr.bin",StringComparison.OrdinalIgnoreCase)||n.EndsWith("/dlssnr-small.bin",StringComparison.OrdinalIgnoreCase))containsModel=true;}
            packageAccepted=hash==HotfixSha256&&f.Length==HotfixBytes&&safeNames&&hasInstaller&&hasExtractor&&!containsModel;
            package=new{path=f.FullName,bytes=f.Length,sha256=hash,expectedSha256=HotfixSha256,entries,safeEntryNames=safeNames,
                hasInstaller,hasExtractor,containsExtractedNvidiaModel=containsModel,accepted=packageAccepted,executed=false,
                source="Official GitHub release asset "+ReleaseTag,license="Project code MIT; bundled third-party components retain their own licenses."};
        }
        var blockers=new List<string>();
        if(!packageAccepted)blockers.Add("Kein vollständig geprüftes offizielles Windows-Neural-Paket angegeben.");
        if(!modelAccepted)blockers.Add("Keine vom Referenzextraktor akzeptierte NVIDIA-Modelldatei vorhanden.");
        blockers.Add("Der veröffentlichte GW2-Pfad lädt ReShade/DLLs in den Spielprozess und verletzt damit die Vorgabe für einen rein externen Host.");
        blockers.Add("Ein externer D3D11/Vulkan-Host für Fensteraufnahme, Inferenz, HUD-Schutz und Ausgabe ist noch nicht implementiert.");
        var result=new{checkedAt=DateTimeOffset.Now,project="Tyria Realism neural preflight",candidate="mochizuki0323/DLSSNR-AMD "+ReleaseTag,
            candidateCodeLicense="MIT",package,model,offlineCandidateInputsReady=packageAccepted&&modelAccepted,
            gw2DirectInstallAllowed=false,externalNeuralPrototypeReady=false,blockers,
            safety="Nur Hash-, ZIP- und Signaturprüfung. Keine DLL geladen, nichts in GW2 kopiert, kein Prozess-Hook."};
        string json=JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true});
        Directory.CreateDirectory(Path.Combine(LocalSetup.Root,"reports"));File.WriteAllText(Path.Combine(LocalSetup.Root,"reports","neural-preflight.json"),json);
        return json;
    }
}

