#region defines
const bool CRITICAL = true;
const bool NONCRITICAL = false;
const bool SHOULD_SUCCEED = true;
const bool SHOULD_FAIL = false;
#endregion defines



#region classes
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

class DeferredActions
{
    List<Action> m_actions = null;



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

class TestSystem
{
    ScriptReporter m_reporter = null;
    int m_numTests = 0;
    int m_numPasses = 0;
    int m_numErrorsExpected = 0;
    int m_numWarningsExpected = 0;
    int m_storeErrorCount = 0;
    int m_storeWarningCount = 0;



    void StoreErrorCount()
    {
        m_storeErrorCount = m_reporter.GetErrorCount();
    }
    void StoreWarningCount()
    {
        m_storeWarningCount = m_reporter.GetWarningCount();
    }

    int GetTestBatchErrorCount()
    {
        return m_reporter.GetErrorCount() - m_storeErrorCount;
    }
    int GetTestBatchWarningCount()
    {
        return m_reporter.GetWarningCount() - m_storeWarningCount;
    }

    void StartTestBatch(string _batchName)
    {
        StoreErrorCount();
        StoreWarningCount();
        m_numTests = 0;
        m_numPasses = 0;
        m_numWarningsExpected = 0;
        m_numErrorsExpected = 0;
        m_reporter.ReportInfo("\n--------------------\n< START TEST BATCH >\n" + _batchName);
    }
    void EndTestBatch(string _batchName)
    {
        m_reporter.ReportInfo("\n< END TEST BATCH >\n" + _batchName
            + "\n" + m_numTests + " TESTS, " + m_numPasses + " PASSED"
            + "\n    (" + GetTestBatchErrorCount() + " ERRORS, " + m_numErrorsExpected + " EXPECTED)"
            + "\n    (" + GetTestBatchWarningCount() + " WARNINGS, " + m_numWarningsExpected + " EXPECTED)"
            + "\n--------------------");
    }



    public TestSystem(ScriptReporter _reporter)
    {
        m_reporter = _reporter;
    }

    public void Test(string _desc, bool _isCritical, bool _expectedResult, Func<bool> _test)
    {
        ++m_numTests;
        m_reporter.ReportInfo("\n----> TEST " + m_numTests + ": " + _desc + " ; should " + (_expectedResult == true ? "succeed" : "fail"));
        if (_expectedResult == SHOULD_FAIL)
        {
            if (_isCritical)
                ++m_numErrorsExpected;
            else
                ++m_numWarningsExpected;
        }
        if (_test() == _expectedResult)
            ++m_numPasses;
    }
    public void RunTestBatch(string _batchName, Action _tests)
    {
        StartTestBatch(_batchName);
        _tests();
        EndTestBatch(_batchName);
    }
}
#endregion classes



#region globals
// boilerplate globals
static List<IMyTerminalBlock> g_blockList = new List<IMyTerminalBlock>(100);
static IMyBlockGroup g_blockGroup = null;
static ScriptReporter g_reporter = new ScriptReporter();
static DeferredActions g_defers = new DeferredActions();
static TestSystem g_testSystem = new TestSystem(g_reporter);
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

    #region tests
    g_testSystem.RunTestBatch("GetFirstBlock", () =>
    {
        g_testSystem.Test("get block not on grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlock<IMySolarPanel>() != null; }
        );
        g_testSystem.Test("get block on grid", CRITICAL, SHOULD_SUCCEED, () =>
            { return GetFirstBlock<IMyTextPanel>() != null; }
        );
    });

    g_testSystem.RunTestBatch("GetFirstBlockInGroup", () =>
    {
        g_testSystem.Test("get block not on grid from group not in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockInGroup<IMySolarPanel>("_FirstBlockInGroup") != null; }
        );
        g_testSystem.Test("get block not on grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockInGroup<IMySolarPanel>("FirstBlockInGroup") != null; }
        );
        g_testSystem.Test("get block not in group", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockInGroup<IMyMotorStator>("FirstBlockInGroup") != null; }
        );
        g_testSystem.Test("get block in group", CRITICAL, SHOULD_SUCCEED, () =>
            { return GetFirstBlockInGroup<IMyTextPanel>("FirstBlockInGroup") != null; }
        );
    });

    g_testSystem.RunTestBatch("GetFirstBlockWithExactName", () =>
    {
        g_testSystem.Test("get block not on grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithExactName<IMySolarPanel>("jeff") != null; }
        );
        g_testSystem.Test("get block with part of name not in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("jess") != null; }
        );
        g_testSystem.Test("get block with part of name in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("jeff") != null; }
        );
        g_testSystem.Test("get block with name not in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jess") != null; }
        );
        g_testSystem.Test("get block with name in grid", CRITICAL, SHOULD_SUCCEED, () =>
            { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jeff") != null; }
        );
    });

    // g_testSystem.RunTestBatch(GetFirstBlockInGroupWithExactName", () =>
    // {
    // });

    g_testSystem.RunTestBatch("GetFirstBlockWithNameIncluding", () =>
    {
        g_testSystem.Test("get block not on grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithNameIncluding<IMySolarPanel>("jeff") != null; }
        );
        g_testSystem.Test("get block with part of name not in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jess") != null; }
        );
        g_testSystem.Test("get block with part of name in grid", CRITICAL, SHOULD_SUCCEED, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jeff") != null; }
        );
        g_testSystem.Test("get block with name not in grid", CRITICAL, SHOULD_FAIL, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jess") != null; }
        );
        g_testSystem.Test("get block with name in grid", CRITICAL, SHOULD_SUCCEED, () =>
            { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jeff") != null; }
        );
    });

    // g_testSystem.RunTestBatch("GetFirstBlockInGroupWithNameIncluding", () =>
    // {
    // });
    #endregion tests

    Echo(g_reporter.GetReport());
    if (g_reporter.ErrorsWereReported())
        return;
}
#endregion script functions
