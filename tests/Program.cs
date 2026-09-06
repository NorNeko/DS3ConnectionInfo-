using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using DS3ConnectionInfo;

internal static class Program
{
    private static int count;
    private static readonly Dictionary<long, byte[]> memory = new Dictionary<long, byte[]>();
    private static readonly Type columns = typeof(Player).Assembly.GetType("DS3ConnectionInfo.ColumnSettings");
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS " + message); count++;
    }
    private static void Set(object target, string property, object value)
    {
        target.GetType().GetProperty(property).SetValue(target, value);
    }
    private static byte[] Read(long address, int length)
    {
        if (!memory.TryGetValue(address, out byte[] value)) throw new UnauthorizedAccessException();
        return value;
    }
    private static void Fixture(int hp, int maximum)
    {
        memory.Clear();
        memory[0x1000 + 0x1FA0] = BitConverter.GetBytes(0x5000L);
        memory[0x1000 + 0x1F90] = BitConverter.GetBytes(0x6000L);
        memory[0x6000 + 0x18] = BitConverter.GetBytes(0x7000L);
        var stats = new byte[48];
        int[] values = {43, 18, 30, 19, 20, 35, 36};
        for (int i = 0; i < values.Length; i++) Array.Copy(BitConverter.GetBytes(values[i]), 0, stats, i * 4, 4);
        Array.Copy(BitConverter.GetBytes(142), 0, stats, 44, 4);
        memory[0x5044] = stats;
        memory[0x70D8] = BitConverter.GetBytes(hp).Concat(BitConverter.GetBytes(maximum)).ToArray();
    }
    private static void Attributes()
    {
        Fixture(800, 1323); var a = PlayerAttributes.Read(0x1000, Read);
        Check(a.Level == 142 && a.Vigor == 43 && a.Attunement == 18 && a.Endurance == 30 &&
            a.Strength == 19 && a.Dexterity == 20 && a.Intelligence == 35 && a.Faith == 36, "attribute block decoded in the intended order");
        Check(a.Health == "800 / 1323", "current and effective maximum HP share one live context");
        Fixture(0,1323); Check(PlayerAttributes.Read(0x1000,Read).Health == "0 / 1323", "death is zero HP, not missing data");
        Fixture(800,1323); memory.Remove(0x70D8); a=PlayerAttributes.Read(0x1000,Read);
        Check(a.Health == "—" && a.Level==142, "missing live HP preserves readable attributes");
        Fixture(1500,1323); Check(PlayerAttributes.Read(0x1000,Read).Health == "—", "inconsistent HP pair is not presented as valid");
        Fixture(800,1323); memory[0x5044]=new byte[12];
        bool failed=false; try { PlayerAttributes.Read(0x1000,Read); } catch(UnauthorizedAccessException) {failed=true;}
        Check(failed,"short attribute read fails instead of displaying zero-filled data");
        failed=false; try { MemoryManager.ReadByteArray(IntPtr.Zero,0,4); } catch(UnauthorizedAccessException) {failed=true;}
        Check(failed,"failed Windows memory read is reported");
    }
    private static void Migration()
    {
        var old=new StringCollection(); for(int i=0;i<11;i++) old.Add(i%2==0 ? "Visible":"Hidden");
        var method=columns.GetMethod("Normalize",BindingFlags.NonPublic|BindingFlags.Static);
        var migrated=(StringCollection)method.Invoke(null,new object[]{old});
        Check(migrated.Count==20 && Enumerable.Range(0,11).All(i=>migrated[i]==old[i]) &&
            Enumerable.Range(11,9).All(i=>migrated[i]=="Hidden"),"old preferences preserved and nine new fields hidden by default");
        migrated[12]="Visible"; var again=(StringCollection)method.Invoke(null,new object[]{migrated});
        Check(again[12]=="Visible" && again.Count==20,"migration preserves newly selected fields on subsequent runs");
        var empty=(StringCollection)method.Invoke(null,new object[]{null});
        Check(empty.Count==20 && empty.Cast<string>().All(x=>x=="Hidden"),"missing preferences handled");
    }
    private static void StalePlayers()
    {
        var field=typeof(Player).GetField("activePlayers",BindingFlags.NonPublic|BindingFlags.Static);
        var players=(IDictionary)field.GetValue(null);
        var player=(Player)FormatterServices.GetUninitializedObject(typeof(Player));
        Fixture(800,1323); Set(player,"Attributes",PlayerAttributes.Read(0x1000,Read));
        Set(player,"CharSlot","1"); Set(player,"CharName","Old player"); Set(player,"TeamId",0);
        var key=Activator.CreateInstance(field.FieldType.GetGenericArguments()[0]); players.Add(key,player);
        Player.UpdateInGameInfo();
        Check(player.Attributes==null && player.CharSlot=="" && player.CharName=="" && player.Health=="—",
            "unavailable slot clears previous identity and attributes");
        players.Clear();
    }
    private static string ResolveText(string value)
    {
        if (value == null) return null;
        var match = System.Text.RegularExpressions.Regex.Match(value, @"\{Binding \[([^\]]+)\]");
        return match.Success ? UiText.Current[match.Groups[1].Value] : value;
    }
    private static void ColumnLayout(string root)
    {
        UiText.Current.SelectLanguage("zh-CN");
        XNamespace w="http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x="http://schemas.microsoft.com/winfx/2006/xaml";
        var main=XDocument.Load(Path.Combine(root,"DS3ConnectionInfo/MainWindow.xaml"));
        var overlay=XDocument.Load(Path.Combine(root,"DS3ConnectionInfo/OverlayWindow.xaml"));
        string[] expected={"等级","生命力","集中力","持久力","力量","敏捷","智力","信仰","当前血量 / 最大生命值"};
        var sessionCols=main.Descendants(w+"DataGrid.Columns").Single().Elements().ToArray();
        var overlayCols=overlay.Descendants(w+"DataGrid.Columns").Single().Elements().ToArray();
        Check(sessionCols.Length==20 && overlayCols.Length==20 &&
            sessionCols.Skip(11).Select(e=>ResolveText((string)e.Attribute("Header"))).SequenceEqual(expected) &&
            overlayCols.Skip(11).Select(e=>ResolveText((string)e.Attribute("Header"))).SequenceEqual(expected),"both grids append exactly the requested columns");
        foreach(var name in new[]{"cbColName","cbOColName"})
        {
            var items=main.Descendants(w+"ComboBox").Single(e=>((string)e.Attribute(x+"Name") ?? (string)e.Attribute("Name"))==name).Elements().Select(e=>ResolveText((string)e.Attribute("Content") ?? e.Value)).ToArray();
            Check(items.Length==20 && items.Skip(11).SequenceEqual(expected),name+" uses matching column indexes");
        }
        Check(Settings.Default.SessColumnDescs.Count==20,"compiled field descriptions include appended fields");
    }
    private static void Render(string output)
    {
        if (Application.Current == null) {var app=new App(); app.InitializeComponent();}
        columns.GetMethod("EnsureCompatible",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var selected=new StringCollection(); for(int i=0;i<20;i++) selected.Add(i==0 || i==1 || i>=11 ? "Visible":"Hidden");
        Settings.Default.OverlayColVisibility=selected;
        var window=new OverlayWindow();
        var grid=(DataGrid)window.FindName("dataGrid");
        var player=(Player)FormatterServices.GetUninitializedObject(typeof(Player));
        Fixture(800,1323); Set(player,"Attributes",PlayerAttributes.Read(0x1000,Read));
        Set(player,"CharSlot","1"); Set(player,"CharName","测试角色"); Set(player,"SteamName","Test"); Set(player,"TeamId",0);
        grid.DataContext=new[]{player};
        grid.Width=1200; // Give the off-screen viewport room to realize every selected column.
        var content=(FrameworkElement)window.Content;
        content.Measure(new Size(2400,300)); content.Arrange(new Rect(content.DesiredSize)); content.UpdateLayout();
        Check(grid.Columns.Count==20 && content.ActualWidth>0,"compiled overlay loads and binds sample attributes");
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(content); var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=File.Create(output)) encoder.Save(file);
        window.Close();
    }
    private static void Capture(FrameworkElement content, string output)
    {
        content.Measure(new Size(960,550)); content.Arrange(new Rect(0,0,960,550)); content.UpdateLayout();
        var bitmap=new RenderTargetBitmap(960,550,96,96,PixelFormats.Pbgra32);
        bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=File.Create(output)) encoder.Save(file);
    }
    private static void Languages(string outputDirectory)
    {
        Check((string)Settings.Default.Properties["UILanguage"].DefaultValue=="zh-CN","first-run language is Simplified Chinese");
        UiText.Current.SelectLanguage("zh-CN");
        if(Application.Current==null){var app=new App();app.InitializeComponent();}
        var window=new MainWindow(false);
        var language=(ComboBox)window.FindName("cbLanguage");
        var tabs=(TabControl)window.FindName("tabCtrl");
        var grid=(DataGrid)window.FindName("dataGridSession");
        var fields=(ComboBox)window.FindName("cbColName");
        var description=(TextBlock)window.FindName("textColDesc");
        var selected=Settings.Default.OverlayColVisibility.Cast<string>().ToArray();
        tabs.SelectedIndex=1;fields.SelectedIndex=12;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"settings-zh.png"));
        Check(language.SelectedIndex==0 && (string)grid.Columns[12].Header=="生命力" && description.Text.Contains("生命力"),"Chinese UI, headings and field descriptions agree");
        language.SelectedIndex=1;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"settings-en.png"));
        Check(UiText.Current.Language=="en" && Settings.Default.UILanguage=="en" &&
            (string)grid.Columns[12].Header=="Vigor" && description.Text.Contains("Vigor"),"selector switches the existing UI to English immediately");
        Check(Team.GetTeamFromId(16).Name=="Dark Spirit","team names follow language");
        Check(Settings.Default.OverlayColVisibility.Cast<string>().SequenceEqual(selected) && fields.SelectedIndex==12,
            "language changes preserve visibility and selected field");
        Settings.Default.HeaderFmtFilterOn="custom {time:HH:mm:ss} {avg}";
        language.SelectedIndex=0;
        Check(Settings.Default.HeaderFmtFilterOn=="custom {time:HH:mm:ss} {avg}" && Team.GetTeamFromId(16).Name=="暗灵", "custom format preserved when returning to Chinese");
        Settings.Default.HeaderFmtFilterOn="[{time:HH:mm:ss}] Ping Filter: {avg}/{abs}";
        UiText.Current.SelectLanguage("zh-CN");
        Check(Settings.Default.HeaderFmtFilterOn.Contains("延迟过滤") && UiText.Current.PingStatus.Contains("延迟过滤"),"built-in header and live filter status are localized");
        tabs.SelectedIndex=2;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"overlay-settings-zh.png"));
        language.SelectedIndex=1;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"overlay-settings-en.png"));
        window.Close();
        UiText.Current.SelectLanguage("zh-CN");
    }
    private static void FieldOptions(string root, string outputDirectory)
    {
        Check(typeof(Player).Assembly.GetType("DS3ConnectionInfo.VersionCheck")==null &&
            !File.ReadAllText(Path.Combine(root,"DS3ConnectionInfo/MainWindow.xaml.cs")).Contains("api.github.com"),
            "remote update checker removed from compiled application");
        var old = new StringCollection();for(int i=0;i<20;i++)old.Add(i==12 ? "Visible":"Hidden");
        Settings.Default.OverlayColVisibility=old;
        Settings.Default.SessColumnVisibility=new StringCollection();
        Settings.Default.SessionColumnOrder=new StringCollection();
        Settings.Default.OverlayColumnColors=new StringCollection();
        var window=new MainWindow(false);
        var grid=(DataGrid)window.FindName("dataGridSession");
        var overlay=(OverlayWindow)typeof(MainWindow).GetField("overlay",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
        var overlayGrid=(DataGrid)overlay.FindName("dataGrid");
        var selector=(ComboBox)window.FindName("cbOColName");
        var visible=window.FindName("swOColVisible");
        var picker=window.FindName("fieldColorPicker");
        selector.SelectedIndex=12;
        Check(grid.Columns[12].Visibility==Visibility.Visible && overlayGrid.Columns[12].Visibility==Visibility.Visible,
            "existing visible overlay fields appear in session list on upgrade");
        Set(visible,"IsOn",false);
        Check(grid.Columns[12].Visibility==Visibility.Collapsed && overlayGrid.Columns[12].Visibility==Visibility.Collapsed,
            "hiding a field removes it from both layouts without a blank column");
        Set(visible,"IsOn",true);
        Check(Settings.Default.SessColumnVisibility[12]=="Visible" && grid.Columns[12].Visibility==Visibility.Visible,
            "showing a field updates the session list immediately");
        Set(picker,"SelectedColor",Colors.Magenta);
        Check(Settings.Default.OverlayColumnColors[12]==Colors.Magenta.ToString(),"selected field color is stored by field identity");
        selector.SelectedIndex=13;
        Check(Settings.Default.OverlayColumnColors[13]=="", "selecting another field does not copy or create a color override");
        Set(visible,"IsOn",true);Set(picker,"SelectedColor",Colors.Cyan);
        grid.Columns[12].DisplayIndex=0;
        typeof(MainWindow).GetMethod("SessionColumnReordered",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(window,new object[]{grid,new DataGridColumnEventArgs(grid.Columns[12])});
        Check(Settings.Default.SessionColumnOrder[0]=="12" && overlayGrid.Columns[12].DisplayIndex==0,
            "drag reorder is saved and applied to the overlay");
        selector.SelectedIndex=12;
        Check((Color?)picker.GetType().GetProperty("SelectedColor").GetValue(picker)==Colors.Magenta && Settings.Default.OverlayColumnColors[13]==Colors.Cyan.ToString(),
            "colors remain attached to their fields after reordering");
        var converter=new FieldColorConverter();
        Check(((SolidColorBrush)converter.Convert(new object[]{Settings.Default.OverlayColumnColors,Brushes.Green},typeof(Brush),"12",null)).Color==Colors.Magenta,
            "custom field color overrides the original dynamic color rule");
        ((Button)window.FindName("resetFieldColor")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(Settings.Default.OverlayColumnColors[12]=="" &&
            ((SolidColorBrush)converter.Convert(new object[]{Settings.Default.OverlayColumnColors,Brushes.Green},typeof(Brush),"12",null)).Color==Colors.Green,
            "Default restores original per-player color rules");
        Set(picker,"SelectedColor",Colors.Magenta);
        var player=(Player)FormatterServices.GetUninitializedObject(typeof(Player));
        Fixture(800,1323);Set(player,"Attributes",PlayerAttributes.Read(0x1000,Read));
        Set(player,"CharSlot","1");Set(player,"CharName","Test");Set(player,"SteamName","Test");Set(player,"TeamId",1);
        ((IList)typeof(MainWindow).GetField("playerData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window)).Add(player);
        var overlayContent=(FrameworkElement)overlay.Content;
        overlayGrid.Width=900;
        Capture(overlayContent,Path.Combine(outputDirectory,"field-colors.png"));
        var vigor=FindVisual<OverlayTextBlock>(overlayContent).FirstOrDefault(t=>t.Text=="43");
        Check(vigor!=null && ((SolidColorBrush)vigor.Fill).Color==Colors.Magenta,"actual overlay cell uses the configured field color");
        var tabs=(TabControl)window.FindName("tabCtrl");tabs.SelectedIndex=2;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"field-controls-zh.png"));
        ((ComboBox)window.FindName("cbLanguage")).SelectedIndex=1;
        Capture((FrameworkElement)window.Content,Path.Combine(outputDirectory,"field-controls-en.png"));
        window.Close();
        var reopened=new MainWindow(false);
        var restored=(DataGrid)reopened.FindName("dataGridSession");
        Check(restored.Columns[12].DisplayIndex==0 && Settings.Default.OverlayColumnColors[12]==Colors.Magenta.ToString(),
            "reopening restores order without moving field colors");
        ((Button)reopened.FindName("btnResetSettings")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(restored.Columns[12].DisplayIndex==12 && Settings.Default.OverlayColumnColors.Cast<string>().All(v=>v=="") &&
            Settings.Default.SessColumnVisibility.Cast<string>().SequenceEqual(Settings.Default.OverlayColVisibility.Cast<string>()),
            "reset restores default order and colors with synchronized visibility");
        reopened.Close();
    }
    private static IEnumerable<T> FindVisual<T>(DependencyObject parent) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
        {
            var child=VisualTreeHelper.GetChild(parent,i);
            if(child is T item) yield return item;
            foreach(var descendant in FindVisual<T>(child)) yield return descendant;
        }
    }
    private static void SaveFields()
    {
        columns.GetMethod("EnsureCompatible",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        var order=new StringCollection();order.Add("12");
        Settings.Default.SessionColumnOrder=(StringCollection)columns.GetMethod("NormalizeOrder",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{order});
        Settings.Default.OverlayColumnColors[12]=Colors.Magenta.ToString();
        Settings.Default.OverlayColVisibility[12]="Visible";
        Settings.Default.Save();
    }
    private static void CheckSavedFields()
    {
        columns.GetMethod("EnsureCompatible",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        Check(Settings.Default.SessionColumnOrder[0]=="12" && Settings.Default.OverlayColumnColors[12]==Colors.Magenta.ToString() &&
            Settings.Default.OverlayColVisibility[12]=="Visible" && Settings.Default.SessColumnVisibility[12]=="Visible",
            "field order, color and visibility survive a new process");
    }
    [DllImport("kernel32.dll",SetLastError=true)] private static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    private static void Live()
    {
        var process=Process.GetProcessesByName("DarkSoulsIII").Single();
        IntPtr handle=OpenProcess(0x410,false,process.Id); // Query and read only.
        if(handle==IntPtr.Zero) throw new Exception("Cannot open game read-only");
        typeof(DS3Interop).GetProperty("Process").SetValue(null,process);
        typeof(DS3Interop).GetProperty("ProcHandle").SetValue(null,handle);
        try
        {
            long host=MemoryManager.ReadGenericPtr<long>(handle,DS3Interop.BaseB,0x80);
            var a=DS3Interop.GetPlayerAttributes(host);
            Check(a.Level>0 && a.MaximumHp.HasValue,"production reader resolves live local character and HP");
            Console.WriteLine($"Local: level {a.Level}; VIG {a.Vigor}; ATT {a.Attunement}; END {a.Endurance}; STR {a.Strength}; DEX {a.Dexterity}; INT {a.Intelligence}; FTH {a.Faith}; HP {a.Health}");
            int remote=0;
            for(int slot=1;slot<=5;slot++)
            {
                long p=DS3Interop.GetPlayerBase(slot); if(p==0)continue;
                var b=DS3Interop.GetPlayerAttributes(p); remote++;
                Console.WriteLine($"Remote slot {slot}: level {b.Level}; HP {b.Health}");
            }
            Console.WriteLine("Remote characters available: "+remote);
        }
        finally {DS3Interop.Detach();}
    }
    [STAThread] private static int Main(string[] args)
    {
        try
        {
            if(args.Contains("--save-fields")){SaveFields();return 0;}
            if(args.Contains("--check-fields")){CheckSavedFields();return 0;}
            if(args.Length==2 && args[0]=="--save-language") { UiText.Current.SelectLanguage(args[1]);Settings.Default.Save();return 0; }
            if(args.Length==2 && args[0]=="--check-language") {Check(UiText.Current.Language==args[1],"saved language restored in a new process");return 0;}
            if(args.Contains("--live")) {Live();return 0;}
            Attributes();Migration();StalePlayers();ColumnLayout(args[0]);
            if(args.Length>1) {Render(args[1]);Languages(Path.GetDirectoryName(args[1]));FieldOptions(args[0],Path.GetDirectoryName(args[1]));}
            Console.WriteLine(count+" checks passed");return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
