using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using Dalamud.Hooking;
using System.Runtime.InteropServices;
using Dalamud.Game.Inventory;


namespace SamplePlugin;

// grabbing delegate from ClientStructs (this is the event we are hooking onto)
using InventoryChangeDelegate = IGameInventory.InventoryChangedDelegate;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    private const string CommandName = "/test";
    private const string AlrightAlright = "/alrightalright"; //creates a new chat command: /alrightalright

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("iHATEffxiv");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // a new command handler.
        CommandManager.AddHandler(AlrightAlright, new CommandInfo(AlrightAlrightMethod)
        {
            HelpMessage = "How many alright alright alrights are in your inventory? Let's find out!"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(AlrightAlright);

        //this._inventoryChangeHook?.Dispose(); //this may have to be in a separate dispose() function declared in the InventoryHook class.
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private unsafe void AlrightAlrightMethod(string command, string args)
    {
        //creates an instance (object) of the player's inventory.
        var inventory = InventoryManager.Instance();

        //id of "alright alright alright" item
        uint alrightAlrightAlrightId = 43681;

        //call to the API with specified parameters.
        int alrightCount = inventory->GetInventoryItemCount(alrightAlrightAlrightId, false, false, false, 0);

        if (alrightCount > 0){
            ChatGui.Print("alright alright alright!");
        }
        else{
            ChatGui.Print(":(");
        }

    }

    public unsafe class InventoryHook : IDisposable {
        private readonly Hook<IGameInventory.InventoryChangedDelegate>? inventoryChangeHook;

        public InventoryHook(){

            inventoryChangeHook = GameInteropProvider.HookFromAddress<IGameInventory.InventoryChangedDelegate>(
                IGameInventory.InventoryChanged
            );

            this.inventoryChangeHook.Enable();
        }

        public void Dispose()
        {
            inventoryChangeHook.Dispose();
        }

        private void DetourInventoryChange(IGameInventory* self, uint set){
            ChatGui.Print("An inventory change occured.");

            try{

            } catch (Exception ex){
                Services.PluginLog.Error(ex, "Error occured when handling hook.");
            }

            this.inventoryChangeHook.Original(self, set);
        }
    }


    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
