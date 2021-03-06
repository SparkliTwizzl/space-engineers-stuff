// ===== CONFIG =====
int updateFrequency = 10; // 1, 10, 100
string doorIntName = "doorInt";
string doorExtName = "doorExt";
string sensorIntName = "sensorInt";
string sensorChmbName = "sensorChmb";
string sensorExtName = "sensorExt";
string ventName = "vent";
string statusPanelIntGroup = "statusPanelsInt";
string statusPanelChmbGroup = "statusPanelsChmb";
string statusPanelExtGroup = "statusPanelsExt";
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
public Program()
{
    switch (updateFrequency)
    {
        case 1:
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            break;
        case 10:
        default:
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            break;
        case 100:
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            break;
    }
}
void Main(string _arg)
{
    string ERR_TXT = "";

    // find doors
    IMyDoor doorInt = null;
    IMyDoor doorExt = null;
    List<IMyDoor> doors = new List<IMyDoor>();
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, FilterThis);
    if (doors.Count == 0) ERR_TXT += "no Door Blocks found\n";
    else
    {
        // find interior door
        for(int i = 0; i < doors.Count; i++)
        {
            if(doors[i].CustomName == doorIntName)
            {
                doorInt = doors[i];
                break;
            }
        }
        if(doorInt == null) ERR_TXT += "no Door Block named " + doorIntName + " found\n";

        // find exterior door
        for(int i = 0; i < doors.Count; i++)
        {
            if(doors[i].CustomName == doorExtName)
            {
                doorExt = doors[i];
                break;
            }
        }
        if(doorExt == null) ERR_TXT += "no Door Block named " + doorExtName + " found\n";
    }

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
    List<IMyTextPanel> statusPanelsInt = new List<IMyTextPanel>();
    List<IMyTextPanel> statusPanelsChmb = new List<IMyTextPanel>();
    List<IMyTextPanel> statusPanelsExt = new List<IMyTextPanel>();
    // find interior panels
    if (GridTerminalSystem.GetBlockGroupWithName(statusPanelIntGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(statusPanelIntGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsInt, FilterThis);
        if (statusPanelsInt.Count == 0) ERR_TXT += "group " + statusPanelIntGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + statusPanelIntGroup + " not found\n";
    // find chamber panels
    if (GridTerminalSystem.GetBlockGroupWithName(statusPanelChmbGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(statusPanelChmbGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsChmb, FilterThis);
        if (statusPanelsChmb.Count == 0) ERR_TXT += "group " + statusPanelChmbGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + statusPanelChmbGroup + " not found\n";
    // find exterior panels
    if (GridTerminalSystem.GetBlockGroupWithName(statusPanelExtGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(statusPanelExtGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsExt, FilterThis);
        if (statusPanelsExt.Count == 0) ERR_TXT += "group " + statusPanelExtGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + statusPanelExtGroup + " not found\n";

    // display errors
    if (ERR_TXT != "")
    {
        Echo("Script Errors:\n" + ERR_TXT + "(make sure block ownership is set correctly)");
        return;
    }
    else Echo("");

    // logic vars
    string doorIntStatus = doorInt.Status.ToString().ToUpper();
    string doorExtStatus = doorExt.Status.ToString().ToUpper();
    bool playerAtInt = sensorInt.IsActive;
    bool playerAtChmb = sensorChmb.IsActive;
    bool playerAtExt = sensorExt.IsActive;
    int chmbPressure = (int)Math.Round(vent.GetOxygenLevel() * 100, 0);
    Color white = new Color(255, 255, 255);
    Color black = new Color(0, 0, 0);
    Color brightRed = new Color(255, 0, 0);
    Color darkYellow = new Color(63, 63, 0);
    Color darkGreen = new Color(0, 31, 0);

    // logic

    // display airlock status
    for (int i = 0; i < statusPanelsInt.Count; i++)
        ChangeTextPanelMessage(statusPanelsInt[i],
            "AIRLOCK PRESSURE: " + chmbPressure + "%\n"
            + "INTERIOR DOOR " + doorIntStatus + "\n"
            + "EXTERIOR DOOR " + doorExtStatus + "\n\n",
            false);
    for (int i = 0; i < statusPanelsChmb.Count; i++)
        ChangeTextPanelMessage(statusPanelsChmb[i],
            "AIRLOCK PRESSURE: " + chmbPressure + "%\n"
            + "INTERIOR DOOR " + doorIntStatus + "\n"
            + "EXTERIOR DOOR " + doorExtStatus + "\n\n",
            false);
    for (int i = 0; i < statusPanelsExt.Count; i++)
        ChangeTextPanelMessage(statusPanelsExt[i],
            "AIRLOCK PRESSURE: " + chmbPressure + "%\n"
            + "INTERIOR DOOR " + doorIntStatus + "\n"
            + "EXTERIOR DOOR " + doorExtStatus + "\n\n",
            false);
    // display player presence messages
    if (playerAtChmb)
    {
        for (int i = 0; i < statusPanelsInt.Count; i++)
            ChangeTextPanelState(statusPanelsInt[i], white, brightRed,
                "AIRLOCK IN USE\nDO NOT ENTER\n", true);
        for (int i = 0; i < statusPanelsExt.Count; i++)
            ChangeTextPanelState(statusPanelsExt[i], white, brightRed,
                "AIRLOCK IN USE\nDO NOT ENTER\n", true);
        if (playerAtInt || playerAtExt)
        {
            for (int i = 0; i < statusPanelsChmb.Count; i++)
                ChangeTextPanelState(statusPanelsChmb[i], black, darkYellow,
                    "PLAYER WAITING\nPLEASE EXIT\n", true);
        }
        else
        {
            for (int i = 0; i < statusPanelsChmb.Count; i++)
                ChangeTextPanelColors(statusPanelsChmb[i], white, black);
        }
    }
    else if (playerAtInt && playerAtExt)
    {
        for (int i = 0; i < statusPanelsInt.Count; i++)
            ChangeTextPanelState(statusPanelsInt[i], white, brightRed,
                "PLAYER WAITING TO ENTER\nDO NOT ENTER", true);
        if (chmbPressure > 0)
        {
            for (int i = 0; i < statusPanelsExt.Count; i++)
                ChangeTextPanelState(statusPanelsExt[i], black, darkYellow,
                    "PLAYER WAITING TO EXIT\nENTER WITH CAUTION\nVENT BEFORE OPENING", true);
        }
        else
        {
            for (int i = 0; i < statusPanelsExt.Count; i++)
                ChangeTextPanelState(statusPanelsExt[i], black, darkYellow,
                    "PLAYER WAITING TO EXIT\nENTER WITH CAUTION", true);
        }
        for (int i = 0; i < statusPanelsChmb.Count; i++)
            ChangeTextPanelColors(statusPanelsChmb[i], white, black);
    }
    else if (playerAtInt)
    {
        for (int i = 0; i < statusPanelsInt.Count; i++)
            ChangeTextPanelState(statusPanelsInt[i], white, darkGreen,
                "ENTER WHEN READY", true);
        for (int i = 0; i < statusPanelsExt.Count; i++)
            ChangeTextPanelColors(statusPanelsExt[i], white, black);
        for (int i = 0; i < statusPanelsChmb.Count; i++)
            ChangeTextPanelColors(statusPanelsChmb[i], white, black);
    }
    else if (playerAtExt)
    {
        if (chmbPressure > 0)
        {
            for (int i = 0; i < statusPanelsExt.Count; i++)
                ChangeTextPanelState(statusPanelsExt[i], black, darkYellow,
                    "VENT BEFORE OPENING", true);
        }
        else
        {
            for (int i = 0; i < statusPanelsExt.Count; i++)
                ChangeTextPanelState(statusPanelsExt[i], white, darkGreen,
                    "ENTER WHEN READY", true);
        }
        for (int i = 0; i < statusPanelsInt.Count; i++)
            ChangeTextPanelColors(statusPanelsInt[i], white, black);
        for (int i = 0; i < statusPanelsChmb.Count; i++)
            ChangeTextPanelColors(statusPanelsChmb[i], white, black);
    }
    else
    {
        for (int i = 0; i < statusPanelsInt.Count; i++)
            ChangeTextPanelColors(statusPanelsInt[i], white, black);
        for (int i = 0; i < statusPanelsExt.Count; i++)
            ChangeTextPanelColors(statusPanelsExt[i], white, black);
        for (int i = 0; i < statusPanelsChmb.Count; i++)
            ChangeTextPanelColors(statusPanelsChmb[i], white, black);
    }
}
