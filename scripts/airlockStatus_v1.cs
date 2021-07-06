// ===== USE INSTRUCTIONS =====
// In all 3 sensors and the airlock chamber vent, set one action to "Run [leave argument blank]"
// and the other to "Run (Default Argument)" for the Programmable Block running this script
// ===== USE INSTRUCTIONS =====

// ===== CONFIG =====
string sensorIntName = "sensorInt";
string sensorChmbName = "sensorChmb";
string sensorExtName = "sensorExt";
string ventName = "vent";
string statusPanelGroup = "statusPanels";
// ===== CONFIG =====

// functions
bool FilterThis(IMyTerminalBlock _block)
{
    return _block.CubeGrid == Me.CubeGrid;
}
void ChangeTextPanelColors(IMyTextPanel _panel, Color _font, Color _bg)
{
    _panel.FontColor = _font;
    _panel.BackgroundColor = _bg;
}
void ChangeTextPanelMessage(IMyTextPanel _panel, string _msg, bool _append)
{
    _panel.WritePublicText(_msg, _append);
    _panel.ShowPublicTextOnScreen();
}
void ChangeTextPanelState(IMyTextPanel _panel, Color _font, Color _bg, string _msg, bool _append)
{
    ChangeTextPanelColors(_panel, _font, _bg);
    ChangeTextPanelMessage(_panel, _msg, _append);
}
void Main(string _arg)
{
    string ERR_TXT = "";

    // find sensors
    IMySensorBlock sensorInt = null;
    IMySensorBlock sensorChmb = null;
    IMySensorBlock sensorExt = null;
    List<IMySensorBlock> sensorBlocks = new List<IMySensorBlock>();
    GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensorBlocks, FilterThis);
    if (sensorBlocks.Count == 0) ERR_TXT += "no Sensor Blocks found\n";
    else
    {
        // find interior sensor
        for (int i = 0; i < sensorBlocks.Count; i++)
            if (sensorBlocks[i].CustomName == sensorIntName)
            {
                sensorInt = sensorBlocks[i];
                break;
            }
        if (sensorInt == null) ERR_TXT += "no Sensor Block named " + sensorIntName + " found\n";
        // find chamber sensor
        for (int i = 0; i < sensorBlocks.Count; i++)
            if (sensorBlocks[i].CustomName == sensorChmbName)
            {
                sensorChmb = sensorBlocks[i];
                break;
            }
        if (sensorChmb == null) ERR_TXT += "no Sensor Block named " + sensorChmbName + " found\n";
        // find exterior sensor
        for (int i = 0; i < sensorBlocks.Count; i++)
            if (sensorBlocks[i].CustomName == sensorExtName)
            {
                sensorExt = sensorBlocks[i];
                break;
            }
        if (sensorExt == null) ERR_TXT += "no Sensor Block named " + sensorExtName + " found\n";
    }

    // find vent
    IMyAirVent vent = null;
    List<IMyAirVent> airVents = new List<IMyAirVent>();
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(airVents, FilterThis);
    if (airVents.Count == 0) ERR_TXT += "no Air Vent Blocks found\n";
    else
    {
        for (int i = 0; i < airVents.Count; i++)
            if (airVents[i].CustomName == ventName)
            {
                vent = airVents[i];
                break;
            }
        if (vent == null) ERR_TXT += "no Air Vent Block named " + ventName + " found\n";
    }

    // find text panels
    List<IMyTextPanel> statusPanels = new List<IMyTextPanel>();
    if (GridTerminalSystem.GetBlockGroupWithName(statusPanelGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(statusPanelGroup).GetBlocksOfType<IMyTextPanel>(statusPanels, FilterThis);
        if (statusPanels.Count == 0) ERR_TXT += "group " + statusPanelGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + statusPanelGroup + " not found\n";

    // display errors
    if (ERR_TXT != "")
    {
        Echo("Script Errors:\n" + ERR_TXT + "(make sure block ownership is set correctly)");
        return;
    }
    else Echo("");

    // logic vars
    bool playerAtInt = sensorInt.IsActive;
    bool playerAtChmb = sensorChmb.IsActive;
    bool playerAtExt = sensorExt.IsActive;
    int chmbPressure = (int)Math.Round((decimal)vent.GetOxygenLevel(), 0);
    Color white = new Color(255, 255, 255);
    Color black = new Color(0, 0, 0);
    Color brightRed = new Color(255, 0, 0);
    Color darkYlw = new Color(63, 63, 0);
    Color darkGrn = new Color(0, 31, 0);

    // logic

    // display chamber pressure
    for (int i = 0; i < statusPanels.Count; i++)
        if (chmbPressure > 0) ChangeTextPanelMessage(statusPanels[i], "FILLED\n\n", false);
        else ChangeTextPanelMessage(statusPanels[i], "DRAINED\n\n", false);
    // display status messages
    if (playerAtChmb)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelState(statusPanels[i], white, brightRed,
                "AIRLOCK IN USE\nDO NOT ENTER\n\nCHAMBER:\nEXIT WHEN READY", true);
    else if (playerAtInt && playerAtExt)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelState(statusPanels[i], black, darkYlw,
                "INTERIOR:\nDO NOT ENTER\n\nEXTERIOR:\nENTER WHEN READY", true);
    else if (playerAtInt)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelState(statusPanels[i], white, darkGrn,
                "INTERIOR:\nENTER WHEN READY", true);
    else if (playerAtExt)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelState(statusPanels[i], white, darkGrn,
                "EXTERIOR:\nENTER WHEN READY", true);
    else
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelState(statusPanels[i], white, black,
                "IDLE", true);
}
