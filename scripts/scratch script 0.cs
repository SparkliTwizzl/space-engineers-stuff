#region defines
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
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get nonexistent block",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlock<IMySolarPanel>() != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block",
            expectedResult = SHOULD_SUCCEED,
            expectedErrors = 0,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlock<IMyTextPanel>() != null; },
        });
    });

    g_testSystem.RunTestBatch("GetFirstBlockInGroup", () =>
    {
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get nonexistent block from nonexistent group",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockInGroup<IMySolarPanel>("_FirstBlockInGroup") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get nonexistent block in group",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockInGroup<IMySolarPanel>("FirstBlockInGroup") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block in nonexistent group",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockInGroup<IMyMotorStator>("FirstBlockInGroup") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block in group",
            expectedResult = SHOULD_SUCCEED,
            expectedErrors = 0,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockInGroup<IMyTextPanel>("FirstBlockInGroup") != null; },
        });
    });

    g_testSystem.RunTestBatch("GetFirstBlockWithExactName", () =>
    {
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get nonexistent block",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithExactName<IMySolarPanel>("jeff") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with part of nonexistent name",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithExactName<IMyTextPanel>("jess") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with part of name",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithExactName<IMyTextPanel>("jeff") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with nonexistent full name",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jess") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with full name",
            expectedResult = SHOULD_SUCCEED,
            expectedErrors = 0,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithExactName<IMyTextPanel>("LCD Panel jeff") != null; },
        });
    });

    // g_testSystem.RunTestBatch(GetFirstBlockInGroupWithExactName", () =>
    // {
    // });

    g_testSystem.RunTestBatch("GetFirstBlockWithNameIncluding", () =>
    {
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get nonexistent block",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithNameIncluding<IMySolarPanel>("jeff") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with part of nonexistent name",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jess") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with part of name",
            expectedResult = SHOULD_SUCCEED,
            expectedErrors = 0,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithNameIncluding<IMyTextPanel>("jeff") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with nonexistent full name",
            expectedResult = SHOULD_FAIL,
            expectedErrors = 1,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jess") != null; },
        });
        g_testSystem.RunTest(new TestSystem.Test
        {
            desc = "get block with full name",
            expectedResult = SHOULD_SUCCEED,
            expectedErrors = 0,
            expectedWarnings = 0,
            Logic = () => { return GetFirstBlockWithNameIncluding<IMyTextPanel>("LCD Panel jeff") != null; },
        });
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
