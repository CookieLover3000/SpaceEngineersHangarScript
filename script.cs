// To use the script, add the programmable block with the run action to a button panel, use the arguments "open" or "close".
// To enable the alarm add after your first word the word "alarm", example: "open alarm".

// Change the text between quotation marks to change what groups the program will search.
const string ventGroupName = "Hangar Vents";
const string doorGroupName = "Hangar Doors";
const string alarmGroupName = "Alarm";

// change to false if you don't want to add an oxygen tank for storing the oxygen from the vents.
// If you keep this enabled your grid should have at least two oxygen tanks.
// All tanks but the exclusive tanks need to have "Oxygen Tank" in their name (without the quotation marks).
bool exclusiveOxygenTank = true;

// Change the text between quotation marks to something else if you want your oxygen tank to have a different name.
// Also don't forget to give your oxygen tank the exact same name.
// Important to note is that this tank may not have oxygen in it's name. The system will break if were that to happen.
const string nameOxygenTank = "Cookie's Tank";




/* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */
/* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */
/* !!!!!!                                                                                !!!!! */
/* !!!!!!       Don't change anything beyond this point!       !!!!! */
/* !!!!!!         Unless you knwow what you're doing.         !!!!! */
/* !!!!!!            In that case, change away.                         !!!!! */
/* !!!!!!                                                                                 !!!!! */
/* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */
/* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */

// Needed to read in the commands.
MyCommandLine _commandLine = new MyCommandLine();
Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

// Construct all the lists and objects.
List<IMyAirtightHangarDoor> doorList = new List<IMyAirtightHangarDoor>();
List<IMyAirVent> ventList = new List<IMyAirVent>();
List<IMyReflectorLight> lightList = new List<IMyReflectorLight>();
List<IMyTerminalBlock> tankList = new List<IMyTerminalBlock>();
List<IMyGasGenerator> generatorList = new List<IMyGasGenerator>();

// Controls if "open" has been called.
bool opening;
// Controls if "close" has been called.
bool closing;
// Controls if the alarm needs to be enabled.
bool alarm;
// value of the Cookie Tank.
double tankVal;

// Program is the Constructor.
public Program()
{
    // Associate the Open and Close methods with the open and close commands.
    _commands["open"] = Open;
    _commands["close"] = Close;

    // Link the blocks in a group with the blocks we'll be using.
    IMyBlockGroup groupVents = GridTerminalSystem.GetBlockGroupWithName(ventGroupName);
    groupVents.GetBlocksOfType(ventList);
    IMyBlockGroup groupDoors = GridTerminalSystem.GetBlockGroupWithName(doorGroupName);
    groupDoors.GetBlocksOfType(doorList);
    if (exclusiveOxygenTank)
    {
        double.TryParse(Storage, out tankVal);
        IMyGasTank cookieTank = GridTerminalSystem.GetBlockWithName(nameOxygenTank) as IMyGasTank;
        cookieTank.Enabled = false;
        OxygenSystem();
    }
}

public void save()
{
    Storage = tankVal.ToString();
}
public void Open()
{
    if (exclusiveOxygenTank)
    {
        IMyGasTank cookieTank = GridTerminalSystem.GetBlockWithName(nameOxygenTank) as IMyGasTank;
        tankVal = cookieTank.FilledRatio;
        save();
    }
    // sets opening to true so doorOpen gets called in the next run.
    opening = true;

    // Adds the option to enable the alarm.
    // If Argument no.1 is "alarm", the alarm will be enabled.
    // If it is something else or nothing has been added the alarm will not be enabled.

    string alarmString = _commandLine.Argument(1);
    if (alarmString == null)
    {
        Echo("cannot enable Alarm");
    }
    else if (string.Equals(alarmString, "alarm", StringComparison.OrdinalIgnoreCase))
        alarm = true;
    // Call the function that opens the door.
    DoorOpen();
}
public void DoorOpen()
{
    if (exclusiveOxygenTank)
        OxygenSystem();
    // Get all the blocks of a specific type (should probably change this to group).
    GridTerminalSystem.GetBlocksOfType(doorList);
    //GridTerminalSystem.GetBlocksOfType(vents);

    // depressurizes the room.
    foreach (var vent in ventList)
    {
        vent.Depressurize = true;
    }
    // Check if the room is pressurized.
    // If this is true, make the program run again until the room is depressurized.
    if (ventList[0].GetOxygenLevel() > 0.000001)
    {
        Runtime.UpdateFrequency = UpdateFrequency.Once;
    }
    // Open the hangar doors.
    else
    {
        foreach (var door in doorList)
            door.OpenDoor();
        opening = false;
    }
}

