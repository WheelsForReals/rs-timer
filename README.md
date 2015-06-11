# rs-timer
A timer intended for use while AFK-ing in RuneScape. This program has been made possible largely thanks to the efforts of the [globalmousekeyhook](https://github.com/gmamaladze/globalmousekeyhook) library. Using it, I was able to have the timer react to mouse and keyboard events even when it didn't have focus. The timer is able to automatically reset itself whenever a click is detected in a RuneScape window. The client and web browsers are both supported. Due to the nature of this program and its reliance on the Windows API, it only works in Windows (sorry Mac/Linux).

How does it work?
-----------------
Every time a MouseDown event happens in Windows, the foreground window changes. Using the Windows API, you're able to obtain a handle to this window at any time and from there, get its title. So following from that, every time a MouseUp event happens, the foreground window may possibly change. Using the [globalmousekeyhook](https://github.com/gmamaladze/globalmousekeyhook) library, we're able to easily listen globally for MouseUp events, read the title of the foreground window, and reset the timer if it matches one of the RuneScape window titles.

Is it safe?
-----------
Yes. I won't expect you to take my word for it though, which is why it's important that the source code is available. It doesn't directly interact with the game at all. It doesn't send your mouse clicks or key presses anywhere, nor does it establish any sort of internet connection for it to send mouse clicks or key presses to. It just listens to mouse clicks and key presses and reacts according to which window it detected them in.
