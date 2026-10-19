using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

static class CompatibilityAudit {
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    static readonly string[] AntiCheatNames={"easyanticheat","easyanticheat_eos","battleye","beservice","xigncode","vgc","vgk"};
    static readonly string[] TemporalDlls={"nvngx_dlss.dll","libxess.dll","amd_fidelityfx_dx12.dll","amd_fidelityfx_loader_dx12.dll","ffx_fsr2_api_x64.dll","ffx_fsr3upscaler_x64.dll"};

    public static string WriteReport(string exePath,string outputPath) {
        var result=Inspect(exePath);
        string json=JsonSerializer.Serialize(result,Json);
        string full=Path.GetFullPath(outputPath);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full,json,new UTF8Encoding(false));
        return json;
    }

    public static CompatibilityResult Inspect(string exePath) {
        string exe=Path.GetFullPath(exePath);
        if(!File.Exists(exe))throw new FileNotFoundException("Spiel-EXE nicht gefunden.",exe);
        var pe=PeImports.Read(exe);
        string gameDir=Path.GetDirectoryName(exe)!;
        var nearby=Directory.EnumerateFiles(gameDir,"*",SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).Where(n=>n!=null).Select(n=>n!).ToArray();
        bool Dll(string name)=>nearby.Contains(name,StringComparer.OrdinalIgnoreCase);
        bool antiCheat=nearby.Any(n=>AntiCheatNames.Any(a=>n.Contains(a,StringComparison.OrdinalIgnoreCase)));
        var temporalFiles=TemporalDlls.Where(Dll).ToArray();
        var markers=ScanAsciiMarkers(exe,new[]{"d3d11.dll","D3D11CreateDevice","d3d12.dll","D3D12CreateDevice","vulkan-1.dll","NVSDK_NGX_D3D11","NVSDK_NGX_D3D12","xessD3D11","xessD3D12","ffxFsr2","ffxFsr3"});
        bool dx11=pe.Imports.Contains("d3d11.dll",StringComparer.OrdinalIgnoreCase)||markers.Contains("d3d11.dll")||markers.Contains("D3D11CreateDevice");
        bool dx12=pe.Imports.Contains("d3d12.dll",StringComparer.OrdinalIgnoreCase)||markers.Contains("d3d12.dll")||markers.Contains("D3D12CreateDevice");
        bool vulkan=pe.Imports.Contains("vulkan-1.dll",StringComparer.OrdinalIgnoreCase)||markers.Contains("vulkan-1.dll");
        bool temporalInput=temporalFiles.Length>0||markers.Any(m=>m.StartsWith("NVSDK_NGX",StringComparison.OrdinalIgnoreCase)||m.StartsWith("xess",StringComparison.OrdinalIgnoreCase)||m.StartsWith("ffxFsr",StringComparison.OrdinalIgnoreCase));
        string api=dx12?"DirectX 12":dx11?"DirectX 11":vulkan?"Vulkan":"Nicht sicher erkannt";
        var routes=new List<RouteAssessment>();
        routes.Add(new("RenoDX / ReShade","Post-Processing, HDR und spielbezogene Shader",dx11||dx12,
            (dx11||dx12)?"Technisch passend; Qualität hängt vom Preset und Spielprofil ab.":"Keine DirectX-API erkannt."));
        routes.Add(new("DLSSNR-AMD ReShade-Feeder","DLSS-5-Neural-Rendering-Stil ohne nativen Upscaler",dx11&&pe.Is64Bit&&!antiCheat,
            antiCheat?"Anti-Cheat-Dateien erkannt; In-Process-Add-ons standardmäßig blockiert.":dx11&&pe.Is64Bit?"Experimenteller Kandidat. Bewegung wird aus dem Bild geschätzt; kein DLSS-Super-Resolution und keine Frame Generation.":"Benötigt ein 64-Bit-DX10/11/12-Spiel und eine unterstützte RDNA4-GPU."));
        routes.Add(new("OptiScaler FSR4 Super Resolution","FSR4 als Ersatz für vorhandenes DLSS/FSR2/XeSS",(dx11||dx12)&&temporalInput&&!antiCheat,
            temporalInput?"Kandidat mit echten Engine-Eingaben; pro Spiel testen.":"Kein temporaler Upscaler-Eingang erkannt. OptiScaler kann fehlende Bewegungsvektoren, Tiefe und Jitter nicht generisch ersetzen."));
        routes.Add(new("OptiScaler Frame Generation","FSR-FG/XeFG über OptiFG oder vorhandenen FG-Eingang",dx12&&temporalInput&&!antiCheat,
            dx11?"OptiFG ist laut Upstream nur für DX12 vorgesehen.":!temporalInput?"Kein geeigneter Upscaler-/FG-Eingang erkannt.":"Experimenteller DX12-Kandidat; HUD-Artefakte, Abstürze und Latenz separat testen."));
        routes.Add(new("AMD AFMF / RSR","Treiberbasierte Skalierung und Frame Generation",dx11||dx12||vulkan,
            "Außerhalb des Spielprozesses konfigurierbar. AFMF erzeugt Frames ohne Engine-Bewegungsvektoren; Bildstabilität und Latenz sind nicht mit nativer FG gleichzusetzen."));
        string recommendation=antiCheat
            ?"Nur externe oder vom Spiel ausdrücklich erlaubte Wege verwenden; keine DLL-Installation vorgeschlagen."
            : dx11&&!temporalInput
                ?"DX11 ohne temporalen Upscaler: DLSSNR-AMD/ReShade für den Look testen; AFMF optional für flüssigere Ausgabe. FSR4-SR und OptiFG sind hier nicht belastbar verfügbar."
                : dx11
                    ?"DX11 mit erkanntem temporalen Eingang: OptiScaler kann FSR4-SR über DX11/DX12-Interop testen; Frame Generation bleibt ein separater Treiberweg."
                    : dx12&&temporalInput
                        ?"DX12 mit temporalen Eingängen: OptiScaler SR und FG sind Testkandidaten; erst in einer Sicherungskopie und mit A/B-Protokoll prüfen."
                        :"Zuerst Grafik-API und temporale Eingänge im laufenden Spiel oder in Herstellerdokumentation verifizieren.";
        return new CompatibilityResult(DateTimeOffset.Now,exe,LocalSetup.Hash(exe),pe.Is64Bit,api,pe.Imports.Order().ToArray(),temporalFiles,markers.ToArray(),antiCheat,routes,recommendation,
            "Statische Prüfung. Keine DLL geladen, kein Spiel gestartet, kein Prozess verändert. Dateinamen und Importtabellen belegen noch keine erfolgreiche Laufzeitintegration.");
    }

    static HashSet<string> ScanAsciiMarkers(string path,IEnumerable<string> needles) {
        var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        byte[] data=File.ReadAllBytes(path);
        foreach(string needle in needles)if(data.AsSpan().IndexOf(Encoding.ASCII.GetBytes(needle))>=0)result.Add(needle);
        return result;
    }
}

