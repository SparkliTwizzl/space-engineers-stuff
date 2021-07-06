#region config
// ========== CONFIG ==========
int updateFrequency = 10; // 1, 10, 100
// ========== CONFIG ==========
#endregion config



#region enums
enum AIRLOCK_STATE
{
    OFF,
    STANDBY,
    STANDBY_TO_ENTER,
    ENTER,
    ENTER_TO_STANDBY,
    STANDBY_TO_EXIT,
    EXIT,
    EXIT_TO_STANDBY,
}
#endregion enums

#region structs
struct TextPanelFormat
{
    public float fontSize;
    public TextAlignment textAlign;
    public Color fontColor;
    public Color bgColor;
};
#endregion structs

#region classes
class ScriptReporter
{
    int m_warningCount;
    int m_errorCount;
    string m_report;



    public ScriptReporter()
    {
        Clear();
    }

    public void Clear()
    {
        m_warningCount = 0;
        m_errorCount = 0;
        m_report = "";
    }

    public int GetWarningCount()
    {
        return m_warningCount;
    }
    public int GetErrorCount()
    {
        return m_errorCount;
    }
    public string GetReport()
    {
        return m_errorCount + " SCRIPT ERRORS\n"
            + m_warningCount + " SCRIPT WARNINGS\n"
            + (WarningsReported() || ErrorsReported() ? "(make sure block ownership is set correctly)\n" : "")
            + m_report;
    }

    public void ReportInfo(string _msg)
    {
        m_report += _msg + "\n";
    }
    public void ReportWarning(string _msg)
    {
        m_report += "-- WARNING #" + ++m_warningCount + ": " + _msg + "\n";        
    }
    public void ReportError(string _msg)
    {
        m_report += "-- ERROR #" + ++m_errorCount + ": " + _msg + "\n";
    }

    public bool WarningsWereReported()
    {
        return (m_warningCount > 0);
    }
    public bool ErrorsWereReported()
    {
        return (m_errorCount > 0);
    }
}

class DeferredActions
{
    List<Action> m_actions;



    public DeferredActions()
    {
        m_actions = new List<Action>();
    }

    public void Clear()
    {
        m_actions.Clear();
    }

    public int Count()
    {
        return m_actions.Count;
    }
    public void Defer(Action _action)
    {
        m_actions.Add(_action);
    }
    public void Until()
    {
        int count = m_actions.Count - 1;
        if (count > -1)
        {
            m_actions[count]();
            m_actions.Remove(m_actions[count]);
        }
    }
    public void Until(Action _action)
    {
        _action();
        Until();
    }
}
#endregion classes



#region globals
// boilerplate globals
List<IMyTerminalBlock> g_blockList = new List<IMyTerminalBlock>(100);
IMyBlockGroup g_blockGroup = null;
ScriptReporter g_reporter = new ScriptReporter();
DeferredActions g_defers = new DeferredActions();
const bool CRITICAL = true;
const bool NONCRITICAL = false;
// script globals
#endregion globals



#region general functions
void Reset()
{
    g_blockList.Clear();
    g_blockGroup = null;
    g_reporter.Clear();
    g_defers.Clear();
}
void SetUpdateFrequency(int _freq)
{
    switch (_freq)
    {
        case 1:
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            break;
        default:
        case 10:
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            break;
        case 100:
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            break;
    }
}
#endregion general functions

#region block functions
bool FilterBlocks(IMyTerminalBlock _block)
{
    return _block.CubeGrid == Me.CubeGrid;
}
string GetBlockTypeName<T>()
{
    string typeName = typeof(T).ToString();
    int blockNameStart = typeName.IndexOf("IMy");
    return typeName.Substring(blockNameStart);
}


