# MonoStereoMod Usage
## Adding MonoStereoMod to Your Mod

> Before you can start working with MonoStereoMod, you first need to add it as a project reference and mod reference. Under normal circumstances, you'd need to add all of the mod's dependencies as references as well - however, MonoStereoMod eliminates this step in the process by bundling itself, along with all of its dependencies, together into one single .dll file.

> Head on over to the [releases](https://github.com/NycroV/MonoStereoMod/releases) tab to find the latest dependency packages!

After downloading the .dll file, place it into a folder named `lib` within your mod directory.<br/>
Then, inside of your `build.txt` file, ensure that `MonoStereoMod` is included in your `modReferences`, and `MonoStereoMod.Dependencies` is included in your `dllReferences`.

![image](https://github.com/user-attachments/assets/c35180b8-1d23-41d6-b605-62cde00962c7)

Lastly, add the .dll as a project reference within your IDE. If you're in Visual Studio, right click your project in the solution explorer, and choose "Add Project Reference." Navigate to your .dll file within the `lib` folder, and select it.

Now you're ready to start developing!

***

## Accessing MonoStereo Components
