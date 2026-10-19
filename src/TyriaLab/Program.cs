using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

static class Program {
    [STAThread] static int Main(string[] args) {
        try {
            if(args.Length>0 && args[0]=="compare") return Compare(args);
            if(args.Length>0 && args[0]=="fps") return Fps(args);
            if(args.Length>0 && args[0]=="selftest") return SelfTest();
            if(args.Length>0 && args[0]=="capture") return Capture(args);
            if(args.Length>0 && args[0]=="analyze") return Analyze(args[1]);
            if(args.Length>0 && args[0]=="texture-detail") {
                if(args.Length!=4) throw new ArgumentException("texture-detail 0xHASH.png output.png strength");
                Console.WriteLine(JsonSerializer.Serialize(TextureEnhancer.Enhance(args[1],args[2],double.Parse(args[3],System.Globalization.CultureInfo.InvariantCulture)),new JsonSerializerOptions{WriteIndented=true})); return 0;
            }
            if(args.Length>0 && args[0]=="texture-classify") {
                if(args.Length!=2) throw new ArgumentException("texture-classify 0xHASH.png");
                Console.WriteLine(JsonSerializer.Serialize(TextureEnhancer.Classify(args[1]),new JsonSerializerOptions{WriteIndented=true})); return 0;
            }
            if(args.Length>0 && args[0]=="texture-pack") {
                if(args.Length!=4) throw new ArgumentException("texture-pack texdump output Natural|Detail");
                Console.WriteLine(JsonSerializer.Serialize(TextureEnhancer.BuildPack(args[1],args[2],args[3]),new JsonSerializerOptions{WriteIndented=true})); return 0;
            }
            if(args.Length>0 && args[0]=="compat-check") {
                if(args.Length<2) throw new ArgumentException("compat-check game.exe [report.json]");
                string report=args.Length>2?args[2]:Path.Combine(LocalSetup.Root,"reports","compatibility-"+Path.GetFileNameWithoutExtension(args[1])+".json");
                Console.WriteLine(CompatibilityAudit.WriteReport(args[1],report)); return 0;
            }
            if(args.Length>0 && args[0]=="bridge") {
                if(args.Length<2) throw new ArgumentException("bridge status|apply|materials|launch ...");
                if(args[1]=="status") { if(args.Length<3) throw new ArgumentException("bridge status Gw2-64.exe [report.json]"); Console.WriteLine(TyriaBridge.WriteStatus(args[2],args.Length>3?args[3]:null)); return 0; }
                if(args[1]=="apply") { if(args.Length<4) throw new ArgumentException("bridge apply Gw2-64.exe Efficient|Balanced|Cinematic|Quality|RSR1080p|Off"); Console.WriteLine(TyriaBridge.Apply(args[2],args[3])); return 0; }
                if(args[1]=="materials") { if(args.Length<4) throw new ArgumentException("bridge materials Gw2-64.exe Natural|Detail|Off"); Console.WriteLine(TyriaBridge.ApplyMaterials(args[2],args[3])); return 0; }
                if(args[1]=="launch") { if(args.Length<3) throw new ArgumentException("bridge launch Gw2-64.exe"); TyriaBridge.Launch(args[2]); return 0; }
                throw new ArgumentException("bridge status|apply|materials|launch");
            }
            if(args.Length>0 && args[0]=="bridge-panel") { ApplicationConfiguration.Initialize(); Application.Run(new BridgePanel()); return 0; }
            if(args.Length>0 && args[0]=="neural-check") { Console.WriteLine(NeuralAudit.Check(args.Length>1?args[1]:null,args.Length>2?args[2]:null));return 0; }
            if(args.Length>0 && args[0]=="install") { LocalSetup.Install().GetAwaiter().GetResult();return 0; }
            if(args.Length>0 && args[0]=="start") { LocalSetup.Select(args.Length>1?args[1]:"Quality",true);return 0; }
            if(args.Length>0 && args[0]=="select") { LocalSetup.Select(args.Length>1?args[1]:"Quality",false);return 0; }
            if(args.Length>0 && args[0]=="check") { Console.WriteLine(LocalSetup.Check(args.Length>1?args[1]:null));return 0; }
            ApplicationConfiguration.Initialize();
            if(args.Length==0||args[0]=="panel"){Application.Run(new ControlPanel());return 0;}
            int width=args.Length>0?int.Parse(args[0]):1920;
            int height=args.Length>1?int.Parse(args[1]):1080;
            Application.Run(new Chart(width,height,args.Contains("--animate"))); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
    static int Compare(string[] a) {
        if(a.Length!=4) throw new ArgumentException("compare original.png processed.png report.json");
        using var x=new Bitmap(a[1]); using var y=new Bitmap(a[2]);
        if(x.Size!=y.Size) throw new ArgumentException("Images must have identical dimensions and alignment.");
        using var diff=new Bitmap(x.Width,x.Height);
        double sum=0,abs=0; long changed=0,clipX=0,clipY=0,borderChanged=0;
        for(int j=0;j<x.Height;j++) for(int i=0;i<x.Width;i++) {
            var p=x.GetPixel(i,j);var q=y.GetPixel(i,j);
            int r=Math.Abs(p.R-q.R),g=Math.Abs(p.G-q.G),b=Math.Abs(p.B-q.B);
            sum+=(double)r*r+g*g+b*b;abs+=r+g+b;if(r+g+b>0){changed++;
                if(i<x.Width*.10||i>x.Width*.90||j<x.Height*.08||j>x.Height*.81)borderChanged++;}
            if(p.R==0||p.R==255||p.G==0||p.G==255||p.B==0||p.B==255)clipX++;
            if(q.R==0||q.R==255||q.G==0||q.G==255||q.B==0||q.B==255)clipY++;
            diff.SetPixel(i,j,Color.FromArgb(Math.Min(255,r*8),Math.Min(255,g*8),Math.Min(255,b*8)));
        }
        double n=(double)x.Width*x.Height,mse=sum/(n*3);
        var data=new { width=x.Width,height=x.Height,mae=abs/(n*3),mse,
          psnrDb=mse==0?(double?)null:10*Math.Log10(255.0*255/mse),identical=mse==0,
          changedFraction=changed/n,protectedBorderChangedPixels=borderChanged,originalClippedFraction=clipX/n,processedClippedFraction=clipY/n,
          interpretation="Difference metrics only. PSNR against original is NOT a realism/quality score. Motion, UI and latency require human review."};
        File.WriteAllText(a[3],JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        diff.Save(Path.ChangeExtension(a[3],"diff.png"),ImageFormat.Png);Console.WriteLine(JsonSerializer.Serialize(data));return 0;
    }
    static int Fps(string[] a) {
        if(a.Length!=4) throw new ArgumentException("fps frametimes.csv columnName report.json; values in milliseconds");
        using var parser=new Microsoft.VisualBasic.FileIO.TextFieldParser(a[1]);parser.SetDelimiters(",");parser.HasFieldsEnclosedInQuotes=true;
        var headers=parser.ReadFields()??throw new ArgumentException("Empty CSV.");int col=Array.FindIndex(headers,s=>string.Equals(s,a[2],StringComparison.OrdinalIgnoreCase));
        if(col<0)throw new ArgumentException("Column missing: "+a[2]);
        var values=new List<double>();while(!parser.EndOfData){var row=parser.ReadFields();if(row==null)continue;
            if(row.Length<=col)throw new ArgumentException("Truncated CSV row.");values.Add(double.Parse(row[col],System.Globalization.CultureInfo.InvariantCulture));}
        var ms=values.ToArray();
        if(ms.Length<2||ms.Any(v=>!double.IsFinite(v)||v<=0))throw new ArgumentException("Need >=2 positive finite frame intervals in ms.");
        Array.Sort(ms);double Percentile(double p)=>ms[(int)Math.Ceiling(p*ms.Length)-1];
        var result=new{samples=ms.Length,averageFps=1000/ms.Average(),medianMs=Percentile(.5),p95Ms=Percentile(.95),p99Ms=Percentile(.99),
          onePercentLowFps=1000/ms.TakeLast(Math.Max(1,(int)Math.Ceiling(ms.Length*.01))).Average(),
          note="FPS derived from input frame intervals; caller must identify GW2 versus Magpie process and source. No generated frames inferred."};
        File.WriteAllText(a[3],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(result));return 0;
    }
    static int SelfTest(){
        var passed=new List<string>();void Assert(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);passed.Add(name);}
        string folder=Path.Combine(LocalSetup.Root,"reports","native-test-fixtures");Directory.CreateDirectory(folder);
        string csv=Path.Combine(folder,"intervals.csv"),report=Path.Combine(folder,"statistics.json");
        File.WriteAllText(csv,"\"name\",\"frameTimeMs\"\n\"a,b\",10\n\"a,b\",20\n\"a,b\",30\n\"a,b\",40\n");
        Fps(new[]{"fps",csv,"frameTimeMs",report});using(var r=JsonDocument.Parse(File.ReadAllText(report))){
          Assert(r.RootElement.GetProperty("averageFps").GetDouble()==40,"quoted CSV and average FPS");
          Assert(r.RootElement.GetProperty("onePercentLowFps").GetDouble()==25,"slowest 1 percent definition");
          Assert(r.RootElement.GetProperty("p99Ms").GetDouble()==40,"p99 frame interval");}
        foreach(string invalid in new[]{"0","-2","NaN","Infinity"}){File.WriteAllText(csv,"frameTimeMs\n10\n"+invalid+"\n");bool rejected=false;
          try{Fps(new[]{"fps",csv,"frameTimeMs",report});}catch(ArgumentException){rejected=true;}Assert(rejected,"reject invalid interval: "+invalid);}
        File.WriteAllText(csv,"Application,ProcessID,SwapChainAddress,msBetweenPresents\nGw2-64.exe,1,0xA,10\nGw2-64.exe,1,0xA,20\nGw2-64.exe,1,0xA,0\nMagpie.exe,2,0xB,30\nMagpie.exe,2,0xB,40\n");
        var previous=Directory.Exists(Path.Combine(LocalSetup.Root,"reports","benchmarks"))?Directory.GetDirectories(Path.Combine(LocalSetup.Root,"reports","benchmarks")).ToHashSet():new HashSet<string>();
        Analyze(csv);
        string regression=Directory.GetDirectories(Path.Combine(LocalSetup.Root,"reports","benchmarks")).Single(p=>!previous.Contains(p));
        using(var r=JsonDocument.Parse(File.ReadAllText(Path.Combine(regression,"summary.json")))){
          var groups=r.RootElement.GetProperty("groups");
          Assert(groups.GetArrayLength()==2,"PresentMon lowercase header and separate processes");
          Assert(groups[0].GetProperty("valid").GetInt32()==2&&groups[0].GetProperty("rejectedNonpositiveOrInvalid").GetInt32()==1,"PresentMon rejects zero interval without discarding valid frames");
        }
        string a=Path.Combine(folder,"a.png"),b=Path.Combine(folder,"b.png");
        using(var x=new Bitmap(8,8)){x.SetPixel(4,4,Color.White);x.Save(a);}using(var x=new Bitmap(9,8)){x.Save(b);}
        Compare(new[]{"compare",a,a,report});using(var r=JsonDocument.Parse(File.ReadAllText(report)))Assert(r.RootElement.GetProperty("identical").GetBoolean(),"identical images");
        bool mismatch=false;try{Compare(new[]{"compare",a,b,report});}catch(ArgumentException){mismatch=true;}Assert(mismatch,"reject mismatched image sizes");
        foreach(string p in new[]{"quality","balanced"}){
          string evidence=Path.Combine(LocalSetup.Root,"reports","synthetic-"+p+"-comparison.json");
          if(!File.Exists(evidence))continue;using var r=JsonDocument.Parse(File.ReadAllText(evidence));var o=r.RootElement;
          Assert(o.GetProperty("width").GetInt32()==2560&&o.GetProperty("height").GetInt32()==1440,p+": saved GPU frame size");
          Assert(!o.GetProperty("identical").GetBoolean()&&o.GetProperty("protectedBorderChangedPixels").GetInt64()==0,p+": saved centre change / outer strip identity");
        }
        Assert(NeuralAudit.ClassifyModelHash(NeuralAudit.Model3108Sha256)=="310.8.0","recognize exact 310.8 neural model hash");
        Assert(NeuralAudit.ClassifyModelHash(NeuralAudit.Model310916Sha256)=="310.9.16","recognize exact 310.9.16 neural model hash");
        Assert(NeuralAudit.ClassifyModelHash("e67dee209320cdafe0e93e45675d7aa34323a53acc57a72b2e40a181581c989a")==null,"reject Crimson Desert hash mismatch candidate");
        string self=Environment.ProcessPath??throw new Exception("Own executable path unavailable.");
        var ownPe=PeImports.Read(self);Assert(ownPe.Is64Bit,"PE parser recognizes x64 executable");
        string markerExe=Path.Combine(folder,"synthetic-dx11-marker.exe");File.Copy(self,markerExe,true);
        using(var append=new FileStream(markerExe,FileMode.Append,FileAccess.Write))append.Write(System.Text.Encoding.ASCII.GetBytes("\0d3d11.dll\0D3D11CreateDevice\0"));
        var compat=CompatibilityAudit.Inspect(markerExe);
        Assert(compat.GraphicsApi=="DirectX 11","DX11 dynamic-load marker detection");
        Assert(!compat.Routes.Single(r=>r.Name.StartsWith("OptiScaler FSR4")).Candidate,"no FSR4 recommendation without temporal input");
        var telemetry=TyriaBridge.ParseTelemetry("[nr] frames: 70515 enhanced, 765 declined; 8.36 ms on the GPU (network 7.21, route 1.15); VRAM 1 / 2 MB");
        Assert(telemetry!=null&&telemetry.EnhancedFrames==70515&&Math.Abs(telemetry.GpuMs-8.36)<.001,"bridge parses last neural GPU telemetry");
        Assert(TyriaBridge.Profiles.Contains("Efficient"),"bridge exposes efficient neural profile");
        string textureInput=Path.Combine(folder,"0x1234ABCD.png"),textureOutput=Path.Combine(folder,"0x1234ABCD-enhanced.png");
        using(var texture=new Bitmap(8,8)){for(int y=0;y<8;y++)for(int x=0;x<8;x++)texture.SetPixel(x,y,Color.FromArgb((x+y)%2==0?110:145,80+x*8,90+y*7,100+x*5));texture.Save(textureInput);}
        var textureReport=TextureEnhancer.Enhance(textureInput,textureOutput,.8);
        using(var before=new Bitmap(textureInput))using(var after=new Bitmap(textureOutput)){Assert(before.Size==after.Size&&before.GetPixel(3,3).A==after.GetPixel(3,3).A,"texture detail preserves size and alpha");Assert(textureReport.InputSha256!=textureReport.OutputSha256,"texture detail changes structured colour data");}
        var textureClass=TextureEnhancer.Classify(textureInput);Assert(textureClass.Category=="SmallOrUI"&&!textureClass.Eligible,"small texture excluded from automatic texture pack");
        var fixtureIni=new[]{"[Int4Mixed]","Enabled = 0","[Network]","ACO Mode = 0","[RenoDX]","Enabled = 1","NRResolutionScale = 1.00"};
        var wanted=new Dictionary<string,Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase){["Int4Mixed"]=new(StringComparer.OrdinalIgnoreCase){["Enabled"]="1"},["Network"]=new(StringComparer.OrdinalIgnoreCase){["ACO Mode"]="1"},["RenoDX"]=new(StringComparer.OrdinalIgnoreCase){["Enabled"]="1",["NRResolutionScale"]="0.75"}};
        var updated=TyriaBridge.ApplyToLines(fixtureIni,wanted);
        Assert(updated.Contains("NRResolutionScale = 0.75")&&updated.Contains("Enabled = 1"),"bridge applies an INI profile transaction payload");
        File.WriteAllText(Path.Combine(LocalSetup.Root,"reports","native-selftests.json"),JsonSerializer.Serialize(new{passed=passed.Count,checks=passed,
          checkedAt=DateTimeOffset.Now,note="Saved GPU evidence validation and bridge configuration tests, not a new GPU or game benchmark."},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(passed.Count+" checks passed.");return 0;
    }
    static int Capture(string[] a){
        if(a.Length!=4)throw new ArgumentException("capture official-PresentMon.exe Native1440|Baseline|Quality|Balanced seconds");
        if(!new[]{"Native1440","Baseline","Quality","Balanced"}.Contains(a[2]))throw new ArgumentException("Invalid label.");
        int seconds=int.Parse(a[3]);if(seconds<5||seconds>180)throw new ArgumentException("Duration must be 5..180 seconds.");
        if(Process.GetProcessesByName("Gw2-64").Length==0)throw new ArgumentException("GW2 läuft nicht. Keine Messung gestartet.");
        string tool=Path.GetFullPath(a[1]);uint trust=Signature.Check(tool);
        if(trust!=0)throw new ArgumentException("PresentMon-Signatur nicht gültig: 0x"+trust.ToString("X8")+". Keine Ausführung.");
        string folder=Path.Combine(LocalSetup.Root,"reports","benchmarks",a[2]+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"capture.json"),JsonSerializer.Serialize(new{label=a[2],seconds,toolHash=LocalSetup.Hash(tool),trust="Valid",
          method="ETW, no injection, separate game and host; no automatic elevation; no input-event tracking."}));
        var start=new ProcessStartInfo(tool){UseShellExecute=false,CreateNoWindow=true};
        foreach(string arg in new[]{"--process_name","Gw2-64.exe","--process_name","Magpie.exe","--multi_csv","--v1_metrics","--no_track_input","--delay","5","--timed",seconds.ToString(),"--terminate_after_timed","--session_name","TyriaRealism-"+Guid.NewGuid().ToString("N"),"--output_file",Path.Combine(folder,"frames.csv")})start.ArgumentList.Add(arg);
        using var p=Process.Start(start)??throw new Exception("Could not start PresentMon.");p.WaitForExit();
        if(p.ExitCode!=0)throw new Exception("PresentMon failed: "+p.ExitCode+". No successful measurement confirmed.");
        var files=Directory.GetFiles(folder,"*.csv");if(files.Length==0)throw new Exception("No CSV produced. No FPS measurement confirmed.");
        foreach(string file in files)Analyze(file);Console.WriteLine(folder);return 0;
    }
    static int Analyze(string file){
        using var parser=new Microsoft.VisualBasic.FileIO.TextFieldParser(file);parser.SetDelimiters(",");parser.HasFieldsEnclosedInQuotes=true;
        var h=parser.ReadFields()??throw new ArgumentException("Empty CSV.");int Column(string name)=>Array.FindIndex(h,s=>string.Equals(s,name,StringComparison.OrdinalIgnoreCase));
        int app=Column("Application"),pid=Column("ProcessID"),chain=Column("SwapChainAddress"),time=Column("MsBetweenPresents");
        if(new[]{app,pid,chain,time}.Any(i=>i<0))throw new ArgumentException("PresentMon v1 columns missing.");
        var groups=new Dictionary<string,List<double>>();var rejected=new Dictionary<string,int>();
        while(!parser.EndOfData){var row=parser.ReadFields();if(row==null)continue;if(row.Length<h.Length)throw new ArgumentException("Truncated CSV row.");
          string key=row[app]+" / "+row[pid]+" / "+row[chain];if(!groups.ContainsKey(key)){groups[key]=new();rejected[key]=0;}
          if(double.TryParse(row[time],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double v)&&double.IsFinite(v)&&v>0)groups[key].Add(v);else rejected[key]++;}
        string output=Path.Combine(LocalSetup.Root,"reports","benchmarks","analysis-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);
        var summary=new List<object>();int index=0,analyzed=0;
        foreach(var (key,values) in groups){index++;if(values.Count<2){summary.Add(new{group=key,status="insufficient_samples",rejected=rejected[key]});continue;}
          string csv=Path.Combine(output,"intervals-"+index+".csv"),json=Path.Combine(output,"statistics-"+index+".json");
          File.WriteAllLines(csv,new[]{"frameTimeMs"}.Concat(values.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
          Fps(new[]{"fps",csv,"frameTimeMs",json});analyzed++;
          summary.Add(new{group=key,status="analyzed",valid=values.Count,rejectedNonpositiveOrInvalid=rejected[key],statistics=json});}
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{source=Path.GetFullPath(file),groups=summary,
          note="Never merge swapchains. Magpie FPS are not GW2 FPS. ETW presentation intervals are not input-to-photon latency."},new JsonSerializerOptions{WriteIndented=true}));
        if(analyzed==0)throw new ArgumentException("No group with sufficient valid samples.");Console.WriteLine(output);return 0;
    }
}

sealed class Chart:Form {
    readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    readonly Stopwatch clock=Stopwatch.StartNew(); readonly bool moving;
    public Chart(int w,int h,bool animate) {
        moving=animate;
        Text="Tyria Realism Testchart - SYNTHETISCH (kein GW2)";ClientSize=new Size(w,h);
        StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;KeyPreview=true;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;
        timer.Tick+=(_,_)=>{if(moving)Invalidate();};timer.Start();
        KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape)Close();};
    }
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;int w=ClientSize.Width,h=ClientSize.Height;
        using(var bg=new System.Drawing.Drawing2D.LinearGradientBrush(new Point(0,0),new Point(w,h),Color.FromArgb(20,35,50),Color.FromArgb(160,180,150)))g.FillRectangle(bg,0,0,w,h);
        using var font=new Font("Segoe UI",22,FontStyle.Bold);using var small=new Font("Segoe UI",12);
        g.DrawString("TYRIA REALISM | SYNTHETISCHER FENSTERTEST",font,Brushes.White,40,35);
        g.DrawString(moving?"Keine Spielaufnahme. BEWEGUNGSTEST | Esc: Schließen":"Keine Spielaufnahme. STATISCHER VERGLEICH | Esc: Schließen",small,Brushes.White,40,85);
        int offset=moving?(int)(Math.Sin(clock.Elapsed.TotalSeconds)*80):0;
        // Deterministic masonry/foliage-like detail and edge targets.
        for(int row=0;row<15;row++)for(int col=0;col<34;col++) {
            int shade=65+(row*17+col*11)%100;
            using var b=new SolidBrush(Color.FromArgb(shade,shade+12,shade+3));
            g.FillRectangle(b,100+col*36+(row%2)*18+offset,150+row*24,33,21);
        }
        for(int i=0;i<256;i++) {using var b=new SolidBrush(Color.FromArgb(i,i,i));g.FillRectangle(b,100+i*4,h-230,4,55);}
        for(int i=0;i<100;i++)g.DrawLine(i%2==0?Pens.White:Pens.Black,w-440+i*3,170,w-440+i*3,h-280);
        g.DrawString("HUD / Chat  AaBb 0123456789 — Lesbarkeit prüfen",small,Brushes.White,35,h-70);
        g.DrawString("HUD-Rand soll ohne Tonwertänderung bleiben",small,Brushes.White,35,h-45);
        using var red=new SolidBrush(Color.FromArgb(180,190,25,35));g.FillRectangle(red,w/2-190,h-80,380,35);
    }
}