bool GetBlockGroupWithName(string _groupName)
{
    g_blockGroup = GridTerminalSystem.GetBlockGroupWithName(_groupName);
    if (g_blockGroup == null)
    {
        g_reporter.ReportError("no block group with name " + _groupName + " found");
        return false;
    }
    return true;
}
T GetFirstMatchingBlock<T, Tcond>(Func<T, Tcond, bool> _blockIsMatch, Tcond _condition, string _errorText)
    where T : class
    where Tcond : class
{
    T block = default(T);
    if (GetBlocksOfType<T>())
    {
        for (int i = 0; i < g_blockList.Count; ++i)
            if (_blockIsMatch((T)g_blockList[i], _condition))
            {
                block = (T)g_blockList[i];
                break;
            }
        if (block == null)
            g_reporter.ReportError("no " + GetBlockTypeName<T>() + " found matching condition (" + _errorText + " ; " + _condition + ")");
    }
    return block;
}


bool GetBlocksOfType<T>() where T : class
{
    g_blockList.Clear();
    GridTerminalSystem.GetBlocksOfType<T>(g_blockList, FilterBlocks);
    if (g_blockList.Count == 0)
    {
        g_reporter.ReportError("no " + GetBlockTypeName<T>() + "s found");
        return false;
    }
    return true;
}
bool GetBlocksOfTypeInGroup<T>(string _groupName) where T : class
{
    g_blockList.Clear();
    if (GetBlockGroupWithName(_groupName))
    {
        g_blockGroup.GetBlocksOfType<T>(g_blockList, FilterBlocks);
        if (g_blockList.Count == 0)
        {
            g_reporter.ReportError("group " + g_blockGroup.Name + " has no " + GetBlockTypeName<T>() + "s");
            return false;
        }
        return true;
    }
    return false;
}


T GetFirstBlock<T>() where T : class
{
    T block = default(T);
    if (GetBlocksOfType<T>())
        block = (T)g_blockList[0];
    return block;
}
T GetFirstBlockInGroup<T>(string _groupName) where T : class
{
    T block = default(T);
    if (GetBlocksOfTypeInGroup<T>(_groupName))
        block = (T)g_blockList[0];
    return block;
}

T GetFirstBlockWithExactName<T>(string _blockName) where T : class
{
    Func<T, string, bool> MatchExactName = (T _block, string _name) => ((IMyTerminalBlock)_block).CustomName == _name;
    return GetFirstMatchingBlock<T, string>(MatchExactName, _blockName, "exact name");
}
T GetFirstBlockInGroupWithExactName<T>(string _blockName, string _groupName) where T : class
{}

T GetFirstBlockWithNameIncluding<T>(string _blockName) where T : class
{
    Func<T, string, bool> MatchNameIncluding = (T _block, string _name) => ((IMyTerminalBlock)_block).CustomName.IndexOf(_name) > -1;
    return GetFirstMatchingBlock<T, string>(MatchNameIncluding, _blockName, "name including");
}
T GetFirstBlockInGroupIncludingName<T>(string _blockName, string _groupName) where T : class
{}


// TODO change to "get n blocks", pass n, 0 = no limit
#endregion block functions

#region door functions
void SetDoorState(IMyDoor _door, bool _on, bool _open)
{
    _door.ApplyAction("OnOff_" + (_on ? "On" : "Off"));
    _door.ApplyAction("Open_" + (_open ? "On" : "Off"));
}
void SetStateOfDoorList(List<IMyDoor> _doors, bool _on, bool _open)
{
    for (int i = 0; i < _doors.Count; ++i)
        SetDoorState(_doors[i], _on, _open);
}
void ForceDoorOffAndOpen(IMyDoor _door)
{
    if (_door.Status != DoorStatus.Open)
    {
        _door.ApplyAction("OnOff_On");
        _door.ApplyAction("Open_On");
    }
    else if (_door.Enabled)
        _door.ApplyAction("OnOff_Off");
}
void ForceOffAndOpenOfDoorList(List<IMyDoor> _doors)
{
    for (int i = 0; i < _doors.Count; ++i)
        ForceDoorOffAndOpen(_doors[i]);
}
void ForceDoorOffAndClosed(IMyDoor _door)
{
    if (_door.Status != DoorStatus.Closed)
    {
        _door.ApplyAction("OnOff_On");
        _door.ApplyAction("Open_Off");
    }
    else if (_door.Enabled)
        _door.ApplyAction("OnOff_Off");
}
void ForceOffAndClosedOfDoorList(List<IMyDoor> _doors)
{
    for (int i = 0; i < _doors.Count; ++i)
        ForceDoorOffAndClosed(_doors[i]);
}
#endregion door functions

