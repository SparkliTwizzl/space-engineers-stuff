// ===== CONFIG =====
int updateFrequency = 100; // 1, 10, 100
string ventName = "vent";
string statusPanelGroup = "statusPanels";
int safeLevel = 75; // shows green below this
int warningLevel = 50; // shows yellow below this
int dangerLevel = 25; // shows red below this
// ===== CONFIG =====

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
    int chmbPressure = (int)Math.Round(vent.GetOxygenLevel() * 100, 0);
    Color black = new Color(0, 0, 0);
    Color cyan = new Color(0, 255, 255);
    Color green = new Color(0, 255, 0);
    Color yellow = new Color(255, 255, 0);
    Color red = new Color(255, 0, 0);

    // logic

    // display vent pressure
    for (int i = 0; i < statusPanels.Count; i++)
        ChangeTextPanelMessage(statusPanels[i], "PRESSURE: " + chmbPressure + "%", false);

    // change font color based on pressure level
    if (chmbPressure >= safeLevel)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelColors(statusPanels[i], cyan, black);
    else if (chmbPressure < safeLevel && chmbPressure >= warningLevel)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelColors(statusPanels[i], green, black);
    else if (chmbPressure < warningLevel && chmbPressure >= dangerLevel)
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelColors(statusPanels[i], yellow, black);
    else
        for (int i = 0; i < statusPanels.Count; i++)
            ChangeTextPanelColors(statusPanels[i], red, black);
}
