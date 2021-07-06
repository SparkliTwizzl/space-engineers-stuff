/* ========== USE INSTRUCTIONS ==========
* Use the Config values to set up the airlock and control its behavior.
*
* To trigger the airlock, set the action of the trigger mechanisms (button panels,
* or sensors if you want to use auto-open) to "Run [argument]" (see below for arguments to use).
*
* Arguments:
* - off : Turn off door controller and use manual operation
* - reset : Reset airlock to Standby state
* - enter : Request Enter state
* - enterAuto : Request Enter state if auto-open is enabled
* - enterOverride : Force Enter state
* - exit : Request Exit state
* - exitAuto : Request Exit state if auto-open is enabled
* - exitOverride : Force Exit state
// ========== /USE INSTRUCTIONS ========== */

// ========== CONFIG ==========
int updateFrequency = 10; // 1, 10, 100; recommended to leave at 10 unless it causes lag

string doorIntGroup = "doorsInt";
string doorExtGroup = "doorsExt";

string ventGroup = "vents";

string sensorIntName = "sensorInt";
string sensorChmbName = "sensorChmb";
string sensorExtName = "sensorExt";

bool useAutoOpen = true;
bool useStatusDisplay = true;
// the below only matter if status display is used

string monitorVentName = "monitorVent";
string monitorDoorIntName = "monitorDoorInt";
string monitorDoorExtName = "monitorDoorExt";

bool useInteriorPanels = true;
string statusPanelIntGroup = "statusPanelsInt";

bool useChamberPanels = true;
string statusPanelChmbGroup = "statusPanelsChmb";

bool useExteriorPanels = true;
string statusPanelExtGroup = "statusPanelsExt";
// ========== /CONFIG ==========


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
AIRLOCK_STATE state;

// functions
bool FilterThis(IMyTerminalBlock _block)
{
    return _block.CubeGrid == Me.CubeGrid;
}
void SetUpdateFrequency(int _freq)
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