#region vent functions
void SetDepressurizeOfVentList(List<IMyTypeBlock> _blocks, bool _depressurize)
{
    string action = "Depressurize_" + (_depressurize ? "On" : "Off");
    for (int i = 0; i < _blocks.Count; ++i)
        _blocks[i].ApplyAction(action);
}
#endregion vent functions

#region text panel functions
void ChangeTextPanelColors(IMyTextPanel _panel, Color _font, Color _bg)
{
    _panel.FontColor = _font;
    _panel.BackgroundColor = _bg;
}
void ChangeColorsOfTextPanelList(List<IMyTextPanel> _panels, Color _font, Color _bg)
{
    for (int i = 0; i < _panels.Count; ++i)
        ChangeTextPanelColors(_panels[i], _font, _bg);
}
void SetTextPanelFormatting(IMyTextPanel _panel, TextPanelFormat _format)
{
    IMyTextSurface surface = (IMyTextSurface)_panel;
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = _format.fontSize;
    surface.Alignment = _format.textAlign;
    SetTextPanelColors(_panel, _format.fontColor, _format.bgColor);
}
void SetFormattingOfTextPanelList(List<IMyTextPanel> _panels, TextPanelFormat _format)
{
    for (int i = 0; i < _panels.Count; ++i)
        SetTextPanelFormatting(_panels[i], _format);
}
void SetTextPanelMessage(IMyTextPanel _panel, string _msg, bool _append)
{
    IMyTextSurface surface = (IMyTextSurface)_panel;
    surface.WriteText((_append ? _panel.GetText() : "") + _msg);
}
void SetMessageOfTextPanelList(List<IMyTextPanel> _panels, string _msg, bool _append)
{
    for (int i = 0; i < _panels.Count; ++i)
        SetTextPanelMessage(_panels[i], _msg, _append);
}
void SetTextPanelState(IMyTextPanel _panel, TextPanelFormat _format, string _msg, bool _append)
{
    SetTextPanelFormatting(_panel, _format);
    SetTextPanelMessage(_panel, _msg, _append);
}
void SetStateOfTextPanelList(List<IMyTextPanel> _panels, TextPanelFormat _format, string _msg, bool _append)
{
    for (int i = 0; i < _panels.Count; ++i)
        SetTextPanelState(_panels[i], _format, _msg, _append);
}
#endregion text panel functions



