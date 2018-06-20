# rs-timer
A timer intended for use while AFK-ing in RuneScape. This program has been made possible largely thanks to the efforts of the [globalmousekeyhook](https://github.com/gmamaladze/globalmousekeyhook) library. Using it, we're able to have the timer react to mouse and keyboard events even when it doesn't have focus. The timer is able to automatically reset itself whenever a click is detected in a RuneScape window. Due to the nature of this program and its reliance on the Windows API, it only works in Windows. There is no plan for Mac or Linux support at this time.

Supported Clients
-----------------
* RS3 official client
* OSRS official client
* RuneLite

How does it work?
-----------------
Every time a MouseDown event happens in Windows, the foreground window might change. Using the Windows API, you're able to obtain a handle to this window and read its title. So following from that, every time a MouseUp event happens, the foreground window may possibly change. Using the [globalmousekeyhook](https://github.com/gmamaladze/globalmousekeyhook) library, we're able to easily listen globally for MouseUp events, read the title of the foreground window, and reset the timer if it matches one of the RuneScape window titles.

Is it safe?
-----------
Yes. Feel free to read through the source code if you're not convinced. The timer doesn't directly interact with the game at all, nor does it send your mouse clicks or key presses anywhere over the Internet, nor save them to your computer. It just listens to mouse clicks and key presses and reacts according to which window it detected them in.

Where can I download it?
------------------------
The latest release can be downloaded [here on Github](https://github.com/bhohler/rs-timer/releases/latest). Just download the zip file and extract it to your desired location.