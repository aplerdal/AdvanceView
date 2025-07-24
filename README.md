# Advance View
A race view tool for Mario Kart: Super Circuit

[Download](github.com/aplerdal/AdvanceView/releases/latest)

## Usage
Currently, the only officially supported emulator is the most recent release of Mesen2 (though the program likely works with older versions as well).

Follow the script setup instructions for your emulator before running the AdvanceView viewer.
### Mesen Instructions
1. Open a new script window. This can be found under Debug > Script Window. You can alternitively reach it by pressing `Ctrl+N`
<img width="202" height="398" alt="Opening a script window" src="https://github.com/user-attachments/assets/8a9c40f7-ed1e-418c-b44e-ea903f5562ce" />

2. Open the script window settings. This can be found in the Script Window under Script > Settings
<img width="171" height="94" alt="Opening the script settings" src="https://github.com/user-attachments/assets/c62b81fe-83f9-4fcf-a6f5-78907b23c516" />

3. In the settings, enable "Allow access to I/O and OS functions" and "Allow network access". AdvanceView works by creating a server on your computer and having the viewer connect to it.
 It will never contact anything outside your computer and works offline. If you have a slower computer you may also want to increase Maximum execution time, but this is rarely neccessary.
<img width="475" height="280" alt="Script settings menu" src="https://github.com/user-attachments/assets/7891465d-8c1b-49a2-acb9-3f353532dc52" />

5. Confirm your settings by pressing "Ok" and then in the script window, open the downloaded server_mesen.lua script. If for some reason you cannot open the file, you can also copy paste it into the text area of the window. The loaded script should look similar to the one below
<img width="516" height="521" alt="Loaded Script" src="https://github.com/user-attachments/assets/d540c092-1421-401e-a8ac-e2c43094f22d" />

6. Finally, you can run the script. Once you get the message "Listening on port 34977" you can open the AdvanceView viewer, and it should now connect.

#### Troubleshooting
If you get the error `Could not bind server port. Please make sure the Advance View client isn't running and try agin. If there is still an error, try restarting the emulator as well.` there are a few possible fixes.
Once you have ensured the AdvanceView client is not running when you attempt to start the script, if you still get the same error, try restarting the emulator. If this doesn't work, you may want to wait a few minutes.
If you still get the error, please create an issue in this repository.

## Advance View Instructions
There are a few important keybinds:
- 1: Toggles track map display
- 2: Toggles AI map display
- 3: Toggles tile behavior display
- C: Toggles freecam. When in freecam mode, you can zoom with the mouse wheel and pan by middle clicking and dragging.

In order for AdvanceView to load the track, the game must load it as well. This is done both when opening a track and restarting it.

Positions of the player will be loaded regardless of if the track is loaded.

## Porting the server
The server depends on `socket.core`. Provided your emulator has that, porting the server is fairly simple.  All mesen specific functions are placed on lines 29 to 47. Replace each of these with the corresponding features from the emulator you want to use.
If your emulator does not include a callback for the script ending, it can be removed, though this can cause some issues for the server when trying to start the script again.

If you do develop a server for another emulator, please create a pull request so I can include it for other users in the future. It would also help greatly if you could write instructions for use in this README. You can use the one I wrote for mesen as an example.

Have fun `:)`
