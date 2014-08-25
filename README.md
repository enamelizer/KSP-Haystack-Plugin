KSP-Haystack-Plugin
===================

Plugin for [Kerbal Space Program](http://www.kerbalspaceprogram.com/) which helps to easily switch vessels and targets.

Original code written by hermes-jr, with additions for compatibility by Aaron DeMarre.


Building
--------
1. Add references to the Assembly-CSharp and UnityEngine assemblies from the version of KSP you wish to target. (A plugin build targeted to one version may not work on another, even if no code changes are necessary for compatibility.)
2. Build in Release configuration. This will copy the Release DLL to \GameData\HrmHaystack\.
3. Zip and release to the world.

Debugging Setup
---------------
1. Copy a testbed version of Kerbal Space Program to the root directory of the repository
2. Deploy Haystack to this testbed version
3. Build in Debug configuration, this will copy the Debug dll to the testbed version
4. The testbed now contains the Debug version of Haystack, run the KSP.exe from this testbed to debug Haystack

Bonus: Add the testbed version of KSP.exe to "External Tools" in VS, then you can add this command to a tool bar to launch the testbed version of KSP.exe. You are now two clicks away from a build, deploy, and debug in VS! 

Reporting Issues
----------------
Please provide as much detail as possible when reporting an issue. That said, if you encounter an issue and aren't able to pin down the cause, post it and explain what you've tried so far. Some bugs are difficult to reproduce, and we need to know about them anyway.

Licence
-------
This code is licensed under The MIT License (MIT).