#region script functions
// script constructor; called only on compile
// use to initialize, load values, etc
public Program()
{
    SetUpdateFrequency(updateFrequency);
    g_blockList = new List<IMyTerminalBlock>();

    // if (Storage.Length > 0)
    // {
    //     var parts = Storage.Split(';');
    //     ...
    // }
}
// save script state; called on world save or recompile
public Save()
{
    // Storage = _val1 + ';' + _val2 + ...;
}
// main method; called every time script runs
void Main(string _arg)
{
    Reset();

    #region find blocks

    IMyTerminalBlock block = GetFirstBlock<IMyTerminalBlock>();
    IMyTerminalBlock block = GetFirstBlockWithExactName<IMyTerminalBlock>("block name");
    IMyTerminalBlock block = GetFirstBlockWithNameIncluding<IMyTerminalBlock>("block name");

    // first block of type in group
    IMyTypeBlock block = null;
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
        if (g_blockList.Count == 0) scriptReport += "group " + groupName + " has no Type Blocks\n";
        else block = g_blockList[0];
    }
    else scriptReport += "group " + groupName + " not found\n";

    // first block of type in group with exact name
    IMyTypeBlock block = null;
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
        if (g_blockList.Count == 0)
            scriptReport += "group " + groupName + " has no Type Blocks\n";
        else
        {
            for (int i = 0; i < g_blockList.Count; ++i)
                if (g_blockList[i].CustomName == blockName)
                {
                    block = g_blockList[i];
                    break;
                }
            if (block == null) scriptReport += "group " + groupName + " has no Type Blocked named " + blockName + "\n";
        }
    }
    else scriptReport += "group " + groupName + " not found\n";

    // first block of type in group including name
    IMyTypeBlock block = null;
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
        if (g_blockList.Count == 0)
            scriptReport += "group " + groupName + " has no Type Blocks\n";
        else
        {
            for (int i = 0; i < g_blockList.Count; ++i)
                if (g_blockList[i].CustomName.IndexOf(blockName) > -1)
                {
                    block = g_blockList[i];
                    break;
                }
            if (block == null) scriptReport += "group " + groupName + " has no Type Blocked with name including " + blockName + "\n";
        }
    }
    else scriptReport += "group " + groupName + " not found\n";

    // all blocks of type
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTypeBlock>(blocks, FilterBlocks);
    if (blocks.Count == 0) scriptReport += "no Type Blocks found\n";

    // all blocks of type with exact name
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
    if (g_blockList.Count > 0)
    {
        for (int i = 0; i < g_blockList.Count; ++i)
            if (g_blockList[i].CustomName == blockName)
                g_blockList.Add(g_blockList[i]);
        if (g_blockList.Count == 0) scriptReport += "no Type Blocks named " + blockName + " found\n";
    }
    else scriptReport += "no Type Blocks found\n";

    // all blocks of type including name
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
    if (g_blockList.Count > 0)
    {
        for (int i = 0; i < g_blockList.Count; ++i)
            if (g_blockList[i].CustomName.IndexOf(blockName) > -1)
                g_blockList.Add(g_blockList[i]);
        if (g_blockList.Count == 0) scriptReport += "no Type Blocks with name including " + blockName + " found\n";
    }
    else scriptReport += "no Type Blocks found\n";

    // all blocks of type in group
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(blocks, FilterBlocks);
        if (blocks.Count == 0) scriptReport += "group " + groupName + " has no Type Blocks\n";
    }
    else scriptReport += "group " + groupName + " not found\n";

    // all blocks of type in group with exact name
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
        if (g_blockList.Count > 0)
        {
            for (int i = 0; i < g_blockList.Count; ++i)
                if (g_blockList[i].CustomName == blockName)
                    blocks.Add(g_blockList[i]);
            if (blocks.Count == 0) scriptReport += "group " + groupName + " has no Type Blocks named " + blockName + "\n";
        }
        else scriptReport += "group " + groupName + " has no Type Blocks\n";
    }
    else scriptReport += "group " + groupName + " not found\n";

    // all blocks of type in group including name
    List<IMyTypeBlock> blocks = new List<IMyTypeBlock>();
    List<IMyTypeBlock> g_blockList = new List<IMyTypeBlock>();
    if (GridTerminalSystem.GetBlockGroupWithName(groupName) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(groupName).GetBlocksOfType<IMyTypeBlock>(g_blockList, FilterBlocks);
        if (g_blockList.Count > 0)
        {
            for (int i = 0; i < g_blockList.Count; ++i)
                if (g_blockList[i].CustomName.IndexOf(blockName) > -1)
                    blocks.Add(g_blockList[i]);
            if (blocks.Count == 0) scriptReport += "group " + groupName + " has no Type Blocks with name including " + blockName + "\n";
        }
        else scriptReport += "group " + groupName + " has no Type Blocks\n";
    }
    else scriptReport += "group " + groupName + " not found\n";

    #endregion find blocks


    if (DisplayErrorReport())
        return;


    // logic vars


	// logic

}
#endregion script functions
