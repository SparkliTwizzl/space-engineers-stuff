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
#endregion globals



#region general functions
void Reset()
{
    g_blockList.Clear();
    g_blockGroup = null;
    g_reporter.Clear();
    g_defers.Clear();
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
{
    return default(T);
}

T GetFirstBlockWithNameIncluding<T>(string _blockName) where T : class
{
    Func<T, string, bool> MatchNameIncluding = (T _block, string _name) => ((IMyTerminalBlock)_block).CustomName.IndexOf(_name) > -1;
    return GetFirstMatchingBlock<T, string>(MatchNameIncluding, _blockName, "name including");
}
T GetFirstBlockInGroupIncludingName<T>(string _blockName, string _groupName) where T : class
{
    return default(T);
}
#endregion block functions



#region script functions
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}
void Main(string _arg)
{
    Reset();



    int storeErrorCount = 0;
    Func<int> GetTestBatchErrorCount = () => g_reporter.GetErrorCount() - storeErrorCount;

    int numTests = 0;
    int numPasses = 0;
    int numErrorsExpected = 0;
    Action<string> StartTestBatch = (string _batchName) =>
    {
        storeErrorCount = g_reporter.GetErrorCount();
        numTests = 0;
        numPasses = 0;
        numErrorsExpected = 0;
        g_reporter.ReportInfo("\n----------\n< start test batch >\n" + _batchName);
    };
    Action<string> EndTestBatch = (string _batchName) =>
    {
        g_reporter.ReportInfo("\n< end test batch >\n" + _batchName
            + "\n" + numTests + " tests, " + numPasses + " passed"
            + "\n(" + GetTestBatchErrorCount() + " errors, " + numErrorsExpected + " expected)"
            + "\n----------");
    };
    Action<string, Action> TestBatch = (string _batchName, Action _tests) =>
    {
        g_defers.Defer(() => EndTestBatch(_batchName));
        g_defers.Until(() =>
        {
            StartTestBatch(_batchName);
            _tests();
        });
    };
    Action<string, bool, Func<bool>> Test = (string _msg, bool _expectedResult, Func<bool> _test) =>
    {
        ++numTests;
        g_reporter.ReportInfo("\n---- TEST #" + numTests + ": " + _msg + " ; should " + (_expectedResult == true ? "succeed" : "fail"));
        if (_expectedResult == false)
            ++numErrorsExpected;
        if (_test() == true)
            ++numPasses;
    };



    TestBatch("GetFirstBlock", () =>
    {
        Test("get block not on grid", false, () =>
            { return GetFirstBlock<IMySolarPanel>() == null; }
        );
        Test("get block on grid", true, () =>
            { return GetFirstBlock<IMyTextPanel>() != null; }
        );
    });

    TestBatch("GetFirstBlockInGroup", () =>
    {
        Test("get block not on grid from group not in grid", false, () =>
            { return GetFirstBlockInGroup<IMySolarPanel>("_FirstBlockInGroup") == null; }
        );
        Test("get block not on grid", false, () =>
            { return GetFirstBlockInGroup<IMySolarPanel>("FirstBlockInGroup") == null; }
        );
        Test("get block not in group", false, () =>
            { return GetFirstBlockInGroup<IMyMotorStator>("FirstBlockInGroup") == null; }
        );
        Test("get block in group", true, () =>
            { return GetFirstBlockInGroup<IMyTextPanel>("FirstBlockInGroup") != null; }
        );
    });

    TestBatch("GetFirstBlockWithExactName", () =>
    {
        Test("get block not on grid", false, () =>
            { return GetFirstBlockWithExactName<IMySolarPanel>("jeff") == null; }
        );
        Test("get block with part of name not in grid", false, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("jess") == null; }
        );
        Test("get block with part of name in grid", false, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("jeff") == null; }
        );
        Test("get block with name not in grid", false, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jess") == null; }
        );
        Test("get block with name in grid", true, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jeff") != null; }
        );
    });

    // TestBatch(GetFirstBlockInGroupWithExactName", () =>
    // {
    // });

    TestBatch("GetFirstBlockWithNameIncluding", () =>
    {
        Test("get block not on grid", false, () =>
            { return GetFirstBlockWithNameIncluding<IMySolarPanel>("jeff") == null; }
        );
        Test("get block with part of name not in grid", false, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jess") == null; }
        );
        Test("get block with part of name in grid", true, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jeff") != null; }
        );
        Test("get block with name not in grid", false, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jess") == null; }
        );
        Test("get block with name in grid", true, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jeff") != null; }
        );
    });

    // TestBatch("GetFirstBlockInGroupWithNameIncluding", () =>
    // {
    // });



    Echo(g_reporter.GetReport());
    if (g_reporter.ErrorsWereReported())
        return;
}
#endregion script functions