void SetDoorState(IMyDoor _door, bool _on, bool _open)
{
    _door.ApplyAction("OnOff_" + (_on ? "On" : "Off"));
    _door.ApplyAction("Open_" + (_open ? "On" : "Off"));
}
void SetStateOfDoorList(List<IMyDoor> _doors, bool _on, bool _open)
{
    string actionOn = "OnOff_" + (_on ? "On" : "Off");
    string actionOpen = "Open_" + (_open ? "On" : "Off");
    for (int i = 0; i < _doors.Count; ++i)
    {
        _doors[i].ApplyAction(actionOn);
        _doors[i].ApplyAction(actionOpen);
    }
}
void ForceDoorOffAndOpen(IMyDoor _door)
{
    if (_door.Status.ToString() != "Open")
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
    if (_door.Status.ToString() != "Closed")
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

void SetDepressurizeOfVentList(List<IMyAirVent> _vents, bool _depressurize)
{
    string action = "Depressurize_" + (_depressurize ? "On" : "Off");
    for (int i = 0; i < _vents.Count; ++i)
        _vents[i].ApplyAction(action);
}

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
void ChangeTextPanelMessage(IMyTextPanel _panel, string _msg, bool _append)
{
    _panel.WritePublicText(_msg, _append);
    _panel.ShowPublicTextOnScreen();
}
void ChangeMessageOfTextPanelList(List<IMyTextPanel> _panels, string _msg, bool _append)
{
    for (int i = 0; i < _panels.Count; ++i)
        ChangeTextPanelMessage(_panels[i], _msg, _append);
}
void ChangeTextPanelState(IMyTextPanel _panel, Color _font, Color _bg, string _msg, bool _append)
{
    ChangeTextPanelColors(_panel, _font, _bg);
    ChangeTextPanelMessage(_panel, _msg, _append);
}
void ChangeStateOfTextPanelList(List<IMyTextPanel> _panels, Color _font, Color _bg, string _msg, bool _append)
{
    for (int i = 0; i < _panels.Count; ++i)
        ChangeTextPanelState(_panels[i], _font, _bg, _msg, _append);
}

public Program()
{
    SetUpdateFrequency(updateFrequency);
}
void Main(string _arg)
{
    string ERR_TXT = "";

    // find doors
    List<IMyDoor> doorsInt = new List<IMyDoor>();
    List<IMyDoor> doorsExt = new List<IMyDoor>();
    // find interior doors
    if (GridTerminalSystem.GetBlockGroupWithName(doorIntGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(doorIntGroup).GetBlocksOfType<IMyDoor>(doorsInt, FilterThis);
        if (doorsInt.Count == 0) ERR_TXT += "group " + doorIntGroup + " has no Text Panel Blocks\n";
    }
    else ERR_TXT += "group " + doorIntGroup + " not found\n";
    // find exterior doors
    if (GridTerminalSystem.GetBlockGroupWithName(doorExtGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(doorExtGroup).GetBlocksOfType<IMyDoor>(doorsExt, FilterThis);
        if (doorsExt.Count == 0) ERR_TXT += "group " + doorExtGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + doorExtGroup + " not found\n";

    // find monitor doors
    IMyDoor monitorDoorInt = null;
    IMyDoor monitorDoorExt = null;
    List<IMyDoor> doorList = new List<IMyDoor>();
    if (useStatusDisplay)
    {
        GridTerminalSystem.GetBlocksOfType<IMyDoor>(doorList, FilterThis);
        if (doorList.Count == 0) ERR_TXT += "no Door Blocks found\n";
        else
        {
            // find int monitor door
            for(int i = 0; i < doorList.Count; i++)
            {
                if(doorList[i].CustomName == monitorDoorIntName)
                {
                    monitorDoorInt = doorList[i];
                    break;
                }
            }
            if(monitorDoorInt == null) ERR_TXT += "no Door Block named " + monitorDoorIntName + " found\n";
            
            // find ext monitor door
            for(int i = 0; i < doorList.Count; i++)
            {
                if(doorList[i].CustomName == monitorDoorExtName)
                {
                    monitorDoorExt = doorList[i];
                    break;
                }
            }
            if(monitorDoorExt == null) ERR_TXT += "no Door Block named " + monitorDoorExtName + " found\n";
        }
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

    // find vents
    List<IMyAirVent> vents = new List<IMyAirVent>();
    if (GridTerminalSystem.GetBlockGroupWithName(ventGroup) != null)
    {
        GridTerminalSystem.GetBlockGroupWithName(ventGroup).GetBlocksOfType<IMyAirVent>(vents, FilterThis);
        if (vents.Count == 0) ERR_TXT += "group " + ventGroup + " has no LCD Panel Blocks\n";
    }
    else ERR_TXT += "group " + ventGroup + " not found\n";

    // find monitor vent
    IMyAirVent monitorVent = null;
    List<IMyAirVent> ventList = new List<IMyAirVent>();
    if (useStatusDisplay)
    {
        GridTerminalSystem.GetBlocksOfType<IMyAirVent>(ventList, FilterThis);
        if (ventList.Count == 0) ERR_TXT += "no Air Vent Blocks found\n";
        else
        {
            for (int i = 0; i < ventList.Count; i++)
                if (ventList[i].CustomName == monitorVentName)
                {
                    monitorVent = ventList[i];
                    break;
                }
            if (monitorVent == null) ERR_TXT += "no Air Vent Block named " + monitorVentName + " found\n";
        }
    }

    // find text panels
    List<IMyTextPanel> statusPanelsInt = new List<IMyTextPanel>();
    List<IMyTextPanel> statusPanelsChmb = new List<IMyTextPanel>();
    List<IMyTextPanel> statusPanelsExt = new List<IMyTextPanel>();
    if (useStatusDisplay)
    {
        // find interior panels
        if (useInteriorPanels)
        {
            if (GridTerminalSystem.GetBlockGroupWithName(statusPanelIntGroup) != null)
            {
                GridTerminalSystem.GetBlockGroupWithName(statusPanelIntGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsInt, FilterThis);
                if (statusPanelsInt.Count == 0) ERR_TXT += "group " + statusPanelIntGroup + " has no LCD Panel Blocks\n";
            }
            else ERR_TXT += "group " + statusPanelIntGroup + " not found\n";
        }
        // find chamber panels
        if (useChamberPanels)
        {
            if (GridTerminalSystem.GetBlockGroupWithName(statusPanelChmbGroup) != null)
            {
                GridTerminalSystem.GetBlockGroupWithName(statusPanelChmbGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsChmb, FilterThis);
                if (statusPanelsChmb.Count == 0) ERR_TXT += "group " + statusPanelChmbGroup + " has no LCD Panel Blocks\n";
            }
            else ERR_TXT += "group " + statusPanelChmbGroup + " not found\n";
        }
        // find exterior panels
        if (useExteriorPanels)
        {
            if (GridTerminalSystem.GetBlockGroupWithName(statusPanelExtGroup) != null)
            {
                GridTerminalSystem.GetBlockGroupWithName(statusPanelExtGroup).GetBlocksOfType<IMyTextPanel>(statusPanelsExt, FilterThis);
                if (statusPanelsExt.Count == 0) ERR_TXT += "group " + statusPanelExtGroup + " has no LCD Panel Blocks\n";
            }
            else ERR_TXT += "group " + statusPanelExtGroup + " not found\n";
        }
    }

    // display errors
    if (ERR_TXT != "")
    {
        Echo("Script Errors:\n" + ERR_TXT + "(make sure block ownership is set correctly)");
        return;
    }
    else Echo("");

    // logic vars
    string doorIntStatus = monitorDoorInt.Status.ToString().ToUpper();
    string doorExtStatus = monitorDoorExt.Status.ToString().ToUpper();
    bool chmbSealed = monitorVent.CanPressurize;
    int chmbPressure = (int)Math.Round(monitorVent.GetOxygenLevel() * 100, 0);
    bool playerAtInt = sensorInt.IsActive;
    bool playerAtChmb = sensorChmb.IsActive;
    bool playerAtExt = sensorExt.IsActive;

    // logic

    // if arg provided, trigger sequence
    if (_arg != "")
    {
        switch (_arg)
        {
            default: break;
            case "off":
                state = AIRLOCK_STATE.OFF;
                SetStateOfDoorList(doorsInt, true, false);
                SetStateOfDoorList(doorsExt, true, false);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                break;
            case "reset":
                state = AIRLOCK_STATE.STANDBY;
                SetUpdateFrequency(updateFrequency);
                break;
            case "enter":
                if (state == AIRLOCK_STATE.STANDBY)
                    state = AIRLOCK_STATE.STANDBY_TO_ENTER;
                break;
            case "enterAuto":
                if (useAutoOpen && state == AIRLOCK_STATE.STANDBY)
                    state = AIRLOCK_STATE.STANDBY_TO_ENTER;
                break;
            case "enterOverride":
                if (state != AIRLOCK_STATE.OFF)
                    state = AIRLOCK_STATE.ENTER;
                break;
            case "exit":
                if (state == AIRLOCK_STATE.STANDBY)
                    state = AIRLOCK_STATE.STANDBY_TO_EXIT;
                break;
            case "exitAuto":
                if (useAutoOpen && state == AIRLOCK_STATE.STANDBY)
                    state = AIRLOCK_STATE.STANDBY_TO_EXIT;
                break;
            case "exitOverride":
                if (state != AIRLOCK_STATE.OFF)
                    state = AIRLOCK_STATE.EXIT;
                break;
        }
    }
    // process sequences
    else
    {
        switch (state)
        {
            case AIRLOCK_STATE.STANDBY:
            default:
                {
                    ForceOffAndClosedOfDoorList(doorsInt);
                    ForceOffAndClosedOfDoorList(doorsExt);
                }
                break;
            case AIRLOCK_STATE.STANDBY_TO_ENTER:
                {
                    SetDepressurizeOfVentList(vents, true);
                    ForceOffAndClosedOfDoorList(doorsInt);
                    ForceOffAndClosedOfDoorList(doorsExt);
                    if (chmbPressure < 5)
                        state = AIRLOCK_STATE.ENTER;
                }
                break;
            case AIRLOCK_STATE.ENTER:
                {
                    SetDepressurizeOfVentList(vents, true);
                    ForceOffAndClosedOfDoorList(doorsInt);
                    ForceOffAndOpenOfDoorList(doorsExt);
                    if (playerAtChmb)
                        state = AIRLOCK_STATE.ENTER_TO_STANDBY;
                }
                break;
            case AIRLOCK_STATE.ENTER_TO_STANDBY:
                {
                    SetDepressurizeOfVentList(vents, false);
                    ForceOffAndClosedOfDoorList(doorsExt);
                    if (chmbPressure > 95)
                    {
                        ForceOffAndOpenOfDoorList(doorsInt);
                        if (playerAtInt)
                            state = AIRLOCK_STATE.STANDBY;
                    }
                    else
                        ForceOffAndClosedOfDoorList(doorsInt);
                }
                break;
            case AIRLOCK_STATE.STANDBY_TO_EXIT:
                {
                    SetDepressurizeOfVentList(vents, false);
                    ForceOffAndClosedOfDoorList(doorsInt);
                    ForceOffAndClosedOfDoorList(doorsExt);
                    if (chmbPressure > 95)
                        state = AIRLOCK_STATE.EXIT;
                }
                break;
            case AIRLOCK_STATE.EXIT:
                {
                    SetDepressurizeOfVentList(vents, false);
                    ForceOffAndOpenOfDoorList(doorsInt);
                    ForceOffAndClosedOfDoorList(doorsExt);
                    if (playerAtChmb)
                        state = AIRLOCK_STATE.EXIT_TO_STANDBY;
                }
                break;
            case AIRLOCK_STATE.EXIT_TO_STANDBY:
                {
                    SetDepressurizeOfVentList(vents, true);
                    ForceOffAndClosedOfDoorList(doorsInt);
                    if (chmbPressure < 5)
                    {
                        ForceOffAndOpenOfDoorList(doorsExt);
                        if (playerAtExt)
                            state = AIRLOCK_STATE.STANDBY;
                    }
                    else
                        ForceOffAndClosedOfDoorList(doorsExt);
                }
                break;
        }
    }

    // display status
    if (useStatusDisplay)
    {
        Color white = new Color(255, 255, 255);
        Color black = new Color(0, 0, 0);
        Color yellow = new Color(63, 63, 0);
        Color green = new Color(0, 31, 0);

        string msgInt = "";
        string msgChmb = "";
        string msgExt = "";
        Color fontInt = white;
        Color fontChmb = white;
        Color fontExt = white;

        // show status
        switch (state)
        {
            default:
            case AIRLOCK_STATE.OFF:
            case AIRLOCK_STATE.STANDBY:
                msgInt = msgExt = msgChmb = state.ToString();
                break;
            case AIRLOCK_STATE.STANDBY_TO_ENTER:
                msgInt = msgExt = msgChmb = "VENTING";
                fontInt = fontChmb = fontExt = yellow;
                break;
            case AIRLOCK_STATE.ENTER:
                msgInt = "STAND CLEAR";
                fontInt = yellow;
                msgExt = "ENTER AIRLOCK";
                fontExt = green;
                break;
            case AIRLOCK_STATE.ENTER_TO_STANDBY:
            case AIRLOCK_STATE.EXIT_TO_STANDBY:
                msgInt = msgExt = "STAND CLEAR";
                msgChmb = "EXIT AIRLOCK";
                fontInt = fontChmb = fontExt = yellow;
                break;
            case AIRLOCK_STATE.STANDBY_TO_EXIT:
                msgInt = msgExt = msgChmb = "FILLING";
                fontInt = fontChmb = fontExt = yellow;
                break;
            case AIRLOCK_STATE.EXIT:
                msgInt = "ENTER AIRLOCK";
                fontInt = green;
                msgExt = "STAND CLEAR";
                fontExt = yellow;
                break;
        }
        string status = state != AIRLOCK_STATE.OFF
            ? ("\n\n"
                + "PRESSURE: " + (chmbSealed ? chmbPressure + "%" : "NO SEAL") + "\n"
                + "INTERIOR DOORS " + doorIntStatus + "\n"
                + "EXTERIOR DOORS " + doorExtStatus + "\n"
                + "\n")
            : "";
        if (useInteriorPanels)
            ChangeStateOfTextPanelList(statusPanelsInt, fontInt, black, msgInt + status, false);
        if (useChamberPanels)
            ChangeStateOfTextPanelList(statusPanelsChmb, fontChmb, black, msgChmb + status, false);
        if (useExteriorPanels)
            ChangeStateOfTextPanelList(statusPanelsExt, fontExt, black, msgExt + status, false);
    }
}
