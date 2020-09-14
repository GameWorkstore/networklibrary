# Network Library

Network Library for Unity projects based on old UNET! Transport layer will be updated for better one when unity drops support for LLAPI, or earlier.
Use it your own risk!

# How to install

At package.json, add these 2 lines of code:
> "com.gameworkstore.networklibrary": "https://github.com/GameWorkstore/networklibrary.git"

> "com.gameworkstore.patterns": "https://github.com/GameWorkstore/patterns.git"

and wait unity download and compile the package.

Is interesting to write a editor script to update them when necessary!

```csharp
using UnityEditor;
using UnityEditor.PackageManager;

public class PackageUpdater
{
    [MenuItem("Help/UpdateGitPackages")]
    public static void TrackPackages()
    {
        Client.Add("https://github.com/GameWorkstore/patterns.git");
        Client.Add("https://github.com/GameWorkstore/networklibrary.git");
    }
}
```