public void Close()
{
    closing = true;
    if (exclusiveOxygenTank)
        OxygenSystem();
    Runtime.UpdateFrequency = UpdateFrequency.Once;
    // Get all the blocks of a specific type.
    GridTerminalSystem.GetBlocksOfType(doorList);

    string alarmString = _commandLine.Argument(1);
    if (alarmString == null)
    {
        Echo("cannot enable Alarm");
    }
    else if (string.Equals(alarmString, "alarm", StringComparison.OrdinalIgnoreCase))
        alarm = true;

    // Close the door and enable pressurization.
    foreach (var door in doorList)
        door.CloseDoor();
    foreach (var vent in ventList)
        vent.Depressurize = false;
}

// doorOpen is a seperate function because it needs to be called more than once.

public void AlarmSystem()
{
    // Checks if a group with the correct name exists.
    // If this is true, get the blocks and assign the correct type
    if (GridTerminalSystem.GetBlockGroupWithName(alarmGroupName) != null)
    {
        IMyBlockGroup groupAlarm = GridTerminalSystem.GetBlockGroupWithName(alarmGroupName);
        groupAlarm.GetBlocksOfType(lightList);
    }
    Runtime.UpdateFrequency = UpdateFrequency.Once;
    // Checks if opening has been called, the door is opening/closing or the vents are pressurizing.
    if ((opening) || (doorList[0].Status == DoorStatus.Opening) || (doorList[0].Status == DoorStatus.Closing) || (ventList[0].Status == VentStatus.Pressurizing))
    {
        // Set the color to red and enable the lights.
        foreach (var light in lightList)
        {
            light.Color = Color.Red;
            light.Enabled = true;
        }
    }
    // Checks if there is enough oxygen in the room.
    // If this is true, disables the alarm.
    else if ((ventList[0].GetOxygenLevel() < 0.9) && closing)
        Runtime.UpdateFrequency = UpdateFrequency.Once;
    else
    {
        // Disable the lights and disable the alarm.
        foreach (var light in lightList)
            light.Enabled = false;
        closing = false;
        alarm = false;
    }
}

public void OxygenSystem()
{
    // initialize all the tanks
    GridTerminalSystem.SearchBlocksOfName("Oxygen Tank", tankList, tank => tank is IMyGasTank);
    GridTerminalSystem.GetBlocksOfType(generatorList);
    IMyGasTank cookieTank = GridTerminalSystem.GetBlockWithName(nameOxygenTank) as IMyGasTank;
    // When the door is opening or the vents are still (de)pressurizing.
    if ((opening) || (cookieTank.FilledRatio > tankVal && closing))
    {
        Runtime.UpdateFrequency = UpdateFrequency.Once;
        // Go through the tanks and disable them.
        for (int i = 0; i < tankList.Count; ++i)
        {
            IMyGasTank tank = tankList[i] as IMyGasTank;
            if (tank == null) continue; // not a tank
            tank.Enabled = false;
        }
        // Disable the generators.
        foreach (var generator in generatorList)
            generator.Enabled = false;
        // Enable the exclusive tank.
        cookieTank.Enabled = true;
    }
    else
    {
        closing = false;
        // Go through the tanks and enable them.
        for (int i = 0; i < tankList.Count; ++i)
        {
            IMyGasTank tank = tankList[i] as IMyGasTank;
            if (tank == null) continue; // not a tank
            tank.Enabled = true;
        }
        // Enable the generators.
        foreach (var generator in generatorList)
            generator.Enabled = true;
        // Disable the exclusive tank (doesn't work correctly at the moment.
        cookieTank.Enabled = false;
    }
}

public void Main(string argument)
{
    if (opening)
        DoorOpen();
    if (alarm)
        AlarmSystem();
    if (exclusiveOxygenTank)
        OxygenSystem();

    if (_commandLine.TryParse(argument))
    {
        Action commandAction;

        // Retrieve the first argument. Switches are ignored.
        string command = _commandLine.Argument(0);

        // Now we must validate that the first argument is actually specified,
        // then attempt to find the matching command delegate.
        if (command == null)
        {
            Echo("No command specified");
        }
        else if (_commands.TryGetValue(_commandLine.Argument(0), out commandAction))
        {
            // We have found a command. Invoke it.
            commandAction();
        }
        else
        {
            Echo($"Unknown command {command}");
        }
    }
}