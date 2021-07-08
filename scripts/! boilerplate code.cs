#region config
// ========== CONFIG ==========
int updateFrequency = 10; // 1, 10, 100
// ========== CONFIG ==========
#endregion config



#region defines
const bool SHOULD_SUCCEED = true;
const bool SHOULD_FAIL = false;
#endregion defines



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
class DeferredActionSystem
{
    List<Action> m_actions = null;



    public DeferredActionSystem()
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

class IntermittentAction
{
    /* Allows logic to be stored and only executed once every n updates rather than every update */

    Action Logic = null;
    int frequency = 0;
    int counter = 0;



    public IntermittentAction(Action _logic, int _freq)
    {
        Logic = _logic;
        frequency = _freq;
        counter = 0;
    }

    public void SetLogic(Action _logic)
    {
        Logic = _logic;
        counter = 0;
    }
    public void SetFrequency(int _freq)
    {
        frequency = _freq;
        counter = 0;
    }
    public void Run()
    {
        ++counter;
        if (counter >= frequency)
        {
            counter = 0;
            Logic();
        }
    }
}

class ScriptReporter
{
    int m_errorCount = 0;
    int m_warningCount = 0;
    string m_report = "";



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

    public int GetErrorCount()
    {
        return m_errorCount;
    }
    public int GetWarningCount()
    {
        return m_warningCount;
    }
    public string GetReport()
    {
        return m_errorCount + " SCRIPT ERRORS\n"
            + m_warningCount + " SCRIPT WARNINGS\n"
            + (WarningsWereReported() || ErrorsWereReported() ? "(make sure block ownership is set correctly)\n" : "")
            + m_report;
    }

    public void ReportError(string _msg)
    {
        m_report += "-- ERROR " + ++m_errorCount + ": " + _msg + "\n";
    }
    public void ReportWarning(string _msg)
    {
        m_report += "-- WARNING " + ++m_warningCount + ": " + _msg + "\n";        
    }
    public void ReportInfo(string _msg)
    {
        m_report += _msg + "\n";
    }

    public bool ErrorsWereReported()
    {
        return (m_errorCount > 0);
    }
    public bool WarningsWereReported()
    {
        return (m_warningCount > 0);
    }
}

class TestSystem
{
    public struct Test
    {
        public string desc;
        public bool expectedResult;
        public int expectedErrors;
        public int expectedWarnings;
        public Func<bool> Logic;
    }



    ScriptReporter m_reporter = null;
    int m_numTests = 0;
    int m_numPasses = 0;

    const string batchSeparator = "------------------------------";
    const string testSeparator = "---------------";



    public TestSystem(ScriptReporter _reporter)
    {
        m_reporter = _reporter;
    }

    public void RunTest(Test _test)
    {
        ++m_numTests;
        m_reporter.ReportInfo("| " + testSeparator + "\n|\n|   START TEST " + m_numTests + ":\n|     " + _test.desc);

        int errorCountPre = m_reporter.GetErrorCount();
        int warningCountPre = m_reporter.GetWarningCount();

        bool resultMatch = _test.Logic() == _test.expectedResult;

        int errorsCaused = m_reporter.GetErrorCount() - errorCountPre;
        bool errorsMatch = errorsCaused == _test.expectedErrors;

        int warningsCaused = m_reporter.GetWarningCount() - warningCountPre;
        bool warningsMatch = warningsCaused == _test.expectedWarnings;

        bool testPassed = resultMatch && errorsMatch && warningsMatch;
        if (testPassed)
            ++m_numPasses;
        m_reporter.ReportInfo("|   END TEST " + m_numTests + ":\n|     " + _test.desc
            + "\n|     " + (testPassed ? "PASSED" : "FAILED")
            + (!resultMatch ? "\n|     UNEXPECTED RESULT" : "")
            + (!errorsMatch ? "\n|     " + _test.expectedErrors + " ERRORS EXPECTED, " + errorsCaused + " CAUSED" : "")
            + (!warningsMatch ? "\n|     " + _test.expectedWarnings + " WARNINGS EXPECTED, " + warningsCaused + " CAUSED" : "")
            );
    }
    public void RunTestBatch(string _batchName, Action _tests)
    {
        m_numTests = 0;
        m_numPasses = 0;
        m_reporter.ReportInfo("\n| " + batchSeparator + "\n|\n| START TEST BATCH:\n|   " + _batchName);

        _tests();

        m_reporter.ReportInfo(
            "| " + testSeparator + "\n|\n| " + m_numTests + " TESTS, " + m_numPasses + " PASSED"
            + "\n| END TEST BATCH:" + "\n|   " + _batchName
            + "\n| " + batchSeparator);
    }
}
#endregion classes



#region globals
// boilerplate globals
static List<IMyTerminalBlock> g_blockList = new List<IMyTerminalBlock>(100);
static IMyBlockGroup g_blockGroup = null;
static DeferredActionSystem g_defers = new DeferredActionSystem();
static ScriptReporter g_reporter = new ScriptReporter();
static TestSystem g_testSystem = new TestSystem(g_reporter);
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
// script-specific functions
bool AcquireBlocks()
{
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

    return false;
}
void RunTests()
{
    g_testSystem.RunTestBatch("Test batch name", () =>
    {
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "test description",
            expectedResult = BOOL,
            expectedErrors = #,
            expectedWarnings = #,
            Logic = () => { return []; },
        });
    });
}

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

    if (!AcquireBlocks())
        g_reporter.ReportError("Acquiring blocks failed");
    else
    {
        RunTests();
    }    

    Echo(g_reporter.GetReport());
    if (g_reporter.ErrorsWereReported())
        return;


    // logic vars


	// logic

}
#endregion script functions
