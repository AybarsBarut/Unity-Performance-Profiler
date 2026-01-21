# Unity Runtime Profiler Monitor

This is a lightweight in-game profiler display for Unity that shows real-time performance data using Unity’s built-in `UnityEngine.Profiler` API.

##  Features

- Real-time FPS display  
- Memory usage (allocated, reserved, Mono heap)  
- Garbage Collector info  
- Draw calls and rendered objects count  
- Simple overlay UI (left-top corner of screen)  
- Works in both 3D and VR projects  
- Only active in Developer Builds

##  Installation

1. Add an empty `GameObject` to your scene.
2. Rename it to `Profiler` (optional but recommended).
3. Attach the `Profiler.cs` script to the object as a component.
4. Enter Play Mode — performance stats will appear on the top-left of the screen.

>  This profiler is automatically disabled in non-development builds.

##  Folder Structure

Assets/
└── Scripts/
└── SimpleProfiler.cs


##  Use Cases

- Debugging and testing game performance at runtime
- Monitoring resource usage in VR or mobile projects
- Quick identification of frame drops or memory spikes

##  Tested With

- Unity 2020.3 and above
- Windows, Android, and VR builds (Oculus/Quest compatible)

## 📸 Screenshot
![WhatsApp Görsel 2025-07-10 saat 13 29 42_432e6bcb](https://github.com/user-attachments/assets/a0f00621-e766-4838-92db-5b313fc8af15)


##  License

MIT License

---