record RouteAssessment(string Name,string Purpose,bool Candidate,string Reason);
record CompatibilityResult(DateTimeOffset CheckedAt,string Executable,string Sha256,bool Is64Bit,string GraphicsApi,string[] ImportedLibraries,string[] NearbyTemporalDlls,string[] ExecutableMarkers,bool AntiCheatMarkerFound,List<RouteAssessment> Routes,string Recommendation,string VerificationBoundary);

static class PeImports {
    public sealed record Result(bool Is64Bit,HashSet<string> Imports);
    sealed record Section(uint VirtualAddress,uint VirtualSize,uint RawOffset,uint RawSize);
    public static Result Read(string path) {
        byte[] b=File.ReadAllBytes(path);
        if(b.Length<0x100||b[0]!='M'||b[1]!='Z')throw new InvalidDataException("Keine gültige PE-Datei.");
        int pe=checked((int)U32(b,0x3c));if(pe<0||pe+24>b.Length||U32(b,pe)!=0x4550)throw new InvalidDataException("PE-Header fehlt.");
        ushort sectionCount=U16(b,pe+6),optSize=U16(b,pe+20);int opt=pe+24;
        ushort magic=U16(b,opt);bool is64=magic==0x20b;if(!is64&&magic!=0x10b)throw new InvalidDataException("Unbekanntes PE-Format.");
        int dataDir=opt+(is64?112:96);uint importRva=U32(b,dataDir+8);int sectionTable=opt+optSize;
        var sections=new List<Section>();for(int i=0;i<sectionCount;i++){int s=sectionTable+i*40;if(s+40>b.Length)throw new InvalidDataException("Beschädigte PE-Sektionstabelle.");sections.Add(new(U32(b,s+12),U32(b,s+8),U32(b,s+20),U32(b,s+16)));}
        int Rva(uint rva){foreach(var s in sections){uint size=Math.Max(s.VirtualSize,s.RawSize);if(rva>=s.VirtualAddress&&rva-s.VirtualAddress<size){long o=(long)s.RawOffset+(rva-s.VirtualAddress);if(o>=0&&o<b.Length)return (int)o;}}return -1;}
        var imports=new HashSet<string>(StringComparer.OrdinalIgnoreCase);if(importRva==0)return new(is64,imports);
        int d=Rva(importRva);if(d<0)return new(is64,imports);
        for(int i=0;i<4096&&d+20<=b.Length;i++,d+=20){uint original=U32(b,d),time=U32(b,d+4),chain=U32(b,d+8),nameRva=U32(b,d+12),first=U32(b,d+16);if((original|time|chain|nameRva|first)==0)break;int n=Rva(nameRva);if(n<0)continue;int end=n;while(end<b.Length&&b[end]!=0&&end-n<512)end++;if(end<b.Length)imports.Add(Encoding.ASCII.GetString(b,n,end-n));}
        return new(is64,imports);
    }
    static ushort U16(byte[] b,int o){if(o<0||o+2>b.Length)throw new InvalidDataException("PE-Daten abgeschnitten.");return BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(o,2));}
    static uint U32(byte[] b,int o){if(o<0||o+4>b.Length)throw new InvalidDataException("PE-Daten abgeschnitten.");return BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o,4));}
}

