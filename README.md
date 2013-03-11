KSP-Haystack-Plugin
===================

Plugin for [Kerbal Space Program](http://www.kerbalspaceprogram.com/) which helps to easily switch vessels and targets.

Building
--------
There's currently no one-step build option. The process is as follows:

1. Build the plugin DLL. Make sure to reference the Assembly-CSharp and UnityEngine assemblies from the version of KSP you wish to target. (A plugin build targeted to one version may not work on another, even if no code changes are necessary for compatibility.)
2. Copy the part .cfg files from the repository Parts/ directory.
3. Copy any other assets from the latest public release.

Reporting Issues
----------------
Please provide as much detail as possible when reporting an issue. That said, if you encounter an issue and aren't able to pin down the cause, post it and explain what you've tried so far. Some bugs are difficult to reproduce, and we need to know about them anyway.
