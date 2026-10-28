using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;
namespace ImageConverterUtility {
internal static class UiSmokeTests {
[StructLayout(LayoutKind.Sequential)] struct ComboInfo { public int Size; public Rectangle Item,Button; public int State; public IntPtr Combo,Edit,List; }
[DllImport("user32.dll")] static extern bool GetComboBoxInfo(IntPtr handle,ref ComboInfo info);
[DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr handle,out Rectangle bounds);
[DllImport("user32.dll")] static extern bool PrintWindow(IntPtr handle,IntPtr dc,int flags);
static object Field(object o,string n) { return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o); }
static void Check(bool ok,string message) { if(!ok) throw new Exception(message); Console.WriteLine("PASS: "+message); }
static object Call(object o,string n,params object[] args) { if(n=="ConvertAllAsync") SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); return o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args); }
static void Capture(Form f,string name) { using(Bitmap b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size)); b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name+".png")); } }
static void Pump(Task task) { while(!task.IsCompleted) { Application.DoEvents(); Thread.Sleep(5); } task.GetAwaiter().GetResult(); }
[STAThread] static int Main() {
try {
ScrollbarTheme.Initialize(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Control.CheckForIllegalCrossThreadCalls=true;
string root=AppDomain.CurrentDomain.BaseDirectory;
foreach(string name in new[]{"sample.png","sample.pdf","sample.mp4","broken.png","red.png","blue.png"}) {
 string path=Path.Combine(root,"Input",name); if(File.Exists(path)) File.Delete(path);
}
using(ConverterForm f=new ConverterForm()) {
f.Show(); Application.DoEvents(); SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
var list=(ListView)Field(f,"fileList"); var formats=(ComboBox)Field(f,"outputFormatBox");
Check(formats.Items.Count==32,"All 32 output formats retained");
if(list.Items.Count==0) Capture(f,"ui-empty");
f.Size=f.MinimumSize; Application.DoEvents();
foreach(Control c in f.Controls) if(c.Visible) Check(f.ClientRectangle.Contains(c.Bounds),"Minimum window contains "+c.GetType().Name+" "+c.Text.Replace("\r\n"," "));
Capture(f,"ui-minimum"); f.Size=new Size(740,520);
using(var sample=new Bitmap(100,60)) { using(var g=Graphics.FromImage(sample)) g.Clear(Color.CornflowerBlue); sample.Save(Path.Combine(root,"sample.png")); }
Call(f,"ImportFiles",(object)new string[]{Path.Combine(root,"sample.png"),Path.Combine(root,"sample.png")});
Check(list.Items.Count==1,"Duplicate imports skipped");
Check(!((Control)Field(f,"emptyState")).Visible,"Empty state hides after import");
((TextBox)Field(f,"widthBox")).Text="50";
foreach(string name in new[]{"PNG","JPEG","WebP","GIF","TIFF","QOI","ICO"}) {
for(int i=0;i<formats.Items.Count;i++) if(((OutputFormat)formats.Items[i]).Name==name) formats.SelectedIndex=i;
var task=(Task)Call(f,"ConvertAllAsync");
bool uiCallback=false; f.BeginInvoke(new Action(delegate {uiCallback=true;}));
Check(!((Control)Field(f,"widthBox")).Enabled && ((Control)Field(f,"cancelButton")).Visible,"Busy state locks settings and exposes cancel");
Pump(task); Application.DoEvents(); Check(uiCallback,"UI message loop remains active during conversion");
var format=(OutputFormat)formats.SelectedItem;
using(var image=new MagickImage(Path.Combine(root,"Output","sample-conv"+format.PrimaryExtension))) Check(image.Width==50 && image.Height==30,name+" conversion preserves aspect ratio");
Check(((Control)Field(f,"convertButton")).Enabled,"Controls restored after batch");
}
((TextBox)Field(f,"widthBox")).Text="200";
Check((string)Call(f,"BuildMagickResize")=="200x>","Dimension resize prevents upscale");
((TextBox)Field(f,"widthBox")).Clear(); ((TextBox)Field(f,"scaleBox")).Text="50";
Check((string)Call(f,"BuildMagickResize")=="50%","Percentage resize retained");
Capture(f,"ui-complete");
File.WriteAllText(Path.Combine(root,"Input","broken.png"),"not an image"); Call(f,"RefreshFileList"); Pump((Task)Call(f,"ConvertAllAsync"));
Check(list.Items.Cast<ListViewItem>().Single(i=>i.Text=="broken.png").SubItems[3].Text.StartsWith("Error") && list.Items.Cast<ListViewItem>().Single(i=>i.Text=="sample.png").SubItems[3].Text=="Done","Bad files do not stop the rest of the batch");
File.Delete(Path.Combine(root,"Input","broken.png"));
for(int i=0;i<12;i++) File.Copy(Path.Combine(root,"sample.png"),Path.Combine(root,"Input","cancel-"+i+".png"),true);
Call(f,"RefreshFileList"); var canceled=(Task)Call(f,"ConvertAllAsync"); ((CancellationTokenSource)Field(f,"conversionCancel")).Cancel(); Pump(canceled);
Check(((Label)Field(f,"statusLabel")).Text.StartsWith("Canceled"),"Cancellation finishes cleanly");
for(int i=0;i<12;i++) File.Delete(Path.Combine(root,"Input","cancel-"+i+".png"));
Call(f,"RefreshFileList");
for(int i=0;i<80;i++) list.Items.Add(new ListViewItem(new[]{"Landscape photograph "+i+".png","Image","2.4 MB","Ready"}));
Call(f,"FitColumnsToList"); Application.DoEvents(); Capture(f,"ui-queue");
formats.DroppedDown=true; Application.DoEvents();
ComboInfo comboInfo=new ComboInfo(); comboInfo.Size=Marshal.SizeOf(typeof(ComboInfo));
Check(GetComboBoxInfo(formats.Handle,ref comboInfo),"Format dropdown scrollbar is accessible");
Rectangle dropdown; GetWindowRect(comboInfo.List,out dropdown);
using(var bitmap=new Bitmap(dropdown.Width-dropdown.X,dropdown.Height-dropdown.Y)) {
using(var graphics=Graphics.FromImage(bitmap)) {IntPtr dc=graphics.GetHdc(); try {PrintWindow(comboInfo.List,dc,0);} finally {graphics.ReleaseHdc(dc);} }
bitmap.Save(Path.Combine(root,"ui-format-dropdown.png")); }
formats.DroppedDown=false;
using(var timer=new System.Windows.Forms.Timer()) {
timer.Interval=150; timer.Tick+=delegate {timer.Stop(); foreach(Form dialog in Application.OpenForms) if(dialog!=f && dialog.Text=="Supported File Types") {Capture(dialog,"ui-help");dialog.Close();break;} };
timer.Start(); Call(f,"ShowSupportedFormatsDialog");
} f.Size=new Size(1400,900); Application.DoEvents(); Capture(f,"ui-wide");
formats.SelectedIndex=0;
if(File.Exists(Path.Combine(root,"sample.pdf"))) {
Call(f,"ImportFiles",(object)new[]{Path.Combine(root,"sample.pdf")});
Pump((Task)Call(f,"ConvertAllAsync"));
foreach(ListViewItem item in list.Items) Console.WriteLine(item.Text+": "+item.SubItems[3].Text);
Check(Directory.Exists(Path.Combine(root,"Output","sample-conv")) && Directory.GetFiles(Path.Combine(root,"Output","sample-conv"),"*.png").Length>=2,"Multi-page PDF conversion");
File.Delete(Path.Combine(root,"Input","sample.pdf"));
}
if(File.Exists(Path.Combine(root,"sample.mp4"))) {
Call(f,"ImportFiles",(object)new[]{Path.Combine(root,"sample.mp4")});
Pump((Task)Call(f,"ConvertAllAsync"));
foreach(ListViewItem item in list.Items) if(((string)item.Tag).EndsWith(".mp4")) { if(item.SubItems[3].Text=="Error: Windows cannot decode this video") Console.WriteLine("SKIP: Windows cannot decode the video fixture (also fails with the original reader)."); else Check(item.SubItems[3].Text=="Done","Video image sequence: "+item.SubItems[3].Text); }
File.Delete(Path.Combine(root,"Input","sample.mp4"));
}
// Animation round-trips use solid colors and mixed dimensions to verify frame order and padding.
Call(f,"RefreshFileList"); f.Size=new Size(740,520);
string red=Path.Combine(root,"red.png"), blue=Path.Combine(root,"blue.png");
using(var image=new MagickImage(MagickColors.Red,32,20)) image.Write(red);
using(var image=new MagickImage(MagickColors.Blue,16,12)) image.Write(blue);
foreach(string formatName in new[]{"GIF","WebP","MNG"}) {
var format=formats.Items.Cast<OutputFormat>().Single(x=>x.Name==formatName);
string saved=(string)Call(f,"CreateAnimationCore",new[]{red,blue},format,"",120,true,CancellationToken.None);
using(var frames=new MagickImageCollection(saved)) {
Check(frames.Count==2,formatName+" retains both frames"); frames.Coalesce();
Check(frames.All(x=>x.Width==32 && x.Height==20),formatName+" centers mixed dimensions on a shared canvas");
Check(frames[0].AnimationDelay==12 && frames[0].AnimationIterations==0,formatName+" preserves timing and looping");
using(var pixels=frames[0].GetPixels()) Check(pixels.GetPixel(16,10).ToColor().R>pixels.GetPixel(16,10).ToColor().B,formatName+" first frame is red");
using(var pixels=frames[1].GetPixels()) Check(pixels.GetPixel(16,10).ToColor().B>pixels.GetPixel(16,10).ToColor().R,formatName+" second frame is blue");
}
var before=Directory.GetDirectories(Path.Combine(root,"Output"));
Call(f,"ExtractFramesCore",saved,CancellationToken.None,(OutputFormat)formats.Items[0],"");
string folder=Directory.GetDirectories(Path.Combine(root,"Output")).Except(before).Single();
Check(Directory.GetFiles(folder,"frame-*.png").Length==2,formatName+" extracts two numbered PNG frames into a folder");
using(var image=new MagickImage(Path.Combine(folder,"frame-00002.png"))) Check(image.Width==32 && image.Height==20,formatName+" extraction writes full-canvas frames");
}
// An optimized GIF contains partial frames; extraction must composite them.
string delta=Path.Combine(root,"delta.gif"), deltaStill=Path.Combine(root,"delta.png");
using(var bitmap=new Bitmap(32,20)) { using(var g=Graphics.FromImage(bitmap)) { g.Clear(Color.Red); g.FillRectangle(Brushes.Blue,0,0,8,8); } bitmap.Save(deltaStill); }
using(var frames=new MagickImageCollection()) {
frames.Add(new MagickImage(red)); frames.Add(new MagickImage(deltaStill));
foreach(var frame in frames) {frame.AnimationDelay=10; frame.Format=MagickFormat.Gif;}
frames.Optimize(); frames.Write(delta);
}
var beforeDelta=Directory.GetDirectories(Path.Combine(root,"Output"));
Call(f,"ExtractFramesCore",delta,CancellationToken.None,(OutputFormat)formats.Items[0],"");
string deltaFolder=Directory.GetDirectories(Path.Combine(root,"Output")).Except(beforeDelta).Single();
using(var image=new MagickImage(Path.Combine(deltaFolder,"frame-00002.png"))) {
Check(image.Width==32 && image.Height==20,"Optimized GIF frames are restored to full size");
using(var pixels=image.GetPixels()) { Check(pixels.GetPixel(1,1).ToColor().B>pixels.GetPixel(1,1).ToColor().R,"Extracted delta appears in the right position"); Check(pixels.GetPixel(20,10).ToColor().R>pixels.GetPixel(20,10).ToColor().B,"Extracted delta preserves unchanged background"); }
}
var gif=formats.Items.Cast<OutputFormat>().Single(x=>x.Name=="GIF");
string once=(string)Call(f,"CreateAnimationCore",new[]{red,blue},gif,"",200,false,CancellationToken.None);
using(var frames=new MagickImageCollection(once)) Check(frames[0].AnimationIterations==1,"Loop off plays once");
using(var canceledSource=new CancellationTokenSource()) {
canceledSource.Cancel(); bool canceledWorker=false;
try { Call(f,"CreateAnimationCore",new[]{red,blue},gif,"",100,true,canceledSource.Token); }
catch(TargetInvocationException ex) { canceledWorker=ex.InnerException is OperationCanceledException; }
Check(canceledWorker,"Animation worker honors cancellation");
}
Check(Directory.GetFiles(Path.Combine(root,"Output"),".animation-*").Length==0,"No temporary animation files remain");
var mode=(ComboBox)Field(f,"modeBox"); mode.SelectedIndex=2;
((TextBox)Field(f,"scaleBox")).Text="100";
Call(f,"ImportFiles",(object)new[]{red,blue});
list.Items[0].Selected=true; Call(f,"MoveSelectedFrames",1);
Check(list.Items[1].Text=="sample.png","Frame ordering buttons change queue order");
Call(f,"RefreshFileList"); Check(list.Items[1].Text=="sample.png","Refresh preserves chosen frame order");
Capture(f,"ui-animation"); f.Size=f.MinimumSize; Application.DoEvents();
foreach(Control control in f.Controls) if(control.Visible) Check(f.ClientRectangle.Contains(control.Bounds),"Compact animation layout contains "+control.GetType().Name);
Capture(f,"ui-animation-minimum");
((TextBox)Field(f,"delayBox")).Text="0"; Pump((Task)Call(f,"ConvertAllAsync"));
Check(((Label)Field(f,"statusLabel")).Text.StartsWith("Frame time"),"Invalid animation timing is rejected");
((TextBox)Field(f,"delayBox")).Text="150";
var creation=(Task)Call(f,"ConvertAllAsync"); Check(!mode.Enabled,"Mode cannot change during animation creation"); Pump(creation);
Check(((Label)Field(f,"statusLabel")).Text.StartsWith("Saved animation"),"Create animation works through the UI workflow");
mode.SelectedIndex=1; formats.SelectedIndex=0; Capture(f,"ui-extract");
Pump((Task)Call(f,"ConvertAllAsync")); Check(list.Items.Cast<ListViewItem>().All(i=>i.SubItems[3].Text=="Done"),"Extraction works through the UI workflow");
File.Delete(Path.Combine(root,"Input","red.png")); File.Delete(Path.Combine(root,"Input","blue.png"));
f.Close();
}
Console.WriteLine("All smoke tests passed."); return 0;
} catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
}
}}
