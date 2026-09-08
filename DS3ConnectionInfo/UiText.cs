using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DS3ConnectionInfo
{
    public sealed class UiText : INotifyPropertyChanged
    {
        private static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            { "Session", new[] { "Session Info", "联机玩家" } },
            { "Settings", new[] { "Settings", "设置" } },
            { "Overlay", new[] { "Overlay", "悬浮显示" } },
            { "Slot", new[] { "Slot", "玩家编号" } },
            { "Character", new[] { "Char. Name", "角色名称" } },
            { "Team", new[] { "Team", "阵营" } },
            { "TeamName", new[] { "Team Name", "阵营" } },
            { "SteamName", new[] { "Steam Name", "Steam 昵称" } },
            { "OverlayName", new[] { "Overlay Name", "显示名称" } },
            { "SteamId", new[] { "Steam ID 64", "Steam ID 64" } },
            { "Ping", new[] { "Ping", "延迟" } },
            { "AveragePing", new[] { "Average Ping", "平均延迟" } },
            { "AvgPing", new[] { "Avg. Ping", "平均延迟" } },
            { "Jitter", new[] { "Jitter", "延迟抖动" } },
            { "LatePackets", new[] { "Late Packet %", "丢包率" } },
            { "Location", new[] { "Location", "地区" } },
            { "Level", new[] { "Level", "等级" } },
            { "Vigor", new[] { "Vigor", "生命力" } },
            { "Attunement", new[] { "Attunement", "集中力" } },
            { "Endurance", new[] { "Endurance", "持久力" } },
            { "Strength", new[] { "Strength", "力量" } },
            { "Dexterity", new[] { "Dexterity", "敏捷" } },
            { "Intelligence", new[] { "Intelligence", "智力" } },
            { "Faith", new[] { "Faith", "信仰" } },
            { "Health", new[] { "Current HP / Max HP", "当前血量 / 最大生命值" } },
            { "FilterWarning", new[] { "WARNING: This ping filter is very crude, and WILL increase the time it takes to find online activity.", "提示：延迟过滤方式较为简单，启用后会增加联机匹配的等待时间。" } },
            { "FilterHelp", new[] { "When the ping filter is on, invasions and summon signs are cancelled and reset if the average or an individual player’s ping is too high. See the FAQ on the", "启用延迟过滤后，若平均延迟或任一玩家的延迟过高，入侵或召唤符将被取消并自动重置。详情请参阅" } },
            { "Github", new[] { "GitHub page", "GitHub 项目说明" } },
            { "PingFilter", new[] { "Ping Filter", "延迟过滤" } },
            { "Enabled", new[] { "Enabled", "开启" } },
            { "Disabled", new[] { "Disabled", "关闭" } },
            { "FilterHotkey", new[] { "Ping Filter Hotkey", "延迟过滤快捷键" } },
            { "MaxAverage", new[] { "Max Average Ping", "平均延迟上限" } },
            { "MaxAbsolute", new[] { "Max Absolute Ping", "单人延迟上限" } },
            { "Delay", new[] { "Filter Delay (s)", "过滤等待时间（秒）" } },
            { "REO", new[] { "REO Hotkey", "血红眼眸宝珠快捷键" } },
            { "RSD", new[] { "RSD Hotkey", "红标记蜡石快捷键" } },
            { "WSD", new[] { "WSD Hotkey", "白标记蜡石快捷键" } },
            { "Leave", new[] { "Leave Session Hotkey", "离开会话快捷键" } },
            { "REOMode", new[] { "REO Hotkey Effect", "红眼球快捷键模式" } },
            { "Spam", new[] { "Spam", "连续使用" } },
            { "Normal", new[] { "Normal", "普通" } },
            { "Hotkeys", new[] { "Enable Hotkeys", "启用快捷键" } },
            { "Yes", new[] { "Yes", "是" } },
            { "No", new[] { "No", "否" } },
            { "HotkeysTip", new[] { "Enable/disable the hotkey system.", "开启或关闭所有快捷键。" } },
            { "SessionColumns", new[] { "Show/Hide Session Info Columns:", "联机列表显示字段：" } },
            { "Shown", new[] { "Shown", "显示" } },
            { "Hidden", new[] { "Hidden", "隐藏" } },
            { "Reset", new[] { "Reset All Settings", "恢复默认设置" } },
            { "DisplayOverlay", new[] { "Display Overlay", "显示悬浮层" } },
            { "OverlayHotkey", new[] { "Overlay Hotkey", "悬浮层快捷键" } },
            { "Borderless", new[] { "Borderless DS3", "游戏无边框模式" } },
            { "BorderlessHotkey", new[] { "Borderless Hotkey", "无边框模式快捷键" } },
            { "Anchor", new[] { "Overlay Anchor", "悬浮层对齐位置" } },
            { "TopRight", new[] { "Top Right", "右上角" } },
            { "TopLeft", new[] { "Top Left", "左上角" } },
            { "BottomRight", new[] { "Bottom Right", "右下角" } },
            { "BottomLeft", new[] { "Bottom Left", "左下角" } },
            { "XOffset", new[] { "Overlay X Offset", "水平偏移" } },
            { "YOffset", new[] { "Overlay Y Offset", "垂直偏移" } },
            { "Font", new[] { "Overlay Font:", "悬浮层字体：" } },
            { "NameFormat", new[] { "Name Format", "名称格式" } },
            { "ConnectingFormat", new[] { "N.F. (Connecting)", "连接中的名称格式" } },
            { "Stroke", new[] { "Stroke Thickness", "文字描边粗细" } },
            { "TextColor", new[] { "Text Color", "文字颜色" } },
            { "StrokeColor", new[] { "Stroke Color", "描边颜色" } },
            { "ConnectingColor", new[] { "Connecting Color", "连接中颜色" } },
            { "Ping50", new[] { "Ping <=50 Color", "延迟 ≤50 ms 颜色" } },
            { "Ping100", new[] { "Ping <=100 Color", "延迟 ≤100 ms 颜色" } },
            { "Ping200", new[] { "Ping <=200 Color", "延迟 ≤200 ms 颜色" } },
            { "PingOver200", new[] { "Ping >200 Color", "延迟 >200 ms 颜色" } },
            { "HeaderOn", new[] { "Header Fmt. (Filter On)", "标题格式（过滤开启）" } },
            { "HeaderOff", new[] { "Header Fmt. (Filter Off)", "标题格式（过滤关闭）" } },
            { "TimeHelp", new[] { "Double click for more info on C# time format strings", "双击查看时间格式说明；保留花括号中的占位符。" } },
            { "ColumnHelp", new[] { "To see a description of these values, see the Settings tab.", "字段含义可在“设置”页查看。" } },
            { "OverlayColumns", new[] { "Show/Hide Overlay Columns:", "悬浮层显示字段：" } },
            { "FieldColorHelp", new[] { "Color of the selected overlay field. Until customized, the original text/team/ping color rules apply.", "当前悬浮字段的颜色；未自定义时沿用原有文字、阵营或延迟颜色规则。" } },
            { "DefaultColor", new[] { "Default", "默认" } },
            { "DefaultColorHelp", new[] { "Restore the original color rules for this field.", "恢复该字段原有的颜色规则。" } },
            { "Language", new[] { "Language", "界面语言" } },
            { "Title", new[] { "DS3 Connection Info", "黑暗之魂3 联机信息" } },
            { "Closed", new[] { "DS3: CLOSED", "DS3：未运行" } },
            { "Running", new[] { "DS3: RUNNING", "DS3：运行中" } },
            { "FilterOff", new[] { "PING FILTER: OFF", "延迟过滤：关闭" } },
            { "FilterStatus", new[] { "PING FILTER: {0}/{1}", "延迟过滤：{0}/{1}" } },
            { "Copy", new[] { "Copy", "复制" } },
            { "Close", new[] { "Close", "关闭" } },
            { "Unhandled", new[] { "Unhandled Exception: {0}", "未处理的错误：{0}" } },
            { "SteamError", new[] { "Steam API Error", "Steam 接口错误" } },
            { "SteamInit", new[] { "Could not initialize Steam API", "无法初始化 Steam 接口" } },
            { "WindowsError", new[] { "WINAPI Error", "Windows 接口错误" } },
            { "KeyboardInit", new[] { "Could not initialize keyboard hook for hotkeys", "无法初始化键盘快捷键" } },
            { "OverlayInit", new[] { "Could not setup overlay message hook", "无法初始化悬浮层窗口监听" } },
            { "SteamRelay", new[] { "[STEAM RELAY]", "[Steam 中继]" } },
            { "WebError", new[] { "WEB ERROR: ", "网络错误：" } },
            { "GeoError", new[] { "GEOLOCATION FAIL: ", "地区查询失败：" } },
            { "HttpError", new[] { "HTTP ERROR ({0}): {1}", "HTTP 错误（{0}）：{1}" } },
            { "AvailableColors", new[] { "Available colors", "可选颜色" } },
            { "StandardColors", new[] { "Standard colors", "标准颜色" } },
            { "RecentColors", new[] { "Recent colors", "最近使用" } },
            { "CustomColors", new[] { "Custom colors", "自定义颜色" } },
            { "Palettes", new[] { "Palettes", "调色板" } },
            { "Advanced", new[] { "Advanced", "高级" } },
            { "Alpha", new[] { "Alpha", "透明度" } },
            { "Red", new[] { "Red", "红" } },
            { "Green", new[] { "Green", "绿" } },
            { "Blue", new[] { "Blue", "蓝" } },
            { "Hue", new[] { "Hue", "色相" } },
            { "Saturation", new[] { "Saturation", "饱和度" } },
            { "Value", new[] { "Value", "明度" } },
            { "ColorName", new[] { "Color name", "颜色名称" } },
            { "Preview", new[] { "Preview", "预览" } },
            { "Team0", new[] { "Host", "房主" } },
            { "Team1", new[] { "Phantom", "白灵" } },
            { "Team2", new[] { "Black Phantom", "黑灵" } },
            { "Team3", new[] { "Hollow", "游魂" } },
            { "Team4", new[] { "Enemy", "敌人" } },
            { "Team5", new[] { "Boss (giants, big lizard)", "首领（巨人、大蜥蜴）" } },
            { "Team6", new[] { "Friend", "友方" } },
            { "Team7", new[] { "AngryFriend", "敌对友方" } },
            { "Team8", new[] { "DecoyEnemy", "诱饵敌人" } },
            { "Team9", new[] { "BloodChild", "血之子" } },
            { "Team10", new[] { "BattleFriend", "战斗友方" } },
            { "Team11", new[] { "Dragon", "龙" } },
            { "Team12", new[] { "Dark Spirit", "暗灵" } },
            { "Team13", new[] { "Watchdog of Farron", "法兰守卫" } },
            { "Team14", new[] { "Aldrich Faithful", "吞噬神明的守护人" } },
            { "Team15", new[] { "Darkwraiths", "吸魂鬼" } },
            { "Team16", new[] { "NPC", "NPC" } },
            { "Team17", new[] { "Hostile NPC", "敌对 NPC" } },
            { "Team18", new[] { "Arena", "竞技场" } },
            { "Team19", new[] { "Mad Phantom", "狂灵（召唤）" } },
            { "Team20", new[] { "Mad Spirit", "狂灵（入侵）" } },
            { "Team21", new[] { "Giant crabs, Dragons from Lothric castle", "巨蟹、洛斯里克城飞龙" } },
            { "Team22", new[] { "None", "无" } },
            { "Description0", new[] { "The slot this player occupies in the game's SessionInfo structure.", "玩家在当前联机会话中的编号（1–5）。" } },
            { "Description1", new[] { "The player's in-game character name.", "游戏中的角色名称。" } },
            { "Description2", new[] { "The player's in-game team (e.g. Host, Dark Spirit, etc.)", "角色阵营，例如房主、白灵、暗灵。" } },
            { "Description3", new[] { "The player's Steam profile name.", "玩家的 Steam 昵称。" } },
            { "Description4", new[] { "A combination of the player's Steam and in-game name, formatted according to overlay settings.", "按名称格式组合的 Steam 昵称与角色名称。" } },
            { "Description5", new[] { "The player's Steam ID 64 (number found in a Steam profile link).", "玩家的 64 位 Steam ID。" } },
            { "Description6", new[] { "The player's ping (roundtrip time latency), calculated using the delay between sent and recieved STUN packets. Displayed in milliseconds.", "与玩家之间的往返延迟，单位为毫秒。" } },
            { "Description7", new[] { "The player's average ping over the last 10 recieved STUN packets. Displayed in milliseconds.", "最近 10 次延迟采样的平均值，单位为毫秒。" } },
            { "Description8", new[] { "The standard deviation of the player's ping over the last 10 samples. A high value indicates an unstable connection. Displayed in milliseconds.", "最近 10 次延迟采样的标准差；越高表示连接越不稳定。" } },
            { "Description9", new[] { "The ratio of late vs. on time STUN reply packets from the player, as a percentage. Another indicator of connection quality.", "延迟返回的探测回复比例，用于判断连接质量。" } },
            { "Description10", new[] { "The state and country of the player, if available.", "根据连接地址查询的地区；使用中继时显示 Steam 中继。" } },
            { "Description11", new[] { "Level: locally synchronized character data.", "等级：读取本地同步的角色数据。" } },
            { "Description12", new[] { "Vigor: locally synchronized character data.", "生命力：读取本地同步的角色数据。" } },
            { "Description13", new[] { "Attunement: locally synchronized character data.", "集中力：读取本地同步的角色数据。" } },
            { "Description14", new[] { "Endurance: locally synchronized character data.", "持久力：读取本地同步的角色数据。" } },
            { "Description15", new[] { "Strength: locally synchronized character data.", "力量：读取本地同步的角色数据。" } },
            { "Description16", new[] { "Dexterity: locally synchronized character data.", "敏捷：读取本地同步的角色数据。" } },
            { "Description17", new[] { "Intelligence: locally synchronized character data.", "智力：读取本地同步的角色数据。" } },
            { "Description18", new[] { "Faith: locally synchronized character data.", "信仰：读取本地同步的角色数据。" } },
            { "Description19", new[] { "Current HP / effective maximum HP. Unavailable data is shown as —.", "角色当前血量 / 当前有效最大生命值；未加载时显示 —。" } },
        };
        private static readonly Dictionary<System.Windows.Media.Color?, string> chineseColors = CreateChineseColors();
        public Dictionary<System.Windows.Media.Color?, string> ColorNames => Language == "en"
            ? MahApps.Metro.Controls.ColorHelper.ColorNamesDictionary : chineseColors;
        private static Dictionary<System.Windows.Media.Color?, string> CreateChineseColors()
        {
            var names = new Dictionary<string, string>
            {
                { "AliceBlue", "爱丽丝蓝" },
                { "AntiqueWhite", "古董白" },
                { "Aqua", "水蓝色" },
                { "Aquamarine", "碧绿色" },
                { "Azure", "蔚蓝色" },
                { "Beige", "米色" },
                { "Bisque", "陶坯色" },
                { "Black", "黑色" },
                { "BlanchedAlmond", "杏仁白" },
                { "Blue", "蓝色" },
                { "BlueViolet", "蓝紫色" },
                { "Brown", "棕色" },
                { "BurlyWood", "实木色" },
                { "CadetBlue", "军校蓝" },
                { "Chartreuse", "黄绿色" },
                { "Chocolate", "巧克力色" },
                { "Coral", "珊瑚色" },
                { "CornflowerBlue", "矢车菊蓝" },
                { "Cornsilk", "玉米丝色" },
                { "Crimson", "绯红色" },
                { "Cyan", "青色" },
                { "DarkBlue", "深蓝色" },
                { "DarkCyan", "深青色" },
                { "DarkGoldenrod", "暗金菊色" },
                { "DarkGray", "深灰色" },
                { "DarkGreen", "深绿色" },
                { "DarkKhaki", "深卡其色" },
                { "DarkMagenta", "深洋红色" },
                { "DarkOliveGreen", "深橄榄绿" },
                { "DarkOrange", "深橙色" },
                { "DarkOrchid", "深兰花紫" },
                { "DarkRed", "深红色" },
                { "DarkSalmon", "深鲑红色" },
                { "DarkSeaGreen", "深海绿" },
                { "DarkSlateBlue", "深岩蓝" },
                { "DarkSlateGray", "深岩灰" },
                { "DarkTurquoise", "深绿松石色" },
                { "DarkViolet", "深紫罗兰色" },
                { "DeepPink", "深粉色" },
                { "DeepSkyBlue", "深天蓝" },
                { "DimGray", "暗灰色" },
                { "DodgerBlue", "道奇蓝" },
                { "Firebrick", "砖红色" },
                { "FloralWhite", "花白色" },
                { "ForestGreen", "森林绿" },
                { "Fuchsia", "紫红色" },
                { "Gainsboro", "淡灰色" },
                { "GhostWhite", "幽灵白" },
                { "Gold", "金色" },
                { "Goldenrod", "金菊色" },
                { "Gray", "灰色" },
                { "Green", "绿色" },
                { "GreenYellow", "绿黄色" },
                { "Honeydew", "蜜瓜色" },
                { "HotPink", "亮粉色" },
                { "IndianRed", "印度红" },
                { "Indigo", "靛蓝色" },
                { "Ivory", "象牙白" },
                { "Khaki", "卡其色" },
                { "Lavender", "薰衣草紫" },
                { "LavenderBlush", "淡紫红" },
                { "LawnGreen", "草坪绿" },
                { "LemonChiffon", "柠檬绸色" },
                { "LightBlue", "浅蓝色" },
                { "LightCoral", "浅珊瑚色" },
                { "LightCyan", "浅青色" },
                { "LightGoldenrodYellow", "浅金菊黄" },
                { "LightGray", "浅灰色" },
                { "LightGreen", "浅绿色" },
                { "LightPink", "浅粉色" },
                { "LightSalmon", "浅鲑红色" },
                { "LightSeaGreen", "浅海绿" },
                { "LightSkyBlue", "浅天蓝" },
                { "LightSlateGray", "浅岩灰" },
                { "LightSteelBlue", "浅钢蓝" },
                { "LightYellow", "浅黄色" },
                { "Lime", "鲜绿色" },
                { "LimeGreen", "青柠绿" },
                { "Linen", "亚麻色" },
                { "Magenta", "洋红色" },
                { "Maroon", "栗色" },
                { "MediumAquamarine", "中碧绿色" },
                { "MediumBlue", "中蓝色" },
                { "MediumOrchid", "中兰花紫" },
                { "MediumPurple", "中紫色" },
                { "MediumSeaGreen", "中海绿" },
                { "MediumSlateBlue", "中岩蓝" },
                { "MediumSpringGreen", "中春绿" },
                { "MediumTurquoise", "中绿松石色" },
                { "MediumVioletRed", "中紫红色" },
                { "MidnightBlue", "午夜蓝" },
                { "MintCream", "薄荷奶油色" },
                { "MistyRose", "雾玫瑰色" },
                { "Moccasin", "鹿皮色" },
                { "NavajoWhite", "纳瓦霍白" },
                { "Navy", "海军蓝" },
                { "OldLace", "旧蕾丝色" },
                { "Olive", "橄榄色" },
                { "OliveDrab", "橄榄褐" },
                { "Orange", "橙色" },
                { "OrangeRed", "橙红色" },
                { "Orchid", "兰花紫" },
                { "PaleGoldenrod", "淡金菊色" },
                { "PaleGreen", "淡绿色" },
                { "PaleTurquoise", "淡绿松石色" },
                { "PaleVioletRed", "淡紫红色" },
                { "PapayaWhip", "番木瓜色" },
                { "PeachPuff", "桃色" },
                { "Peru", "秘鲁棕" },
                { "Pink", "粉色" },
                { "Plum", "李紫色" },
                { "PowderBlue", "粉末蓝" },
                { "Purple", "紫色" },
                { "Red", "红色" },
                { "RosyBrown", "玫瑰棕" },
                { "RoyalBlue", "皇家蓝" },
                { "SaddleBrown", "马鞍棕" },
                { "Salmon", "鲑红色" },
                { "SandyBrown", "沙棕色" },
                { "SeaGreen", "海绿色" },
                { "SeaShell", "贝壳色" },
                { "Sienna", "赭色" },
                { "Silver", "银色" },
                { "SkyBlue", "天蓝色" },
                { "SlateBlue", "岩蓝色" },
                { "SlateGray", "岩灰色" },
                { "Snow", "雪白色" },
                { "SpringGreen", "春绿色" },
                { "SteelBlue", "钢蓝色" },
                { "Tan", "棕褐色" },
                { "Teal", "蓝绿色" },
                { "Thistle", "蓟紫色" },
                { "Tomato", "番茄红" },
                { "Transparent", "透明" },
                { "Turquoise", "绿松石色" },
                { "Violet", "紫罗兰色" },
                { "Wheat", "小麦色" },
                { "White", "白色" },
                { "WhiteSmoke", "烟白色" },
                { "Yellow", "黄色" },
                { "YellowGreen", "黄绿褐色" },
            };
            var result = new Dictionary<System.Windows.Media.Color?, string>();
            foreach (var property in typeof(System.Windows.Media.Colors).GetProperties())
            {
                var color = (System.Windows.Media.Color)property.GetValue(null);
                result[color] = names.TryGetValue(property.Name, out string name) ? name : color.ToString();
            }
            return result;
        }

        public static readonly System.Windows.DependencyProperty PaletteNamesProperty =
            System.Windows.DependencyProperty.RegisterAttached("PaletteNames", typeof(Dictionary<System.Windows.Media.Color?, string>), typeof(UiText),
                new System.Windows.PropertyMetadata(null, (sender, args) =>
                {
                    if (sender is MahApps.Metro.Controls.ColorPickerBase picker)
                    {
                        var names = (Dictionary<System.Windows.Media.Color?, string>)args.NewValue;
                        picker.SetCurrentValue(MahApps.Metro.Controls.ColorPickerBase.ColorNamesDictionaryProperty, names);
                        // MahApps 2.4.5 does not refresh the current name when its dictionary changes.
                        if (picker.SelectedColor.HasValue) picker.SetCurrentValue(MahApps.Metro.Controls.ColorPickerBase.ColorNameProperty,
                            names != null && names.TryGetValue(picker.SelectedColor, out string colorName) ? colorName : picker.SelectedColor.Value.ToString());
                    }
                }));
        public static void SetPaletteNames(System.Windows.DependencyObject target, Dictionary<System.Windows.Media.Color?, string> value)
            => target.SetValue(PaletteNamesProperty, value);
        public static Dictionary<System.Windows.Media.Color?, string> GetPaletteNames(System.Windows.DependencyObject target)
            => (Dictionary<System.Windows.Media.Color?, string>)target.GetValue(PaletteNamesProperty);
        public static UiText Current { get; } = new UiText();
        public event PropertyChangedEventHandler PropertyChanged;
        public string Language => Settings.Default.UILanguage == "en" ? "en" : "zh-CN";
        public string this[string key] => texts.TryGetValue(key, out string[] values)
            ? values[Language == "en" ? 0 : 1] : key;
        public string PingStatus => Settings.Default.UsePingFilter
            ? string.Format(this["FilterStatus"], Settings.Default.MaxAvgPing, Settings.Default.MaxAbsPing)
            : this["FilterOff"];

        private UiText()
        {
            Settings.Default.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "UILanguage")
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorNames)));
                }
                if (e.PropertyName == "UILanguage" || e.PropertyName == "UsePingFilter" ||
                    e.PropertyName == "MaxAvgPing" || e.PropertyName == "MaxAbsPing")
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PingStatus)));
            };
        }
        public void SelectLanguage(string language)
        {
            string normalized = language == "en" ? "en" : "zh-CN";
            // Translate built-in formats only; preserve user-edited templates verbatim.
            string oldOn = Settings.Default.HeaderFmtFilterOn;
            string oldOff = Settings.Default.HeaderFmtFilterOff;
            Settings.Default.HeaderFmtFilterOn = LocalizeHeader(oldOn, true, normalized);
            Settings.Default.HeaderFmtFilterOff = LocalizeHeader(oldOff, false, normalized);
            Settings.Default.UILanguage = normalized;
        }
        public static string LocalizeHeader(string value, bool enabled, string language)
        {
            string en = enabled ? "[{time:HH:mm:ss}] Ping Filter: {avg}/{abs}" : "[{time:HH:mm:ss}]";
            string zh = enabled ? "[{time:HH:mm:ss}] 延迟过滤：{avg}/{abs}" : "[{time:HH:mm:ss}]";
            return value == en || value == zh ? (language == "en" ? en : zh) : value;
        }
    }
}
